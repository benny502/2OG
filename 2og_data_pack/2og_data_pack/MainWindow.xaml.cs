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

namespace _2og_data_pack
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void UpdateDelegate(string tag, string filename, string message);
        private string ERROR = "ERROR";
        private string INFO = "INFO";

        public MainWindow()
        {
            InitializeComponent();
        }

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
            if (string.IsNullOrEmpty(App.tblStr)) throw new Exception("请先选择码表路径");
            Dictionary<string, string> tbl = helper.GetTbl(App.tblStr);
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                try
                {
                    string finder = filename.Substring(0, filename.Length - 4);
                    if (string.IsNullOrEmpty(App.folder)) throw new Exception("请先选择原文件路径");
                    string source = App.folder + "\\" + finder;
                    string dest = App.folder + "\\new\\" + finder;

                    string[] strs = helper.GetString(file, Encoding.UTF8);
                    for (int i = 0; i < strs.Length; ++i)
                    {
                        strs[i] = strs[i].Replace("\r\n", "\n");
                    }
                    getfileData(source);
                    getSegment();
                    getPointers();
                    Import(strs, tbl);
                    UpdatePointers();
                    WriteToBin(dest);
                    console(INFO, filename, "导入完成");
                }
                catch (Exception ex)
                {
                    console(ERROR, filename, ex.Message);
                }
            }
        }



        private byte[] data;
        private int segOffset = 0x10;
        private int dataOffset;
        private int DOFSOffset;
        private int SOFSOffset;
        private int STRIOffset;
        private List<int> addrArray = new List<int>();
        private List<int> pointers = new List<int>();

        private void getfileData(string path)
        {
            if (!File.Exists(path)) throw new Exception("未检测到对应的原文件");
            _2ogFileHelper helper = new _2ogFileHelper(path);
            data = helper.GetBytes();
        }

        private void getSegment()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                int tag;
                reader.BaseStream.Seek(segOffset, SeekOrigin.Begin);
                while(0x49525453 != (tag = reader.ReadInt32()))
                {
                    int nextP = 0;
                    switch (tag)
                    {
                        case 0x41544144: //data
                            dataOffset = Convert.ToInt32(reader.BaseStream.Position);
                            nextP = FileHelper.SwapEndian(reader.ReadInt32()) + Convert.ToInt32(reader.BaseStream.Position) + 0x4;
                            break;
                        case 0x53464f44: //DOFS
                            DOFSOffset = Convert.ToInt32(reader.BaseStream.Position);
                            nextP = FileHelper.SwapEndian(reader.ReadInt32()) + Convert.ToInt32(reader.BaseStream.Position);
                            break;
                        case 0x53464f53: //SOFS
                            SOFSOffset = Convert.ToInt32(reader.BaseStream.Position);
                            nextP = FileHelper.SwapEndian(reader.ReadInt32()) + Convert.ToInt32(reader.BaseStream.Position);
                            break;
                    }
                    reader.BaseStream.Seek(nextP, SeekOrigin.Begin);
                }
                STRIOffset = Convert.ToInt32(reader.BaseStream.Position);
            }
        }

        private void getPointers()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(SOFSOffset, SeekOrigin.Begin);
                int length = FileHelper.SwapEndian(reader.ReadInt32());
                int count = length / 4;
                for (int i = 0; i < count; ++i)
                {
                    int addr = FileHelper.SwapEndian(reader.ReadInt32());
                    addrArray.Add(addr);
                }
            }
        }

        private void Import(string[] strs,Dictionary<string, string> tbl)
        {
            int length = 0;
            int count = 0;

            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(STRIOffset, SeekOrigin.Begin);
                length = FileHelper.SwapEndian(reader.ReadInt32());
                count = FileHelper.SwapEndian(reader.ReadInt32());
            }
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                byte[] zero = new byte[length];
                _2ogFileHelper helper = new _2ogFileHelper();
                writer.BaseStream.Seek(STRIOffset + 8, SeekOrigin.Begin);
                writer.Write(zero);
                writer.BaseStream.Seek(STRIOffset + 8, SeekOrigin.Begin);
                int pos = 0;
                foreach(string str in strs)
                {
                    pointers.Add(pos);
                    string[] buffers = str.Split('\n');
                    ushort len = FileHelper.SwapEndian(Convert.ToUInt16(buffers.Length));
                    writer.Write(len);
                    pos += 0x2;
                    ushort off = 0x0;
                    List<byte> bytebuf = new List<byte>();
                    foreach (string buf in buffers)
                    {
                        ushort l = Convert.ToUInt16(buf.Length);
                        writer.Write(FileHelper.SwapEndian(l));
                        pos += 0x2;
                        writer.Write(FileHelper.SwapEndian(off));
                        pos += 0x2;
                        byte[] b = helper.Trans(buf, tbl);
                        bytebuf.AddRange(b);
                        bytebuf.Add(0x0);
                        off += Convert.ToUInt16(bytebuf.Count);
                    }
                    pos += bytebuf.Count;
                    writer.Write(bytebuf.ToArray());
                }     
            }
        }

        private void UpdatePointers()
        {
            using(BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                writer.BaseStream.Seek(SOFSOffset + 0x4, SeekOrigin.Begin);
                foreach (int pos in pointers)
                {
                    writer.Write(FileHelper.SwapEndian(pos));
                }
            }
        }

        private void WriteToBin(string dest)
        {
            _2ogFileHelper helper = new _2ogFileHelper();
            helper.Write(dest, data);
        }

        private void console(string TAG, string filename, string msg)
        {
            Dispatcher.Invoke(new UpdateDelegate((t, f, m) =>
            {
                xbox.Text += String.Format("{0}: {1} - {2}\n", t, f, m);
            }), TAG, filename, msg);
        }
    }
}
