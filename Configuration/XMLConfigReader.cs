// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="XMLConfigReader.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NUnitRunner.Configuration
{
    /// <summary>
    /// XML-based implementation for reading NUnitRunner configuration files.
    /// </summary>
    public class XMLConfigReader : IConfigReader
    {
        private int _currentId = 0;
        public TestConfiguration GetConfiguration(string configurationFile)
        {
            var xml = XDocument.Load(configurationFile);
            int maxParallelRuns = int.Parse(xml.Root.Descendants("MaxParallelRuns").FirstOrDefault().Value);

            var testNodes = xml.Root.Descendants("Test");
            var testRuns = new List<TestRun>();

            foreach (var node in testNodes)
            {
                string name = node.Descendants("Name").First().Value;
                string fixture = string.Empty;

                if (node.Descendants("Fixture").Any())
                {
                    fixture = node.Descendants("Fixture").First().Value;
                }

                if (!node.Descendants("Categories").Any())
                {
                    testRuns.Add(new TestRun
                    {
                        Id = _currentId++,
                        Name = name,
                        Fixture = fixture,
                        OutputFileName = GetUniqueOutputFileName(name, testRuns)
                    });
                }
                else
                {
                    var categories = node.Descendants("Categories").First().Descendants("Category").Select(c => c.Value);
                    foreach (var cat in categories)
                    {
                        testRuns.Add(new TestRun
                        {
                            Id = _currentId++,
                            Name = name,
                            Fixture = fixture,
                            Category = cat,
                            OutputFileName = GetUniqueOutputFileName(name, testRuns)
                        });
                    }
                }
            }

            return new TestConfiguration { MaxParallelRuns = maxParallelRuns, TestRuns = testRuns };
        }

        private string GetUniqueOutputFileName(string testName, List<TestRun> availableRuns)
        {
            string outputFilename = testName;
            int i = 1;
            while (availableRuns.Any(t => t.OutputFileName == outputFilename))
            {
                i++;
                outputFilename = $"{testName}_{i}";
            }
            return outputFilename;
        }
    }
}
