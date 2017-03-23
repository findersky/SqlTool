namespace SqlTool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class WindowsService
    {
        private static string Server = @"\\.";
        public static List<string> WindowsServices = GetServices();

        public static bool Exists(string sServiceName)
        {
            return WindowsServices.Contains(sServiceName.ToUpper());
        }

        private static List<string> GetServices()
        {
            List<string> list = new List<string>();
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = string.Format("/c sc {0} query state= all", Server)
            };
            string[] strArray = Process.Start(startInfo).StandardOutput.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in strArray)
            {
                if (-1 != str.IndexOf("SERVICE_NAME"))
                {
                    list.Add(str.Split(new char[] { ':' })[1].Trim().ToUpper());
                }
            }
            return list;
        }

        public static StartType GetServiceStartType(string sServiceName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = string.Format("/c sc {0} qc {1}", Server, sServiceName)
            };
            string str = Process.Start(startInfo).StandardOutput.ReadToEnd();
            if (-1 != str.ToUpper().IndexOf("AUTO"))
            {
                return StartType.AUTO;
            }
            if (-1 != str.ToUpper().IndexOf("DEMAND"))
            {
                return StartType.DEMAND;
            }
            if (-1 != str.ToUpper().IndexOf("DISABLED"))
            {
                return StartType.DISABLED;
            }
            return StartType.NONE;
        }

        public static bool Running(string sServiceName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = string.Format("/c sc {0} query {1}", Server, sServiceName)
            };
            string str = Process.Start(startInfo).StandardOutput.ReadToEnd();
            return (-1 != str.ToUpper().IndexOf("RUNNING"));
        }

        public static void SetServer(string sServer)
        {
            Server = string.Format(@"\\{0}", sServer);
            WindowsServices = GetServices();
        }

        public static void SetServiceStartType(string sServiceName, StartType type)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                CreateNoWindow = false,
                UseShellExecute = false,
                Arguments = string.Format("/c sc {0} config {1} start= {2}", Server, sServiceName, type.ToString())
            };
            Process process = Process.Start(startInfo);
        }

        public enum StartType
        {
            AUTO = 2,
            BOOT = 0,
            DEMAND = 3,
            DISABLED = 4,
            NONE = -1,
            SYSTEM = 1
        }

        public enum State
        {
        }
    }
}

