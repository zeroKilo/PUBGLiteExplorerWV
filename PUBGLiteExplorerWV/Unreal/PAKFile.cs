using System;
using System.IO;
using System.Collections.Generic;

namespace PUBGLiteExplorerWV
{
    public class PAKFile
    {
        public static byte[] mobileHeaderKey = { 0x3C, 0xB9, 0x21, 0xC9, 0xA2, 0x93, 0xBA, 0x2E, 0x38, 0x75, 0xD9, 0xC8, 0xFC, 0xAF, 0x96, 0xC4, 0xE0, 0x2F, 0xA3, 0x3B, 0x63, 0x7C, 0x59, 0xB8, 0x0A, 0xEE, 0xFB, 0x1F, 0x10, 0xFA, 0xA0, 0xD8, 0xB4, 0x7A, 0xD1, 0xA6 };
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
            if (!header.isValid)
            {
                fs.Close();
                return;
            }
            switch (header.pakType)
            {
                case PAKHeader.PAKType.LitePC:
                    fs.Seek((long)header.offset, 0);
                    table = new PAKFileTable(this, fs, header.size, 0x79);
                    fs.Close();
                    break;
                case PAKHeader.PAKType.Mobile:
                    fs.Seek(0, SeekOrigin.End);
                    ulong end = (ulong)fs.Position;
                    fs.Seek((long)header.offset, 0);
                    switch (header.version)
                    {
                        case 6:
                            table = new PAKFileTable(this, fs, end - header.offset);
                            break;
                        case 7:
                            table = new PAKFileTable(this, fs, end - header.offset, 0x79);
                            break;
                        default:
                            fs.Close();
                            header.isValid = false;
                            return;
                    }
                    fs.Close();
                    break;
            }
        }

        public byte[] getEntryData(PAKFileEntry e)
        {
            FileStream fs = new FileStream(myPath, FileMode.Open, FileAccess.Read);
            MemoryStream m = new MemoryStream();
            e.CopyDecryptedData(fs, m);
            fs.Close();
            return m.ToArray();
        }

        public void ExportData(PAKFileEntry e, string path)
        {
            FileStream fIn = new FileStream(myPath, FileMode.Open, FileAccess.Read);
            FileStream fOut = new FileStream(path, FileMode.Create, FileAccess.Write);
            e.CopyDecryptedData(fIn, fOut);
            fIn.Close();
            fOut.Close();
        }
    }

    public class PAKHeader
    {
        public enum PAKType
        {
            LitePC,
            Mobile
        }
        public byte encrypted;
        public uint magic;
        public uint version;
        public ulong offset;
        public ulong size;
        public PAKType pakType;
        public bool isValid = false;
        public PAKHeader(Stream s)
        {
            encrypted = (byte)s.ReadByte();
            magic = Helper.ReadU32(s);
            version = Helper.ReadU32(s);
            switch (magic)
            {
                case 0x20171216:
                    pakType = PAKType.LitePC;
                    offset = Helper.ReadU64(s);
                    size = Helper.ReadU64(s);
                    isValid = true;
                    break;
                case 0x506e0406:
                    pakType = PAKType.Mobile;
                    byte[] buff = new byte[0x24];
                    s.Read(buff, 0, 0x24);
                    for (int i = 0; i < 0x24; i++)
                        buff[i] = (byte)(buff[i] ^ PAKFile.mobileHeaderKey[i]);
                    offset = BitConverter.ToUInt64(buff, 0x1C);
                    switch(version)
                    {
                        case 6:
                        case 7:
                        case 8:
                            offset = offset ^ 0x0CD8C051;
                            break;
                        default:
                            return;
                    }
                    isValid = true;
                    break;
            }
        }
    }

    public class PAKFileTable
    {
        public string mPoint;
        public List<PAKFileEntry> entries;

        public PAKFileTable(PAKFile p, Stream s, ulong size, byte xorByte = 0)
        {
            entries = new List<PAKFileEntry>();
            byte[] data = new byte[size];
            s.Read(data, 0, (int)size);
            if(xorByte != 0)
                for (ulong i = 0; i < size; i++)
                    data[i] = (byte)(data[i] ^ xorByte);
            MemoryStream m = new MemoryStream(data);
            mPoint = Helper.ReadUString(m).Substring(9);
            uint count = Helper.ReadU32(m);
            for (uint i = 0; i < count; i++)
                entries.Add(new PAKFileEntry(p, m, mPoint));
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

        public PAKFileEntry(PAKFile p, Stream s, string mPoint)
        {
            _offset = s.Position;
            path = mPoint + Helper.ReadUString(s);
            switch (p.header.pakType)
            {
                case PAKHeader.PAKType.LitePC:
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
                    break;
                case PAKHeader.PAKType.Mobile:
                    hash = new byte[20];
                    s.Read(hash, 0, 20);
                    pos = Helper.ReadU64(s);
                    usize = Helper.ReadU64(s);
                    cMethod = Helper.ReadU32(s);
                    size = Helper.ReadU64(s);
                    s.Seek(0x15, SeekOrigin.Current);
                    if (cMethod == 1)
                    {
                        cBlocks = new List<PAKCompressionBlock>();
                        uint count = Helper.ReadU32(s);
                        for (uint i = 0; i < count; i++)
                            cBlocks.Add(new PAKCompressionBlock(s));
                    }
                    cBlockSize = Helper.ReadU32(s);
                    encrypted = (byte)s.ReadByte();
                    break;
            }
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
                o.Write(buff, 0, buff.Length);
            }
            if (cMethod == 1)
            {
                foreach (PAKCompressionBlock b in cBlocks)
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
