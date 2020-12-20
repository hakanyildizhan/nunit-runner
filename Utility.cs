// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TestRunResult.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace NUnitRunner
{
    public static class Utility
    {
        public static TestRunResult ParseRunResult(string input)
        {
            bool success = Enum.TryParse(input, out TestRunResult result);
            return success ? result : TestRunResult.Unknown;
        }
    }
}
