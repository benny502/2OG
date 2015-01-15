using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace src系统文本导出
{
    public class FixedDataStruct
    {
        public byte[] prev { get; set; }
        public string text { get; set; }
    }
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Grid_Drop_1(object sender, DragEventArgs e)
        {
            var pathArray = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s,a) =>
                {
                    for (int i = 0; i < pathArray.Length; ++i)
                    {
                        string path = pathArray.GetValue(i).ToString();
                        string extentions = System.IO.Path.GetExtension(path);
                        Export(path);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            log.Text += string.Format("\t[{0}/{1}]\n", i + 1, pathArray.Length);
                        }));
                    }
                };
            worker.RunWorkerAsync();
        }

        private void Export(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            string fileName = System.IO.Path.GetFileName(path);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                log.Text += string.Format("{0}", fileName);
                            }));
                            UInt32 lpAddr = 0, lpText;
                            List<UInt32> addrArray = null;
                            List<string> textArray = null;
                            List<FixedDataStruct> fixDataArr = null;
                            UInt32 tag = SwapEnding(reader.ReadUInt32());
                            switch(tag)
                            {
                                case 0x4C4F474F:
                                    reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
                                    lpAddr = SwapEnding(reader.ReadUInt32());
                                    reader.BaseStream.Seek(0x4C, SeekOrigin.Begin);
                                    lpText = SwapEnding(reader.ReadUInt32());
                                    addrArray = GetAddr(lpAddr, reader);
                                    textArray = GetText(addrArray, lpText, reader);
                                    WriteText(textArray, path);
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        log.Text += string.Format("...Done!");
                                    }));
                                    break;
                                case 0x4C444249: //talk
                                    reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                                    lpAddr = SwapEnding(reader.ReadUInt32());
                                    addrArray = GetStoryAddr(lpAddr, reader);
                                    textArray = GetStoryText(addrArray, reader);
                                    WriteText(textArray, path);
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        log.Text += string.Format("...Done!");
                                    }));
                                    break;
                                case 0x46495848: //FixData
                                    reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
                                    UInt32 tag1 = SwapEnding(reader.ReadUInt32());
                                    if (tag1 == 0x44415441)
                                    {
                                        lpAddr = SwapEnding(reader.ReadUInt32());
                                        lpAddr += Convert.ToUInt32(reader.BaseStream.Position) + 0x8;
                                    }
                                    else
                                    {
                                        lpAddr = SwapEnding(reader.ReadUInt32());
                                        lpAddr += Convert.ToUInt32(reader.BaseStream.Position) + 0x4;
                                        reader.BaseStream.Seek(lpAddr, SeekOrigin.Begin);
                                        lpAddr = SwapEnding(reader.ReadUInt32());
                                        lpAddr += Convert.ToUInt32(reader.BaseStream.Position) + 0x8;
                                    }
                                    addrArray = GetFixDataAddr(lpAddr, reader);
                                    fixDataArr = GetFixDataText(addrArray, reader);
                                    WriteFixDataText(fixDataArr, path);
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        log.Text += string.Format("...Done!");
                                    }));
                                    break;
                                default:
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        log.Text += string.Format("...文件格式不符");
                                    }));
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string fileName = System.IO.Path.GetFileName(path);
                Dispatcher.Invoke(new Action(() =>
                {
                    log.Text += string.Format("读取 {0} 文件出错 ： {1}\n", fileName, ex.Message);
                }));
            }
        }

        private void WriteFixDataText(List<FixedDataStruct> fixDataArr, string path)
        {
            if (fixDataArr != null && fixDataArr.Count > 0)
            {
                try
                {
                    string npath = path += ".txt";
                    using (FileStream stream = new FileStream(npath, FileMode.Create))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            int i = 0;
                            foreach (var text in fixDataArr)
                            {
                                ushort num1 = SwapEnding(BitConverter.ToUInt16(text.prev, 0x0));
                                ushort num2 = SwapEnding(BitConverter.ToUInt16(text.prev, 0x2));
                                ushort num3 = SwapEnding(BitConverter.ToUInt16(text.prev, 0x4));
                                writer.WriteLine("#### [{0:X4}{1:X4}{2:X4}]-{3} ####", num1, num2, num3, i);
                                writer.Write("{0}", text.text.Replace("@", "\r\n"));
                                writer.WriteLine("{end}");
                                writer.WriteLine();
                                i++;
                            }
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string fileName = System.IO.Path.GetFileName(path);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        log.Text += string.Format("写入 {0} 文件出错 ： {1}\n", fileName, ex.Message);
                    }));
                }
            }
        }

        ushort SwapEnding(ushort p)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(p).Reverse().ToArray(),0);
        }

        private List<FixedDataStruct> GetFixDataText(List<UInt32> addrArray, BinaryReader reader)
        {
            List<FixedDataStruct> textArray = new List<FixedDataStruct>();
            while (true)
            {
                UInt32 tag = SwapEnding(reader.ReadUInt32());
                if (tag == 0x53545249)
                {
                    break;
                }
            }
            reader.BaseStream.Seek(0x8, SeekOrigin.Current);
            UInt32 offset = Convert.ToUInt32(reader.BaseStream.Position);
            if (addrArray != null && addrArray.Count > 0)
            {
                foreach (var addr in addrArray)
                {
                    FixedDataStruct fixedData = new FixedDataStruct();
                    UInt32 realAddr = addr + offset;
                    reader.BaseStream.Seek(realAddr, SeekOrigin.Begin);
                    fixedData.prev = reader.ReadBytes(0x6);
                    List<byte> textBuff = new List<byte>();
                    while (true)
                    {
                        byte buff = reader.ReadByte();
                        if (buff == 0x0)
                        {
                            break;
                        }
                        textBuff.Add(buff);
                    }
                    fixedData.text = Encoding.UTF8.GetString(textBuff.ToArray());
                    textArray.Add(fixedData);
                }
            }
            return textArray;
        }

        private List<UInt32> GetFixDataAddr(UInt32 lpAddr, BinaryReader reader)
        {
            List<UInt32> addrArray = new List<UInt32>();
            reader.BaseStream.Seek(lpAddr, SeekOrigin.Begin);
            UInt32 length = SwapEnding(reader.ReadUInt32());
            UInt32 count = length / 4;
            for (UInt32 i = 0; i < count; ++i)
            {
                UInt32 addr = SwapEnding(reader.ReadUInt32());
                addrArray.Add(addr);
            }
            return addrArray;
        }

        private List<string> GetStoryText(List<UInt32> addrArray, BinaryReader reader)
        {
            List<string> textAddr = new List<string>();
            if (addrArray != null && addrArray.Count > 0)
            {
                foreach (var addr in addrArray)
                {
                    reader.BaseStream.Seek(addr, SeekOrigin.Begin);
                    List<byte> textBuff = new List<byte>();
                    while (true)
                    {
                        byte buff = reader.ReadByte();
                        if (buff == 0x0)
                        {
                            break;
                        }
                        textBuff.Add(buff);
                    }
                    string text = Encoding.UTF8.GetString(textBuff.ToArray());
                    textAddr.Add(text);
                }
            }
            return textAddr;
        }

        private List<UInt32> GetStoryAddr(UInt32 lpAddr, BinaryReader reader)
        {
            List<UInt32> addrArray = new List<UInt32>();
            reader.BaseStream.Seek(lpAddr, SeekOrigin.Begin);
            while (true)
            {
                UInt32 addr = SwapEnding(reader.ReadUInt32());
                if (addr == 0x0)
                {
                    break;
                }
                addrArray.Add(addr);
            }
            return addrArray;
        }

        private void WriteText(List<string> textArray, string path)
        {
            if (textArray != null && textArray.Count > 0)
            {
                try
                {
                    string npath = path += ".txt";
                    using (FileStream stream = new FileStream(npath, FileMode.Create))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            int i = 0;
                            foreach (var text in textArray)
                            {
                                writer.WriteLine("#### {0} ####",i);
                                writer.Write("{0}", text.Replace("@", "\r\n"));
                                writer.WriteLine("{end}");
                                writer.WriteLine();
                                i++;
                            }
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string fileName = System.IO.Path.GetFileName(path);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        log.Text += string.Format("写入 {0} 文件出错 ： {1}\n", fileName, ex.Message);
                    }));
                }
            }
        }

        private List<string> GetText(List<UInt32> addrArray, UInt32 lpText, BinaryReader reader)
        {
            List<string> textAddr = new List<string>();
            if(addrArray != null && addrArray.Count > 0)
            {
                foreach(var addr in addrArray)
                {
                    if (addr != addrArray[addrArray.Count - 1])
                    {
                        UInt32 address = addr + lpText;
                        reader.BaseStream.Seek(address, SeekOrigin.Begin);
                        List<byte> textBuff = new List<byte>();
                        while (true)
                        {
                            byte buff = reader.ReadByte();
                            if (buff == 0x0)
                            {
                                break;
                            }
                            textBuff.Add(buff);
                        }
                        string text = Encoding.UTF8.GetString(textBuff.ToArray());
                        textAddr.Add(text);
                    }
                }
            }
            return textAddr;
        }

        private List<UInt32> GetAddr(UInt32 lpAddr, BinaryReader reader)
        {
            List<UInt32> addrArray = new List<UInt32>();
            reader.BaseStream.Seek(0x40,SeekOrigin.Begin);
            UInt32 l1 = SwapEnding(reader.ReadUInt32());
            reader.BaseStream.Seek(0x50, SeekOrigin.Begin);
            UInt32 l2 = SwapEnding(reader.ReadUInt32());
            UInt32 count = l1 + l2;
            reader.BaseStream.Seek(lpAddr,SeekOrigin.Begin);
            for (UInt32 i = 0; i < count; ++i)
            {
                UInt32 addr = SwapEnding(reader.ReadUInt32());
                addrArray.Add(addr);
            }
            return addrArray;
        }

        private UInt32 SwapEnding(UInt32 p)
        {
            return ((p & 0xFF000000) >> 24) |
                    ((p & 0x00FF0000) >> 8) |
                    ((p & 0x0000FF00) << 8) |
                    ((p & 0x000000FF) << 24);
        }

        private void log_PreviewDragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}
