namespace SqlTool
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using System.Threading;
    using System.Reflection;

    public class SqlTool : Form
    {
        #region 属性
        private BackgroundWorker bgwGetInstances;
        private Button btnBackup;
        private Button btnBrowseBackup;
        private Button btnBrowserFile;
        private Button btnBrowserPath;
        private Button btnBrowseScript;
        private Button btnBrowseScript1;
        private Button btnClearLog;
        private Button btnExecuteSql;
        private Button btnExit;
        private Button btnGetInstances;
        private Button btnReadLog;
        private Button btnRefresh;
        private Button btnSearch;
        private Button btnSelectDB;
        private Button btnStore;
        private Button btnTest;
        private Button btnTestConnect;
        private ComboBox cbxDatabases;
        private ComboBox cbxServers;
        private CheckedListBox chkListDataBases;
        private IContainer components = null;
        private FolderBrowserDialog folderBrowserDialog;
        private Label label1;
        private Label label10;
        private Label label11;
        private Label label12;
        private Label label13;
        private Label label14;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Label label9;
        private OpenFileDialog openFileDialog;
        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private ProgressBar progressBar;
        private RadioButton rbtNo;
        private RadioButton rbtYes;
        private static string SqlServerServiceName = "MSSQLSERVER";
        private TabControl tabBackup;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private ToolTip toolTip;
        private TextBox txtBackupPath;
        private TextBox txtDBName;
        private TextBox txtFileName;
        private TextBox txtKeywords;
        private TextBox txtLoginName;
        private TextBox txtPassword;
        private TextBox txtScriptPath;
        private TextBox txtScriptPath1;
        private TextBox txtStoreFile;
        private StatusStrip StatusBar;
        private ToolStripStatusLabel tlstatusTime;
        private System.Windows.Forms.Timer TimerNowDate;
        private ToolStripStatusLabel tltattusDisProcess;
        private CheckBox ckblBakcDatabase;
        private CheckBox chkRegex;
        private TextBox txtStorePath;
        private delegate void ReBackDataBase(string path);
        private delegate void ReStoreDataBase();
        private delegate void SetprogressBar(ProgressBar pb,int Value);
        private delegate string GetTextBoxValue(TextBox tb);
        private delegate string GetComboBoxValue(ComboBox cb);
        private delegate string GetCheckValue(RadioButton tb);
        private delegate bool GetCheckValues(CheckBox cb);
        #endregion

        public SqlTool()
        {
            this.InitializeComponent();
            this.InitializeOtherControls();
            this.InitializeBackGroundWorker();
        }

        private void btnBackup_Click(object sender, EventArgs e)
        {
            if (!SqlManagement.Exists(this.cbxServers.Text.Trim()))
            {
                MessageBox.Show("数据库服务器不存在！", "提示");
            }
            else if (!(!string.IsNullOrEmpty(this.txtBackupPath.Text.Trim()) && Directory.Exists(this.txtBackupPath.Text.Trim())))
            {
                MessageBox.Show("无效的备份路径！", "提示");
            }
            else
            {
                DirectoryInfo info = new DirectoryInfo(this.txtBackupPath.Text.Trim());
                if (info.Name.Contains<char>(' '))
                {
                    MessageBox.Show("备份路径目标文件夹中不能包含空格！", "提示");
                }
                else if (-1 == this.cbxDatabases.SelectedIndex)
                {
                    MessageBox.Show("请选择要备份的数据库！", "提示");
                }
                else if (string.IsNullOrEmpty(this.txtFileName.Text.Trim()))
                {
                    MessageBox.Show("请输入文件名！", "提示");
                }
                else
                {
                    string path = string.Format(@"{0}\{1}", this.txtBackupPath.Text.Trim(), this.txtFileName.Text.Trim());
                    if (File.Exists(path))
                    {
                        MessageBox.Show("指定文件与现有文件重名，请指定另一文件名！", "提示");
                    }
                    else
                    {
                        #region 原有处理备份的方法
                        /*
                        while (this.progressBar.Value < 50)
                        {
                            this.progressBar.Value += 5;
                        }
                        SqlManagement management = new SqlManagement(this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim());
                        if (!management.BackupDatabase(path, this.cbxDatabases.Text.Trim()))
                        {
                            MessageBox.Show("备份失败，请查看日志文件！", "提示");
                            this.progressBar.Value = 0;
                        }
                        else
                        {
                            while (this.progressBar.Value < this.progressBar.Maximum)
                            {
                                this.progressBar.Value += 5;
                            }
                            MessageBox.Show("备份完毕！", "提示");
                            this.progressBar.Value = 0;
                            FileManagement.OpenFolder(this.txtBackupPath.Text.Trim());
                        }
                        */
                        #endregion
                        ControlUseTrim();
                        //线程处理'
                        Thread td = new Thread(delegate() {
                            ThreadBackUpDataBase(path);
                        });
                        this.btnBackup.Enabled = false;
                        SetStatusDis("已经准备开始备份数据库……");
                        Thread.Sleep(20);
                        td.Start();
                    }
                }
            }
        }
        private void SetStatusDis(string msg)
        {
            if (this.StatusBar.InvokeRequired)
            {
                this.StatusBar.Invoke((MethodInvoker)delegate() { 
                  this.tltattusDisProcess.Text = "状态:"+msg;
                });
            }
            else
            {
                this.tltattusDisProcess.Text = "状态:"+msg;
            }
        }
        #region 备份数据库
        /// <summary>
        /// 去除指定控件的空格
        /// </summary>
        private void ControlUseTrim()
        {
            this.cbxServers.Text = this.cbxServers.Text.Trim();
            this.txtLoginName.Text = this.txtLoginName.Text.Trim();
            this.txtPassword.Text = this.txtPassword.Text.Trim();
            this.cbxDatabases.Text = this.cbxDatabases.Text.Trim();
            this.txtBackupPath.Text = this.txtBackupPath.Text.Trim();
        }
        /// <summary>
        /// 备份线程运行的地方法
        /// </summary>
        /// <param name="path"></param>
        private void ThreadBackUpDataBase(string path)
        {
            SetStatusDis("开始备份……");
            Thread.Sleep(50);
            //实例委托
            ReBackDataBase rdb = new ReBackDataBase(BackUpDataBase);
            IAsyncResult result = rdb.BeginInvoke(path, null, null);//开始异步
            SetStatusDis("正在进行备份数据库，请等待……");
            int i = 1;
            //轮询判断异步是否结束 如果没有结束就循环进度条
            while (!result.IsCompleted)
            {
                Thread.Sleep(50);
                if (this.progressBar.InvokeRequired)
                {
                    SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    this.progressBar.Invoke(sbr, this.progressBar, i);
                }
                else
                {
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    SetprogressBarMethod(this.progressBar, i);
                }
                i++;
            }
            i =0;
            rdb.EndInvoke(result);
            SetStatusDis("数据库备份完成！");
            if (this.progressBar.InvokeRequired)
            {
                SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                this.progressBar.Invoke(sbr, this.progressBar, 0);
            }
            else
            {
                SetprogressBarMethod(this.progressBar, 0);
            }

            this.btnBackup.Invoke((MethodInvoker)delegate() {
                this.btnBackup.Enabled = true;
            });
        }

        private void SetprogressBarMethod(ProgressBar Pbr, int Value)
        {
            Pbr.Value = Value;
        }
        /// <summary>
        /// 获得指定文本的值
        /// </summary>
        /// <param name="tb"></param>
        /// <returns></returns>
        private string GetTextBoxValueStr(TextBox tb)
        {
            return tb.Text;
        }
        private string GetGetComboBoxValueValueStr(ComboBox cb)
        {
            return cb.Text;
        }
        private bool GetCheckValueBool(CheckBox cb)
        {
            return cb.Checked;
        }
        /// <summary>
        /// 备份数据库
        /// </summary>
        private void BackUpDataBase(string path)
        {
            GetTextBoxValue gbv = new GetTextBoxValue(GetTextBoxValueStr);
            GetComboBoxValue cbv = new GetComboBoxValue(GetGetComboBoxValueValueStr);
            string cbxServers_Str = this.cbxServers.Invoke(cbv, this.cbxServers) as string;
            string txtLoginName_Str = this.txtLoginName.Invoke(gbv, this.txtLoginName) as string;
            string txtPassword_Str = this.txtPassword.Invoke(gbv, this.txtPassword) as string;
            string cbxDatabases_Str = this.cbxDatabases.Invoke(cbv, this.cbxDatabases) as string;
            string txtBackupPath_Str = this.txtBackupPath.Invoke(gbv, this.txtBackupPath) as string;

            SqlManagement management = new SqlManagement(cbxServers_Str, txtLoginName_Str, txtPassword_Str);
            if (!management.BackupDatabase(path, cbxDatabases_Str))
            {
                this.progressBar.Invoke((MethodInvoker)delegate()
                {
                    this.progressBar.Value = 0;
                });
                MessageBox.Show("备份失败，请查看日志文件！", "提示");

            }
            else
            {

                this.progressBar.Invoke((MethodInvoker)delegate()
                {
                    this.progressBar.Value = 0;
                });
                Thread.Sleep(20);
                MessageBox.Show("备份完毕！", "提示");
                FileManagement.OpenFolder(txtBackupPath_Str);
            }

        } 
        #endregion

        private void btnBrowseBackup_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtBackupPath.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        private void btnBrowserFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog.FileName = string.Empty;
            this.openFileDialog.Filter = "(备份文件*.bak;*.trn)|*.bak;*.trn;|(所有文件*)|*.*";
            if (this.openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtStoreFile.Text = this.openFileDialog.FileName;
            }
        }

        private void btnBrowserPath_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtStorePath.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        private void btnBrowseScript_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtScriptPath.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        private void btnBrowseScript1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtScriptPath1.Text = this.folderBrowserDialog.SelectedPath;
            }
        }
        /// <summary>
        /// 清空日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            
            Log.ClearLog();
            MessageBox.Show("日志清空完成！","提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        private string getRadioButtonValue(RadioButton rb)
        {
            return rb.Checked.ToString();
        }
        private void btnExecuteSql_Click(object sender, EventArgs e)
        {
            if (!SqlManagement.Exists(this.cbxServers.Text.Trim()))
            {
                MessageBox.Show("数据库服务器不存在！", "提示");
            }
            else if (string.IsNullOrEmpty(this.txtLoginName.Text.Trim()))
            {
                MessageBox.Show("请输入登录名！", "提示");
            }
            else if (!(!string.IsNullOrEmpty(this.txtScriptPath.Text.Trim()) && Directory.Exists(this.txtScriptPath.Text.Trim())))
            {
                MessageBox.Show("无效路径！", "提示");
            }
            else
            {
                #region 执行脚本原有方法
                /*
                int num = 0;
                List<string> lstDatabases = new List<string>();
                StringBuilder builder = new StringBuilder(string.Empty);
                if (this.rbtYes.Checked)
                {
                    for (int i = 0; i < this.chkListDataBases.Items.Count; i++)
                    {
                        if (this.chkListDataBases.GetItemChecked(i))
                        {
                            string itemText = this.chkListDataBases.GetItemText(this.chkListDataBases.Items[i]);
                            if (!lstDatabases.Contains(itemText))
                            {
                                lstDatabases.Add(itemText);
                                builder.Append(itemText);
                                builder.Append("                 \r\n");
                            }
                        }
                    }
                    if (0 == lstDatabases.Count)
                    {
                        MessageBox.Show("请选择要升级的数据库！", "提示");
                        return;
                    }
                }
                FileManagement management = new FileManagement();
                List<string> lstDirectories = new List<string>();
                List<string> lstFiles = new List<string>();
                management.GetFiles(this.txtScriptPath.Text, lstDirectories, lstFiles);
                if (lstFiles.Count == 0)
                {
                    MessageBox.Show("没有要执行的脚本文件！", "提示");
                }
                else
                {
                    SqlManagement management2 = new SqlManagement(this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim());
                    num = management2.BatchAnalysisScript(lstFiles, CheckDataBase, this.rbtYes.Checked);
                    if (0 != num)
                    {
                        MessageBox.Show(string.Format("脚本分析完毕,有{0}处错误，详情请查看日志！", num), "提示");
                    }
                    else
                    {
                        DialogResult result = MessageBox.Show(this.rbtYes.Checked ? string.Format("确认要在以下数据库执行吗？执行后将无法恢复！\r\n{0}", builder) : "确认要执行吗？执行后将无法恢复！", "？", MessageBoxButtons.OKCancel);
                        DateTime now = DateTime.Now;
                        if (result == DialogResult.OK)
                        {
                            string str3;
                            while (this.progressBar.Value < 50)
                            {
                                this.progressBar.Value += 5;
                            }
                            if (NeedBackupDatabase)
                            {
                                if (!this.rbtYes.Checked)
                                {
                                    lstDatabases = management2.GetDataBases(lstFiles);
                                }
                                List<string> serverDatabases = management2.GetServerDatabases();
                                foreach (string str2 in lstDatabases)
                                {
                                    if (serverDatabases.Contains(str2))
                                    {
                                        management2.BackupDatabase(string.Format(@"{0}\{1}_backup_{2}.bak", DataBaseBackupPath, str2, DateTime.Now.ToString("yyyyMMddHHmmss")), str2);
                                    }
                                }
                            }
                            num = this.rbtYes.Checked ? management2.BatchExecuteSql(lstDatabases, lstFiles) : management2.BatchExecuteSql(lstFiles);
                            DateTime time2 = DateTime.Now;
                            TimeSpan ts = new TimeSpan(now.Ticks);
                            TimeSpan span3 = new TimeSpan(time2.Ticks).Subtract(ts);
                            decimal num3 = Convert.ToDecimal((int)(((((span3.Hours * 0xe10) * 0x3e8) + ((span3.Minutes * 60) * 0x3e8)) + (span3.Seconds * 0x3e8)) + span3.Milliseconds)) / 1000M;
                            while (this.progressBar.Value < this.progressBar.Maximum)
                            {
                                this.progressBar.Value += 5;
                            }
                            if (num == -1)
                            {
                                str3 = string.Format("脚本执行出现{0}处异常,耗时{1}秒，详情请查看日志", num, num3);
                            }
                            else if (num == 0)
                            {
                                str3 = string.Format("脚本执行完毕，耗时{0}秒!", num3);
                            }
                            else
                            {
                                str3 = string.Format("脚本执行完毕！共有{0}处未正常执行，耗时{0}秒，详情请查看日志!", num, num3);
                            }
                            management2.RecordScriptUpdateLog(lstFiles.Count, lstDirectories[lstDirectories.Count - 1], lstFiles[lstFiles.Count - 1], num3, str3);
                            MessageBox.Show(str3, "提示");
                            this.progressBar.Value = 0;
                        }
                    }
                } 
                 * */
                #endregion
                //线程启动脚本升级
                this.btnExecuteSql.Enabled = false;
                Thread th = new Thread(new ThreadStart(UpdaterDataBase));
                th.Start();
                SetStatusDis("数据脚本准备升级……");
                
            }
        }
        /// <summary>
        /// 线程调用升级脚本
        /// </summary>
        private void UpdaterDataBase()
        {
            SetStatusDis("开始升级数据库……");
            Thread.Sleep(50);
            //实例委托
            ReStoreDataBase rdb = new ReStoreDataBase(ThreadUpdateDataBase);
            IAsyncResult result = rdb.BeginInvoke(null, null);//开始异步
            SetStatusDis("数据库升级进行中……");
            int i = 1;
            //轮询判断异步是否结束 如果没有结束就循环进度条
            while (!result.IsCompleted)
            {
                Thread.Sleep(50);
                if (this.progressBar.InvokeRequired)
                {
                    SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    this.progressBar.Invoke(sbr, this.progressBar, i);
                }
                else
                {
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    SetprogressBarMethod(this.progressBar, i);
                }
                i++;
            }
            i = 0;
            rdb.EndInvoke(result);
            //SetStatusDis("数据库还原完成！");
            if (this.progressBar.InvokeRequired)
            {
                SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                this.progressBar.Invoke(sbr, this.progressBar, 0);
            }
            else
            {
                SetprogressBarMethod(this.progressBar, 0);
            }

            this.btnExecuteSql.Invoke((MethodInvoker)delegate()
            {
                this.btnExecuteSql.Enabled = true;
            });     
        
        }
        /// <summary>
        /// 
        /// </summary>
        private void ThreadUpdateDataBase()
        {
            int num = 0;
            List<string> lstDatabases = new List<string>();
            StringBuilder builder = new StringBuilder(string.Empty);
            GetTextBoxValue gbv = new GetTextBoxValue(GetTextBoxValueStr);
            GetCheckValue gkv=new GetCheckValue(getRadioButtonValue);
            GetComboBoxValue cbv = new GetComboBoxValue(GetGetComboBoxValueValueStr);
            string rbtYesstr = (this.rbtYes.Invoke(gkv, this.rbtYes)) as string;
            if ("true" == rbtYesstr.ToLower())
            {
               
                if (this.chkListDataBases.InvokeRequired)
                {
                    this.chkListDataBases.Invoke((MethodInvoker)delegate(){
                        for (int i = 0; i < this.chkListDataBases.Items.Count; i++)
                        {
                            if (this.chkListDataBases.GetItemChecked(i))
                            {
                                string itemText = this.chkListDataBases.GetItemText(this.chkListDataBases.Items[i]);
                                if (!lstDatabases.Contains(itemText))
                                {
                                    lstDatabases.Add(itemText);
                                    builder.Append(itemText);
                                    builder.Append("                 \r\n");
                                }
                            }
                        } 
                    
                    });
               

                }
                if (0 == lstDatabases.Count)
                {
                    MessageBox.Show("请选择要升级的数据库！", "提示");
                    return;
                }
            }
            string cbxServers_Str = this.cbxServers.Invoke(cbv, this.cbxServers) as string;
            string txtLoginName_Str = this.txtLoginName.Invoke(gbv, this.txtLoginName) as string;
            string txtPassword_Str = this.txtPassword.Invoke(gbv, this.txtPassword) as string;
            string txtScriptPath_Str = this.txtScriptPath.Invoke(gbv, this.txtScriptPath) as string;
            FileManagement management = new FileManagement();
            List<string> lstDirectories = new List<string>();
            List<string> lstFiles = new List<string>();
            management.GetFiles(txtScriptPath_Str, lstDirectories, lstFiles);
            if (lstFiles.Count == 0)
            {
                SetStatusDis("没有要执行的脚本");
                MessageBox.Show("没有要执行的脚本文件！", "提示");
                
            }
            else
            {
                SqlManagement management2 = new SqlManagement(cbxServers_Str, txtLoginName_Str, txtPassword_Str);
                SetStatusDis("正在分析脚本，请等待……");
                num = management2.BatchAnalysisScript(lstFiles, CheckDataBase, this.rbtYes.Checked);
                if (0 != num)
                {
                    SetStatusDis("脚本分析有错误，请查看日志");
                    MessageBox.Show(string.Format("脚本分析完毕,有{0}处错误，详情请查看日志！", num), "提示");
                    
                    
                }
                else
                {
                    SetStatusDis("脚本分析完毕准备询问执行");
                    Thread.Sleep(100);
                    DialogResult result = MessageBox.Show(("true"==rbtYesstr.ToLower())? string.Format("确认要在以下数据库执行吗？执行后将无法恢复！\r\n{0}", builder) : "确认要执行吗？执行后将无法恢复！", "？", MessageBoxButtons.OKCancel);
                    DateTime now = DateTime.Now;
                    if (result == DialogResult.OK)
                    {
                        
                        string str3;
                        //if (NeedBackupDatabase)
                        if (this.ckblBakcDatabase.Checked)
                        {
                            SetStatusDis("准备进行数据库备份……");
                            if (!("true" == rbtYesstr.ToLower()))
                            {
                                lstDatabases = management2.GetDataBases(lstFiles);
                            }
                            List<string> serverDatabases = management2.GetServerDatabases();
                            SetStatusDis("开始数据库备份……");
                            #region foreach对数据库进行备份
                            foreach (string str2 in lstDatabases)
                            {
                                if (serverDatabases.Contains(str2))
                                {
                                    //string temp = "正在对数据库：" + str2 + " 进行备份……";
                                    //SetStatusDis(temp);
                                    //Thread.Sleep(200);
                                    management2.BackupDatabase(string.Format(@"{0}\{1}_backup_{2}.bak", DataBaseBackupPath, str2, DateTime.Now.ToString("yyyyMMddHHmmss")), str2);
                                }
                            }
                            #endregion
                                SetStatusDis("数据库备份完毕……");
                        }
                        SetStatusDis("正在升级脚本……");
                        Thread.Sleep(100);
                        num = ("true"==rbtYesstr.ToLower())? management2.BatchExecuteSql(lstDatabases, lstFiles) : management2.BatchExecuteSql(lstFiles);
                        DateTime time2 = DateTime.Now;
                        TimeSpan ts = new TimeSpan(now.Ticks);
                        TimeSpan span3 = new TimeSpan(time2.Ticks).Subtract(ts);
                        decimal num3 = Convert.ToDecimal((int)(((((span3.Hours * 0xe10) * 0x3e8) + ((span3.Minutes * 60) * 0x3e8)) + (span3.Seconds * 0x3e8)) + span3.Milliseconds)) / 1000M;
                        if (num == -1)
                        {
                            SetStatusDis("脚本执行有异常");
                            str3 = string.Format("脚本执行出现{0}处异常,耗时{1}秒，详情请查看日志", num, num3);
                            
                        }
                        else if (num == 0)
                        {
                            SetStatusDis("脚本执行完毕");
                            str3 = string.Format("脚本执行完毕，耗时{0}秒!", num3);
                            
                        }
                        else
                        {
                            SetStatusDis("脚本执行完毕");
                            str3 = string.Format("脚本执行完毕！共有{0}处未正常执行，耗时{0}秒，详情请查看日志!", num, num3);
                            
                        }
                        management2.RecordScriptUpdateLog(lstFiles.Count, lstDirectories[lstDirectories.Count - 1], lstFiles[lstFiles.Count - 1], num3, str3);
                        MessageBox.Show(str3, "提示");
                        this.progressBar.Invoke((MethodInvoker)delegate()
                        {
                            this.progressBar.Value = 0;
                        });
                    }
                }
            } 
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认要退出吗？", "提示", MessageBoxButtons.OKCancel,MessageBoxIcon.Question) == DialogResult.OK)
            {
                GC.Collect();
                Application.ExitThread();
            }
        }

        private void btnGetInstances_Click(object sender, EventArgs e)
        {
            this.cbxServers.Items.Clear();
            this.cbxServers.Items.Add("正在获取数据库实例...");
            this.cbxServers.BackColor = Color.LightGray;
            this.cbxServers.SelectedIndex = 0;
            this.bgwGetInstances.RunWorkerAsync();
            this.btnGetInstances.Enabled = false;
        }
        /// <summary>
        /// 打开日志文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadLog_Click(object sender, EventArgs e)
        {
            Log.OpenLog();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.cbxServers.Text.Trim()))
            {
                MessageBox.Show("请输入或获取数据库服务器名称！", "提示");
            }
            else if (string.IsNullOrEmpty(this.txtLoginName.Text.Trim()))
            {
                MessageBox.Show("请输入登录名！", "提示");
            }
            else
            {
                SqlManagement management = new SqlManagement(this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim());
                if (!management.Connect())
                {
                    MessageBox.Show("连接失败，详情请查看日志");
                }
                else
                {
                    List<string> serverDatabases = management.GetServerDatabases();
                    if (serverDatabases.Count > 0)
                    {
                        this.cbxDatabases.Items.Clear();
                        foreach (string str in serverDatabases)
                        {
                            this.cbxDatabases.Items.Add(str);
                        }
                        this.cbxDatabases.SelectedIndex = 0;
                        this.txtFileName.Text = string.Format("{0}{1}.bak", this.cbxDatabases.SelectedItem.ToString(), DateTime.Now.ToString("yyyyMMdd"));
                    }
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!(!string.IsNullOrEmpty(this.txtScriptPath1.Text.Trim()) && Directory.Exists(this.txtScriptPath1.Text.Trim())))
            {
                MessageBox.Show("无效路径！", "提示");
            }
            else
            {
                #region 单线程原有查询方法
                /*
                FileManagement management = new FileManagement();
                List<string> lstDirectories = new List<string>();
                List<string> lstFiles = new List<string>();
                management.GetFiles(this.txtScriptPath1.Text, lstDirectories, lstFiles);
                if (lstFiles.Count == 0)
                {
                    MessageBox.Show("没有要执行的脚本文件！", "提示");
                }
                else if (string.Empty == this.txtKeywords.Text.Trim())
                {
                    MessageBox.Show("请输入关键字！", "提示");
                }
                else
                {
                    this.progressBar.Maximum = 100;
                    this.progressBar.Style = ProgressBarStyle.Blocks;
                    this.progressBar.Value = 0;
                    this.progressBar.Step = 5;
                    while (this.progressBar.Value < 50)
                    {
                        this.progressBar.Value += 5;
                    }
                    DateTime now = DateTime.Now;
                    List<string> list3 = new SqlManagement().SearchScript(lstFiles, this.txtKeywords.Text);
                    DateTime time2 = DateTime.Now;
                    TimeSpan ts = new TimeSpan(now.Ticks);
                    TimeSpan span3 = new TimeSpan(time2.Ticks).Subtract(ts);
                    decimal num = Convert.ToDecimal((int)(((((span3.Hours * 0xe10) * 0x3e8) + ((span3.Minutes * 60) * 0x3e8)) + (span3.Seconds * 0x3e8)) + span3.Milliseconds)) / 1000M;
                    while (this.progressBar.Value < this.progressBar.Maximum)
                    {
                        this.progressBar.Value += 5;
                    }
                    if (0 == list3.Count)
                    {
                        MessageBox.Show(string.Format("共找到0个匹配项，耗时{0}秒！", num), "提示");
                    }
                    else
                    {
                        MessageBox.Show(string.Format("共找到{0}个匹配项，耗时{1}秒！", list3.Count, num), "提示");
                        string sFilePath = string.Format("{0}/Search.txt", Application.StartupPath);
                        FileManagement.DeleteFile(sFilePath);
                        StringBuilder builder = new StringBuilder();
                        builder.Append(string.Format("关键字为{0},匹配的脚本文件为\r\n", this.txtKeywords.Text));
                        for (int i = 0; i < list3.Count; i++)
                        {
                            builder.Append(string.Format("{0}、{1}\r\n", i + 1, list3[i]));
                        }
                        management.WriteFile(sFilePath, builder.ToString());
                        FileManagement.OpenFile(Application.StartupPath, "Search.txt");
                        this.progressBar.Value = 0;
                    }
                } 
                 */
                #endregion
                Thread th = new Thread(new ThreadStart(ThreadFinderKeysInFile));
                SetStatusDis("准备开始查找关键字…………");
                
                th.Start();
            }
        }
        private void ThreadFinderKeysInFile()
        {   
            Thread.Sleep(300);
            SetStatusDis("开始关键字查找……");
            Thread.Sleep(100);
            //实例委托
            ReStoreDataBase rdb = new ReStoreDataBase(FinderKeysInFile);
            IAsyncResult result = rdb.BeginInvoke(null, null);//开始异步
            SetStatusDis("正在进行关键字查找……");
            int i = 1;
            //轮询判断异步是否结束 如果没有结束就循环进度条
            while (!result.IsCompleted)
            {
                Thread.Sleep(50);
                if (this.progressBar.InvokeRequired)
                {
                    SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    this.progressBar.Invoke(sbr, this.progressBar, i);
                }
                else
                {
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    SetprogressBarMethod(this.progressBar, i);
                }
                i++;
            }
            i = 0;
            rdb.EndInvoke(result);
            //SetStatusDis("数据库还原完成！");
            if (this.progressBar.InvokeRequired)
            {
                SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                this.progressBar.Invoke(sbr, this.progressBar, 0);
            }
            else
            {
                SetprogressBarMethod(this.progressBar, 0);
            }

            this.btnSearch.Invoke((MethodInvoker)delegate()
            {
                this.btnSearch.Enabled = true;
            });    
        }
        private void FinderKeysInFile()
        {   
            GetTextBoxValue gbv = new GetTextBoxValue(GetTextBoxValueStr);
            GetCheckValues ckv = new GetCheckValues(GetCheckValueBool);
            FileManagement management = new FileManagement();
            List<string> lstDirectories = new List<string>();
            List<string> lstFiles = new List<string>();
            string txtScriptPath1_Str = this.txtScriptPath1.Invoke(gbv,this.txtScriptPath1) as string;
            bool Regexbl = (bool)this.chkRegex.Invoke(ckv,this.chkRegex);
            management.GetFiles(txtScriptPath1_Str, lstDirectories, lstFiles);
            if (lstFiles.Count == 0)
            {
                SetStatusDis("没有要执行的脚本文件");
                MessageBox.Show("没有要执行的脚本文件！", "提示");

            }
            else if (string.Empty == this.txtKeywords.Text.Trim())
            {
                SetStatusDis("请输入关键字");
                MessageBox.Show("请输入关键字！", "提示");
            }
            else
            {
                string txtKeyword_Str = this.txtKeywords.Invoke(gbv, this.txtKeywords) as string;
                SetStatusDis("正在进行关键字查找……");
                DateTime now = DateTime.Now;
                List<string> list3 = new SqlManagement().SearchScript(lstFiles, txtKeyword_Str,Regexbl);
                DateTime time2 = DateTime.Now;
                TimeSpan ts = new TimeSpan(now.Ticks);
                TimeSpan span3 = new TimeSpan(time2.Ticks).Subtract(ts);
                decimal num = Convert.ToDecimal((int)(((((span3.Hours * 0xe10) * 0x3e8) + ((span3.Minutes * 60) * 0x3e8)) + (span3.Seconds * 0x3e8)) + span3.Milliseconds)) / 1000M;
                if (0 == list3.Count)
                {
                    SetStatusDis("未找到任何关键字相符文件");
                    MessageBox.Show(string.Format("共找到0个匹配项，耗时{0}秒！", num), "提示");

                }
                else
                {
                    SetStatusDis("找到符合相应关键字的文件");
                    MessageBox.Show(string.Format("共找到{0}个匹配项，耗时{1}秒！", list3.Count, num), "提示");
                    string sFilePath = string.Format("{0}/Search.txt", Application.StartupPath);
                    FileManagement.DeleteFile(sFilePath);
                    StringBuilder builder = new StringBuilder();
                    builder.Append(string.Format("关键字为{0},匹配的脚本文件为\r\n", txtKeyword_Str));
                    for (int i = 0; i < list3.Count; i++)
                    {
                        builder.Append(string.Format("{0}、{1}\r\n", i + 1, list3[i]));
                    }
                    management.WriteFile(sFilePath, builder.ToString());
                    FileManagement.OpenFile(Application.StartupPath, "Search.txt");

                }
            }
        
        }
        private void btnStore_Click(object sender, EventArgs e)
        {
            if (!SqlManagement.Exists(this.cbxServers.Text.Trim()))
            {
                MessageBox.Show("数据库服务器不存在！", "提示");
            }
            else if (!(!string.IsNullOrEmpty(this.txtStoreFile.Text.Trim()) && File.Exists(this.txtStoreFile.Text.Trim())))
            {
                MessageBox.Show("无效的备份文件！", "提示");
            }
            else if (!(!string.IsNullOrEmpty(this.txtStorePath.Text.Trim()) && Directory.Exists(this.txtStorePath.Text.Trim())))
            {
                MessageBox.Show("无效的目标路径！", "提示");
            }
            else if (string.IsNullOrEmpty(this.txtDBName.Text.Trim()))
            {
                MessageBox.Show("请输入目标数据库！", "提示");
            }
            else if (SqlManagement.DBExists(this.txtDBName.Text, this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim()))
            {
                MessageBox.Show("目标数据库已经存在，无法替换！", "提示");
            }
            else
            {
                #region 原有还原的方法
                /*
                this.progressBar.Maximum = 100;
                this.progressBar.Style = ProgressBarStyle.Blocks;
                this.progressBar.Value = 0;
                this.progressBar.Step = 5;
                while (this.progressBar.Value < 50)
                {
                    this.progressBar.Value += 5;
                }
                SqlManagement management = new SqlManagement(this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim());
                if (!management.StoreDatabase(this.txtStoreFile.Text.Trim(), this.txtStorePath.Text.Trim(), this.txtDBName.Text.Trim()))
                {
                    MessageBox.Show("还原失败，请查看日志文件！", "提示");
                    this.progressBar.Value = 0;
                }
                else
                {
                    while (this.progressBar.Value < this.progressBar.Maximum)
                    {
                        this.progressBar.Value += 5;
                    }
                    MessageBox.Show("还原成功！", "提示");
                    this.progressBar.Value = 0;
                    FileManagement.OpenFolder(this.txtStorePath.Text.Trim());
                } 
                 */
                #endregion
                //线程处理'
                Thread td = new Thread(delegate()
                {
                    ThreadStoreDataBase();
                });
                this.btnStore.Enabled = false;
                SetStatusDis("已经准备开始还原数据库……");
                Thread.Sleep(50);
                td.Start();
            }
        }
