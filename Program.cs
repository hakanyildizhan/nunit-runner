// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="Program.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using NUnitRunner.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NUnitRunner
{
    class Program
    {
        /// <summary>
        /// This console application brings parallel-run capabilities for NUnit tests, which are supplied in an *.xml configuration file. These arguments need to be supplied to this application:
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

            IConfigReader configReader = new XMLConfigReader();
            var config = configReader.GetConfiguration(args[2]);
            config.Assembly = args[0];
            config.NUnitExecutable = args[1];
            config.OutputDirectory = args[3];
            RunResult result = new Runner(config).Run();

            if (!result.TestResults.Select(r => r.Succeeded).Contains(true))
            {
                Trace.TraceError("All tests have failed.");
            }
            else if (result.TestResults.Select(r => r.Succeeded).Contains(false))
            {
                Trace.TraceWarning("Some tests were not successful.");
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
            File.Copy("NUnitRunner.log", Path.Combine(config.OutputDirectory, "NUnitRunner.log"), true);
            return 0;
        }
    }
}
