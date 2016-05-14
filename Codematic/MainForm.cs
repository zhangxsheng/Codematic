using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using Crownwood.Magic.Common;
using Crownwood.Magic.Docking;
using System.Net;
using System.Collections.Specialized;
using Maticsoft.CodeHelper;
using Crownwood.Magic.Menus;
using Maticsoft.Utility;

//Դ������  www.51aspx.com
namespace Codematic
{
    public partial class MainForm : Form
    {
        Thread threadUpdate;
        //Thread threadSetup;
        public Mutex mutex;
        public static Maticsoft.CmConfig.ModuleSettings setting = new Maticsoft.CmConfig.ModuleSettings();
        
        Maticsoft.CmConfig.AppSettings appsettings;
        string cmcfgfile = Application.StartupPath + @"\cmcfg.ini";
        Maticsoft.Utility.INIFile cfgfile;

        FrmSearch frmSearch;

        private object[] persistedSearchItems;

        delegate void SetStatusCallback(string text);
        delegate void AddNewTabPageCallback(Control control, string Title);

        private Hashtable Languagelist = Language.LoadFromCfg("SystemMenu.lan");
        /// <summary>
        /// ��ǰ����Ĵ���ģ�崰��
        /// </summary>
        private CodeTemplate ActiveCodeTemplate
        {
            get
            {
                foreach (Crownwood.Magic.Controls.TabPage tabPage in this.tabControlMain.TabPages)
                {
                    if (tabPage.Selected && tabPage.Control.Name == "CodeTemplate")
                    {
                        foreach (Control control in tabPage.Control.Controls)
                        {
                            if (control.Name == "tabControl1")
                            {
                                return (CodeTemplate)tabPage.Control;
                            }
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// ��ǰ�����SQL��ѯ����
        /// </summary>
        private DbQuery ActiveDbQuery
        {
            get
            {
                foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
                {
                    if (page.Selected)
                    {
                        if (page.Control.Name == "DbQuery")
                        {
                            foreach (Control ctr in page.Control.Controls)
                            {
                                if ((ctr.ProductName == "LTPTextEditor") && (ctr.Name == "txtContent"))
                                {
                                    return (DbQuery)page.Control;
                                }
                            }
                        }
                    }
                }
                return null;
            }
            set
            {
                ActiveDbQuery = value;
            }
        }
        /// <summary>
        /// ��ǰ����Ĵ������ɴ���
        /// </summary>
        private CodeMaker ActiveCodeMaker
        {
            get
            {
                foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
                {
                    if (page.Selected)
                    {
                        if (page.Control.Name == "CodeMaker")
                        {
                            foreach (Control ctr in page.Control.Controls)
                            {
                                if (ctr.Name == "tabControl1")
                                {
                                    return ((CodeMaker)page.Control);
                                }
                            }
                        }
                    }
                }
                return null;
            }
            set
            {
                ActiveCodeMaker = value;
            }
        }

        /// <summary>
        /// ��ǰ����༭������
        /// </summary>
        private CodeEditor ActiveCodeEditor
        {
            get
            {
                foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
                {
                    if (page.Selected)
                    {
                        if (page.Control.Name == "CodeEditor")
                        {
                            foreach (Control ctr in page.Control.Controls)
                            {
                                if ((ctr.ProductName == "LTP.TextEditor") && (ctr.Name == "txtContent"))
                                {
                                    return (CodeEditor)page.Control;
                                }
                            }
                        }
                    }
                }
                return null;
            }
            set
            {
                ActiveCodeEditor = value;
            }
        }
        #region ����Docking Manager����
        /// <summary>
        /// Dockk�ؼ�
        /// </summary>
        private DockingManager dockManager;
        //Content solutionExplorerContent;
        //Content classViewContent;

        private DockingManager DBdockManager;        
        Content DbViewContent;
        Content tempViewContent;
        
        #endregion


        public MainForm()
        {
            InitializeComponent();
            this.SetLanguage();
            mutex = new Mutex(false, "SINGLE_INSTANCE_MUTEX");
            if (!mutex.WaitOne(0, false))
            {
                mutex.Close();
                mutex = null;
            }
            string str = "��������������";
            if (this.Languagelist["SoftName"] != null)
            {
                str = this.Languagelist["SoftName"].ToString();
            }
            this.Text = str + "  V" + Application.ProductVersion;
            //webBrowser1.Url = new System.Uri("" + Application.ProductVersion, System.UriKind.Absolute);

            #region  �Ҳ���ͼ��������
            
            dockManager = new DockingManager(this, VisualStyle.IDE);
            dockManager.OuterControl = statusBar;
            dockManager.InnerControl = tabControlMain;
            tabControlMain.IDEPixelBorder = true;
            tabControlMain.IDEPixelArea = true;
            tempViewContent = new Content(dockManager);
            tempViewContent.Control = new TempView(this);
            Size tempViewSize = tempViewContent.Control.Size;
            string text = "ģ�����";
            if (this.Languagelist["TempView"] != null)
            {
                text = this.Languagelist["TempView"].ToString();
            }
            tempViewContent.Title = text;
            tempViewContent.FullTitle = text;
            tempViewContent.AutoHideSize = tempViewSize;
            tempViewContent.DisplaySize = tempViewSize;
            tempViewContent.ImageList = leftViewImgs;
            tempViewContent.ImageIndex = 1;

            //���������ں;����ڸ��������б������������ϵ����
            dockManager.Contents.Add(tempViewContent);

            dockManager.AddContentWithState(tempViewContent, State.DockRight);

            //DockingManager�����ݳ�ԱOuterControl��InnerControl
            //��������DockingManager���ڵĴ�������Щ�����ܵ�DockingManagerͣ�����ڵ�Ӱ�� 
            //Docking Manager����Ӱ����OuterControl�����Ժ���������ڵĶ���Ĵ������� 
            //Docking ManagerҲ����Ӱ����InnerControl������ǰ���������ڵĶ���Ĵ������� 


            //����OuterControl��Docking Manager�����ע�ö����Ժ����ɵĶ���Ĵ�������
            //����InnerControl��Docking Manager�����ע�ڸö���������ǰ�Ķ���Ĵ�������
           

            //����Conten���󣬸ö������DockingManager�����ĸ�������
            //���ø������ڵ����ԣ�title�Ǵ��������Ժ�ı���  //FullTitle�Ǵ�����ʾʱ�ı���

            //���������Դ������
            //solutionExplorerContent = new Content(dockManager);
            //solutionExplorerContent.Control = new SolutionExplorer();
            //Size solutionExplorerSize = solutionExplorerContent.Control.Size;
            //solutionExplorerContent.Title = "���������Դ������";
            //solutionExplorerContent.FullTitle = "���������Դ������";
            //solutionExplorerContent.AutoHideSize = solutionExplorerSize;
            //solutionExplorerContent.DisplaySize = solutionExplorerSize;
            //solutionExplorerContent.ImageList = viewImgs;
            //solutionExplorerContent.ImageIndex = 0;
            //solutionExplorerContent.PropertyChanged += new Content.PropChangeHandler(PropChange);

          
            //����ͼ


            //���������ں;����ڸ��������б������������ϵ����
            //dockManager.Contents.Add(solutionExplorerContent);
            //WindowContent wc = dockManager.AddContentWithState(solutionExplorerContent, State.DockRight);

            //ģ����ͼ
            
            #endregion

            #region �����ͼ

            DBdockManager = new DockingManager(this, VisualStyle.IDE);
            
            //�������OuterControl��Docking Manager�����ע�ö����Ժ����ɵĶ���Ĵ�������
            //����InnerControl��Docking Manager�����ע�ڸö���������ǰ�Ķ���Ĵ�������
            DBdockManager.OuterControl = statusBar;
            DBdockManager.InnerControl = tabControlMain;

            //���ݿ���ͼ
            DbViewContent = new Content(DBdockManager);
            DbViewContent.Control = new DbView(this);
            Size DbViewSize = DbViewContent.Control.Size;
            string text2 = "���ݿ���ͼ";
            if (this.Languagelist["DBView"] != null)
            {
                text2 = this.Languagelist["DBView"].ToString();
            }
            this.DbViewContent.Title = text2;
;
            DbViewContent.FullTitle =text2;
            DbViewContent.AutoHideSize = DbViewSize;
            DbViewContent.DisplaySize = DbViewSize;
            DbViewContent.ImageList = leftViewImgs;
            DbViewContent.ImageIndex = 0;

            DBdockManager.Contents.Add(DbViewContent);
            //DBdockManager.AddContentToWindowContent(tempViewContent, wcdb);
            this.DBdockManager.AddContentWithState(this.DbViewContent, State.DockLeft);
            #endregion

            #region ��ʼҳ
            appsettings = Maticsoft.CmConfig.AppConfig.GetSettings();
            switch (appsettings.AppStart)
            {
                case "startuppage"://��ʾ��ʼҳ
                    {
                        #region //������ʼҳ
                        try
                        {                            
                            LoadStartPage();
                        }
                        catch(System.Exception ex)
                        {
                            LogInfo.WriteLog(ex);                            
                        }                        
                        #endregion
                    }
                    break;
                case "blank"://��ʾ�ջ���
                    {
                    }
                    break;
                case "homepage": //����ҳ
                    {
                        #region 
                        string selstr = "��ҳ";
                        string link = "http://www.maticsoft.com";
                        if (appsettings.HomePage != null && appsettings.HomePage != "")
                        {
                            link = appsettings.HomePage;
                        }
                        //��ʼҳ
                        Crownwood.Magic.Controls.TabPage page = new Crownwood.Magic.Controls.TabPage();
                        page.Title = selstr;
                        page.Control = new IEView(this, link);
                        tabControlMain.TabPages.Add(page);
                        tabControlMain.SelectedTab = page;

                        #endregion
                    }
                    break;
            }
            #endregion

            this.tabControlMain.MouseUp += new MouseEventHandler(OnMouseUpTabPage);

            #region ������������

            //if (!IsHasChecked())
            //{
            //    try
            //    {
            //        threadUpdate = new Thread(new ThreadStart(ProcUpdate));
            //        threadUpdate.Start();
            //        //ProcUpdate();
            //    }
            //    catch (System.Exception ex)
            //    {
            //        LogInfo.WriteLog(ex);
            //        MessageBox.Show(ex.Message, "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            //    }
            //}
            #endregion

            #region ���Ͱ�װ��Ϣ
            //try
            //{
            //    bool issetup = appsettings.Setup;
            //    if (!issetup)
            //    {
            //        threadSetup = new Thread(new ThreadStart(SendSetup));
            //        threadSetup.Start();
            //    }
            //}
            //catch
            //{
            //}
            #endregion

            #region װ�ز��

            #endregion

        }

        private void SetLanguage()
        {
            this.menuCommand1.Description = this.Languagelist["File"].ToString();
            this.menuCommand1.Text = this.Languagelist["File"].ToString() + " (&F)";
            this.menuCommand2.Description = "MenuItem";
            this.menuCommand2.Text = this.Languagelist["Edit"].ToString() + " (&E)";
            this.menuCommand3.Description = "MenuItem";
            this.menuCommand3.Text = this.Languagelist["Project"].ToString() + " (&P)";
            this.toolStripMenuItem1.Text = this.Languagelist["File"].ToString() + " (&F)";
            this.�½�ToolStripMenuItem.Text = this.Languagelist["New"].ToString() + "(&N)";
            this.��ĿPToolStripMenuItem.Text = this.Languagelist["Project"].ToString() + " (&P)";
            this.�ļ�FToolStripMenuItem.Text = this.Languagelist["File"].ToString() + " (&F)";
            this.���ݿ�����SToolStripMenuItem.Text = this.Languagelist["DBConnect"].ToString() + " (&S)";
            this.��ToolStripMenuItem.Text = this.Languagelist["Open"].ToString() + " (&O)";
            this.�ر�CToolStripMenuItem.Text = this.Languagelist["Close"].ToString() + " (&C)";
            this.����ΪToolStripMenuItem.Text = this.Languagelist["SaveAS"].ToString() + " (&S)...";
            this.�˳�.Text = this.Languagelist["Exit"].ToString() + " (&X)";
            this.toolStripMenuItem2.Text = this.Languagelist["Edit"].ToString() + "(&E)";
            this.�ָ�ZToolStripMenuItem.Text = this.Languagelist["Restore"].ToString() + " (&Z)";
            this.�༭ToolStripMenuItem.Text = this.Languagelist["Cut"].ToString() + " (&T)";
            this.����ToolStripMenuItem.Text = this.Languagelist["Copy"].ToString() + " (&C)";
            this.ճ��ToolStripMenuItem.Text = this.Languagelist["Paste"].ToString() + " (&P)";
            this.�ָ�ToolStripMenuItem.Text = this.Languagelist["Delete"].ToString() + " (&D)";
            this.ȫѡAToolStripMenuItem.Text = this.Languagelist["SelectAll"].ToString() + " (&A)";
            this.����ToolStripMenuItem.Text = this.Languagelist["Find"].ToString() + " ...";
            this.������һ��ToolStripMenuItem.Text = (this.Languagelist["FindNext"].ToString() ?? "");
            this.�滻ToolStripMenuItem.Text = (this.Languagelist["Replace"].ToString() ?? "");
            this.ת����ToolStripMenuItem.Text = this.Languagelist["GotoLine"].ToString() + "...";
            this.��ͼVToolStripMenuItem.Text = this.Languagelist["View"].ToString() + "(&V)";
            this.��������Դ������SToolStripMenuItem.Text = this.Languagelist["ServerManager"].ToString() + "(&S)";
            this.ģ�������TToolStripMenuItem.Text = this.Languagelist["TempManager"].ToString() + "(&T)";
            this.�������ToolStripMenuItem.Text = this.Languagelist["SolutionManager"].ToString() + "(&P)";
            this.����ͼToolStripMenuItem.Text = this.Languagelist["ClassView"].ToString() + "(&C)";
            this.���ݿ�ժҪToolStripMenuItem.Text = this.Languagelist["DBManager"].ToString();
            this.��ʼҳGToolStripMenuItem.Text = this.Languagelist["StartPage"].ToString() + "(&G)";
            this.��ѯQToolStripMenuItem.Text = this.Languagelist["Query"].ToString() + " (&Q)";
            this.�򿪽ű�ToolStripMenuItem.Text = (this.Languagelist["OpenSql"].ToString() ?? "");
            this.����ű�ToolStripMenuItem.Text = this.Languagelist["SaveSql"].ToString();
            this.���е�ǰ��ѯToolStripMenuItem.Text = this.Languagelist["RunSql"].ToString();
            this.ֹͣ��ѯToolStripMenuItem.Text = this.Languagelist["StopSql"].ToString();
        }

        #region ���Ͱ�װ��Ϣ

        void SendSetup()
        {
            try
            {
                WebClient wc = new WebClient();
                string url = "http://www.maticsoft.com/setup.aspx";

                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("SoftName", "Codematic");
                nvc.Add("Version", Application.ProductVersion);
                //nvc.Add("OS", "1");
                //nvc.Add("Mac", "ee-ee-ff-ds");
                nvc.Add("SQLinfo", "ee-ee-ff-ds");
                byte[] databuffer = wc.UploadValues(url, "POST", nvc);
                string text = Encoding.Default.GetString(databuffer);
                wc.Dispose();
                appsettings.Setup = true;
                Maticsoft.CmConfig.AppConfig.SaveSettings(appsettings);
            }
            catch (System.Exception ex)
            {
                LogInfo.WriteLog(ex);
            }
        }
        #endregion

        #region ������������

        #endregion

        #region  ������ʼҳ
        void LoadStartPage()
        {
            string RssPath = appsettings.StartUpPage;
            SetStatusText("���ڼ�����ʼҳ...");
            AddSinglePage(new StartPageForm(this, RssPath), "��ʼҳ");
            SetStatusText("���");
        }

        #endregion

        #region  tabControlMain��������Ķ�ҳ���

        /// <summary>
        /// ��TabControl���Ҽ��м���˵�
        /// </summary>
        protected void OnMouseUpTabPage(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.tabControlMain.TabPages.Count > 0 && e.Button == MouseButtons.Right && this.tabControlMain.SelectedTab.Selected)
            {
                Crownwood.Magic.Menus.MenuControl muMenu = new Crownwood.Magic.Menus.MenuControl();
                MenuCommand menu1 = new MenuCommand(this.Languagelist["Save"].ToString() + "(&S)", new EventHandler(this.OnSaveSelected));
                MenuCommand menu2 = new MenuCommand(this.Languagelist["Close"].ToString() + "(&C)", new EventHandler(this.OnColseSelected));
                MenuCommand menu3 = new MenuCommand(this.Languagelist["CloseAll"].ToString() + "(&A)", new EventHandler(this.OnColseUnSelected));
			
                Crownwood.Magic.Menus.PopupMenu pm = new Crownwood.Magic.Menus.PopupMenu();
                string name = this.tabControlMain.SelectedTab.Control.Name;
                if (name!= null && (name == "DbQuery" || name == "CodeEditor" || name == "HtmlEditor"))
                {
                    pm.MenuCommands.AddRange(new MenuCommand[]{menu1,menu2,menu3});
                }
                else
                {
                    pm.MenuCommands.AddRange(new MenuCommand[]{menu2,menu3});
                }
                //pm.MenuCommands.AddRange(new Crownwood.Magic.Menus.MenuCommand[] { menu1, menu2, menu3 });
                pm.TrackPopup(this.tabControlMain.PointToScreen(new Point(e.X, e.Y)));
            }
            if (this.tabControlMain.TabPages.Count > 0 && e.Button == MouseButtons.Left && this.tabControlMain.SelectedTab.Selected)
            {
                toolBtn_SQLExe.Visible = false;
                toolBtn_Run.Visible = false;
                ��ѯQToolStripMenuItem.Visible = false;

                switch (this.tabControlMain.SelectedTab.Control.Name)
                {
                    case "DbQuery":
                        {
                            toolBtn_SQLExe.Visible = true;
                            ��ѯQToolStripMenuItem.Visible = true;
                        }
                        break;
                    case "DbBrowser":
                        {

                        }
                        break;
                    case "StartPageForm":
                        {

                        }
                        break;
                    case "CodeMaker":
                        {
                        }
                        break;
                    case "CodeTemplate":
                        {
                            toolBtn_Run.Visible = true;
                            //��ѯQToolStripMenuItem.Visible = false;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// �ر�TabControl����ѡ��TabPage
        /// </summary>
        protected void OnColseSelected(object sender, EventArgs e)
        {
            if (tabControlMain.TabPages.Count > 0)
            {
                OnCloseTabPage(tabControlMain.SelectedTab);
                tabControlMain.TabPages.Remove(tabControlMain.SelectedTab);
                if (tabControlMain.TabPages.Count == 0)
                {
                    tabControlMain.Visible = false;
                }
            }
        }

        /// <summary>
        /// ����TabControl����ѡ��TabPage
        /// </summary>
        protected void OnSaveSelected(object sender, EventArgs e)
        {
            string name;
            if ((name = this.tabControlMain.SelectedTab.Control.Name) != null && !(name == "DbQuery"))
            {
                if (!(name == "CodeEditor"))
                {
                    if (!(name == "HtmlEditor"))
                    {
                        return;
                    }
                }
                else
                {
                    CodeEditor codeEditor = (CodeEditor)this.tabControlMain.SelectedTab.Control;
                    codeEditor.Save();
                }
            }
        }
        /// <summary>
        /// �ر�TabControl��δѡ�������TabPage
        /// </summary>
        protected void OnColseUnSelected(object sender, EventArgs e)
        {
            if (tabControlMain.TabPages.Count > 0)
            {
                ArrayList pagelist = new ArrayList();
                foreach (Crownwood.Magic.Controls.TabPage tabpage in tabControlMain.TabPages)
                {
                    if (tabpage != tabControlMain.SelectedTab)
                    {
                        pagelist.Add(tabpage);
                    }
                }
                foreach (Crownwood.Magic.Controls.TabPage tabpage in pagelist)
                {
                    tabControlMain.TabPages.Remove(tabpage);
                }
                if (tabControlMain.TabPages.Count == 0)
                {
                    tabControlMain.Visible = false;
                }
            }
        }

        //�ر�ĳҳʱ���Ĵ���
        private void OnCloseTabPage(Crownwood.Magic.Controls.TabPage page)
        {
            switch (page.Control.Name)
            {
                case "DbQuery":
                    {
                        toolBtn_SQLExe.Visible = false;
                        ��ѯQToolStripMenuItem.Visible = false;
                    }
                    break;
                case "DbBrowser":
                    {

                    }
                    break;
                case "StartPageForm":
                    {

                    }
                    break;
                case "CodeMaker":
                    {
                    }
                    break;
                case "CodeTemplate":
                    {
                        toolBtn_Run.Visible = false;
                        //��ѯQToolStripMenuItem.Visible = false;
                    }
                    break;
                default:

                    break;
            }
            page.Control.Dispose();
        }

        public void PropChange(Content obj, Crownwood.Magic.Docking.Content.Property prop)
        {
            //MessageBox.Show(obj.Title + "  " + prop.ToString());
        }

        #endregion

        #region �˵�

        #region �ļ�
        private void ���ݿ�����SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DbView dbview = new DbView(this);
            dbview.backgroundWorkerReg.RunWorkerAsync();
            
        }
        private void ��ĿPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewProject newpro = new NewProject();
            newpro.ShowDialog(this);

        }
        private void �ļ�FToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewFile newfile = new NewFile(this);
            newfile.ShowDialog(this);

            ////�հ��ļ�
            //if (tabControlMain.Visible == false)
            //{
            //    tabControlMain.Visible = true;
            //}
            //Crownwood.Magic.Controls.TabPage page = new Crownwood.Magic.Controls.TabPage();
            //page.Title = "Exam.cs";
            //page.Control = new CodeEditor();
            //tabControlMain.TabPages.Add(page);
            //tabControlMain.SelectedTab = page;

        }
        private void ����ΪToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                SaveFileDialog sqlsavedlg = new SaveFileDialog();
                sqlsavedlg.Title = this.Languagelist["SaveSQL"].ToString();
                sqlsavedlg.Filter = "sql files (*.sql)|*.sql|All files (*.*)|*.*";
                DialogResult dlgresult = sqlsavedlg.ShowDialog(this);
                if (dlgresult == DialogResult.OK)
                {
                    string filename = sqlsavedlg.FileName;
                    string text = ActiveDbQuery.txtContent.Text;

                    StreamWriter sw = new StreamWriter(filename, false, Encoding.Default);//,false);
                    sw.Write(text);
                    sw.Flush();//�ӻ�����д����������ļ���
                    sw.Close();
                }
            }
            if (ActiveCodeMaker != null)
            {
                CodeMaker cm = ActiveCodeMaker;
                SaveFileDialog sqlsavedlg = new SaveFileDialog();
                sqlsavedlg.Title = this.Languagelist["SaveCode"].ToString();
                string text = "";
                if (cm.codeview.txtContent_CS.Visible)
                {                    
                    sqlsavedlg.Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*";
                    text = cm.codeview.txtContent_CS.Text;
                }
                if (cm.codeview.txtContent_SQL.Visible)
                {                 
                    sqlsavedlg.Filter = "SQL files (*.sql)|*.cs|All files (*.*)|*.*";
                    text = cm.codeview.txtContent_SQL.Text;
                }
                if (cm.codeview.txtContent_Web.Visible)
                {                 
                    sqlsavedlg.Filter = "Aspx files (*.aspx)|*.cs|All files (*.*)|*.*";
                    text = cm.codeview.txtContent_Web.Text;
                }                
                DialogResult dlgresult = sqlsavedlg.ShowDialog(this);
                if (dlgresult == DialogResult.OK)
                {
                    string filename = sqlsavedlg.FileName;
                    
                    StreamWriter sw = new StreamWriter(filename, false, Encoding.Default);//,false);
                    sw.Write(text);
                    sw.Flush();//�ӻ�����д����������ļ���
                    sw.Close();
                }
            }
            if (this.ActiveCodeTemplate != null)
            {
                CodeTemplate activeCodeTemplate = this.ActiveCodeTemplate;
                SaveFileDialog saveFileDialog3 = new SaveFileDialog();
                saveFileDialog3.Title = this.Languagelist["SaveCode"].ToString();
                string value2 = "";
                if (activeCodeTemplate.codeview.txtContent_CS.Visible)
                {
                    saveFileDialog3.Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*";
                    value2 = activeCodeTemplate.codeview.txtContent_CS.Text;
                }
                if (activeCodeTemplate.codeview.txtContent_SQL.Visible)
                {
                    saveFileDialog3.Filter = "SQL files (*.sql)|*.cs|All files (*.*)|*.*";
                    value2 = activeCodeTemplate.codeview.txtContent_SQL.Text;
                }
                if (activeCodeTemplate.codeview.txtContent_Web.Visible)
                {
                    saveFileDialog3.Filter = "Aspx files (*.aspx)|*.cs|All files (*.*)|*.*";
                    value2 = activeCodeTemplate.codeview.txtContent_Web.Text;
                }
                DialogResult dialogResult3 = saveFileDialog3.ShowDialog(this);
                if (dialogResult3 == DialogResult.OK)
                {
                    string fileName3 = saveFileDialog3.FileName;
                    StreamWriter streamWriter3 = new StreamWriter(fileName3, false, Encoding.Default);
                    streamWriter3.Write(value2);
                    streamWriter3.Flush();
                    streamWriter3.Close();
                }
            }
            if (ActiveCodeEditor != null)
            {
                SaveFileDialog sqlsavedlg = new SaveFileDialog();
                sqlsavedlg.Title = this.Languagelist["SaveCode"].ToString();
                sqlsavedlg.Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*";
                DialogResult dlgresult = sqlsavedlg.ShowDialog(this);
                if (dlgresult == DialogResult.OK)
                {
                    string filename = sqlsavedlg.FileName;
                    string text = ActiveCodeEditor.txtContent.Text;

                    StreamWriter sw = new StreamWriter(filename, false, Encoding.Default);//,false);
                    sw.Write(text);
                    sw.Flush();//�ӻ�����д����������ļ���
                    sw.Close();
                }
            }
        }

        private void �ر�CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControlMain.TabPages.Count > 0)
            {
                OnCloseTabPage(tabControlMain.SelectedTab);
                tabControlMain.TabPages.Remove(tabControlMain.SelectedTab);
                if (tabControlMain.TabPages.Count == 0)
                {
                    tabControlMain.Visible = false;
                }
            }
        }

        private void ��ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void �˳�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        #region �༭

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                frmSearch = new FrmSearch(ActiveDbQuery);
                frmSearch.Closing += new CancelEventHandler(frmSearch_Closing);
                frmSearch.SearchItems = persistedSearchItems;
                frmSearch.TopMost = true;
                frmSearch.Show();//Dialog(ActiveQueryForm);
                frmSearch.Focus();
            }
        }

