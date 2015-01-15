using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace _2og_logic_pack
{
    public class _2ogFileHelper : FileHelper
    {
        public string[] GetString(string path, Encoding encoding)
        {
            using (StreamReader reader = GetStream(path, encoding))
            {
                List<string> strs = new List<string>();
                string line = "";
                while (!reader.EndOfStream)
                {
                    string tmp = reader.ReadLine();
                    if (!tmp.StartsWith("##") && !tmp.ToUpper().EndsWith("{END}"))
                    {
                        line += tmp +"\r\n";
                    }
                    else if (!tmp.StartsWith("##") && tmp.ToUpper().EndsWith("{END}"))
                    {
                        line += tmp.Substring(0, tmp.Length - "{END}".Length);
                        strs.Add(line);
                    }
                    else
                    {
                        line = "";
                    }
                }
                return strs.ToArray();
            }
        }
    }
}
