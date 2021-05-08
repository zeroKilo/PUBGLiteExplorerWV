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
        public string name;
        public string type;
        public UProp prop;

        public bool _isValid = false;

        public UProperty(Stream s, UAsset asset)
        {
            name = asset.GetName((int)Helper.ReadU64(s));
            if (name == "None")
            {
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
                default:
                    return false;
            }
            return true;
        }
    }

    public abstract class UProp
    {
        public abstract string ToDetails(string name);
    }

    public class UStructProperty : UProp
    {
        public string structType;
        public byte[] data;

        public UStructProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            structType = asset.GetName((int)Helper.ReadU64(s));
            byte[] unk = new byte[0x11];
            s.Read(unk, 0, 0x11);
            data = new byte[size];
            s.Read(data, 0, (int)size);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " (StructProperty " + structType + ") = {");
            switch (structType)
            {
                case "Vector":
                    MemoryStream m = new MemoryStream(data);
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m) + "; ");
                    sb.Append(Helper.ReadFloat(m));
                    break;
                default:
                    foreach (byte b in data)
                        sb.Append(" " + b.ToString("X2"));
                    break;
            }
            sb.Append("}");
            return sb.ToString();
        }
    }

    public class UObjectProperty : UProp
    {
        public int value;
        public string objName;

        public UObjectProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = (int)Helper.ReadU32(s);
            objName = "";
            if (value > 0 && value <= asset.exportCount)
                objName = asset.exportTable[(int)value - 1]._name;
            if (value < 0 && -value <= asset.importCount)
                objName = asset.importTable[(int)-value - 1]._name;
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " ObjectProperty = 0x" + value.ToString("X8") + " (" + objName + ")");
            return sb.ToString();
        }
    }

    public class UFloatProperty : UProp
    {
        public float value;

        public UFloatProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = Helper.ReadFloat(s);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " FloatProperty = " + value);
            return sb.ToString();
        }
    }

    public class UStrProperty : UProp
    {
        public string value;

        public UStrProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = Helper.ReadUString(s);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " StrProperty = " + value);
            return sb.ToString();
        }
    }

    public class UBoolProperty : UProp
    {
        public bool value;

        public UBoolProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = s.ReadByte() == 1;
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " BoolProperty = " + (value ? "True" : "False"));
            return sb.ToString();
        }
    }

    public class UByteProperty : UProp
    {
        public string type;
        public byte value;

        public UByteProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            value = (byte)s.ReadByte();
            s.Seek(7, SeekOrigin.Current);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " ByteProperty (" + type + ") = 0x" + value.ToString("X2"));
            return sb.ToString();
        }
    }

    public class UIntProperty : UProp
    {
        public int value;

        public UIntProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = (int)Helper.ReadU32(s);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " IntProperty = 0x" + value.ToString("X8") + " = " + value);
            return sb.ToString();
        }
    }

    public class UUInt32Property : UProp
    {
        public uint value;

        public UUInt32Property(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = Helper.ReadU32(s);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " UInt32Property = 0x" + value.ToString("X8") + " = " + value);
            return sb.ToString();
        }
    }

    public class UEnumProperty : UProp
    {
        public string type;
        public string value;

        public UEnumProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            value = asset.GetName((int)Helper.ReadU64(s));
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " EnumProperty (" + type + ") = " + value);
            return sb.ToString();
        }
    }

    public class UArrayProperty : UProp
    {
        public string type;
        public byte[] data;


        public UArrayProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            type = asset.GetName((int)Helper.ReadU64(s));
            s.ReadByte();
            data = new byte[size];
            s.Read(data, 0, (int)size);
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " ArrayProperty (" + type + ") Size = 0x" + data.Length.ToString("X"));
            return sb.ToString();
        }
    }

    public class UNameProperty : UProp
    {
        public string value;

        public UNameProperty(Stream s, UAsset asset)
        {
            uint size = Helper.ReadU32(s);
            uint flags = Helper.ReadU32(s);
            s.ReadByte();
            value = asset.GetName((int)Helper.ReadU64(s));
        }

        public override string ToDetails(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name + " NameProperty = " + value);
            return sb.ToString();
        }
    }
}