#region  还原数据库
        private void ThreadStoreDataBase() 
        {
            SetStatusDis("开始还原数据库……");
            Thread.Sleep(50);
            //实例委托
            ReStoreDataBase rdb = new ReStoreDataBase(StoreUpDataBase);
            IAsyncResult result = rdb.BeginInvoke(null, null);//开始异步
            SetStatusDis("正在还原数据库，请等待……");
            int i = 1;
            //轮询判断异步是否结束 如果没有结束就循环进度条
            while (!result.IsCompleted)
            {
                Thread.Sleep(50);
                if (this.progressBar.InvokeRequired)
                {
                    SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    this.progressBar.Invoke(sbr, this.progressBar, i);
                }
                else
                {
                    if (!(i <= this.progressBar.Maximum))
                    {
                        i = 1;
                    }
                    SetprogressBarMethod(this.progressBar, i);
                }
                i++;
            }
            i = 0;
            rdb.EndInvoke(result);
            if (this.progressBar.InvokeRequired)
            {
                SetprogressBar sbr = new SetprogressBar(SetprogressBarMethod);
                this.progressBar.Invoke(sbr, this.progressBar, 0);
            }
            else
            {
                SetprogressBarMethod(this.progressBar, 0);
            }

            this.btnStore.Invoke((MethodInvoker)delegate()
            {
                this.btnStore.Enabled = true;
            });
        }
        private void StoreUpDataBase()
        {
            GetTextBoxValue gbv = new GetTextBoxValue(GetTextBoxValueStr);
            GetComboBoxValue cbv = new GetComboBoxValue(GetGetComboBoxValueValueStr);
            string cbxServers_Str =(this.cbxServers.Invoke(cbv, this.cbxServers) as string).Trim();
            string txtLoginName_Str = (this.txtLoginName.Invoke(gbv, this.txtLoginName) as string).Trim();
            string txtPassword_Str = (this.txtPassword.Invoke(gbv, this.txtPassword) as string).Trim();
            string txtStoreFile_Str = (this.txtStoreFile.Invoke(gbv, this.txtStoreFile) as string).Trim();
            string txtStorePath_Str = (this.txtStorePath.Invoke(gbv, this.txtStorePath) as string).Trim();
            string txtDBNameStr = (this.txtDBName.Invoke(gbv, this.txtDBName) as string).Trim();

            SqlManagement management = new SqlManagement(cbxServers_Str, txtLoginName_Str, txtPassword_Str);
            if (!management.StoreDatabase(txtStoreFile_Str, txtStorePath_Str, txtDBNameStr))
               {
                   this.progressBar.Invoke((MethodInvoker)delegate()
                   {
                       this.progressBar.Value = 0;
                   });
                   SetStatusDis("数据库还原失败！");
                   MessageBox.Show("还原失败，请查看日志文件！", "提示");
               }
               else
               {
                   this.progressBar.Invoke((MethodInvoker)delegate()
                   {
                       this.progressBar.Value = 0;
                   });
                   SetStatusDis("数据库还原完成！");
                   MessageBox.Show("还原成功！", "提示");
                   FileManagement.OpenFolder(txtStorePath_Str);
               }
        }
