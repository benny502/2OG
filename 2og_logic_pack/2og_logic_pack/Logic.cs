using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _2og_logic_pack
{
    class Logic
    {
        //文本数
        private int _count;
        public int count { get { return _count; } }
        private int _txtEntry;
        public int txtEntry { get { return _txtEntry;} }
        //索引入口
        private int _Entry;
        public int Entry { get { return _Entry; } }
        // 文本指针
        public int[] pointers { get; set; }
        //数据长度
        private int _length;
        public int length
        {
            get { return _length; }
        }
        
        private byte[] _data;
        public byte[] data
        {
            set
            {
                _data = value;
                _length = data.Length;
            }

            get
            {
                return _data;
            }
        }
        //文本开始位置
        public int start
        {
            get { return pointers[0] + txtEntry; }
        }
        //文本结束位置
        public int end
        {
            get { return pointers[pointers.Length - 1] + txtEntry; }
        }

        public Logic(string path)
        {
            // TODO: Complete member initialization
            if (!File.Exists(path)) throw new Exception("未检测到对应的原文件");
            FileHelper helper = new FileHelper(path);
            data = helper.GetBytes();
            _count = getCount();
            pointers = getPointers();
        }

        public int getCount()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x40, SeekOrigin.Begin);
                int l1 = FileHelper.SwapEndian(reader.ReadInt32());
                reader.BaseStream.Seek(0x50, SeekOrigin.Begin);
                int l2 = FileHelper.SwapEndian(reader.ReadInt32());
                return l1 + l2;
            }
        }

        public int[] getPointers()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
                _Entry = FileHelper.SwapEndian(reader.ReadInt32());
                reader.BaseStream.Seek(0x4C, SeekOrigin.Begin);
                _txtEntry = FileHelper.SwapEndian(reader.ReadInt32());
                reader.BaseStream.Seek(_Entry, SeekOrigin.Begin);
                int[] pointers = new int[_count];
                for (int i = 0; i < _count; i++)
                {
                    pointers[i] = FileHelper.SwapEndian(reader.ReadInt32());
                }
                return pointers;
            }
        }

        public void Import(string[] strs, Dictionary<string, string> tbl)
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                byte[] buffer = new byte[end - start];
                writer.Seek(start, SeekOrigin.Begin);
                writer.Write(buffer);
                writer.Flush();
                writer.Seek(start, SeekOrigin.Begin);
                FileHelper helper = new FileHelper();
                for (int i = 0; i < strs.Length; ++i)
                {
                    pointers[i] = Convert.ToInt32(writer.BaseStream.Position) - _txtEntry;
                    writer.Write(helper.Trans(strs[i], tbl));
                    byte zero = 0;
                    writer.Write(zero);
                }
                writer.Flush();
            }
        }

        public void UpdatePointers()
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                writer.BaseStream.Seek(_Entry, SeekOrigin.Begin);
                foreach (var pointer in pointers)
                {
                    writer.Write(FileHelper.SwapEndian(pointer));
                }
                writer.Flush();
            }
        }

        public void WriteToBin(string path)
        {
            FileHelper helper = new FileHelper();
            helper.Write(path, data);
        }
    }
}
