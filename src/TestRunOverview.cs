// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestRunOverview.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NUnitRunner
{
    /// <summary>
    /// Test run statistics contained in a test output file.
    /// </summary>
    public class TestRunOverview
    {
        public string FilePath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public int TotalTestCases { get; set; }
        public string TestFixture { get; set; }
        public int Asserts { get; set; }
        public int Errors { get; set; }
        public int Failures { get; set; }
        public int NotRun { get; set; }
        public int Inconclusive { get; set; }
        public int Ignored { get; set; }
        public int Skipped { get; set; }
        public int Invalid { get; set; }
        public TestRunResult Result { get; set; }
        public bool Success { get; set; }
        public List<string> GeneralCategories { get; set; }
        public IEnumerable<XElement> TestCaseNodes { get; set; }
    }
}
