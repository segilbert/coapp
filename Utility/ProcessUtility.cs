//-----------------------------------------------------------------------
// <copyright company="Codeplex Foundation">
//     Original Copyright (c) 2009 Microsoft Corporation. All rights reserved.
//     Changes Copyright (c) 2010  Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

/// -----------------------------------------------------------------------
/// Original Code: 
/// (c) 2009 Microsoft Corporation -- All rights reserved
/// This code is licensed under the MS-PL
/// http://www.opensource.org/licenses/ms-pl.html
/// Courtesy of the Open Source Techology Center: http://port25.technet.com
/// -----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;

namespace CoApp.Toolkit.Utility
{
    using System.ComponentModel;

    public class ProcessUtility
    {
        private Process currentProcess;
        private readonly string executable;

        private StringBuilder sErr = new StringBuilder();
        private StringBuilder sOut = new StringBuilder();

        private void CurrentProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            sErr.AppendLine(e.Data);
        }

        private void CurrentProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            sOut.AppendLine(e.Data);
        }

        private void CurrentProcess_Exited(object sender, EventArgs e)
        {
            Failed = currentProcess.ExitCode != 0;
        }

        public bool Failed { get; set; }

        public bool IsRunning
        {
            get
            {
                return currentProcess != null ? !currentProcess.HasExited : false;
            }
        }

        public void SendToStandardIn(string text)
        {
            if (!string.IsNullOrEmpty(text) && IsRunning)
                currentProcess.StandardInput.Write(text);
        }

        public string StandardOut
        {
            get
            {
                // if the process is still running, give what's here, and clear it.
                StringBuilder sCurrent = sOut;
                if (IsRunning)
                    sOut = new StringBuilder();

                return sCurrent.ToString();
            }
        }

        public string StandardError
        {
            get
            {
                return sErr.ToString();
            }
        }

        public ProcessUtility(string filename)
        {
            executable = filename;
        }

        public void WaitForExit()
        {
            if (IsRunning)
                currentProcess.WaitForExit();
        }

        public void ExecAsync(string arguments, params string[] args)
        {
            if (IsRunning)
                throw new InvalidAsynchronousStateException("Process is currently running.");

            Failed = false;
            sErr = new StringBuilder();
            sOut = new StringBuilder();

            currentProcess = new Process { StartInfo = { FileName = executable, Arguments = string.Format(arguments, args), WorkingDirectory = Environment.CurrentDirectory, RedirectStandardError = true, RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false } };

            currentProcess.ErrorDataReceived += CurrentProcess_ErrorDataReceived;
            currentProcess.OutputDataReceived += CurrentProcess_OutputDataReceived;
            currentProcess.Exited += CurrentProcess_Exited;

            currentProcess.Start();
            currentProcess.BeginErrorReadLine();
            currentProcess.BeginOutputReadLine();
        }

        public int Exec(string arguments, params string[] args)
        {
            try
            {
                ExecAsync(arguments, args);
                WaitForExit();
            }
            catch (Exception e)
            {
                currentProcess = null;
                sErr.AppendFormat("Failed to execute program [{0}]\r\n   {1}", executable, e.Message);
                return 100;
            }

            return currentProcess.ExitCode;
        }

        public int ExecWithStdin(string stdIn, string arguments, params string[] args)
        {
            try
            {
                ExecAsync(arguments, args);
                SendToStandardIn(stdIn);
                WaitForExit();
            }
            catch (Exception e)
            {
                currentProcess = null;
                sErr.AppendFormat("Failed to execute program [{0}]\r\n   {1}", executable, e.Message);
                return 100;
            }

            return currentProcess.ExitCode;
        }
    }
}
