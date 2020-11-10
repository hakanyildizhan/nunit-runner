// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="RunResult.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NUnitRunner
{
    /// <summary>
    /// Class that represents the test run results including all individual tests.
    /// </summary>
    public class RunResult
    {
        public List<TestResult> TestResults { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
