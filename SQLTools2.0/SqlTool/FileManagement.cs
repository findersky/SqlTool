namespace SqlTool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Configuration;

    public class FileManagement
    {
        public static void DeleteFile(string sFilePath)
        {
            if (File.Exists(sFilePath))
            {
                File.Delete(sFilePath);
                GC.Collect();
            }
        }

        public void GetFiles(string sCurrentDirectory, List<string> lstDirectories, List<string> lstFiles)
        {
            
            string directoriso=ConfigurationSettings.AppSettings["IgnoreFolders"].ToString();
            Regex reg = new Regex(@"(" + directoriso + ")$");
            DirectoryInfo info = new DirectoryInfo(sCurrentDirectory);
            List<FileSystemItem> list = new List<FileSystemItem>();
            foreach (FileSystemInfo info2 in info.GetFileSystemInfos())
            {
                if (Directory.Exists(info2.FullName.ToString()))
                {
                    if (reg.IsMatch(info2.FullName.ToString()))
                    {
                        continue;
                    }
                }
                list.Add(new FileSystemItem(info2.FullName, info2.Attributes));
            }
            list.Sort(delegate (FileSystemItem item1, FileSystemItem item2) {
                double num = 0L;
                double num2 = 0L;
                Match match = Regex.Match(Path.GetFileName(item1.FullName), @"\d+");
                if (match.Captures.Count != 0)
                {
                    num = double.Parse(match.Captures[0].Value);
                }
                Match match2 = Regex.Match(Path.GetFileName(item2.FullName), @"\d+");
                if (match2.Captures.Count != 0)
                {
                    num2 = double.Parse(match2.Captures[0].Value);
                }
                return num.CompareTo(num2);
            });
            foreach (FileSystemItem item in list)
            {
                if (item.FileAttribute.ToString().Contains(FileAttributes.Directory.ToString()))
                {
                    this.GetFiles(item.FullName, lstDirectories, lstFiles);
                }
                else
                {
                    lstDirectories.Add(sCurrentDirectory);
                    lstFiles.Add(item.FullName);
                }
            }
        }

        public static void OpenFile(string sPath, string sFile)
        {
            if (!File.Exists(sFile))
            {
                File.Create(sFile);
                GC.Collect();
            }
            ProcessStartInfo startInfo = new ProcessStartInfo {
                WorkingDirectory = sPath,
                FileName = sFile,
                Arguments = ""
            };
            Process.Start(startInfo);
        }

        public static void OpenFolder(string sPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = "explorer.exe",
                Arguments = sPath
            };
            Process.Start(startInfo);
        }

        public string ReadFile(string sFilePath)
        {
            string str = string.Empty;
            FileStream stream=null;
            StreamReader reader=null;
            Encoding encodinger = GetFileEncodeType(sFilePath);
            try
            {
                 stream = new FileStream(sFilePath, FileMode.OpenOrCreate);
                 reader = new StreamReader(stream, Encoding.Default);
                 reader = new StreamReader(stream, encodinger);
                 str = reader.ReadToEnd();

            }catch(Exception ex)
            {
                Log.WriteLog(ex.ToString());
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            return str;
        }

        public void WriteFile(string sFilePath, string sAppendContent)
        {
            Encoding encodinger = GetFileEncodeType(sFilePath);
            FileStream stream = new FileStream(sFilePath, FileMode.Append);
            //StreamWriter writer = new StreamWriter(stream, Encoding.Default);
            StreamWriter writer = new StreamWriter(stream, encodinger);
            writer.Write(sAppendContent);
            writer.Close();
            stream.Close();
        }
        /// <summary>
        /// 获取指定文件的编码
        /// 以防止在不知道文件编码格式的情况下处理文件而造成的乱码问题
        /// </summary>
        /// <param name="filename">文件路径</param>
        /// <returns></returns>
        public System.Text.Encoding GetFileEncodeType(string filename)
        {
            if (!File.Exists(filename))
            {
                return System.Text.Encoding.Default;
            }
            System.Text.Encoding ReturnReturn = null;
            System.IO.FileStream fs = null;
            System.IO.BinaryReader br = null;
            try
            {
                fs = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                br = new System.IO.BinaryReader(fs);
                Byte[] buffer = br.ReadBytes(2);
                if (buffer.Length>0&&buffer[0] >= 0xEF)
                {
                    if (buffer[0] == 0xEF && buffer[1] == 0xBB)
                    {
                        ReturnReturn = System.Text.Encoding.UTF8;
                    }
                    else if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                    {
                        ReturnReturn = System.Text.Encoding.BigEndianUnicode;
                    }
                    else if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                    {
                        ReturnReturn = System.Text.Encoding.Unicode;
                    }
                    else
                    {
                        ReturnReturn = System.Text.Encoding.Default;
                    }
                }
                else if (buffer.Length>0&&buffer[0] == 0xe4 && buffer[1] == 0xbd) //无BOM的UTF-8
                {
                    ReturnReturn = System.Text.Encoding.UTF8;
                }
                else
                {
                    ReturnReturn = System.Text.Encoding.Default;
                }
            }
            catch (Exception ex)
            {
                ReturnReturn = System.Text.Encoding.Default;
                Log.WriteLog(ex.ToString());
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
                
            }
            return ReturnReturn;
        }
        protected class FileSystemItem
        {
            public FileAttributes FileAttribute;
            public string FullName;

            public FileSystemItem(string sFullName, FileAttributes fileAttribute)
            {
                this.FullName = sFullName;
                this.FileAttribute = fileAttribute;
            }
        }
    }
}

