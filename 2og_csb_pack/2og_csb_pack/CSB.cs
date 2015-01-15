using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _2og_csb_pack
{

    class Pointer
    {
        public int count;
        public int off;
        public int pOff;

        public Pointer(int off, int pOff)
        {
            this.pOff = pOff;
            this.off = off;
        }
    }

    class CSB
    {
        private byte[] data;

        private int magicOffset = 0x18;

        private int LNPOffset;

        private int LNTOffset;

        private int count;

        private Pointer[] pointers;

        private int[] lnt;

        private byte[] exdata;

        private int start { get { return pointers[0].off; } }
        private int end { get { return pointers[pointers.Length - 1].off; } }

        public CSB(string path) 
        {
            if (!File.Exists(path)) throw new Exception("未检测到对应的原文件");
            FileHelper helper = new FileHelper(path);
            data = helper.GetBytes();
            GetLNTOffset();
            lnt = GetLNT();
            pointers = GetPointers();
        }

        private int[] GetLNT()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(LNPOffset + LNTOffset + magicOffset + 0x8, SeekOrigin.Begin);
                count = FileHelper.SwapEndian(reader.ReadInt32());
                List<int> lnt = new List<int>();
                for (int i = 0; i < count; ++i)
                {
                    lnt.Add(FileHelper.SwapEndian(reader.ReadInt32()));
                }
                return lnt.ToArray();
                
            }
        }

        private Pointer[] GetPointers()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                List<Pointer> pointers = new List<Pointer>();
                foreach (int l in lnt)
                {
                    reader.BaseStream.Seek(l, SeekOrigin.Begin);
                    int count = FileHelper.SwapEndian(reader.ReadInt32());
                    int pOff = FileHelper.SwapEndian(reader.ReadInt32());
                    reader.BaseStream.Seek(pOff, SeekOrigin.Begin);
                    for (int i = 0; i < count; ++i)
                    {
                        int addr = Convert.ToInt32(reader.BaseStream.Position);
                        int off = FileHelper.SwapEndian(reader.ReadInt32());
                        pointers.Add(new Pointer(off,addr));
                    }
                }
                return pointers.ToArray();
            }
        }

        private void GetLNTOffset()
        {
            using (BinaryReader reader = FileHelper.GetMemoryReader(data))
            {
                reader.BaseStream.Seek(0x1C, SeekOrigin.Begin);
                LNPOffset = FileHelper.SwapEndian(reader.ReadInt32());
                reader.BaseStream.Seek(LNPOffset + magicOffset + 0x4, SeekOrigin.Begin);
                LNTOffset = FileHelper.SwapEndian(reader.ReadInt32());
            }
        }

        public void Import(string[] strs, Dictionary<string, string> tbl)
        {
            FileHelper helper = new FileHelper();
            int pos = 0;
            List<byte> exdata = new List<byte>();
            for (int i = 0; i < strs.Length; ++i)
            {
                pointers[i].off = pos + data.Length;
                byte[] opt = helper.Trans(strs[i], tbl);
                pos += opt.Length + 1;
                exdata.AddRange(opt);
                byte zero = 0;
                exdata.Add(zero);
            }
            this.exdata = exdata.ToArray();
        }

        public void UpdatePointers()
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                foreach (Pointer p in pointers)
                {
                    writer.BaseStream.Seek(p.pOff, SeekOrigin.Begin);
                    writer.Write(FileHelper.SwapEndian(p.off));
                }
                writer.Flush();
            }
        }

        private void WriteRealOff(Pointer pointer)
        {
            using (BinaryWriter writer = FileHelper.GetMemoryWriter(data))
            {
                writer.BaseStream.Seek(pointer.pOff, SeekOrigin.Begin);
                writer.Write(FileHelper.SwapEndian(pointer.off));
                writer.Flush();
            }
        }

        public void WriteToBin(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            FileHelper helper = new FileHelper();
            using (BinaryWriter writer = helper.GetBinaryWriter(path))
            {
                writer.Write(data);
                writer.Write(exdata);
                writer.Flush();
            }
        }
    }
}
