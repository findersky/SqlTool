namespace SqlTool
{
    using Microsoft.SqlServer.Management.Smo;
    using Microsoft.Win32;
    using SQLDMO;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class SqlManagement
    {
        public string Password;
        public string Server;
        public static DataTable Servers;
        public string UserID;

        static SqlManagement()
        {
            if ((Servers == null) || (Servers.Rows.Count == 0))
            {
                Servers = GetDataBaseServers();
            }
        }

        public SqlManagement()
        {
        }

        public SqlManagement(string sServer, string sUID, string sPWD)
        {
            this.Server = sServer;
            this.UserID = sUID;
            this.Password = sPWD;
        }

        public bool BackupDatabase(string sFullName, string sDataBase)
        {
            bool flag = false;
            SQLDMO.Backup backup = new BackupClass();
            SQLServer serverObject = new SQLServerClass();
            try
            {
                serverObject.LoginSecure = false;
                serverObject.Connect(this.Server, this.UserID, this.Password);
                backup.Action = SQLDMO_BACKUP_TYPE.SQLDMOBackup_Database;
                backup.Database = sDataBase;
                backup.Files = string.Format("[{0}]", sFullName);
                backup.BackupSetName = sDataBase;
                backup.BackupSetDescription = string.Format("数据库备份：{0}", sDataBase);
                backup.Initialize = true;
                backup.SQLBackup(serverObject);
                if (System.IO.File.Exists(sFullName))
                {
                    flag = true;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLog(string.Format("备份数据库[{0}]：BackupDatabase(string sFullName, string sDataBase)", sDataBase), exception.Message);
                return flag;
            }
            finally
            {
                serverObject.DisConnect();
            }
            return flag;
        }

        public int BatchAnalysisScript(List<string> lstFiles, bool blnCheckDataBase, bool blnYesOrNo)
        {
            Exception exception;
            FileManagement management = new FileManagement();
            string sSql = string.Empty;
            int num = 0;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("/**************************************[{0}]开始批量分析脚本**************************************/\r\n", DateTime.Now.ToString()));
            SQLServer server = new SQLServerClass();
            try
            {
                server.Connect(this.Server, this.UserID, this.Password);
                foreach (string str2 in lstFiles)
                {
                    if (Path.GetExtension(str2) != ".sql")
                    {
                        num++;
                        builder.Append(string.Format("\r\n{0}、[{1}]：{2} 非脚本文件\r\n", num, DateTime.Now.ToString(), str2));
                    }
                    else
                    {
                        try
                        {
                            sSql = management.ReadFile(str2);
                            if (blnCheckDataBase)
                            {
                                if (blnYesOrNo)
                                {
                                    if (this.CheckUseDatabase(sSql))
                                    {
                                        num++;
                                        builder.Append(string.Format("\r\n{0}、[{1}] 脚本文件“{2}”指定了数据库名\r\n", num, DateTime.Now.ToString(), str2));
                                    }
                                }
                                else if (!this.CheckUseDatabase(sSql))
                                {
                                    num++;
                                    builder.Append(string.Format("\r\n{0}、[{1}] 脚本文件“{2}”未指定数据库名\r\n", num, DateTime.Now.ToString(), str2));
                                }
                            }
                            server.ExecuteImmediate(string.Format("SET PARSEONLY ON;{0}", sSql), SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                        }
                        catch (Exception exception1)
                        {
                            exception = exception1;
                            num++;
                            builder.Append(string.Format("\r\n{0}、[{1}]：{2} 脚本文件异常\r\n异常信息为：{3}\r\n", new object[] { num, DateTime.Now.ToString(), str2, exception.Message }));
                        }
                        finally
                        {
                            server.ExecuteImmediate("SET PARSEONLY OFF; ", SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                        }
                    }
                }
                builder.Append(string.Format("\r\n/**************************************[{0}]结束批量分析脚本**************************************/\r\n\r\n", DateTime.Now.ToString()));
                Log.WriteLog(builder.ToString());
            }
            catch (Exception exception2)
            {
                exception = exception2;
                num = -1;
                Log.WriteLog("批量分析脚本：BatchAnalysisScript(string lstFiles, bool blnCheckDataBase, bool blnYesOrNo)", exception.Message);
                return num;
            }
            finally
            {
                server.DisConnect();
                GC.Collect();
            }
            return num;
        }

        public int BatchExecuteSql(List<string> lstFiles)
        {
            Exception exception;
            int num = 0;
            int num2 = 0;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("/**************************************[{0}]开始批量执行脚本**************************************/\r\n", DateTime.Now.ToString()));
            FileManagement management = new FileManagement();
            SQLServer server = new SQLServerClass();
            try
            {
                server.Connect(this.Server, this.UserID, this.Password);
                string command = string.Empty;
                foreach (string str2 in lstFiles)
                {
                    try
                    {
                        command = management.ReadFile(str2);
                        server.ExecuteImmediate(command, SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                        num++;
                    }
                    catch (Exception exception1)
                    {
                        exception = exception1;
                        num2++;
                        builder.Append(string.Format("\r\n{0}、[{1}]：{2} 脚本文件异常\r\n异常信息为：{3}\r\n", new object[] { num2, DateTime.Now.ToString(), str2, exception.Message }));
                    }
                }
                builder.Append(string.Format("\r\n共{0}个脚本文件，{1}执行成功，{2}执行失败\r\n", lstFiles.Count, num, num2));
                builder.Append(string.Format("\r\n/**************************************[{0}]结束批量执行脚本**************************************/\r\n\r\n", DateTime.Now.ToString()));
                Log.WriteLog(builder.ToString());
            }
            catch (Exception exception2)
            {
                exception = exception2;
                num2 = -1;
                Log.WriteLog("批量执行数据库升级脚本：BatchExecuteSql(string lstFiles)", exception.Message);
                return num2;
            }
            finally
            {
                server.DisConnect();
                GC.Collect();
            }
            return num2;
        }

        public int BatchExecuteSql(List<string> lstDatabases, List<string> lstFiles)
        {
            Exception exception;
            FileManagement management = new FileManagement();
            int num = 0;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("/**************************************[{0}]开始批量执行脚本**************************************/\r\n", DateTime.Now.ToString()));
            SQLServer server = new SQLServerClass();
            try
            {
                server.Connect(this.Server, this.UserID, this.Password);
                string command = string.Empty;
                foreach (string str2 in lstDatabases)
                {
                    builder.Append(string.Format("\r\n[{0}] 数据库[{1}]准备升级\r\n", DateTime.Now.ToString(), str2));
                    foreach (string str3 in lstFiles)
                    {
                        try
                        {
                            command = string.Format("USE {0}\r\n{1}", str2, management.ReadFile(str3));
                            server.ExecuteImmediate(command, SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                        }
                        catch (Exception exception1)
                        {
                            exception = exception1;
                            num++;
                            builder.Append(string.Format("\r\n{0}、[{1}]：{2} 脚本文件异常\r\n异常信息为：{3}\r\n", new object[] { num, DateTime.Now.ToString(), str3, exception.Message }));
                        }
                    }
                    builder.Append(string.Format("\r\n[{0}] 数据库[{1}]升级完毕\r\n", DateTime.Now.ToString(), str2));
                }
                builder.Append(string.Format("\r\n/**************************************[{0}]结束批量执行脚本**************************************/\r\n\r\n", DateTime.Now.ToString()));
                Log.WriteLog(builder.ToString());
            }
            catch (Exception exception2)
            {
                exception = exception2;
                num = -1;
                Log.WriteLog("批量执行数据库升级脚本：BatchExecuteSql(List<string> lstDatabases, List<string> lstFiles) ", exception.Message);
                return num;
            }
            finally
            {
                server.DisConnect();
                GC.Collect();
            }
            return num;
        }

        public bool CheckScriptGrammar(string sSql, out string sMessage)
        {
            bool flag2;
            sMessage = string.Empty;
            SQLServer server = new SQLServerClass();
            bool flag = false;
            try
            {
                server.Connect(this.Server, this.UserID, this.Password);
                flag = true;
                server.ExecuteImmediate(string.Format("SET PARSEONLY ON;{0}", sSql), SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                flag2 = true;
            }
            catch (Exception exception)
            {
                sMessage = exception.Message;
                flag2 = false;
            }
            finally
            {
                if (flag)
                {
                    server.ExecuteImmediate("SET PARSEONLY OFF; ", SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
                    server.DisConnect();
                }
            }
            return flag2;
        }

        public bool CheckUseDatabase(string sSql)
        {
            string pattern = @"[\s|(\r\n)]*(USE)\s+(\[)?[A-Za-z0-9]+(\])?(\r\n)*\s+";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(sSql))
            {
                if ((match.Captures.Count != 0) && (match.Captures[0].Value.Replace("\r\n", "").Replace("[", "").Replace("]", "").Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 1))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckUseDatabase(string sSql, out List<string> lstDBNames)
        {
            lstDBNames = new List<string>();
            bool flag = false;
            string pattern = @"[\s|(\r\n)]*(USE)\s+(\[)?[A-Za-z0-9]+(\])?(\r\n)*\s+";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(sSql))
            {
                if (match.Captures.Count != 0)
                {
                    string[] strArray = match.Captures[0].Value.Replace("\r\n", "").Replace("[", "").Replace("]", "").Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (strArray.Length > 1)
                    {
                        flag = true;
                        lstDBNames.Add(strArray[1]);
                    }
                }
            }
            return flag;
        }

        public bool CheckUseDatabase(List<string> lstFiles, bool blnYesOrNo)
        {
            bool flag = true;
            int num = 0;
            FileManagement management = new FileManagement();
            string sSql = string.Empty;
            foreach (string str2 in lstFiles)
            {
                if (".sql" == Path.GetExtension(str2).ToLower())
                {
                    sSql = management.ReadFile(str2);
                    if (blnYesOrNo)
                    {
                        if (this.CheckUseDatabase(sSql))
                        {
                            num++;
                            flag = false;
                            Log.WriteLog(string.Format("\r\n{0}、[{1}] 脚本文件“{2}”指定了数据库名\r\n", num, DateTime.Now.ToString(), str2));
                        }
                    }
                    else if (!this.CheckUseDatabase(sSql))
                    {
                        num++;
                        flag = false;
                        Log.WriteLog(string.Format("\r\n{0}、[{1}] 脚本文件“{2}”未指定数据库名\r\n", num, DateTime.Now.ToString(), str2));
                    }
                }
            }
            return flag;
        }

        public bool Connect()
        {
            bool flag;
            SQLServer server = new SQLServerClass();
            try
            {
                server.LoginSecure = false;
                server.Connect(this.Server, this.UserID, this.Password);
                flag = true;
            }
            catch (Exception exception)
            {
                Log.WriteLog(string.Format("连接数据库服务器[{0}]：Connect()", this.Server), exception.Message);
                flag = false;
            }
            finally
            {
                server.DisConnect();
            }
            return flag;
        }

        public static bool DBExists(string sDBName, string sServer, string sUID, string sPWD)
        {
            bool flag = false;
            SQLServer server = new SQLServerClass();
            try
            {
                server.LoginSecure = false;
                server.Connect(sServer, sUID, sPWD);
                Databases databases = server.Databases;
                if (databases.Count > 0)
                {
                    foreach (SQLDMO.Database database in databases)
                    {
                        if (sDBName.Trim() == database.Name.Trim())
                        {
                            return true;
                        }
                    }
                    return flag;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLog("判断连接的数据库服务器上有无同名数据库：DatabaseExists(string sDBName)", exception.Message);
                return flag;
            }
            finally
            {
                server.DisConnect();
            }
            return flag;
        }

        public void ExecuteSql(string sSql)
        {
            SQLServer server = new SQLServerClass();
            try
            {
                server.Connect(this.Server, this.UserID, this.Password);
                server.ExecuteImmediate(sSql, SQLDMO_EXEC_TYPE.SQLDMOExec_Default, null);
            }
            catch (Exception exception)
            {
                Log.WriteLog("执行脚本：ExecuteSql(string sSql)", exception.Message);
            }
            finally
            {
                server.DisConnect();
            }
        }

        public static bool Exists(string sServer)
        {
            if ((Servers == null) || (Servers.Rows.Count == 0))
            {
                return true;
            }
            if ("." == sServer)
            {
                sServer = sServer.Replace(".", Dns.GetHostName());
            }
            string str = IPAddress.Parse("127.0.0.1").ToString();
            if (sServer.StartsWith(str))
            {
                sServer = sServer.Replace(str, Dns.GetHostName());
            }
            if (sServer.StartsWith(@".\"))
            {
                sServer = sServer.Replace(@".\", string.Format(@"{0}\", Dns.GetHostName()));
            }
            foreach (DataRow row in Servers.Rows)
            {
                if (row[0].ToString().ToUpper() == sServer.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }

        public List<string> GetDataBases(List<string> lstFiles)
        {
            List<string> list = new List<string>();
            FileManagement management = new FileManagement();
            string sSql = string.Empty;
            foreach (string str2 in lstFiles)
            {
                if (".sql" == Path.GetExtension(str2).ToLower())
                {
                    List<string> list2;
                    sSql = management.ReadFile(str2);
                    this.CheckUseDatabase(sSql, out list2);
                    foreach (string str3 in list2)
                    {
                        if (!list.Contains(str3.ToLower()))
                        {
                            list.Add(str3.ToLower());
                        }
                    }
                }
            }
            return list;
        }

        public static DataTable GetDataBaseServers()
        {
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("Name"));
            foreach (string str in WindowsService.WindowsServices)
            {
                if (str.ToUpper() == "MSSQLSERVER")
                {
                    table.Rows.Add(new object[] { Dns.GetHostName().ToUpper() });
                }
                else if (str.Contains("MSSQL$"))
                {
                    table.Rows.Add(new object[] { string.Format(@"{0}\{1}", Dns.GetHostName().ToUpper(), str.Replace("MSSQL$", "")) });
                }
            }
            try
            {
                if (table.Rows.Count == 0)
                {
                    Application application = new ApplicationClass();
                    NameList list = application.ListAvailableSQLServers();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list.Item(i + 1).Contains(Dns.GetHostName()))
                        {
                            table.Rows.Add(new object[] { list.Item(i + 1) });
                        }
                    }
                    if (table.Rows.Count == 0)
                    {
                        foreach (DataRow row in SmoApplication.EnumAvailableSqlServers(true).Rows)
                        {
                            table.Rows.Add(new object[] { row["name"] });
                        }
                    }
                }
                if (table.Rows.Count != 0)
                {
                    return table;
                }
                RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                foreach (string str2 in key.GetValueNames())
                {
                    table.Rows.Add(new object[] { (str2.ToUpper() == "MSSQLSERVER") ? Dns.GetHostName().ToUpper() : str2.ToUpper() });
                }
            }
            catch (Exception exception)
            {
                Log.WriteLog("获取本机安装数据库服务器：GetDataBaseServers()", exception.Message);
            }
            return table;
        }

        public List<string> GetServerDatabases()
        {
            List<string> list = new List<string>();
            SQLServer server = new SQLServerClass();
            try
            {
                server.LoginSecure = false;
                server.Connect(this.Server, this.UserID, this.Password);
                Databases databases = server.Databases;
                if (databases.Count > 0)
                {
                    foreach (SQLDMO.Database database in databases)
                    {
                        list.Add(database.Name.ToLower());
                    }
                }
            }
            catch (Exception exception)
            {
                Log.WriteLog("获取数据库服务器数据库列表：GetServerDatabases()", exception.Message);
                return list;
            }
            finally
            {
                server.DisConnect();
            }
            return list;
        }

        public void RecordScriptUpdateLog(int nScriptFilesCount, string sLastFolderName, string sLastScriptFileName, decimal TimeConsume, string sDescription)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(" USE [master] ");
            builder.Append(" IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ScriptUpdateLog]') AND type in (N'U')) ");
            builder.Append(" BEGIN ");
            builder.Append(" CREATE TABLE [dbo].[ScriptUpdateLog]( ");
            builder.Append(" [UpdateID] [int] IDENTITY(1,1) NOT NULL, ");
            builder.Append(" [ScriptFilesCount] [int] NULL, ");
            builder.Append(" [LastFolderName] [nvarchar](255) NOT NULL, ");
            builder.Append(" [LastScriptFileName] [nvarchar](255) NOT NULL, ");
            builder.Append(" [TimeConsume] [numeric](18, 3) NULL, ");
            builder.Append(" [Description] [nvarchar](500) NULL, ");
            builder.Append(" [LoginUserName] [nvarchar](255) NOT NULL, ");
            builder.Append(" [UpdateTime] [datetime] NOT NULL, ");
            builder.Append(" [ClientIP] [varchar](500) NOT NULL, ");
            builder.Append(" [ClientHostName] [nvarchar](255) NOT NULL ");
            builder.Append(" ) ON [PRIMARY] ");
            builder.Append(" END ");
            StringBuilder builder2 = new StringBuilder();
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress address in hostEntry.AddressList)
            {
                builder2.Append(address.ToString());
                builder2.Append(",");
            }
            builder.Append(" INSERT INTO [dbo].[ScriptUpdateLog]([ScriptFilesCount],[LastFolderName],[LastScriptFileName],[TimeConsume],[Description],[LoginUserName],[UpdateTime],[ClientIP],[ClientHostName])");
            builder.Append(string.Format(" VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", new object[] { nScriptFilesCount, sLastFolderName, sLastScriptFileName, TimeConsume, sDescription, Environment.UserName, DateTime.Now, builder2, Dns.GetHostName() }));
            this.ExecuteSql(builder.ToString());
        }

        public List<string> SearchScript(List<string> lstFiles, string sKeywords)
        {
            List<string> list = new List<string>();
            string[] strArray = sKeywords.Replace("，", ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length != 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(string.Format("/**************************************[{0}]开始查找脚本文件**************************************/\r\n", DateTime.Now.ToString()));
                builder.Append(string.Format("关键字为{0},匹配的脚本文件为\r\n", sKeywords));
                FileManagement management = new FileManagement();
                string str = string.Empty;
                foreach (string str2 in lstFiles)
                {
                    str = management.ReadFile(str2);
                    for (int i = 0; i < strArray.Length; i++)
                    {
                        if (str.Contains(strArray[i]))
                        {
                            builder.Append(string.Format("{0}、{1}\r\n", list.Count + 1, str2));
                            list.Add(str2);
                            break;
                        }
                    }
                }
                builder.Append(string.Format("/**************************************[{0}]结束查找脚本文件**************************************/\r\n\r\n", DateTime.Now.ToString()));
                Log.WriteLog(builder.ToString());
            }
            return list;
        }
        public List<string> SearchScript(List<string> lstFiles, string sKeywords,bool Regexboll)
        {
            List<string> list = new List<string>();
            Regex reg;
            string[] strArray = sKeywords.Replace("，", ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length != 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(string.Format("/**************************************[{0}]开始查找脚本文件**************************************/\r\n", DateTime.Now.ToString()));
                builder.Append(string.Format("关键字为{0},匹配的脚本文件为\r\n", sKeywords));
                FileManagement management = new FileManagement();
                string str = string.Empty;
                
                    reg = new Regex(sKeywords);
                
            
                foreach (string str2 in lstFiles)
                {
                    str = management.ReadFile(str2);
                    if (Regexboll)
                    {
                        if (reg.IsMatch(str)) 
                        {
                            builder.Append(string.Format("{0}、{1}\r\n", list.Count + 1, str2));
                            list.Add(str2);
                        }
                        continue;
                    }
                    for (int i = 0; i < strArray.Length; i++)
                    {
                        if (str.Contains(strArray[i]))
                        {
                            builder.Append(string.Format("{0}、{1}\r\n", list.Count + 1, str2));
                            list.Add(str2);
                            break;
                        }
                    }
                }
                builder.Append(string.Format("/**************************************[{0}]结束查找脚本文件**************************************/\r\n\r\n", DateTime.Now.ToString()));
                Log.WriteLog(builder.ToString());
            }
            return list;
        }

        public bool StoreDatabase(string sFile, string sPath, string sDataBase)
        {
            bool flag = false;
            SQLServer serverObject = new SQLServerClass();
            SQLDMO.Restore restore = new RestoreClass();
            try
            {
                int num3;
                serverObject.LoginSecure = false;
                serverObject.Connect(this.Server, this.UserID, this.Password);
                QueryResults results = serverObject.EnumProcesses(-1);
                int column = -1;
                int num2 = -1;
                for (num3 = 1; num3 < results.Columns; num3++)
                {
                    string str = results.get_ColumnName(num3);
                    if (str.ToUpper().Trim() == "SPID")
                    {
                        column = num3;
                    }
                    else if (str.ToUpper().Trim() == "DBNAME")
                    {
                        num2 = num3;
                    }
                    if ((column != -1) && (num2 != -1))
                    {
                        break;
                    }
                }
                num3 = 1;
                while (num3 < results.Rows)
                {
                    int columnLong = results.GetColumnLong(num3, column);
                    if (results.GetColumnString(num3, num2).ToUpper() == sDataBase.ToUpper())
                    {
                        serverObject.KillProcess(columnLong);
                    }
                    num3++;
                }
                QueryResults results2 = serverObject.ExecuteWithResults(string.Format("RESTORE FILELISTONLY FROM DISK = '{0}'", sFile), null);
                int num5 = 0;
                int num6 = 0;
                for (num3 = 1; num3 < results2.Columns; num3++)
                {
                    if ("LOGICALNAME" == results2.get_ColumnName(num3).ToUpper())
                    {
                        num5 = num3;
                    }
                    if ("PHYSICALNAME" == results2.get_ColumnName(num3).ToUpper())
                    {
                        num6 = num3;
                    }
                    if ((num6 != 0) && (num5 != 0))
                    {
                        break;
                    }
                }
                StringBuilder builder = new StringBuilder();
                for (num3 = 1; num3 <= results2.Rows; num3++)
                {
                    builder.Append(string.Format(@"[{0}],[{1}\{2}]", results2.GetColumnString(num3, num5), sPath, Path.GetFileName(results2.GetColumnString(num3, num6))));
                    if (num3 != results2.Rows)
                    {
                        builder.Append(",");
                    }
                }
                restore.Action = SQLDMO_RESTORE_TYPE.SQLDMORestore_Database;
                restore.Database = sDataBase;
                restore.Files = sFile;
                restore.FileNumber = 1;
                restore.ReplaceDatabase = false;
                restore.RelocateFiles = builder.ToString();
                restore.SQLRestore(serverObject);
                Databases databases = serverObject.Databases;
                foreach (SQLDMO.Database database in databases)
                {
                    if (database.Name == sDataBase)
                    {
                        return true;
                    }
                }
                return flag;
            }
            catch (Exception exception)
            {
                Log.WriteLog(string.Format("还原数据库[{0}]：StoreDatabase(string sFile,string sPath,string sDataBase)", sDataBase), exception.Message);
                return flag;
            }
            finally
            {
                serverObject.DisConnect();
            }
            return flag;
        }
    }
}

