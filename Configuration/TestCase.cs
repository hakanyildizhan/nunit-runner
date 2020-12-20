// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestConfiguration.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NUnitRunner.Configuration
{
    /// <summary>
    /// Represents individual test case in a <see cref="TestRun"/>.
    /// </summary>
    public class TestCase
    {
        public string TestRunName { get; set; }
        public string TestRunOutputFileName { get; set; }
        public string TestCaseName { get; set; }
        public bool Executed { get; set; }
        public TestRunResult Result { get; set; }
        public bool Success { get; set; }
        public double Duration { get; set; }
    }
}
