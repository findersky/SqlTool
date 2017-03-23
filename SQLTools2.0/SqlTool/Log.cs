namespace SqlTool
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Windows.Forms;

    public class Log
    {
        public static void ClearLog()
        {
            if (File.Exists(LogFileName))
            {
                File.Delete(LogFileName);
                GC.Collect();
            }
        }

        public static void OpenLog()
        {
            FileManagement.OpenFile(LogFilePath, LogFileName);
        }

        public static void WriteLog(string sLogContent)
        {
            new FileManagement().WriteFile(LogFileName, sLogContent);
        }

        public static void WriteLog(string sDescription, string sExceptionMessage)
        {
            new FileManagement().WriteFile(LogFileName, string.Format("[{0}]：{1}出现异常，异常信息为：{2}\r\n\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sDescription, sExceptionMessage));
        }

        public static string LogFileName
        {
            get
            {
                return string.Format("{0}/{1}.txt", LogFilePath, DateTime.Now.ToString("yyyyMMdd"));
            }
        }

        public static string LogFilePath
        {
            get
            {
                string str = string.Empty;
                try
                {
                    str = ConfigurationSettings.AppSettings["LogFilePath"];
                }
                catch
                {
                }
                if (string.IsNullOrEmpty(str))
                {
                    str = string.Format("{0}/Log", Application.StartupPath);
                }
                if (!Directory.Exists(str))
                {
                    Directory.CreateDirectory(str);
                }
                return str;
            }
        }
    }
}

