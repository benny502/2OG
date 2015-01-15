using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _2og_talk_pack
{
    public class Talk
    {
        private int _count;
        public int count { get { return _count; } }

        private int _Entry;
        public int Entry { get { return _Entry; } }

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

        public int[] pointers { get; set; }

        private int _length;
        public int length 
        {
            get{ return _length; }
        }

        public int start
        {
            get { return pointers[0]; }
        }

        public int end
        {
            get { return pointers[pointers.Length - 1]; }
        }

        public Talk(string path)
        {
            if (!File.Exists(path)) throw new Exception("未检测到对应的原文件");
            FileHelper helper = new FileHelper(path);
            data = helper.GetBytes();
            _count = getCount();
            pointers = getPointers();
        }

        public int getCount()
        {
            using(BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0xC, SeekOrigin.Begin);
                return FileHelper.SwapEndian(reader.ReadInt32());
            }
        }

        public int[] getPointers()
        {
            using(BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
                _Entry = FileHelper.SwapEndian(reader.ReadInt32());
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
                byte[] buffer = new byte[end-start];
                writer.Seek(start, SeekOrigin.Begin);
                writer.Write(buffer);
                writer.Flush();
                writer.Seek(start, SeekOrigin.Begin);
                FileHelper helper = new FileHelper();
                for (int i = 0; i < strs.Length; ++i)
                {
                    pointers[i] = Convert.ToInt32(writer.BaseStream.Position);
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
                writer.BaseStream.Seek(_Entry,SeekOrigin.Begin);
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
