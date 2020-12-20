// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestResult.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using NUnitRunner.Configuration;
using System;
using System.Collections.Generic;

namespace NUnitRunner
{
    /// <summary>
    /// Represents the results of an individual test run.
    /// </summary>
    public class TestResult
    {
        public TestRun Test { get; set; }
        public IList<TestCase> TestCaseResults { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Succeeded { get; set; }
    }
}
