// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG" file="TraceListener.cs">
//   Copyright © Siemens AG 2020. All rights reserved. Confidential.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace NUnitRunner
{
    /// <summary>
    /// Used for outputting application messages to a file. See App.config file for details.
    /// </summary>
    public class TraceListener : TextWriterTraceListener
    {
        public TraceListener(string fileName) : base(fileName) { }

        public override void Write(string message)
        {
            base.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff ") + message);
        }
    }

    /// <summary>
    /// Used for outputting application messages to the console. See App.config file for details.
    /// </summary>
    public class ConsoleListener : ConsoleTraceListener
    {
        public ConsoleListener() : base() { }

        public override void Write(string message)
        {
            base.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff ") + message);
        }
    }
}
