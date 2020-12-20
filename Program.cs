// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="Program.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using NUnitRunner.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NUnitRunner
{
    /// <summary>
    /// This console application brings parallel-run capabilities for NUnit tests, which are supplied in an *.xml configuration file.
    /// </summary>
    class Program
    {
        /// <summary>
        /// These arguments need to be supplied to this application:
        /// 1. Full path of the NUnit Test Assembly
        /// 2. Path of the NUnit console test executor
        /// 3. XML configuration file
        /// 4. Output directory
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Trace.TraceError("Invalid arguments.");
                return 1;
            }

            if (!File.Exists(args[0]))
            {
                Trace.TraceError("Supplied assembly file does not exist.");
                return 1;
            }

            if (!File.Exists(args[1]))
            {
                Trace.TraceError("Supplied NUnit console executable file does not exist.");
                return 1;
            }

            if (!File.Exists(args[2]))
            {
                Trace.TraceError("Supplied configuration file does not exist.");
                return 1;
            }

            if (!Directory.Exists(args[3]))
            {
                Trace.TraceError("Supplied output directory does not exist.");
                return 1;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            IConfigReader configReader = new XMLConfigReader();
            var config = configReader.GetConfiguration(args[2]);
            config.Assembly = args[0];
            config.NUnitExecutable = args[1];
            config.OutputDirectory = args[3];
            var nunitRunner = new Runner(config);
            RunResult result = nunitRunner.Run();

            if (!result.TestResults.Select(r => r.Succeeded).Contains(true))
            {
                Trace.TraceError("All tests have failed.");
            }
            else if (result.TestResults.Select(r => r.Succeeded).Contains(false))
            {
                Trace.TraceWarning("Some tests were not successful.");
            }

            if (config.RetryFailedTests)
            {
                Trace.TraceInformation("Failed tests will be retried.");
                var failedTestCases = new List<TestCase>();
                result.TestResults.ForEach(r => r.TestCaseResults.Where(tc => tc.Executed && (tc.Result == TestRunResult.Failure || tc.Result == TestRunResult.Error)).ToList().ForEach(tc => failedTestCases.Add(tc)));

                if (failedTestCases.Any())
                {
                    Trace.TraceInformation($"There are {failedTestCases.Count} failed tests to retry.");
                    nunitRunner.RunTestCases(failedTestCases);

                    foreach (var testCase in failedTestCases)
                    {
                        string testRunOutputFilePath = Path.Combine(config.OutputDirectory, testCase.TestRunOutputFileName) + ".xml";
                        new NUnitXmlParser(testRunOutputFilePath).UpdateOutputFile(testCase);
                    }
                }

                Trace.TraceInformation("Rerun completed.");
            }
            
            // combine results if necessary
            var groupedByName = config.TestRuns.GroupBy(s => s.Name).Select(g => new { Name = g.Key, Count = g.Count() });
            bool combine = groupedByName.Any(g => g.Count > 1);

            if (combine)
            {
                var aggregator = new TestOverviewAggregator();
                List<string> combinedTestNames = groupedByName.Where(g => g.Count > 1).Select(g => g.Name).ToList();
                int countOffilesToCombine = groupedByName.Where(g => g.Count > 1).Select(g => g.Count).Aggregate((a, b) => a + b);
                Trace.TraceInformation($"There are {countOffilesToCombine} files to combine into {combinedTestNames.Count} files. ({string.Join(", ", combinedTestNames)})");

                foreach (var testName in combinedTestNames)
                {
                    var filesToCombine = config.TestRuns.Where(t => t.Name.Equals(testName)).Select(t => Path.Combine(config.OutputDirectory, t.OutputFileName + ".xml")).ToList();
                    aggregator.Create(filesToCombine, Path.Combine(config.OutputDirectory, testName + ".xml"));
                }
            }

            Trace.TraceInformation("Completed");
            stopwatch.Stop();
            Trace.TraceInformation($"Total duration: {GetRunDuration(stopwatch.Elapsed)}");

            string logFile = Path.Combine(Directory.GetCurrentDirectory(), "NUnitRunner.log");
            if (File.Exists(logFile))
            {
                File.Copy(logFile, Path.Combine(config.OutputDirectory, "NUnitRunner.log"), true);
            }
            
            return 0;
        }

        private static string GetRunDuration(TimeSpan span)
        {
            StringBuilder sb = new StringBuilder();
            if (span.TotalHours >= 24)
            {
                sb.Append(span.TotalHours % 24 > 1 ? string.Format("{0:%d} days ", span) : string.Format("{0:%d} day ", span));
            }

            if (span.TotalHours % 24 >= 1)
            {
                sb.Append(span.TotalHours % 24 > 1 ? string.Format("{0:%h} hours ", span) : string.Format("{0:%h} hour ", span));
            }

            if (span.Minutes % 60 > 0)
            {
                sb.Append(span.Minutes % 60 > 1 ? string.Format("{0:%m} minutes ", span) : string.Format("{0:%m} minute ", span));
            }

            if (span.Seconds % 60 > 0)
            {
                sb.Append(span.Seconds % 60 > 1 ? string.Format("{0:%s} seconds", span) : string.Format("{0:%s} second", span));
            }

            return sb.ToString();
        }
    }
}
