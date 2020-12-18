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

            var consoleArgs = new List<string>
            {
                _config.Assembly, // Assembly
                $"/xml:\"{Path.Combine(_config.OutputDirectory, $"{test.OutputFileName}.xml")}\"", // XML Output file
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
            return new TestResult { Succeeded = process.ExitCode == 0, Duration = stopwatch.Elapsed, Test = test };
        }

        private void Process(TestResult result)
        {
            TimeSpan span = TimeSpan.FromSeconds(result.Duration.TotalSeconds);
            string duration = (span.Hours > 0 ? $"{span.Hours} hour(s)" : "") +
                (span.Minutes > 0 ? $" {span.Minutes} minute(s)" : "") +
                (span.Seconds > 0 ? $" {span.Seconds} second(s)" : "") +
                (span.Milliseconds > 0 ? $" {span.Milliseconds} milisecond(s)" : "");

            Trace.TraceInformation($"Test {result.Test.OutputFileName} is completed in {duration}.");
            results.Add(result);
        }
    }
}
