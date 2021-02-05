// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestRun.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NUnitRunner.Configuration
{
    /// <summary>
    /// Class that represents an individual test run.
    /// </summary>
    public class TestRun
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OutputFileName { get; set; }
        public string Fixture { get; set; }
        public string Category { get; set; }
    }
}
