using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PUBGLiteExplorerWV
{
    public class PAKFile
    {
        public string myPath;
        public PAKHeader header;
        public PAKFileTable table;

        public PAKFile(string path)
        {
            if (!File.Exists(path))
                return;
            myPath = path;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(-45, SeekOrigin.End);
            header = new PAKHeader(fs);
            if (!isValid())
            {
                fs.Close();
                return;
            }
            fs.Seek((long)header.offset, 0);
            table = new PAKFileTable(fs, header.size);
            fs.Close();
        }

        public bool isValid()
        {
            if (header == null || header.magic != 0x20171216 || header.encrypted != 1)
                return false;
            return true;
        }

        public byte[] getEntryData(PAKFileEntry e)
        {
            FileStream fs = new FileStream(myPath, FileMode.Open, FileAccess.Read);
            MemoryStream m = new MemoryStream();
            e.CopyDecryptedData(fs, m);
            fs.Close();
            return m.ToArray();
        }
    }


    public class PAKHeader
    {
        public byte encrypted;
        public uint magic;
        public uint version;
        public ulong offset;
        public ulong size;
        public PAKHeader(Stream s)
        {
            encrypted = (byte)s.ReadByte();
            magic = Helper.ReadU32(s);
            version = Helper.ReadU32(s);
            offset = Helper.ReadU64(s);
            size = Helper.ReadU64(s);
        }
    }

    public class PAKFileTable
    {
        public string mPoint;
        public List<PAKFileEntry> entries;

        public PAKFileTable(Stream s, ulong size)
        {
            entries = new List<PAKFileEntry>();
            byte[] data = new byte[size];
            for (ulong i = 0; i < size; i++)
                data[i] = (byte)(s.ReadByte() ^ 0x79);
            MemoryStream m = new MemoryStream(data);
            mPoint = Helper.ReadString(m).Substring(9);
            uint count = Helper.ReadU32(m);
            for (uint i = 0; i < count; i++)
                entries.Add(new PAKFileEntry(m));
        }
    }

    public class PAKFileEntry
    {
        public long _offset;
        public string path;
        public ulong pos;
        public ulong size;
        public ulong usize;
        public uint cMethod;
        public byte[] hash;
        public List<PAKCompressionBlock> cBlocks;
        public byte encrypted;
        public uint cBlockSize;

        public PAKFileEntry(Stream s)
        {
            _offset = s.Position;
            path = Helper.ReadString(s);
            pos = Helper.ReadU64(s);
            size = Helper.ReadU64(s);
            usize = Helper.ReadU64(s);
            cMethod = Helper.ReadU32(s);
            hash = new byte[20];
            s.Read(hash, 0, 20);
            if (cMethod == 1)
            {
                cBlocks = new List<PAKCompressionBlock>();
                uint count = Helper.ReadU32(s);
                for (uint i = 0; i < count; i++)
                    cBlocks.Add(new PAKCompressionBlock(s));
            }
            encrypted = (byte)s.ReadByte();
            cBlockSize = Helper.ReadU32(s);
        }

        public void CopyDecryptedData(Stream s, Stream o)
        {
            MemoryStream m = new MemoryStream();
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
                o.Write(buff, 0, buff.Length);

            }
            if (cMethod == 1)
            {
                foreach (PAKCompressionBlock b in cBlocks)
                {
                    ulong size = b.end - b.start;
                    s.Seek((long)b.start, 0);
                    buff = new byte[size];
                    for (ulong i = 0; i < size; i++)
                        if (encrypted == 1)
                            buff[i] = (byte)(s.ReadByte() ^ 0x79);
                        else
                            buff[i] = (byte)(s.ReadByte());
                    m.Write(buff, 0, (int)size);
                }
                buff = m.ToArray();
                buff = Helper.Decompress(buff);
                o.Write(buff, 0, buff.Length);
            }
        }
    }

    public class PAKCompressionBlock
    {
        public ulong start;
        public ulong end;

        public PAKCompressionBlock(Stream s)
        {
            start = Helper.ReadU64(s);
            end = Helper.ReadU64(s);
        }
    }
}
