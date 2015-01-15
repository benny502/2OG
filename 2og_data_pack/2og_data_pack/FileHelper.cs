using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _2og_data_pack
{
    class FileHelper
    {
        public string path { get; set; }
        private Encoding _encoding = Encoding.Default;
        public Encoding encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        public FileHelper()
        {
        }

        public FileHelper(string path)
        {
            this.path = path;
        }

        public FileHelper(string path, Encoding encoding)
        {
            this.path = path;
            _encoding = encoding;
        }

        public byte[] GetBytes(string path)
        {
            BinaryReader reader = GetBinary(path);
            byte[] data = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));
            reader.Close();
            return data;
        }

        public byte[] GetBytes()
        {
            return GetBytes(path);
        }

        public static BinaryReader GetMemoryReader(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            return new BinaryReader(stream);
        }

        public static BinaryWriter GetMemoryWriter(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            return new BinaryWriter(stream);
        }

        public BinaryReader GetBinary(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return new BinaryReader(stream);
        }

        public BinaryReader GetBinary()
        {
            return GetBinary(path);
        }

        public StreamReader GetStream(string path, Encoding encoding)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return new StreamReader(stream, encoding);
        }

        public StreamReader GetStream(string path)
        {
            return GetStream(path, _encoding);
        }

        public StreamReader GetStream()
        {
            return GetStream(path, _encoding);
        }

        public static UInt32 SwapEndian(UInt32 p)
        {
            return ((p & 0xFF000000) >> 24) |
                    ((p & 0x00FF0000) >> 8) |
                    ((p & 0x0000FF00) << 8) |
                    ((p & 0x000000FF) << 24);
        }

        public static Int32 SwapEndian(Int32 p)
        {
            byte[] data = BitConverter.GetBytes(p);
            byte buffer = data[0];
            data[0] = data[3];
            data[3] = buffer;
            buffer = data[1];
            data[1] = data[2];
            data[2] = buffer;
            return BitConverter.ToInt32(data, 0);
        }

        public static ushort SwapEndian(ushort p)
        {
            return Convert.ToUInt16(((p & 0xFF00) >> 8) | ((p & 0x00FF) << 8));
        }

        public byte[] Trans(string str, Dictionary<string, string> hash)
        {
            char[] chars = str.ToCharArray();
            List<byte> bytes = new List<byte>();
            foreach (var ch in chars)
            {
                if (!hash.ContainsKey(Convert.ToString(ch))) //throw new Exception(String.Format("码表中缺少: \"{0}\"", ch));
                {
                    //bytes.AddRange(BitConverter.GetBytes(ch));
                }
                else
                {
                    string code = hash[Convert.ToString(ch)];
                    if (Convert.ToInt32(code, 16) < 0xFF)
                    {
                        byte tmp = Convert.ToByte(code, 16);
                        bytes.Add(Convert.ToByte(code, 16));
                    }
                    else if (Convert.ToInt32(code, 16) < 0xFFFF)
                    {
                        string val1 = code.Substring(0, 2);
                        string val2 = code.Substring(2, 2);
                        bytes.Add(Convert.ToByte(val1, 16));
                        bytes.Add(Convert.ToByte(val2, 16));
                    }
                    else
                    {
                        string val1 = code.Substring(0, 2);
                        string val2 = code.Substring(2, 2);
                        string val3 = code.Substring(4, 2);
                        bytes.Add(Convert.ToByte(val1, 16));
                        bytes.Add(Convert.ToByte(val2, 16));
                        bytes.Add(Convert.ToByte(val3, 16));
                    }
                }
            }
            return bytes.ToArray();
        }

        public Dictionary<string, string> GetTbl(string path)
        {
            Dictionary<string, string> hash = new Dictionary<string, string>();
            using (StreamReader reader = GetStream(path, Encoding.Unicode))
            {
                while (!reader.EndOfStream)
                {
                    string text = reader.ReadLine();
                    if (!string.IsNullOrEmpty(text))
                    {
                        string[] array = text.Split('=');
                        if (hash.ContainsKey(array[1]))
                        {
                            hash[array[1]] = array[0];
                        }
                        else
                        {
                            hash.Add(array[1], array[0]);
                        }
                    }
                }
            }
            return hash;
        }

        public void Write(string path, byte[] data)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, data);
        }

        public void Write(byte[] data)
        {
            Write(path, data);
        }
    }
}
