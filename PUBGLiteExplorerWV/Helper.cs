using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace PUBGLiteExplorerWV
{

    public static class Helper
    {
        public static ushort ReadU16(Stream s)
        {
            byte[] buff = new byte[2];
            s.Read(buff, 0, 2);
            return BitConverter.ToUInt16(buff, 0);
        }

        public static uint ReadU32(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToUInt32(buff, 0);
        }

        public static ulong ReadU64(Stream s)
        {
            byte[] buff = new byte[8];
            s.Read(buff, 0, 8);
            return BitConverter.ToUInt64(buff, 0);
        }

        public static string ReadString(Stream s)
        {
            int len = (int)ReadU32(s);
            StringBuilder sb = new StringBuilder();
            if (len > 0)
            {
                for (int i = 0; i < len - 1; i++)
                    sb.Append((char)s.ReadByte());
                s.ReadByte();
            }
            else
            {
                for (int i = 0; i < -len - 1; i++)
                    sb.Append((char)ReadU16(s));
                ReadU16(s);
            }
            return sb.ToString();
        }

        public static byte[] Decompress(byte[] data)
        {
            byte[] result = null;
            using (InflaterInputStream inf = new InflaterInputStream(new MemoryStream(data)))
            {
                MemoryStream m = new MemoryStream();
                inf.CopyTo(m);
                result = m.ToArray();
            }
            return result;
        }
    }
}
