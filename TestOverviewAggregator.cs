// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestOverviewAggregator.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace NUnitRunner
{
    /// <summary>
    /// Combines tests belonging to different categories under the same test name into a single output file.
    /// </summary>
    public class TestOverviewAggregator
    {
        private readonly ConcurrentBag<TestRunOverview> _overviews;

        public TestOverviewAggregator()
        {
            _overviews = new ConcurrentBag<TestRunOverview>();
        }

        public void Create(IList<string> files, string outputFile)
        {
            var parseOutputBlock = new TransformBlock<string, TestRunOverview>(
                filePath =>
                {
                    return ParseOutput(filePath);
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
                });

            var gatherBlock = new ActionBlock<TestRunOverview>(overview => _overviews.Add(overview));

            parseOutputBlock.LinkTo(
                gatherBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true
                });

            foreach (var file in files)
            {
                if (File.Exists(file))
                    parseOutputBlock.Post(file);
            }
            
            parseOutputBlock.Complete();
            gatherBlock.Completion.Wait();

            TestRunAggregateOverview combinedData = AggregateOutputs();
            CreateAggregateFile(combinedData, outputFile);
            files.Where(f => f != outputFile).ToList().ForEach(f => File.Delete(f));
        }

        private void CreateAggregateFile(TestRunAggregateOverview data, string outputFile)
        {
            var doc = data.TemplateDocument;
            doc.Element("test-results").Attribute("total").Value = data.TotalTestCases.ToString();
            doc.Element("test-results").Attribute("errors").Value = data.Errors.ToString();
            doc.Element("test-results").Attribute("failures").Value = data.Failures.ToString();
            doc.Element("test-results").Attribute("not-run").Value = data.NotRun.ToString();
            doc.Element("test-results").Attribute("inconclusive").Value = data.Inconclusive.ToString();
            doc.Element("test-results").Attribute("ignored").Value = data.Ignored.ToString();
            doc.Element("test-results").Attribute("skipped").Value = data.Skipped.ToString();
            doc.Element("test-results").Attribute("invalid").Value = data.Invalid.ToString();
            doc.Element("test-results").Attribute("date").Value = data.FinishTime.ToString("yyyy-MM-dd");
            doc.Element("test-results").Attribute("time").Value = data.FinishTime.ToString("HH:mm:ss");
            doc.Descendants("test-suite").Attributes("time").ToList().ForEach(t => t.Value = data.Duration.ToString("0.000", CultureInfo.InvariantCulture));
            doc.Descendants("test-suite").Attributes("result").ToList().ForEach(r => r.Value = data.Result.ToString());
            doc.Descendants("test-suite").Attributes("success").ToList().ForEach(s => s.Value = data.Success.ToString().First().ToString().ToUpper() + data.Success.ToString().Substring(1));
            doc.Descendants("test-suite").Where(d => d.Attribute("type").Value == "TestFixture").First().Element("categories").Remove();
            doc.Descendants("test-suite").Where(d => d.Attribute("type").Value == "TestFixture").First().Element("results").Descendants("test-case").Remove();
            doc.Descendants("test-suite").Where(d => d.Attribute("type").Value == "TestFixture").First().Element("results").Add(data.TestCaseNodes);
            doc.Descendants("test-suite").Where(d => d.Attribute("type").Value == "TestFixture").First().Attribute("asserts").Value = data.Asserts.ToString();

            doc.Save(outputFile);
        }

        private TestRunAggregateOverview AggregateOutputs()
        {
            var results = _overviews.ToList();
            DateTime startTime = results.Select(r => r.StartTime).OrderBy(d => d).First();
            DateTime finishTime = results.Select(r => r.FinishTime).OrderByDescending(d => d).First();
            double duration = finishTime.Subtract(startTime).TotalSeconds;
            int total = results.Select(r => r.TotalTestCases).Aggregate((a, b) => a + b);
            int errors = results.Select(r => r.Errors).Aggregate((a, b) => a + b);
            int failures = results.Select(r => r.Failures).Aggregate((a, b) => a + b);
            int notRun = results.Select(r => r.NotRun).Aggregate((a, b) => a + b);
            int inconclusive = results.Select(r => r.Inconclusive).Aggregate((a, b) => a + b);
            int ignored = results.Select(r => r.Ignored).Aggregate((a, b) => a + b);
            int skipped = results.Select(r => r.Skipped).Aggregate((a, b) => a + b);
            int invalid = results.Select(r => r.Invalid).Aggregate((a, b) => a + b);
            int asserts = results.Select(r => r.Asserts).Aggregate((a, b) => a + b);
            TestRunResult result = results.Select(r => r.Result).Contains(TestRunResult.Failure) ? TestRunResult.Failure : TestRunResult.Success;
            bool success = results.Select(r => r.Success).Contains(false) ? false : true;
            IEnumerable<XElement> testCaseNodes = results.SelectMany(r => r.TestCaseNodes);

            return new TestRunAggregateOverview
            {
                Duration = duration,
                Errors = errors,
                Failures = failures,
                Ignored = ignored,
                Inconclusive = inconclusive,
                Invalid = invalid,
                NotRun = notRun,
                Result = result,
                Skipped = skipped,
                Asserts = asserts,
                StartTime = startTime,
                FinishTime = finishTime,
                Success = success,
                TemplateDocument = XDocument.Load(results.OrderBy(r => r.TestFixture.Length).First().FilePath),
                TotalTestCases = total,
                TestCaseNodes = testCaseNodes
            };
        }

        private TestRunOverview ParseOutput(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var tr = doc.Element("test-results");

            int total = Convert.ToInt32(tr.Attribute("total").Value);
            int errors = Convert.ToInt32(tr.Attribute("errors").Value);
            int failures = Convert.ToInt32(tr.Attribute("failures").Value);
            int notRun = Convert.ToInt32(tr.Attribute("not-run").Value);
            int inconclusive = Convert.ToInt32(tr.Attribute("inconclusive").Value);
            int ignored = Convert.ToInt32(tr.Attribute("ignored").Value);
            int skipped = Convert.ToInt32(tr.Attribute("skipped").Value);
            int invalid = Convert.ToInt32(tr.Attribute("invalid").Value);

            string finishTimeString = tr.Attribute("date").Value + " " + tr.Attribute("time").Value;
            DateTime finishTime = DateTime.Parse(finishTimeString);

            var ts = tr.Element("test-suite");
            TestRunResult result = ts.Attribute("result").Value == "Success" ? TestRunResult.Success : TestRunResult.Failure;
            bool success = ts.Attribute("success").Value == "True" ? true : false;

            double duration = Convert.ToDouble(ts.Attribute("time").Value, CultureInfo.InvariantCulture);
            DateTime startTime = finishTime.Subtract(TimeSpan.FromSeconds(duration));

            var fixtureNode = doc.Descendants("test-suite").Where(ele => ele.Attribute("type").Value == "TestFixture").FirstOrDefault();
            List<string> generalCategories = fixtureNode.Element("categories").Descendants("category").Select(c => c.Attribute("name").Value).ToList();
            string testFixture = fixtureNode.Attribute("name").Value;
            int asserts = doc.Descendants("test-case").Select(d => Convert.ToInt32(d.Attribute("asserts").Value)).Aggregate((a, b) => a + b);

            doc.Descendants("test-case").ToList().ForEach(d =>
            {
                generalCategories.ForEach(c =>
                {
                    d.Element("categories").Add(new XElement("category", new XAttribute("name", c)));
                });
            });

            return new TestRunOverview
            {
                Errors = errors,
                Failures = failures,
                FilePath = filePath,
                FinishTime = finishTime,
                GeneralCategories = generalCategories,
                Ignored = ignored,
                Inconclusive = inconclusive,
                Invalid = invalid,
                NotRun = notRun,
                Result = result,
                Skipped = skipped,
                StartTime = startTime,
                Success = success,
                TestCaseNodes = doc.Descendants("test-case"),
                TotalTestCases = total,
                TestFixture = testFixture,
                Asserts = asserts
            };
        }
    }
}
