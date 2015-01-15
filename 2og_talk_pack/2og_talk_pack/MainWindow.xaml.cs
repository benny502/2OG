using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace _2og_talk_pack
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private delegate void UpdateDelegate(string tag,string filename,string message);
        private string ERROR = "ERROR";
        private string INFO = "INFO";

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            switch (btn.Name) 
            {
                case "Button1":
                    FolderBrowserDialog folder = new FolderBrowserDialog();
                    folder.Description = "选择原文件目录";
                    folder.RootFolder = Environment.SpecialFolder.Desktop;
                    folder.ShowNewFolderButton = true;
                    if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
                    {
                        App.folder = folder.SelectedPath;
                    }
                    break;
                case "Button2":
                    System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
                    dlg.Filter = "文本文件|*.txt";
                    dlg.Multiselect = true;
                    dlg.FileOk += dlg_FileOk;
                    dlg.ShowDialog();
                    break;
                case "tblBtn":
                    System.Windows.Forms.OpenFileDialog tbldlg = new System.Windows.Forms.OpenFileDialog();
                    tbldlg.Filter = "码表文件|*.txt;*.tbl";
                    tbldlg.FileOk += (s, a) =>
                    {
                        App.tblStr = tbldlg.FileName;
                    };
                    tbldlg.ShowDialog();
                    break;
            }

        }

        void dlg_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

            System.Windows.Forms.OpenFileDialog dlg = sender as System.Windows.Forms.OpenFileDialog;
            string[] files = dlg.FileNames;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerAsync(files);

        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] files = e.Argument as string[];
            _2ogFileHelper helper = new _2ogFileHelper();
            helper.message += helper_message;
            if (string.IsNullOrEmpty(App.tblStr)) throw new Exception("请先选择码表路径");
            Dictionary<string, string> tbl = helper.GetTbl(App.tblStr);
            foreach (string file in files) 
            {
                string filename = Path.GetFileName(file);
                try
                {
                    string finder = filename.TrimEnd(".txt".ToCharArray());
                    if (string.IsNullOrEmpty(App.folder)) throw new Exception("请先选择原文件路径");
                    string source = App.folder + "\\" + finder;
                    string dest = App.folder + "\\new\\" + finder;
                    Talk talk = new Talk(source);
                    string[] strs = helper.GetString(file, Encoding.UTF8);
                    for (int i = 0; i < strs.Length; ++i)
                    {
                        strs[i] = strs[i].Replace("\r\n", "@");
                    }
                    talk.Import(strs, tbl);
                    talk.UpdatePointers();
                    talk.WriteToBin(dest);
                    console(INFO, filename, "导入完成");
                }
                catch (Exception ex)
                {
                    console(ERROR, filename, ex.Message);
                }
            }
        }

        public void helper_message(Object sender, MessageEventArgs e)
        {
            string filename = (sender as FileHelper).filename;
            console(INFO, filename, e.msg);
        }

        private void console(string TAG, string filename, string msg)
        {
            Dispatcher.Invoke(new UpdateDelegate((t,f,m) =>
            {
                xbox.Text += String.Format("{0}: {1} - {2}\n", t, f, m);
            }), TAG, filename, msg);
        }

    }
}
