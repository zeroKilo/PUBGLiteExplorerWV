using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UProperty
    {
        public long _offset;
        public string name;
        public string type;
        public UProp prop;

        public bool _isValid = false;

        public UProperty(Stream s, UAsset asset)
        {
            _offset = s.Position;
            name = asset.GetName((int)Helper.ReadU64(s));
            if(name == null)
            {
                _isValid = false;
                return;
            }
            if (name == "None")
            {
                s.Seek(4, SeekOrigin.Current);
                type = "None";
                _isValid = true;
                return;
            }
            type = asset.GetName((int)Helper.ReadU64(s));
            _isValid = Parse(s, asset);
        }

        public bool Parse(Stream s, UAsset asset)
        {
            switch (type)
            {
                case "StructProperty":
                    prop = new UStructProperty(s, asset);
                    break;
                case "ObjectProperty":
                    prop = new UObjectProperty(s, asset);
                    break;
                case "FloatProperty":
                    prop = new UFloatProperty(s, asset);
                    break;
                case "StrProperty":
                    prop = new UStrProperty(s, asset);
                    break;
                case "BoolProperty":
                    prop = new UBoolProperty(s, asset);
                    break;
                case "ByteProperty":
                    prop = new UByteProperty(s, asset);
                    break;
                case "IntProperty":
                    prop = new UIntProperty(s, asset);
                    break;
                case "UInt32Property":
                    prop = new UUInt32Property(s, asset);
                    break;
                case "EnumProperty":
                    prop = new UEnumProperty(s, asset);
                    break;
                case "ArrayProperty":
                    prop = new UArrayProperty(s, asset);
                    break;
                case "NameProperty":
                    prop = new UNameProperty(s, asset);
                    break;
                case "LazyObjectProperty":
                    prop = new ULazyObjectProperty(s, asset);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }

    public abstract class UProp
    {
        public uint size;
        public uint flags;
        public abstract string ToDetails(int tabs, long offset, string name);

        public string MakeTabs(int tabs)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tabs; i++)
                sb.Append('\t');
            return sb.ToString();
        }
        public void ReadSizeAndFlags(Stream s)
        {
            size = Helper.ReadU32(s);
            flags = Helper.ReadU32(s);
        }
    }

    public class UStructProperty : UProp
    {
        public string structType;
        public byte[] data;
        public List<UProperty> subProps = new List<UProperty>();

        public UStructProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            structType = asset.GetName((int)Helper.ReadU64(s));
            byte[] unk = new byte[0x11];
            s.Read(unk, 0, 0x11);
            data = new byte[size];
            s.Read(data, 0, (int)size);
            MemoryStream m = new MemoryStream(data);
            while(m.Position < data.Length)
            {
                try
                {
                    UProperty p = new UProperty(m, asset);
                    if (!p._isValid || p.name == "None")
                        break;
                    subProps.Add(p);
                }
                catch { break; }
            }
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            MemoryStream m;
            sb.Append(MakeTabs(tabs));
            sb.Append(offset.ToString("X8") + " : " + name + " (StructProperty " + structType + ") = {");
            switch (structType)
            {
                case "Vector":
                    m = new MemoryStream(data);
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m));
                    break;
                case "Vector4":
                case "LinearColor":
                    m = new MemoryStream(data);
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m));
                    break;
                default:
                    foreach (byte b in data)
                        sb.Append(" " + b.ToString("X2"));
                    break;
            }
            sb.AppendLine("}");
            foreach (UProperty p in subProps)
                sb.Append(p.prop.ToDetails(tabs + 1, p._offset, p.name));
            return sb.ToString();
        }
    }

    public class UObjectProperty : UProp
    {
        public int value;
        public string objName;

        public UObjectProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = (int)Helper.ReadU32(s);
            objName = "";
            if (value == 0)
                objName = "None";
            if (value > 0 && value <= asset.exportCount)
                objName = asset.exportTable[(int)value - 1]._name;
            if (value < 0 && -value <= asset.importCount)
                objName = asset.importTable[(int)-value - 1]._name;
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " ObjectProperty = 0x" + value.ToString("X8") + " (" + objName + ")");
            return sb.ToString();
        }
    }

    public class UFloatProperty : UProp
    {
        public float value;

        public UFloatProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = Helper.ReadFloat(s);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " FloatProperty = " + value);
            return sb.ToString();
        }
    }

    public class UStrProperty : UProp
    {
        public string value;

        public UStrProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = Helper.ReadUString(s);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " StrProperty = " + value);
            return sb.ToString();
        }
    }

    public class UBoolProperty : UProp
    {
        public bool value;

        public UBoolProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            value = s.ReadByte() == 1;
            s.ReadByte();
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " BoolProperty = " + (value ? "True" : "False"));
            return sb.ToString();
        }
    }

    public class UByteProperty : UProp
    {
        public string type;
        public byte value;

        public UByteProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            value = (byte)s.ReadByte();
            s.Seek(size - 1, SeekOrigin.Current);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " ByteProperty (" + type + ") = 0x" + value.ToString("X2"));
            return sb.ToString();
        }
    }

    public class UIntProperty : UProp
    {
        public int value;

        public UIntProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = (int)Helper.ReadU32(s);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " IntProperty = 0x" + value.ToString("X8") + " = " + value);
            return sb.ToString();
        }
    }

    public class UUInt32Property : UProp
    {
        public uint value;

        public UUInt32Property(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = Helper.ReadU32(s);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " UInt32Property = 0x" + value.ToString("X8") + " = " + value);
            return sb.ToString();
        }
    }

    public class UEnumProperty : UProp
    {
        public string type;
        public string value;

        public UEnumProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            value = asset.GetName((int)Helper.ReadU64(s));
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " EnumProperty (" + type + ") = " + value);
            return sb.ToString();
        }
    }

    public class UArrayProperty : UProp
    {
        private UAsset myAsset;
        public string type;
        public byte[] data;
        public List<UProperty> subProps = new List<UProperty>();

        public UArrayProperty(Stream s, UAsset asset)
        {
            myAsset = asset;
            ReadSizeAndFlags(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            data = new byte[size];
            s.Read(data, 0, (int)size);
            MemoryStream m = new MemoryStream(data);
            uint count = Helper.ReadU32(m);
            for (int i = 0; i < count; i++)
            {
                if (m.Position >= data.Length)
                    break;
                UProperty p = new UProperty(m, asset);
                if (!p._isValid)
                    break;
                subProps.Add(p);
            }
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " ArrayProperty (" + type + ") Size = 0x" + data.Length.ToString("X"));
            foreach (UProperty p in subProps)
                sb.Append(p.prop.ToDetails(tabs + 1, p._offset, p.name));
            if (type == "ObjectProperty" && data.Length >= 4 && data.Length % 4 == 0)
            {
                MemoryStream m = new MemoryStream(data);
                while (m.Position < data.Length)
                {
                    int index = (int)Helper.ReadU32(m);
                    if (index > 0)
                    {
                        index--;
                        sb.Append(MakeTabs(tabs));
                        sb.AppendLine(" - Export 0x" + index.ToString("X") + " " + myAsset.exportTable[index]._name);
                    }
                    if (index < 0)
                    {
                        index = -index - 1;
                        sb.Append(MakeTabs(tabs));
                        sb.AppendLine(" - Import 0x" + index.ToString("X") + " " + myAsset.importTable[index]._name);
                    }
                }
            }
            return sb.ToString();
        }
    }

    public class UNameProperty : UProp
    {
        public string value;

        public UNameProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = asset.GetName((int)Helper.ReadU64(s));
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.AppendLine(offset.ToString("X8") + " : " + name + " NameProperty = " + value);
            return sb.ToString();
        }
    }

    public class ULazyObjectProperty : UProp
    {
        public byte[] value;

        public ULazyObjectProperty(Stream s, UAsset asset)
        {
            ReadSizeAndFlags(s);
            s.ReadByte();
            value = new byte[size];
            s.Read(value, 0, (int)size);
        }

        public override string ToDetails(int tabs, long offset, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MakeTabs(tabs));
            sb.Append(offset.ToString("X8") + " : " + name + " LazyObjectProperty = {");
            foreach (byte b in value)
                sb.Append(b.ToString("X2") + " ");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
