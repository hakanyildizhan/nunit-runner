using NUnitRunner.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NUnitRunner
{
    public class NUnitXmlParser
    {
        private readonly XDocument _document;
        private readonly string _filePath;

        public NUnitXmlParser(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"NUnit XML file {filePath} does not exist.");
            }

            _filePath = filePath;
            _document = XDocument.Load(filePath);
        }

        public IList<TestCase> GetTestCases()
        {
            var testCases = new List<TestCase>();

            foreach (var testCaseNode in _document.Descendants("test-case"))
            {
                testCases.Add(new TestCase()
                {
                    Executed = testCaseNode.Attribute("executed").Value == "True",
                    Result = Utility.ParseRunResult(testCaseNode.Attribute("result").Value),
                    Success = testCaseNode.Attribute("success")?.Value == "True",
                    TestCaseName = testCaseNode.Attribute("name").Value,
                    Duration = testCaseNode.Attribute("time") != null ? Convert.ToDouble(testCaseNode.Attribute("time").Value, CultureInfo.InvariantCulture) : 0
                });
            }

            return testCases;
        }

        public void UpdateOutputFile(TestCase testCase)
        {
            var testCaseNode = _document.Descendants("test-case").FirstOrDefault(tc => tc.Attribute("name").Value == testCase.TestCaseName);

            if (testCaseNode == null) // no such test case inside XML file
            {
                return;
            }

            // if the test succeeded, remove failure element from test-case & reduce failures by 1
            TestRunResult previousResult = Utility.ParseRunResult(testCaseNode.Attribute("result").Value);
            if (testCase.Result == TestRunResult.Success && previousResult != testCase.Result)
            {
                testCaseNode.Element("failure")?.Remove();
                string errorCategoryCount = testCaseNode.Attribute("result").Value == "Error" ? "errors" : "failures";
                int currentErrorCount = int.Parse(_document.Element("test-results").Attribute(errorCategoryCount).Value);
                if (currentErrorCount != 0) // if it is not already 0
                {
                    _document.Element("test-results").Attribute(errorCategoryCount).Value = (currentErrorCount - 1).ToString();
                }
            }

            // update test-case attributes
            testCaseNode.Attribute("result").Value = testCase.Result.ToString();
            testCaseNode.Attribute("executed").Value = testCase.Executed ? "True" : "False";

            if (testCaseNode.Attribute("success") != null)
            {
                testCaseNode.Attribute("success").Value = testCase.Success ? "True" : "False";
            }
            else if (testCase.Result != TestRunResult.Ignored)
            {
                testCaseNode.Add(new XAttribute("success", testCase.Success ? "True" : "False"));
            }

            double newDuration = Math.Round(Convert.ToDouble(testCaseNode.Attribute("time").Value, CultureInfo.InvariantCulture) + testCase.Duration, 3);

            if (testCaseNode.Attribute("time") != null)
            {
                testCaseNode.Attribute("time").Value = newDuration.ToString("0.000", CultureInfo.InvariantCulture);
            }
            else if (testCase.Result != TestRunResult.Ignored)
            {
                testCaseNode.Add(new XAttribute("time", newDuration.ToString("0.000", CultureInfo.InvariantCulture)));
            }
            
            // add re-run duration to test-suite durations
            _document.Descendants("test-suite").Attributes("time").ToList().ForEach(t => t.Value = (Convert.ToDouble(t.Value, CultureInfo.InvariantCulture) + testCase.Duration).ToString("0.000", CultureInfo.InvariantCulture));

            // if the last failed test is cleared, update test-suite result & success attributes
            if (_document.Element("test-results").Attribute("failures").Value == "0" &&
                _document.Element("test-results").Attribute("errors").Value == "0")
            {
                _document.Descendants("test-suite").Attributes("result").ToList().ForEach(t => t.Value = "Success");
                _document.Descendants("test-suite").Attributes("success").ToList().ForEach(t => t.Value = "True");
            }

            _document.Save(_filePath);
        }
    }
}
