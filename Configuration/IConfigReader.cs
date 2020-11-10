// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="IConfigReader.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NUnitRunner.Configuration
{
    /// <summary>
    /// Interface to be used for reading an NUnitRunner configuration file.
    /// </summary>
    public interface IConfigReader
    {
        TestConfiguration GetConfiguration(string configurationFile);
    }
}
