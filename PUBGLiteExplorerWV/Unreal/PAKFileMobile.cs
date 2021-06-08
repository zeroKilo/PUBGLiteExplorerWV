using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PUBGLiteExplorerWV
{
    public class PAKFileMobile
    {
        public static byte[] patternNormal = { 0x00, 0x00, 0x00, 0x2E, 0x2E, 0x2F, 0x2E, 0x2E, 0x2F, 0x2E, 0x2E, 0x2F };
        public static byte[] patternXored = { 0x79, 0x79, 0x79, 0x57, 0x57, 0x56, 0x57, 0x57, 0x56, 0x57, 0x57, 0x56 };
        public string myPath;
        public PAKHeaderMobile header;
        public PAKFileTableMobile table;
        public MemoryStream pak_data;

        public PAKFileMobile(string path)
        {
            if (!File.Exists(path))
                return;
            myPath = path;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(-45, SeekOrigin.End);
            header = new PAKHeaderMobile(fs);
            if (!isValid())
            {
                fs.Close();
                return;
            }
            fs.Close();
            byte[] pak = File.ReadAllBytes(path);
            int pos = pak.Length - patternNormal.Length;
            while(--pos > 0)
            {
                bool found = true;
                for (int i = 0; i < patternNormal.Length; i++)
                    if (pak[pos + i] != patternNormal[i] &&
                        pak[pos + i] != patternXored[i])
                    {
                        found = false;
                        break;
                    }
                if (found)
                    break;
            }
            if(pos > 1)
            {
                MemoryStream m = new MemoryStream(pak);
                m.Seek(pos - 1, 0);
                table = new PAKFileTableMobile(m, (ulong)(pak.Length - pos), pak[pos] == 0x79);
                pak_data = new MemoryStream(pak);
            }
        }

        public bool isValid()
        {
            if (header == null || header.magic != 0x506e0406)
                return false;
            return true;
        }

        public byte[] getEntryData(PAKFileEntryMobile e)
        {
            MemoryStream m = new MemoryStream();
            e.CopyDecryptedData(pak_data, m);
            return m.ToArray();
        }

        public void ExportData(PAKFileEntryMobile e, string path)
        {
            FileStream fIn = new FileStream(myPath, FileMode.Open, FileAccess.Read);
            FileStream fOut = new FileStream(path, FileMode.Create, FileAccess.Write);
            e.CopyDecryptedData(fIn, fOut);
            fIn.Close();
            fOut.Close();
        }
    }

    public class PAKHeaderMobile
    {
        public byte encrypted;
        public uint magic;
        public uint version;
        public ulong offset;
        public ulong size;
        public PAKHeaderMobile(Stream s)
        {
            encrypted = (byte)s.ReadByte();
            magic = Helper.ReadU32(s);
            version = Helper.ReadU32(s);
        }
    }

    public class PAKFileTableMobile
    {
        public string mPoint;
        public List<PAKFileEntryMobile> entries;

        public PAKFileTableMobile(Stream s, ulong size, bool xored)
        {
            entries = new List<PAKFileEntryMobile>();
            byte[] data = new byte[size];
            if (xored)
                for (ulong i = 0; i < size; i++)
                    data[i] = (byte)(s.ReadByte() ^ 0x79);
            else
                for (ulong i = 0; i < size; i++)
                    data[i] = (byte)s.ReadByte();
            MemoryStream m = new MemoryStream(data);
            mPoint = Helper.ReadUString(m).Substring(9);
            uint count = Helper.ReadU32(m);
            for (uint i = 0; i < count; i++)
                entries.Add(new PAKFileEntryMobile(m, mPoint));
        }
    }

    public class PAKFileEntryMobile
    {
        public long _offset;
        public string path;
        public ulong pos;
        public ulong size;
        public ulong usize;
        public uint cMethod;
        public byte[] hash;
        public List<PAKCompressionBlockMobile> cBlocks;
        public byte encrypted;
        public uint cBlockSize;

        public PAKFileEntryMobile(Stream s, string mPoint)
        {
            _offset = s.Position;
            path = mPoint + Helper.ReadUString(s);
            hash = new byte[20];
            s.Read(hash, 0, 20);
            pos = Helper.ReadU64(s);
            usize = Helper.ReadU64(s);
            cMethod = Helper.ReadU32(s);
            size = Helper.ReadU64(s);
            s.Seek(0x15, SeekOrigin.Current);
            if (cMethod == 1)
            {
                cBlocks = new List<PAKCompressionBlockMobile>();
                uint count = Helper.ReadU32(s);
                for (uint i = 0; i < count; i++)
                    cBlocks.Add(new PAKCompressionBlockMobile(s));
            }
            cBlockSize = Helper.ReadU32(s);
            encrypted = (byte)s.ReadByte();
        }

        public void CopyDecryptedData(Stream s, Stream o)
        {
            byte[] buff;
            if (cMethod == 0)
            {
                s.Seek((long)pos + 8, 0);
                ulong test = Helper.ReadU64(s);
                if (test != size)
                    return;
                s.Seek((long)pos + 0x35, 0);
                buff = new byte[size];
                for (ulong i = 0; i < size; i++)
                    if (encrypted == 1)
                        buff[i] = (byte)(s.ReadByte() ^ 0x79);
                    else
                        buff[i] = (byte)(s.ReadByte());

            }
            if (cMethod == 1)
            {
                foreach (PAKCompressionBlockMobile b in cBlocks)
                {
                    ulong bSize = b.end - b.start;
                    s.Seek((long)b.start, 0);
                    buff = new byte[bSize];
                    for (ulong i = 0; i < bSize; i++)
                        if (encrypted == 1)
                            buff[i] = (byte)(s.ReadByte() ^ 0x79);
                        else
                            buff[i] = (byte)(s.ReadByte());
                    buff = Helper.Decompress(buff);
                    o.Write(buff, 0, buff.Length);
                }
            }
        }
    }

    public class PAKCompressionBlockMobile
    {
        public ulong start;
        public ulong end;

        public PAKCompressionBlockMobile(Stream s)
        {
            start = Helper.ReadU64(s);
            end = Helper.ReadU64(s);
        }
    }
}
