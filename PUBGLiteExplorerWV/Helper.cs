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

        public static float ReadFloat(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToSingle(buff, 0);
        }

        public static string ReadUString(Stream s)
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

        public static void WriteU16(Stream s, ushort u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 2);
        }

        public static void WriteU32(Stream s, uint u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 4);
        }

        public static void WriteU64(Stream s, ulong u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 8);
        }

        public static void WriteFloat(Stream s, float f)
        {
            s.Write(BitConverter.GetBytes(f), 0, 4);
        }

        public static void WriteCString(Stream s, string str)
        {
            foreach(char c in str)
                s.WriteByte((byte)c);
        }

        public static float Half2Float(ushort h)
        {   
	        int sign = (h >> 15) & 0x00000001;
	        int exp  = (h >> 10) & 0x0000001F;
	        int mant =  h        & 0x000003FF;
	        exp  = exp + (127 - 15);
	        uint tmp = (uint)((sign << 31) | (exp << 23) | (mant << 13));
            byte[] buff = BitConverter.GetBytes(tmp);
            return BitConverter.ToSingle(buff, 0);
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
