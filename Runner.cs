// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="Runner.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using NUnitRunner.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace NUnitRunner
{
    /// <summary>
    /// Runs NUnit tests with the specified degree of parallelism.
    /// </summary>
    public class Runner
    {
        private readonly TestConfiguration _config;
        private readonly ConcurrentDictionary<int, TestRun> runningTests;
        private readonly ConcurrentBag<TestResult> results;
        private static readonly object lockObj = new object();

        public Runner(TestConfiguration config)
        {
            _config = config;
            runningTests = new ConcurrentDictionary<int, TestRun>();
            results = new ConcurrentBag<TestResult>();
        }

        public RunResult Run()
        {
            var stopwatch = Stopwatch.StartNew();
            RunAllTests();
            stopwatch.Stop();
            return new RunResult { Duration = stopwatch.Elapsed, TestResults = results.ToList() };
        }

        private void RunAllTests()
        {
            var runTestBlock = new TransformBlock<TestRun, TestResult>(
                test =>
                {
                    lock (lockObj)
                    {
                        runningTests.TryAdd(test.Id, test);
                        Trace.TraceInformation($"Starting test {test.OutputFileName}. Currently {runningTests.Count} tests are running.");
                    }

                    TestResult result = RunTest(test);

                    lock (lockObj)
                    {
                        runningTests.TryRemove(test.Id, out TestRun finishedTest);
                        Trace.TraceInformation($"Finished test {test.OutputFileName}. Currently {runningTests.Count} tests are running.");
                    }

                    return result;
                }, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _config.MaxParallelRuns == -1 ? DataflowBlockOptions.Unbounded : _config.MaxParallelRuns
                });

            var writeOutputBlock = new ActionBlock<TestResult>(result => Process(result));

            runTestBlock.LinkTo(
                writeOutputBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true
                });

            foreach (var test in _config.TestRuns)
                runTestBlock.Post(test);

            runTestBlock.Complete();
            writeOutputBlock.Completion.Wait();
        }

        public TestResult RunTest(TestRun test)
        {
            var stopwatch = Stopwatch.StartNew();
            string xmlOutputFile = Path.Combine(_config.OutputDirectory, $"{test.OutputFileName}.xml");

            var consoleArgs = new List<string>
            {
                _config.Assembly, // Assembly
                $"/xml:\"{xmlOutputFile}\"", // XML Output file
                $"/output:\"{Path.Combine(_config.OutputDirectory, $"TestOutput_{test.OutputFileName}.log")}\"", // Output log file
                "/labels",
                "/trace=Error",
                "/noshadow",
                "/nologo"
            };

            if (!string.IsNullOrEmpty(test.Category))
            {
                consoleArgs.Add($"/include:\"{test.Category}\"");
            }

            if (!string.IsNullOrEmpty(test.Fixture))
            {
                consoleArgs.Add($"/fixture:\"{test.Fixture}\"");
            }

            string arg = string.Join(" ", consoleArgs.ToArray());

            Process process = new Process();
            process.StartInfo = new ProcessStartInfo($"\"{_config.NUnitExecutable}\"", arg);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            stopwatch.Stop();
            File.WriteAllText(Path.Combine(_config.OutputDirectory, $"NUnitTrace_{test.OutputFileName}.log"), output);

            var testCases = new NUnitXmlParser(xmlOutputFile).GetTestCases();
            testCases.ToList().ForEach(tc =>
            {
                tc.TestRunName = test.Name;
                tc.TestRunOutputFileName = test.OutputFileName;
            });

            return new TestResult
            {
                Succeeded = process.ExitCode == 0,
                Duration = stopwatch.Elapsed,
                Test = test,
                TestCaseResults = testCases
            };
        }

        /// <summary>
        /// Runs individual test cases serially.
        /// </summary>
        /// <param name="testCases"></param>
        /// <returns></returns>
        public void RunTestCases(List<TestCase> testCases)
        {
            string xmlOutputFile = Path.Combine(_config.OutputDirectory, "FailedTestRerun.xml");
            string outputLogFile = Path.Combine(_config.OutputDirectory, $"TestOutput_FailedTestRerun.log");
            string traceLogFile = Path.Combine(_config.OutputDirectory, $"NUnitTrace_FailedTestRerun.log");
            string runArgument = string.Join(",", testCases.Select(tc => tc.TestCaseName));

            var consoleArgs = new List<string>
                {
                    _config.Assembly, // Assembly
                    $"/xml:\"{xmlOutputFile}\"", // XML Output file
                    $"/output:\"{outputLogFile}\"", // Output log file
                    $"/run:{runArgument}", // comma-separated full names of the individual test-cases to run
                    "/labels",
                    "/trace=Error",
                    "/noshadow",
                    "/nologo"
                };

            string arg = string.Join(" ", consoleArgs.ToArray());
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo($"\"{_config.NUnitExecutable}\"", arg);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            File.WriteAllText(traceLogFile, output);

            var testCaseResults = new NUnitXmlParser(xmlOutputFile).GetTestCases();

            foreach (var testCaseResult in testCaseResults)
            {
                var testCase = testCases.FirstOrDefault(tc => tc.TestCaseName == testCaseResult.TestCaseName);

                if (testCase != null)
                {
                    testCase.Duration = Math.Round(testCase.Duration + testCaseResult.Duration, 3); // add to previous run duration
                    testCase.Result = testCaseResult.Result;
                    testCase.Success = testCaseResult.Success;
                    testCase.Executed = testCaseResult.Executed;
                }
            }

            // Delete temporary XML output
            if (File.Exists(xmlOutputFile))
            {
                File.Delete(xmlOutputFile);
            }
        }

        private void Process(TestResult result)
        {
            TimeSpan span = TimeSpan.FromSeconds(result.Duration.TotalSeconds);
            string duration = (span.Hours > 0 ? $"{span.Hours} hour(s)" : "") +
                (span.Minutes > 0 ? $" {span.Minutes} minute(s)" : "") +
                (span.Seconds > 0 ? $" {span.Seconds} second(s)" : "") +
                (span.Milliseconds > 0 ? $" {span.Milliseconds} milisecond(s)" : "");

            Trace.TraceInformation($"Test {result.Test.OutputFileName} is completed in {duration}.");

            int failedTestCount = result.TestCaseResults.Count(tc => !tc.Success);

            if (failedTestCount > 0)
            {
                Trace.TraceInformation($"Test {result.Test.OutputFileName} has {failedTestCount} failed tests.");
            }

            results.Add(result);
        }
    }
}
