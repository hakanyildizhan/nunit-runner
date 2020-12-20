// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestConfiguration.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace NUnitRunner.Configuration
{
    /// <summary>
    /// Information that is parsed out from the NUnitRunner configuration file.
    /// </summary>
    public class TestConfiguration
    {
        private int _maxParallelRuns;
        public List<TestRun> TestRuns { get; set; }
        public int MaxParallelRuns
        {
            get { return _maxParallelRuns; }
            set
            {
                _maxParallelRuns = value == 0 ? 1 : value;
            }
        }
        public string Assembly { get; set; }
        public string OutputDirectory { get; set; }
        public string NUnitExecutable { get; set; }
        public bool RetryFailedTests { get; set; }
    }
}
