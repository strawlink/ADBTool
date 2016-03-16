using System;
using System.Diagnostics;
using System.Threading;

namespace ADBTool
{
    class ADBConnection
    {
        Action<string> Output;

        public ADBConnection(Action<string> output)
        {
            this.Output = output;
        }
        
        private string GetADBPath()
        {
            string adbPath = SettingsManager.Settings.ADBPath;

            if (string.IsNullOrEmpty(adbPath))
            {
                Output("ADB path not set, starting setup");
                
                try
                {
                    Output("Checking environment variables");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.FileName = "adb";

                    Process p = new Process();
                    p.StartInfo = startInfo;
                    p.Start();

                    adbPath = "adb";

                    Output("Found ADB in environment variables");
                    SettingsManager.Settings.ADBPath = adbPath;
                    SettingsManager.SaveSettings();
                    p.Close();
                }
                catch
                {
                    // Temp hardcoded
                    Output("Unable to find ADB in environment variables, using hardcoded value");
                    Output("You can update this value manually in %appdata%\\ADBTool\\settings.ini");
                    adbPath = @"C:\Data\Programs\Android SDK\platform-tools\adb.exe";
                    SettingsManager.Settings.ADBPath = adbPath;
                    SettingsManager.SaveSettings();

                }
            }

            return adbPath;
        }

        private Process activeProcess;
        private Thread activeThread;
        
        // TODO: Implement callbacks
        public void StartExecuteCommand(string command)
        {
            if(activeThread != null && activeThread.IsAlive && activeProcess != null && !activeProcess.HasExited)
            {
                // Can't execute, process already running
                return;
            }

            activeThread = new Thread(() => ExecuteCommand(command));
            activeThread.IsBackground = true;
            activeThread.Start();
        }

        private void ExecuteCommand(string command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = GetADBPath();
            startInfo.Arguments = command;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            activeProcess = new Process();
            activeProcess.StartInfo = startInfo;
            activeProcess.OutputDataReceived += new DataReceivedEventHandler(DataReceivedHandler);

            Output(string.Format("Executing command: {0}", command));

            activeProcess.Start();
            activeProcess.BeginOutputReadLine();
            
            // TODO: Update progressbar, implement timeout
            activeProcess.WaitForExit();
            activeProcess.Close();
            Output("Idle");
        }
        
        public void DataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            string text = e.Data.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                Output(string.Format("[ADB] {0}", text));
            }
        }
    }
}
