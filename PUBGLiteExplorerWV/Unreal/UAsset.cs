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
        public uint dependsOffset;
        public uint stringRefCount;
        public uint stringRefOffset;
        public uint searchableNamesOffset;
        public uint thumbnailOffset;
        public byte[] guid;
        public ulong bulkDataStartOffset;


        public List<string> nameTable;
        public List<uint> nameTableHashes;
        public List<UExport> exportTable;
        public List<UImport> importTable;
        public byte[] _ubulkData = null;

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
            dependsOffset = Helper.ReadU32(data);
            stringRefCount = Helper.ReadU32(data);
            stringRefOffset = Helper.ReadU32(data);
            searchableNamesOffset = Helper.ReadU32(data);
            thumbnailOffset = Helper.ReadU32(data);
            guid = new byte[16];
            data.Read(guid, 0, 16);
            uint genCount = Helper.ReadU32(data);
            data.Seek(8 * genCount + 0x30, SeekOrigin.Current);
            bulkDataStartOffset = Helper.ReadU64(data);
            ReadNameTable(data);
            ReadImportTable(data);
            ReadExportTable(data, uexp);
            if (ubulk != null)
                _ubulkData = ubulk.ToArray();
            _isValid = true;
        }

        public string ParseProperties(UExport ex, UAsset asset)
        {
            MemoryStream m = new MemoryStream(ex._data);
            StringBuilder sb = new StringBuilder();
            while ((ulong)m.Position < ex.dataSize)
            {
                long pos = m.Position;
                UProperty p = new UProperty(m, this);
                if (p.name == "None")
                    break;
                if (!p._isValid)
                {
                    sb.AppendLine("Error parsing at 0x" + pos.ToString("X") + " Name=" + p.name + " Type=" + p.type);
                    break;
                }
                sb.Append(p.prop.ToDetails(0, p._offset, p.name, asset));
            }
            return sb.ToString();
        }

        public string GetName(int idx)
        {
            if (idx >= 0 && idx < nameTable.Count)
                return nameTable[idx];
            if (idx < 0 && -idx <= importTable.Count)
                return importTable[-idx - 1]._name;
            return null;
        }

        public string GetClassName(int idx)//ExpressionInput
        {
            if (idx == 0)
                return "None";
            if (idx > 0)
                return exportTable[idx - 1]._name;
            else
                return importTable[-idx - 1]._name;
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
                exp._name = GetName(exp.nameIdx);
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

        private void ReadImportTable(Stream s)
        {
            importTable = new List<UImport>();
            s.Seek(importOffset, 0);
            for (int i = 0; i < importCount; i++)
                importTable.Add(new UImport(s));
            for (int i = 0; i < importCount; i++)
            {
                UImport imp = importTable[i];
                imp._className = GetName((int)imp.className);
                imp._name = GetName((int)imp.objectName);
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

    public class UImport
    {
        public ulong classPackage;
        public ulong className;
        public int packageIdx;
        public ulong objectName;

        public string _className;
        public string _name;
        public UImport(Stream s)
        {
            classPackage = Helper.ReadU64(s);
            className = Helper.ReadU64(s);
            packageIdx = (int)Helper.ReadU32(s);
            objectName = Helper.ReadU64(s);
        }
    }
}
