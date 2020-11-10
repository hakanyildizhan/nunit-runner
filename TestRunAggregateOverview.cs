// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestRunAggregateOverview.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NUnitRunner
{
    /// <summary>
    /// Used for creating an aggregate output file.
    /// </summary>
    public class TestRunAggregateOverview
    {
        public XDocument TemplateDocument { get; set; }
        public IEnumerable<XElement> TestCaseNodes { get; set; }
        public string AggregateTestName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public double Duration { get; set; }
        public int TotalTestCases { get; set; }
        public int Errors { get; set; }
        public int Failures { get; set; }
        public int NotRun { get; set; }
        public int Inconclusive { get; set; }
        public int Ignored { get; set; }
        public int Skipped { get; set; }
        public int Invalid { get; set; }
        public int Asserts { get; set; }
        public TestRunResult Result { get; set; }
        public bool Success { get; set; }
    }
}
