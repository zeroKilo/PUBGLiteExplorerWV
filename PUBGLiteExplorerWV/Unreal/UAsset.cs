using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UAsset
    {
        public uint magic;
        public int version;
        public ulong unk1;
        public ulong unk2;
        public uint fileSize;
        public string packageGroup;
        public uint packageFlags;
        public uint nameCount;
        public uint nameOffset;
        public ulong unk3;
        public uint exportCount;
        public uint exportOffset;
        public uint importCount;
        public uint importOffset;
        public List<string> nameTable;
        public List<uint> nameTableHashes;
        public List<UExport> exportTable;

        public bool _isValid = false;

        public UAsset(Stream data, Stream uexp, MemoryStream ubulk)
        {
            magic = Helper.ReadU32(data);
            if (magic != 0x9e2a83c1)
                return;
            version = (int)Helper.ReadU32(data);
            if (version != -7)
                return;
            unk1 = Helper.ReadU64(data);
            unk2 = Helper.ReadU64(data);
            if (unk1 != 0 || unk2 != 0)
                return;
            fileSize = Helper.ReadU32(data);
            if (fileSize != data.Length)
                return;
            packageGroup = Helper.ReadUString(data);
            packageFlags = Helper.ReadU32(data);
            nameCount = Helper.ReadU32(data);
            nameOffset = Helper.ReadU32(data);
            unk3 = Helper.ReadU64(data);
            if (unk3 != 0)
                return;
            exportCount = Helper.ReadU32(data);
            exportOffset = Helper.ReadU32(data);
            importCount = Helper.ReadU32(data);
            importOffset = Helper.ReadU32(data);
            ReadNameTable(data);
            ReadExportTable(data, uexp);
            _isValid = true;
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            foreach (UExport exp in exportTable)
            {
                if (exp._name != null)
                    sb.AppendLine("Export Name: " + exp._name);
                else
                    sb.AppendLine("Export Name: not found!");
                sb.AppendLine(" Archetype    : " + exp.archType.ToString("X8"));
                sb.AppendLine(" Flags        : " + exp.flags.ToString("X8"));
                sb.Append(" Data Preview :");
                if (exp._data != null)
                    for (int i = 0; i < 16 && i < (int)exp.dataSize; i++)
                        sb.Append(" " + exp._data[i].ToString("X2"));
                sb.AppendLine(" ...");
            }
            return sb.ToString();
        }

        private void ReadNameTable(Stream s)
        {
            nameTable = new List<string>();
            nameTableHashes = new List<uint>();
            s.Seek(nameOffset, 0);
            for (int i = 0; i < nameCount; i++)
            {
                nameTable.Add(Helper.ReadUString(s));
                nameTableHashes.Add(Helper.ReadU32(s));
            }
        }

        private void ReadExportTable(Stream s, Stream extra)
        {
            exportTable = new List<UExport>();
            s.Seek(exportOffset, 0);
            for (int i = 0; i < exportCount; i++)
                exportTable.Add(new UExport(s));
            for (int i = 0; i < exportCount; i++)
            {
                UExport exp = exportTable[i];
                if (exp.nameIdx > 0 && exp.nameIdx <= nameTable.Count)
                    exp._name = nameTable[exp.nameIdx - 1];
                if (extra != null)
                {
                    ulong offset = exportTable[i].dataOffset - fileSize;
                    if (offset + exportTable[i].dataSize < (ulong)extra.Length)
                    {
                        exp._data = new byte[exp.dataSize];
                        extra.Seek((long)offset, 0);
                        extra.Read(exp._data, 0, (int)exp.dataSize);
                    }
                }
            }
        }
    }

    public class UExport
    {
        public int classIdx;
        public int superIdx;
        public int templateIdx;
        public int packageIdx;
        public int nameIdx;
        public uint flags;
        public int archType;
        public ulong dataSize;
        public ulong dataOffset;
        public byte[] unk;

        public string _name;
        public byte[] _data;

        public UExport(Stream s)
        {
            classIdx = (int)Helper.ReadU32(s);
            superIdx = (int)Helper.ReadU32(s);
            templateIdx = (int)Helper.ReadU32(s);
            packageIdx = (int)Helper.ReadU32(s);
            nameIdx = (int)Helper.ReadU32(s);
            archType = (int)Helper.ReadU32(s);
            flags = Helper.ReadU32(s);
            dataSize = Helper.ReadU64(s);
            dataOffset = Helper.ReadU64(s);
            unk = new byte[0x3C];
            s.Read(unk, 0, 0x3C);
        }
    }
}