#endregion

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTestConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.cbxServers.Text.Trim()))
            {
                MessageBox.Show("请输入或获取数据库服务器名称！", "提示");
            }
            else if (string.IsNullOrEmpty(this.txtLoginName.Text.Trim()))
            {
                MessageBox.Show("请输入登录名！", "提示");
            }
            else
            {
                SqlManagement management = new SqlManagement(this.cbxServers.Text.Trim(), this.txtLoginName.Text.Trim(), this.txtPassword.Text.Trim());
                if (!management.Connect())
                {
                    MessageBox.Show("连接失败，详情请查看日志");
                }
                else
                {
                    MessageBox.Show("连接成功！", "提示");
                    List<string> serverDatabases = management.GetServerDatabases();
                    if (serverDatabases.Count > 0)
                    {
                        this.cbxDatabases.Items.Clear();
                        this.chkListDataBases.Items.Clear();
                        foreach (string str in serverDatabases)
                        {
                            this.cbxDatabases.Items.Add(str);
                            this.chkListDataBases.Items.Add(str);
                        }
                        this.cbxDatabases.SelectedIndex = 0;
                        this.txtFileName.Text = string.Format("{0}{1}.bak", this.cbxDatabases.SelectedItem.ToString(), DateTime.Now.ToString("yyyyMMdd"));
                    }
                }
            }
        }

        private void cbxDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.txtFileName.Text = string.Format("{0}{1}.bak", this.cbxDatabases.SelectedItem.ToString(), DateTime.Now.ToString("yyyyMMdd"));
        }

        private void cbxServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.txtLoginName.Text = string.Empty;
            this.txtPassword.Text = string.Empty;
            this.cbxDatabases.Items.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeBackGroundWorker()
        {
            this.bgwGetInstances.WorkerReportsProgress = true;
            this.bgwGetInstances.WorkerSupportsCancellation = true;
            this.bgwGetInstances.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                e.Result = SqlManagement.Servers;
            };
            this.bgwGetInstances.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
            {
                this.progressBar.Value = e.ProgressPercentage;
            };
            this.bgwGetInstances.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
                this.cbxServers.Items.Clear();
                DataTable result = e.Result as DataTable;
                if (result.Rows.Count == 0)
                {
                    MessageBox.Show("获取失败，请手动输入！", "提示");
                    this.cbxServers.SelectAll();
                    this.cbxServers.SelectedText = "";
                    this.cbxServers.Focus();
                }
                else
                {
                   
                    foreach (DataRow row in result.Rows)
                    {
                        this.cbxServers.Items.Add(row[0]);
                    }
                    this.cbxServers.SelectedIndex = 0;
                }
                this.btnGetInstances.Enabled = true;
                this.cbxServers.BackColor = Color.White;
            };
        }
        #region 界面设计
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlTool));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.btnBrowseScript = new System.Windows.Forms.Button();
            this.btnExecuteSql = new System.Windows.Forms.Button();
            this.txtScriptPath = new System.Windows.Forms.TextBox();
            this.btnBackup = new System.Windows.Forms.Button();
            this.txtBackupPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btnBrowseBackup = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cbxDatabases = new System.Windows.Forms.ComboBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.btnReadLog = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtLoginName = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnGetInstances = new System.Windows.Forms.Button();
            this.cbxServers = new System.Windows.Forms.ComboBox();
            this.btnTestConnect = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnExit = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.label8 = new System.Windows.Forms.Label();
            this.tabBackup = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnStore = new System.Windows.Forms.Button();
            this.btnBrowserPath = new System.Windows.Forms.Button();
            this.btnBrowserFile = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.txtDBName = new System.Windows.Forms.TextBox();
            this.txtStoreFile = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtStorePath = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.ckblBakcDatabase = new System.Windows.Forms.CheckBox();
            this.chkListDataBases = new System.Windows.Forms.CheckedListBox();
            this.btnSelectDB = new System.Windows.Forms.Button();
            this.rbtYes = new System.Windows.Forms.RadioButton();
            this.rbtNo = new System.Windows.Forms.RadioButton();
            this.label13 = new System.Windows.Forms.Label();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.chkRegex = new System.Windows.Forms.CheckBox();
            this.txtKeywords = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.btnSearch = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.btnBrowseScript1 = new System.Windows.Forms.Button();
            this.txtScriptPath1 = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.bgwGetInstances = new System.ComponentModel.BackgroundWorker();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.StatusBar = new System.Windows.Forms.StatusStrip();
            this.tlstatusTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.tltattusDisProcess = new System.Windows.Forms.ToolStripStatusLabel();
            this.TimerNowDate = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tabBackup.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.panel2.SuspendLayout();
            this.StatusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnBrowseScript
            // 
            this.btnBrowseScript.Location = new System.Drawing.Point(256, 7);
            this.btnBrowseScript.Name = "btnBrowseScript";
            this.btnBrowseScript.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseScript.TabIndex = 0;
            this.btnBrowseScript.Text = "浏  览";
            this.btnBrowseScript.UseVisualStyleBackColor = true;
            this.btnBrowseScript.Click += new System.EventHandler(this.btnBrowseScript_Click);
            // 
            // btnExecuteSql
            // 
            this.btnExecuteSql.Location = new System.Drawing.Point(121, 64);
            this.btnExecuteSql.Name = "btnExecuteSql";
            this.btnExecuteSql.Size = new System.Drawing.Size(75, 23);
            this.btnExecuteSql.TabIndex = 2;
            this.btnExecuteSql.Text = "执  行";
            this.btnExecuteSql.UseVisualStyleBackColor = true;
            this.btnExecuteSql.Click += new System.EventHandler(this.btnExecuteSql_Click);
            // 
            // txtScriptPath
            // 
            this.txtScriptPath.AllowDrop = true;
            this.txtScriptPath.Location = new System.Drawing.Point(88, 8);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(160, 21);
            this.txtScriptPath.TabIndex = 6;
            this.toolTip.SetToolTip(this.txtScriptPath, "可以拖动目标文件夹获得路径");
            this.txtScriptPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtScriptPath_DragDrop);
            this.txtScriptPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtScriptPath_DragEnter);
            // 
            // btnBackup
            // 
            this.btnBackup.Location = new System.Drawing.Point(259, 66);
            this.btnBackup.Name = "btnBackup";
            this.btnBackup.Size = new System.Drawing.Size(75, 23);
            this.btnBackup.TabIndex = 8;
            this.btnBackup.Text = "备  份";
            this.btnBackup.UseVisualStyleBackColor = true;
            this.btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
            // 
            // txtBackupPath
            // 
            this.txtBackupPath.AllowDrop = true;
            this.txtBackupPath.Location = new System.Drawing.Point(88, 37);
            this.txtBackupPath.Name = "txtBackupPath";
            this.txtBackupPath.Size = new System.Drawing.Size(160, 21);
            this.txtBackupPath.TabIndex = 10;
            this.txtBackupPath.Text = "D:\\";
            this.toolTip.SetToolTip(this.txtBackupPath, "可以拖动目标文件夹获得路径");
            this.txtBackupPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtBackupPath_DragDrop);
            this.txtBackupPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtBackupPath_DragEnter);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 11;
            this.label2.Text = "备份路径：";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 14);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 12;
            this.label4.Text = "脚本路径：";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(5, 131);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(356, 15);
            this.progressBar.TabIndex = 19;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 26;
            this.label3.Text = "文件名：";
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(88, 67);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(160, 21);
            this.txtFileName.TabIndex = 25;
            // 
            // btnBrowseBackup
            // 
            this.btnBrowseBackup.Location = new System.Drawing.Point(259, 36);
            this.btnBrowseBackup.Name = "btnBrowseBackup";
            this.btnBrowseBackup.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseBackup.TabIndex = 24;
            this.btnBrowseBackup.Text = "浏  览";
            this.btnBrowseBackup.UseVisualStyleBackColor = true;
            this.btnBrowseBackup.Click += new System.EventHandler(this.btnBrowseBackup_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 23;
            this.label1.Text = "数据库：";
            // 
            // cbxDatabases
            // 
            this.cbxDatabases.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxDatabases.FormattingEnabled = true;
            this.cbxDatabases.Location = new System.Drawing.Point(88, 9);
            this.cbxDatabases.Name = "cbxDatabases";
            this.cbxDatabases.Size = new System.Drawing.Size(160, 20);
            this.cbxDatabases.TabIndex = 22;
            this.cbxDatabases.SelectedIndexChanged += new System.EventHandler(this.cbxDatabases_SelectedIndexChanged);
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(143, 7);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 14;
            this.btnClearLog.Text = "清空日志";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // btnReadLog
            // 
            this.btnReadLog.Location = new System.Drawing.Point(22, 7);
            this.btnReadLog.Name = "btnReadLog";
            this.btnReadLog.Size = new System.Drawing.Size(75, 23);
            this.btnReadLog.TabIndex = 13;
            this.btnReadLog.Text = "查看日志";
            this.btnReadLog.UseVisualStyleBackColor = true;
            this.btnReadLog.Click += new System.EventHandler(this.btnReadLog_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(24, 8);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 12);
            this.label5.TabIndex = 15;
            this.label5.Text = "服务器名称：";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(24, 41);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 12);
            this.label6.TabIndex = 16;
            this.label6.Text = "登录名：";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(24, 69);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 12);
            this.label7.TabIndex = 17;
            this.label7.Text = "密码：";
            // 
            // txtLoginName
            // 
            this.txtLoginName.Location = new System.Drawing.Point(97, 37);
            this.txtLoginName.Name = "txtLoginName";
            this.txtLoginName.Size = new System.Drawing.Size(160, 21);
            this.txtLoginName.TabIndex = 19;
            this.txtLoginName.Text = "sa";
            this.txtLoginName.MouseClick += new System.Windows.Forms.MouseEventHandler(this.txtLoginName_MouseClick);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(97, 66);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(160, 21);
            this.txtPassword.TabIndex = 20;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnTest);
            this.panel1.Controls.Add(this.btnGetInstances);
            this.panel1.Controls.Add(this.cbxServers);
            this.panel1.Controls.Add(this.btnTestConnect);
            this.panel1.Controls.Add(this.txtPassword);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.txtLoginName);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Location = new System.Drawing.Point(15, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(367, 98);
            this.panel1.TabIndex = 16;
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(265, 36);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 27;
            this.btnTest.Text = "测  试";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Visible = false;
            // 
            // btnGetInstances
            // 
            this.btnGetInstances.Location = new System.Drawing.Point(264, 7);
            this.btnGetInstances.Name = "btnGetInstances";
            this.btnGetInstances.Size = new System.Drawing.Size(75, 23);
            this.btnGetInstances.TabIndex = 26;
            this.btnGetInstances.Text = "获取实例";
            this.btnGetInstances.UseVisualStyleBackColor = true;
            this.btnGetInstances.Click += new System.EventHandler(this.btnGetInstances_Click);
            // 
            // cbxServers
            // 
            this.cbxServers.FormattingEnabled = true;
            this.cbxServers.Location = new System.Drawing.Point(97, 8);
            this.cbxServers.Name = "cbxServers";
            this.cbxServers.Size = new System.Drawing.Size(160, 20);
            this.cbxServers.TabIndex = 25;
            this.cbxServers.Text = ".";
            this.cbxServers.SelectedIndexChanged += new System.EventHandler(this.cbxServers_SelectedIndexChanged);
            // 
            // btnTestConnect
            // 
            this.btnTestConnect.Location = new System.Drawing.Point(264, 65);
            this.btnTestConnect.Name = "btnTestConnect";
            this.btnTestConnect.Size = new System.Drawing.Size(75, 23);
            this.btnTestConnect.TabIndex = 21;
            this.btnTestConnect.Text = "测试连接";
            this.btnTestConnect.UseVisualStyleBackColor = true;
            this.btnTestConnect.Click += new System.EventHandler(this.btnTestConnect_Click);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.btnReadLog);
            this.panel3.Controls.Add(this.btnExit);
            this.panel3.Controls.Add(this.btnClearLog);
            this.panel3.Location = new System.Drawing.Point(15, 277);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(367, 39);
            this.panel3.TabIndex = 17;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(264, 7);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 18;
            this.btnExit.Text = "退 出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(23, 135);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(0, 12);
            this.label8.TabIndex = 27;
            // 
            // tabBackup
            // 
            this.tabBackup.Controls.Add(this.tabPage1);
            this.tabBackup.Controls.Add(this.tabPage2);
            this.tabBackup.Controls.Add(this.tabPage3);
            this.tabBackup.Controls.Add(this.tabPage4);
            this.tabBackup.Location = new System.Drawing.Point(5, 5);
            this.tabBackup.Name = "tabBackup";
            this.tabBackup.SelectedIndex = 0;
            this.tabBackup.Size = new System.Drawing.Size(357, 121);
            this.tabBackup.TabIndex = 19;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnRefresh);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.cbxDatabases);
            this.tabPage1.Controls.Add(this.txtFileName);
            this.tabPage1.Controls.Add(this.txtBackupPath);
            this.tabPage1.Controls.Add(this.btnBrowseBackup);
            this.tabPage1.Controls.Add(this.btnBackup);
            this.tabPage1.Location = new System.Drawing.Point(4, 21);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(349, 96);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "数据库备份";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(259, 8);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 27;
            this.btnRefresh.Text = "刷  新";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnStore);
            this.tabPage2.Controls.Add(this.btnBrowserPath);
            this.tabPage2.Controls.Add(this.btnBrowserFile);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.txtDBName);
            this.tabPage2.Controls.Add(this.txtStoreFile);
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Controls.Add(this.txtStorePath);
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Location = new System.Drawing.Point(4, 21);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(349, 96);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "数据库还原";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnStore
            // 
            this.btnStore.Location = new System.Drawing.Point(258, 65);
            this.btnStore.Name = "btnStore";
            this.btnStore.Size = new System.Drawing.Size(75, 23);
            this.btnStore.TabIndex = 34;
            this.btnStore.Text = "还  原";
            this.btnStore.UseVisualStyleBackColor = true;
            this.btnStore.Click += new System.EventHandler(this.btnStore_Click);
            // 
            // btnBrowserPath
            // 
            this.btnBrowserPath.Location = new System.Drawing.Point(258, 37);
            this.btnBrowserPath.Name = "btnBrowserPath";
            this.btnBrowserPath.Size = new System.Drawing.Size(75, 23);
            this.btnBrowserPath.TabIndex = 33;
            this.btnBrowserPath.Text = "浏  览";
            this.btnBrowserPath.UseVisualStyleBackColor = true;
            this.btnBrowserPath.Click += new System.EventHandler(this.btnBrowserPath_Click);
            // 
            // btnBrowserFile
            // 
            this.btnBrowserFile.Location = new System.Drawing.Point(258, 9);
            this.btnBrowserFile.Name = "btnBrowserFile";
            this.btnBrowserFile.Size = new System.Drawing.Size(75, 23);
            this.btnBrowserFile.TabIndex = 32;
            this.btnBrowserFile.Text = "浏  览";
            this.btnBrowserFile.UseVisualStyleBackColor = true;
            this.btnBrowserFile.Click += new System.EventHandler(this.btnBrowserFile_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(2, 69);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(77, 12);
            this.label9.TabIndex = 24;
            this.label9.Text = "目标数据库：";
            // 
            // txtDBName
            // 
            this.txtDBName.Location = new System.Drawing.Point(88, 66);
            this.txtDBName.Name = "txtDBName";
            this.txtDBName.Size = new System.Drawing.Size(160, 21);
            this.txtDBName.TabIndex = 25;
            // 
            // txtStoreFile
            // 
            this.txtStoreFile.AllowDrop = true;
            this.txtStoreFile.Location = new System.Drawing.Point(88, 10);
            this.txtStoreFile.Name = "txtStoreFile";
            this.txtStoreFile.Size = new System.Drawing.Size(160, 21);
            this.txtStoreFile.TabIndex = 29;
            this.toolTip.SetToolTip(this.txtStoreFile, "可以把目标文件直接拖放到这里");
            this.txtStoreFile.TextChanged += new System.EventHandler(this.txtStoreFile_TextChanged);
            this.txtStoreFile.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtStoreFile_DragDrop);
            this.txtStoreFile.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtStoreFile_DragEnter);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(15, 14);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(65, 12);
            this.label11.TabIndex = 28;
            this.label11.Text = "备份文件：";
            // 
            // txtStorePath
            // 
            this.txtStorePath.AllowDrop = true;
            this.txtStorePath.Location = new System.Drawing.Point(88, 38);
            this.txtStorePath.Name = "txtStorePath";
            this.txtStorePath.Size = new System.Drawing.Size(160, 21);
            this.txtStorePath.TabIndex = 27;
            this.toolTip.SetToolTip(this.txtStorePath, "可以拖动目标文件夹获得路径");
            this.txtStorePath.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtStorePath_DragDrop);
            this.txtStorePath.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtStorePath_DragEnter);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(14, 43);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 12);
            this.label10.TabIndex = 26;
            this.label10.Text = "目标路径：";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.ckblBakcDatabase);
            this.tabPage3.Controls.Add(this.chkListDataBases);
            this.tabPage3.Controls.Add(this.btnExecuteSql);
            this.tabPage3.Controls.Add(this.btnSelectDB);
            this.tabPage3.Controls.Add(this.rbtYes);
            this.tabPage3.Controls.Add(this.rbtNo);
            this.tabPage3.Controls.Add(this.label13);
            this.tabPage3.Controls.Add(this.label4);
            this.tabPage3.Controls.Add(this.btnBrowseScript);
            this.tabPage3.Controls.Add(this.txtScriptPath);
            this.tabPage3.Location = new System.Drawing.Point(4, 21);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(349, 96);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "数据库升级";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // ckblBakcDatabase
            // 
            this.ckblBakcDatabase.AutoSize = true;
            this.ckblBakcDatabase.Location = new System.Drawing.Point(202, 71);
            this.ckblBakcDatabase.Name = "ckblBakcDatabase";
            this.ckblBakcDatabase.Size = new System.Drawing.Size(84, 16);
            this.ckblBakcDatabase.TabIndex = 21;
            this.ckblBakcDatabase.Text = "备份数据库";
            this.ckblBakcDatabase.UseVisualStyleBackColor = true;
            // 
            // chkListDataBases
            // 
            this.chkListDataBases.CheckOnClick = true;
            this.chkListDataBases.Enabled = false;
            this.chkListDataBases.FormattingEnabled = true;
            this.chkListDataBases.Location = new System.Drawing.Point(88, 6);
            this.chkListDataBases.Name = "chkListDataBases";
            this.chkListDataBases.Size = new System.Drawing.Size(160, 84);
            this.chkListDataBases.TabIndex = 16;
            this.chkListDataBases.Visible = false;
            // 
            // btnSelectDB
            // 
            this.btnSelectDB.Enabled = false;
            this.btnSelectDB.Location = new System.Drawing.Point(256, 37);
            this.btnSelectDB.Name = "btnSelectDB";
            this.btnSelectDB.Size = new System.Drawing.Size(75, 23);
            this.btnSelectDB.TabIndex = 20;
            this.btnSelectDB.Text = "选  择";
            this.btnSelectDB.UseVisualStyleBackColor = true;
            // 
            // rbtYes
            // 
            this.rbtYes.AutoSize = true;
            this.rbtYes.Location = new System.Drawing.Point(88, 42);
            this.rbtYes.Name = "rbtYes";
            this.rbtYes.Size = new System.Drawing.Size(47, 16);
            this.rbtYes.TabIndex = 18;
            this.rbtYes.Text = "指定";
            this.rbtYes.UseVisualStyleBackColor = true;
            // 
            // rbtNo
            // 
            this.rbtNo.AutoSize = true;
            this.rbtNo.Checked = true;
            this.rbtNo.Location = new System.Drawing.Point(191, 40);
            this.rbtNo.Name = "rbtNo";
            this.rbtNo.Size = new System.Drawing.Size(59, 16);
            this.rbtNo.TabIndex = 17;
            this.rbtNo.TabStop = true;
            this.rbtNo.Text = "不指定";
            this.rbtNo.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(24, 42);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(53, 12);
            this.label13.TabIndex = 15;
            this.label13.Text = "数据库：";
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.chkRegex);
            this.tabPage4.Controls.Add(this.txtKeywords);
            this.tabPage4.Controls.Add(this.label14);
            this.tabPage4.Controls.Add(this.btnSearch);
            this.tabPage4.Controls.Add(this.label12);
            this.tabPage4.Controls.Add(this.btnBrowseScript1);
            this.tabPage4.Controls.Add(this.txtScriptPath1);
            this.tabPage4.Location = new System.Drawing.Point(4, 21);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(349, 96);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "文件查找";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // chkRegex
            // 
            this.chkRegex.AutoSize = true;
            this.chkRegex.Location = new System.Drawing.Point(256, 66);
            this.chkRegex.Name = "chkRegex";
            this.chkRegex.Size = new System.Drawing.Size(72, 16);
            this.chkRegex.TabIndex = 19;
            this.chkRegex.Text = "正则搜索";
            this.chkRegex.UseVisualStyleBackColor = true;
            // 
            // txtKeywords
            // 
            this.txtKeywords.Location = new System.Drawing.Point(88, 37);
            this.txtKeywords.Multiline = true;
            this.txtKeywords.Name = "txtKeywords";
            this.txtKeywords.Size = new System.Drawing.Size(160, 45);
            this.txtKeywords.TabIndex = 18;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(24, 36);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(53, 12);
            this.label14.TabIndex = 17;
            this.label14.Text = "关键字：";
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(256, 36);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(75, 23);
            this.btnSearch.TabIndex = 16;
            this.btnSearch.Text = "查  找";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 12);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 12);
            this.label12.TabIndex = 15;
            this.label12.Text = "脚本路径：";
            // 
            // btnBrowseScript1
            // 
            this.btnBrowseScript1.Location = new System.Drawing.Point(256, 7);
            this.btnBrowseScript1.Name = "btnBrowseScript1";
            this.btnBrowseScript1.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseScript1.TabIndex = 13;
            this.btnBrowseScript1.Text = "浏  览";
            this.btnBrowseScript1.UseVisualStyleBackColor = true;
            this.btnBrowseScript1.Click += new System.EventHandler(this.btnBrowseScript1_Click);
            // 
            // txtScriptPath1
            // 
            this.txtScriptPath1.AllowDrop = true;
            this.txtScriptPath1.Location = new System.Drawing.Point(88, 8);
            this.txtScriptPath1.Name = "txtScriptPath1";
            this.txtScriptPath1.Size = new System.Drawing.Size(160, 21);
            this.txtScriptPath1.TabIndex = 14;
            this.toolTip.SetToolTip(this.txtScriptPath1, "可以拖动目标文件夹获得路径");
            this.txtScriptPath1.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtScriptPath1_DragDrop);
            this.txtScriptPath1.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtScriptPath1_DragEnter);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.tabBackup);
            this.panel2.Controls.Add(this.progressBar);
            this.panel2.Location = new System.Drawing.Point(15, 116);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(367, 153);
            this.panel2.TabIndex = 20;
            // 
            // StatusBar
            // 
            this.StatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tlstatusTime,
            this.tltattusDisProcess});
            this.StatusBar.Location = new System.Drawing.Point(0, 319);
            this.StatusBar.Name = "StatusBar";
            this.StatusBar.Size = new System.Drawing.Size(396, 22);
            this.StatusBar.TabIndex = 21;
            this.StatusBar.Text = "statusStrip1";
            // 
            // tlstatusTime
            // 
            this.tlstatusTime.Name = "tlstatusTime";
            this.tlstatusTime.Size = new System.Drawing.Size(0, 17);
            // 
            // tltattusDisProcess
            // 
            this.tltattusDisProcess.Name = "tltattusDisProcess";
            this.tltattusDisProcess.Size = new System.Drawing.Size(83, 17);
            this.tltattusDisProcess.Text = "状态:就绪……";
            // 
            // TimerNowDate
            // 
            this.TimerNowDate.Enabled = true;
            this.TimerNowDate.Interval = 200;
            this.TimerNowDate.Tick += new System.EventHandler(this.TimerNowDate_Tick);
            // 
            // SqlTool
            // 
            this.ClientSize = new System.Drawing.Size(396, 341);
            this.Controls.Add(this.StatusBar);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SqlTool";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SqlTool  ";
            this.Load += new System.EventHandler(this.SqlTool_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SqlTool_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.tabBackup.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.StatusBar.ResumeLayout(false);
            this.StatusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private void InitializeOtherControls()
        {
            this.rbtNo.MouseHover += (sender, e) => this.toolTip.SetToolTip(this.rbtNo, "已经在脚本中通过USE [数据库名]显示指定数据库，不需要手动指定数据库");
            this.rbtNo.CheckedChanged += delegate (object sender, EventArgs e) {
                RadioButton button = sender as RadioButton;
                if (button.Checked)
                {
                    this.chkListDataBases.Enabled = false;
                    this.btnSelectDB.Enabled = false;
                }
                else
                {
                    this.chkListDataBases.Enabled = true;
                    this.btnSelectDB.Enabled = true;
                }
            };
            this.rbtYes.MouseHover += (sender, e) => this.toolTip.SetToolTip(this.rbtYes, "没有在脚本中通过USE [数据库名]显示指定数据库，需要手动指定数据库");
            this.btnSelectDB.Click += delegate (object sender, EventArgs e) {
                this.chkListDataBases.Visible = !this.chkListDataBases.Visible;
                if (!this.chkListDataBases.Visible)
                {
                    this.btnSelectDB.Text = "选  择";
                }
                else
                {
                    this.btnSelectDB.Text = "确  定";
                }
            };
            this.toolTip.SetToolTip(this.txtKeywords, "多个关键字之间用英文逗号隔开");
        }

        private void SqlTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确认要退出吗？", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                GC.Collect();
                Application.ExitThread();
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region FormLoad
        private void SqlTool_Load(object sender, EventArgs e)
        {
            this.Text += string.Format("  版本:Beta {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            if (!WindowsService.Exists(SqlServerServiceName))
            {
                SqlServerServiceName = string.Empty;
                foreach (string str in WindowsService.WindowsServices)
                {
                    if (str.Contains("MSSQL$"))
                    {
                        SqlServerServiceName = str;
                        break;
                    }
                }
                if (SqlServerServiceName == string.Empty)
                {
                    MessageBox.Show("SQL Server不存在或访问被拒绝");
                    base.Enabled = false;
                    Application.ExitThread();
                }
            }
            if (!WindowsService.Running(SqlServerServiceName))
            {
                MessageBox.Show("SQL Server服务未启动");
                base.Enabled = false;
                Application.ExitThread();
            }
            this.progressBar.Maximum = 100;
            this.progressBar.Style = ProgressBarStyle.Blocks;
            this.progressBar.Value = 0;
            this.progressBar.Step = 5;
            this.ckblBakcDatabase.Checked = NeedBackupDatabase;
        } 
        #endregion

        private void txtLoginName_MouseClick(object sender, MouseEventArgs e)
        {
            this.txtLoginName.SelectAll();
        }

        private void txtStoreFile_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(this.txtStoreFile.Text.Trim()))
            {
                this.txtStorePath.Text = Path.GetDirectoryName(this.txtStoreFile.Text.Trim());
                string fileName = Path.GetFileName(this.txtStoreFile.Text.Trim());
                this.txtDBName.Text = (fileName.IndexOf(".") == -1) ? fileName : fileName.Replace(Path.GetExtension(this.txtStoreFile.Text.Trim()), "");
            }
        }

        private static bool CheckDataBase
        {
            get
            {
                bool flag = true;
                try
                {
                    flag = "0" != ConfigurationSettings.AppSettings["CheckDataBase"].ToString();
                }
                catch
                {
                }
                return flag;
            }
        }

        private static string DataBaseBackupPath
        {
            get
            {
                string str = string.Empty;
                try
                {
                    str = ConfigurationSettings.AppSettings["DataBaseBackupPath"].ToString();
                }
                catch
                {
                }
                if (string.IsNullOrEmpty(str))
                {
                    str = string.Format("{0}/Backup", Application.StartupPath);
                }
                if (!Directory.Exists(str))
                {
                    Directory.CreateDirectory(str);
                }
                return str;
            }
        }

        private static bool NeedBackupDatabase
        {
            get
            {
                bool flag = true;
                try
                {
                    flag = "0" != ConfigurationSettings.AppSettings["NeedBackupDatabase"].ToString();
                }
                catch
                {
                }
                return flag;
            }
        }

        private void TimerNowDate_Tick(object sender, EventArgs e)
        {
            tlstatusTime.Text = "时间："+DateTime.Now.ToString();
        }

        #region 数据库备份文件拖放
        private void txtStoreFile_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link; //重要代码：表明是链接类型的数据，比如文件路径
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void txtStoreFile_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (Path.HasExtension(path))
            {
                string Extension = Path.GetExtension(path);
                if (Extension.Contains(".bak") || Extension.Contains(".trn"))
                {
                    this.txtStoreFile.Text = path;
                }
               /* else
                { 
                  SetStatusDis("文件拖放失败，只支持.bak或者.trn类型文件。");
                }*/
            }
        }
        #endregion


        #region 数据库备份之备份路径的拖放
        private void txtBackupPath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
              e.Effect=DragDropEffects.Link;
            }
            else
            {
              e.Effect=DragDropEffects.None;
            }
        }

        #region 路径拖放
        private void txtBackupPath_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (Directory.Exists(path))
            {
                this.txtBackupPath.Text = path;
            }
        } 
        #endregion
    
        
        private void txtStorePath_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (Directory.Exists(path))
            {
                this.txtStorePath.Text = path;
            }
        }

        private void txtStorePath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void txtScriptPath_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (Directory.Exists(path))
            {
                this.txtScriptPath.Text = path;
            }
        }

        private void txtScriptPath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void txtScriptPath1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void txtScriptPath1_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (Directory.Exists(path))
            {
                this.txtScriptPath1.Text = path;
            }
        }

        #endregion
    }
}