        private void frmSearch_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                persistedSearchItems = frmSearch.SearchItems;
            }
            catch { return; }
        }

        private void ������һ��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActiveDbQuery.FindNext();
        }

        private void �滻ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                FrmSearch frmSearch = new FrmSearch(ActiveDbQuery, true);
                frmSearch.Closing += new CancelEventHandler(frmSearch_Closing);
                frmSearch.SearchItems = persistedSearchItems;
                frmSearch.Show();
                frmSearch.Focus();
            }
        }

        private void ת����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.GoToLine();
            }
        }

        private void ȫѡAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
            {                
                if (page.Selected)
                {
                    if (page.Control.Name == "DbQuery") 
                    {
                        foreach (Control ctr in page.Control.Controls)
                        {
                            if ((ctr.ProductName == "LTPTextEditor") && (ctr.Name == "txtContent"))
                            {
                                LTPTextEditor.Editor.TextEditorControlWrapper txtContent = (LTPTextEditor.Editor.TextEditorControlWrapper)ctr;
                                txtContent.Select(0, txtContent.Text.Length);
                            }                           
                        }
                    }
                    //if (page.Control.Name == "CodeMaker")
                    //{
                    //    foreach (Control ctr in page.Control.Controls)
                    //    {
                    //        if ((ctr.ProductName == "LTP.TextEditor") && (ctr.Name == "txtContent"))
                    //        {
                    //            LTPTextEditor.Editor.TextEditorControlWrapper txtContent = (LTPTextEditor.Editor.TextEditorControlWrapper)ctr;
                    //            txtContent.Select(0, txtContent.Text.Length);
                    //        }
                    //    }
                    //}
                    //if (page.Control.Name == "CodeEditor")
                    //{
                    //    foreach (Control ctr in page.Control.Controls)
                    //    {
                    //        if ((ctr.ProductName == "LTP.TextEditor") && (ctr.Name == "txtContent"))
                    //        {
                    //            LTP.TextEditor.TextEditorControl txtContent = (LTP.TextEditor.TextEditorControl)ctr;
                    //            txtContent.Select(0, txtContent.Text.Length);
                    //        }
                    //    }
                    //}
                }
            }
        }

        private void �ָ�ZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.Undo();
            }
        }
        private void ճ��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.Paste();
            }
        }

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.Copy();
            }
        }

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.Cut();
            }

        }
        private void ɾ��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.Cut();
            }
        }

        #endregion

        #region ��ͼ
        private void ��������Դ������SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string title = "���ݿ���ͼ";
                if (this.Languagelist["DBView"] != null)
                {
                    title = this.Languagelist["DBView"].ToString();
                }
                Content c = this.DBdockManager.Contents[title];
                if (this.��������Դ������SToolStripMenuItem.Checked)
                {
                    this.DBdockManager.HideContent(c);
                    this.��������Դ������SToolStripMenuItem.Checked = false;
                }
                else
                {
                    this.DBdockManager.ShowContent(c);
                    this.��������Դ������SToolStripMenuItem.Checked = true;
                }
            }
            catch
            {
            }
        }
        private void ģ�������TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = "ģ�����";
            if (this.Languagelist["TempView"] != null)
            {
                title = this.Languagelist["TempView"].ToString();
            }
            Content content = DBdockManager.Contents[title];
            if (ģ�������TToolStripMenuItem.Checked)
            {
                DBdockManager.HideContent(content);
                ģ�������TToolStripMenuItem.Checked = false;
            }
            else
            {
                DBdockManager.ShowContent(content);
                ģ�������TToolStripMenuItem.Checked = true;
            }
        }

        private void �������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Content content = dockManager.Contents["���������Դ������"];
            if (�������ToolStripMenuItem.Checked)
            {
                //dockManager.HideContent(content);
                �������ToolStripMenuItem.Checked = false;
            }
            else
            {
                ////dockManager.ShowContent(content);
                �������ToolStripMenuItem.Checked = true;
            }
        }

        private void ����ͼToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Content content = dockManager.Contents["����ͼ"];
            if (����ͼToolStripMenuItem.Checked)
            {
                dockManager.HideContent(content);
                ����ͼToolStripMenuItem.Checked = false;
            }
            else
            {
                //dockManager.ShowContent(content);
                ����ͼToolStripMenuItem.Checked = true;
            }
        }

        private void ���ݿ�ժҪToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.AddSinglePage(new DbBrowser(), this.Languagelist["Summary"].ToString());
        }

        private void ��ʼҳGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //AddSinglePage(new StartPageForm(this), "��ʼҳ");
            LoadStartPage();
        }
        #endregion

        #region ����
        private void �������ݽű�ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ���ɴ洢����ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void �洢����ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ���ݽű�ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ������ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ������ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region ����
        private void ����WToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void StatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void ��ʾ�������ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void ���ô��ڲ���ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            ////���������Դ������            
            //DBdockManager.ShowContent(solutionExplorerContent);

            ////����ͼ            
            //DBdockManager.ShowContent(classViewContent);
                        
            //���ݿ���ͼ            
            DBdockManager.ShowContent(DbViewContent);
            
            //ģ�������ͼ            
            DBdockManager.ShowContent(tempViewContent);
        }
        private void �ر������ĵ�LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControlMain.TabPages.Count > 0)
            {
                ArrayList pagelist = new ArrayList();
                foreach (Crownwood.Magic.Controls.TabPage tabpage in tabControlMain.TabPages)
                {
                    pagelist.Add(tabpage);
                }
                foreach (Crownwood.Magic.Controls.TabPage tabpage in pagelist)
                {
                    tabControlMain.TabPages.Remove(tabpage);
                }
                if (tabControlMain.TabPages.Count == 0)
                {
                    tabControlMain.Visible = false;
                }
            }

        }
        #endregion

        #region ��ѯ
        private void �򿪽ű�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                OpenFileDialog sqlfiledlg = new OpenFileDialog();
                sqlfiledlg.Title = "��sql�ű��ļ�";
                sqlfiledlg.Filter = "sql files (*.sql)|*.sql|All files (*.*)|*.*";
                DialogResult result = sqlfiledlg.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    string filename = sqlfiledlg.FileName;

                    StreamReader srFile = new StreamReader(filename, Encoding.Default);
                    string Contents = srFile.ReadToEnd();
                    srFile.Close();
                    ActiveDbQuery.txtContent.Text = Contents;

                    //ActiveDbQuery.txtContent.LoadFile(filename, true);
                }

            }
        }

        private void ����ű�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                SaveFileDialog sqlsavedlg = new SaveFileDialog();
                sqlsavedlg.Title = "���浱ǰ��ѯ";
                sqlsavedlg.Filter = "sql files (*.sql)|*.sql|All files (*.*)|*.*";
                DialogResult dlgresult = sqlsavedlg.ShowDialog(this);
                if (dlgresult == DialogResult.OK)
                {
                    string filename = sqlsavedlg.FileName;
                    string text = ActiveDbQuery.txtContent.Text;

                    StreamWriter sw = new StreamWriter(filename, false, Encoding.Default);//,false);
                    sw.Write(text);
                    sw.Flush();//�ӻ�����д����������ļ���
                    sw.Close();
                }
            }

        }

        private void ���е�ǰ��ѯToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.RunCurrentQuery();
            }
        }

        private void ֹͣ��ѯToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ��֤��ǰ��ѯToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.miValidateCurrentQuery_Click(sender, e);
            }

        }

        private void �ű�Ƭ�Ϲ���ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmAddToSnippet frm = new FrmAddToSnippet("");
            frm.ShowDialog(this);
        }

        private void ת������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.GoToDefenition();
            }
        }

        private void ת����������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveDbQuery != null)
            {
                ActiveDbQuery.GoToReferenceObject();
            }
        }

        #endregion


        #region ����
        private void ���ݿ������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddSinglePage(new DbBrowser(), "ժҪ");
        }

        private void ��ѯ������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddSinglePage(new DbQuery(this, ""), "��ѯ������");
            this.toolBtn_SQLExe.Visible = true;
        }
        private void SearchTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dbViewSelServer = FormCommon.GetDbViewSelServer();
            if (dbViewSelServer == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            SearchTables searchTables = new SearchTables(this, dbViewSelServer);
            searchTables.ShowDialog(this);
        }
        private void ����������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AddSinglePage(new CodeMaker(), "��������");
        }
        private void dB�ű�������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DbToScript ce = new DbToScript(longservername);
            ce.ShowDialog(this);
        }
        private void ģ�����������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AddSinglePage(new CodeTemplate(this), "ģ�����������");
        }

        private void �����Զ������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CodeExport ce = new CodeExport(longservername);
            ce.ShowDialog(this);
        }

        private void ģ�������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dbViewSelServer = FormCommon.GetDbViewSelServer();
            if (dbViewSelServer == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            TemplateBatch templateBatch = new TemplateBatch(dbViewSelServer, false);
            templateBatch.ShowDialog(this);
        }

        private void �������ݿ��ĵ�ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                DbToWord dbToWord = new DbToWord(longservername);
                dbToWord.Show();
            }
            catch
            {
                DialogResult dialogResult = MessageBox.Show("�����ĵ�������ʧ�ܣ������Ƿ�װ��Office��������ȷ��������������ݿ⡣\r\n �������ѡ��������ҳ��ʽ���ĵ�����Ҫ������", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (dialogResult == DialogResult.Yes)
                {
                    DbToWeb dbToWeb = new DbToWeb(longservername);
                    dbToWeb.Show();
                }
            }
        }

        private void c����ת��ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConverteCS2VB csvb = new ConverteCS2VB();
            csvb.Show();
        }
        private void wEB��Ŀ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectExp pro = new ProjectExp();
            pro.Show();
        }

        private void ѡ��OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionFrm of = new OptionFrm(this);
            of.Show();
        }
        #endregion

        #region ����

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    Process proc = new Process();
            //    proc.StartInfo.FileName = "help.chm";
            //    proc.StartInfo.Arguments = "";
            //    proc.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            //    proc.Start();
            //}
            //catch
            //{
            //    MessageBox.Show("����ʣ�http://ltp.cnblogs.com", "���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            try
            {
                Process proc = new Process();
                Process.Start("IExplore.exe", "http://help.maticsoft.com");
            }
            catch
            {
                MessageBox.Show("����ʣ�http://www.maticsoft.com", "���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ����Maticsoftվ��NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process proc = new Process();
                Process.Start("IExplore.exe", "http://www.maticsoft.com");
            }
            catch
            {
                MessageBox.Show("����ʣ�http://www.maticsoft.com", "���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ��Ҫ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                new Process();
                Process.Start("IExplore.exe", "http://www.maticsoft.com/bug.aspx");
            }
            catch
            {
                MessageBox.Show("����ʣ�http://www.maticsoft.com", "���", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
        private void ��̳����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process proc = new Process();
                Process.Start("IExplore.exe", "http://bbs.maticsoft.com");
            }
            catch
            {
                MessageBox.Show("����ʣ�http://bbs.maticsoft.com", "���", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ����CodematicAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout fa = new FormAbout();
            fa.ShowDialog(this);
        }
        #endregion

        #endregion

        #region ���÷���

        public void SetStatusText(string text)
        {
            //if (this.StatusLabel1.InvokeRequired)
            //{
            //    SetStatusCallback d = new SetStatusCallback(SetStatusText);
            //    this.Invoke(d, new object[] { text });
            //}
            //else
            //{
            this.StatusLabel1.Text = text;

            //}
        }


        //�õ���ǰ���ݿ������ѡ�еķ���������
        //private string GetDbViewSelServer()
        //{
        //    DbView dbviewfrm1 = (DbView)Application.OpenForms["DbView"];
        //    TreeNode SelNode = dbviewfrm1.treeView1.SelectedNode;
        //    if (SelNode == null)
        //        return "";
        //    string longservername = "";
        //    switch (SelNode.Tag.ToString())
        //    {
        //        case "serverlist":
        //            return "";
        //        case "server":
        //            {
        //                longservername = SelNode.Text;
        //            }
        //            break;
        //        case "db":
        //            {
        //                longservername = SelNode.Parent.Text;
        //            }
        //            break;
        //        case "tableroot":
        //        case "viewroot":
        //            {
        //                longservername = SelNode.Parent.Parent.Text;
        //            }
        //            break;
        //        case "table":
        //        case "view":
        //            {
        //                longservername = SelNode.Parent.Parent.Parent.Text;
        //            }
        //            break;
        //        case "column":
        //            longservername = SelNode.Parent.Parent.Parent.Parent.Text;
        //            break;
        //    }

        //    return longservername;
        //}

        /// <summary>
        /// ������ݿ����������
        /// </summary>
        
        public void CheckDbServer()
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
        }

        //�Ƿ��Ѿ����ڸô���ҳ
        private bool ExistPage(string CtrName)
        {
            bool Exist = false;
            if (tabControlMain.Visible == false)
            {
                tabControlMain.Visible = true;
            }
            foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
            {
                string str = page.Control.Name;
                if (page.Control.Name == CtrName)
                {
                    Exist = true;
                }
                //if (page.Title == "ժҪ")
                //{
                //    showed = true;
                //}
            }
            return Exist;
        }

        //�����µĴ���ҳ
        //private void AddNewTabPage(Control control, string Title)
        //{
        //    Crownwood.Magic.Controls.TabPage page = new Crownwood.Magic.Controls.TabPage();
        //    page.Title = Title;
        //    page.Control = control;
        //    tabControlMain.TabPages.Add(page);
        //    tabControlMain.SelectedTab = page;
        //}

        public void AddNewTabPage(Control control, string Title)
        {
            if (this.tabControlMain.InvokeRequired)
            {
                AddNewTabPageCallback d = new AddNewTabPageCallback(AddNewTabPage);
                this.Invoke(d, new object[] { control, Title });
            }
            else
            {
                Crownwood.Magic.Controls.TabPage page = new Crownwood.Magic.Controls.TabPage();
                page.Title = Title;
                page.Control = control;
                tabControlMain.TabPages.Add(page);
                tabControlMain.SelectedTab = page;
            }
        }

        // ����TabPage
        public void AddTabPage(string pageTitle, Control ctrForm)
        {
            if (tabControlMain.Visible == false)
            {
                tabControlMain.Visible = true;
            }
            Crownwood.Magic.Controls.TabPage page = new Crownwood.Magic.Controls.TabPage();
            page.Title = pageTitle;
            page.Control = ctrForm;
            tabControlMain.TabPages.Add(page);
            tabControlMain.SelectedTab = page;
        }


        // �����µ�Ψһ����ҳ���������ظ��ģ�
        public void AddSinglePage(Control control, string Title)
        {
            if (tabControlMain.Visible == false)
            {
                tabControlMain.Visible = true;
            }
            bool showed = false;
            Crownwood.Magic.Controls.TabPage currPage = null;
            foreach (Crownwood.Magic.Controls.TabPage page in tabControlMain.TabPages)
            {
                if (page.Control.Name == control.Name)
                {
                    showed = true;
                    currPage = page;
                }
            }
            if (!showed)//������
            {
                AddNewTabPage(control, Title);
            }
            else
            {
                tabControlMain.SelectedTab = currPage;
            }
        }
        /// <summary>
        /// �Ƿ��Ѿ��������°汾
        /// </summary>
        /// <returns></returns>
        private bool IsHasChecked()
        {
            if (!File.Exists(this.cmcfgfile))
            {
                return false;
            }
            this.cfgfile = new INIFile(this.cmcfgfile);
            string text = this.cfgfile.IniReadValue("update", "autoupdate");
            if (string.IsNullOrEmpty(text) || text == "0")
            {
                return false;
            }
            string a = this.cfgfile.IniReadValue("update", "today");
            return a == DateTime.Today.ToString("yyyyMMdd");
        }
        /// <summary>
        /// ��ǽ����Ѿ����˰汾���
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="loginfo"></param>
        private void CheckMarker()
        {            
            cfgfile.IniWriteValue("update", "today", DateTime.Today.ToString("yyyyMMdd"));           
        }

        #endregion

        #region ������

        //���ݿ������
        private void toolBtn_DbView_Click(object sender, EventArgs e)
        {
            AddSinglePage(new DbBrowser(), "ժҪ");
        }
        //��ѯ������
        private void toolBtn_SQL_Click(object sender, EventArgs e)
        {
            AddSinglePage(new DbQuery(this, ""), "��ѯ������");
            this.toolBtn_SQLExe.Visible = true;
        }
        //����������
        private void toolBtn_CreatCode_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AddSinglePage(new CodeMaker(), "��������");
        }
        //ģ���������
        private void toolBtn_CreatTempCode_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AddSinglePage(new CodeTemplate(this), "ģ�����������");

        }

        //�Զ��������
        private void toolBtn_OutCode_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CodeExport ce = new CodeExport(longservername);
            ce.ShowDialog(this);

        }

        private void toolBtn_SQLExe_Click(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab.Control.Name == "DbQuery")
            {
                DbQuery dqfrm = (DbQuery)tabControlMain.SelectedTab.Control;
                dqfrm.RunCurrentQuery();

            }
        }

        private void toolBtn_Run_Click(object sender, EventArgs e)
        {
            if (tabControlMain.SelectedTab.Control.Name == "CodeTemplate")
            {
                CodeTemplate ctfrm = (CodeTemplate)tabControlMain.SelectedTab.Control;
                ctfrm.Run();
            }
            CodeEditor arg_43_0 = this.ActiveCodeEditor;
        }


        //���ݿ��ĵ�
        private void toolBtn_Word_Click(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (longservername == "")
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                DbToWord dbToWord = new DbToWord(longservername);
                dbToWord.Show();
            }
            catch
            {
                DialogResult dialogResult = MessageBox.Show("�����ĵ�������ʧ�ܣ������Ƿ�װ��Office��������ȷ��������������ݿ⡣\r\n �������ѡ��������ҳ��ʽ���ĵ�����Ҫ������", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (dialogResult == DialogResult.Yes)
                {
                    DbToWeb dbToWeb = new DbToWeb(longservername);
                    dbToWeb.Show();
                }
            }

        }

        //web��Ŀ����
        private void toolBtn_Web_Click(object sender, EventArgs e)
        {
            ProjectExp pro = new ProjectExp();
            pro.Show();
        }

        //�˳�
        private void toolBtn_Exit_Click(object sender, EventArgs e)
        {
            //this.notifyIcon1.Visible = false;
            Application.Exit();
            Environment.Exit(0);

        }

        // ���ݿ�ѡ���б�
        private void toolComboBox_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string longservername = FormCommon.GetDbViewSelServer();
            if (string.IsNullOrEmpty(longservername))
            {
                MessageBox.Show("û�п��õ����ݿ����ӣ������������ݿ��������", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Maticsoft.IDBO.IDbObject dbobj = ObjHelper.CreatDbObj(longservername);
            string dbname = toolComboBox_DB.Text;
            DataTable dt = dbobj.GetTabViews(dbname);
            toolComboBox_Table.Items.Clear();
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string tablename = row["name"].ToString();
                    this.toolComboBox_Table.Items.Add(tablename);
                }
                if (toolComboBox_Table.Items.Count > 0)
                {
                    this.toolComboBox_Table.SelectedIndex = 0;
                }
            }
            this.StatusLabel3.Text = "��ǰ��:" + dbname;
        }


        //��
        private void toolComboBox_Table_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        #endregion
    }
}