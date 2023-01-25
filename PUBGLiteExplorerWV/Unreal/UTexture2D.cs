using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UTexture2D
    {
        public List<UProperty> props;
        
        public ushort unk1;
        public ushort unk2;
        public uint unk3;
        public string pixelFormat;
        public uint offset;
        public uint width;
        public uint height;
        public uint numSlices;
        public string format;
        public uint firstMip;
        public uint numMips;
        public List<UTexture2DMipMap> mips;
        public string unk4;

        public UTexture2D(Stream s, UAsset asset, MemoryStream ubulk)
        {
            props = new List<UProperty>();
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            unk1 = Helper.ReadU16(s);
            unk2 = Helper.ReadU16(s);
            unk3 = Helper.ReadU32(s);
            pixelFormat = asset.GetName((int)Helper.ReadU64(s));
            offset = Helper.ReadU32(s);
            width = Helper.ReadU32(s);
            height = Helper.ReadU32(s);
            numSlices = Helper.ReadU32(s);
            format = Helper.ReadUString(s);
            firstMip = Helper.ReadU32(s);
            numMips = Helper.ReadU32(s);
            if (ubulk != null)
            {
                mips = new List<UTexture2DMipMap>();
                for (int i = 0; i < numMips; i++)
                    mips.Add(new UTexture2DMipMap(s, asset, ubulk));
            }
            else
            {
                mips = new List<UTexture2DMipMap>();
                for (int i = 0; i < numMips; i++)
                    mips.Add(new UTexture2DMipMap(s, asset, null));
            }
            unk4 = asset.GetName((int)Helper.ReadU64(s));
        }
    }

    public class UTexture2DMipMap
    {
        public uint unk1;
        public uint unk2;
        public uint size1;
        public uint size2;
        public long offset;
        public byte[] data;
        public uint width;
        public uint height;

        public UTexture2DMipMap(Stream s, UAsset asset, MemoryStream ubulk)
        {
            unk1 = Helper.ReadU32(s);
            unk2 = Helper.ReadU32(s);
            size1 = Helper.ReadU32(s);
            size2 = Helper.ReadU32(s);
            offset = (long)Helper.ReadU64(s);
            offset += (long)asset.bulkDataStartOffset;
            data = new byte[size1];
            if (ubulk == null)
                s.Read(data, 0, (int)size1);
            else
            {
                ubulk.Seek(offset, 0);
                ubulk.Read(data, 0, (int)size1);
            }
            width = Helper.ReadU32(s);
            height = Helper.ReadU32(s);
        }

        public Bitmap MakeBitmap()
        {
            Bitmap result = new Bitmap((int)width, (int)height);
            for(int y = 0; y < height;y++)
                for (int x = 0; x < width; x++)
                    result.SetPixel(x, y, Color.FromArgb(BitConverter.ToInt32(data, (int)(y * width * 4 + x * 4))));
            return result;
        }

        public byte[] MakeDDS(UTexture2D tex, UExport ex)
        {
            MemoryStream result = new MemoryStream();
            byte[] header0, header1;
            switch (tex.format)
            {
                case "PF_DXT1":
                    header0 = new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 
                                           0x07, 0x10, 0x0A, 0x00 };
                    header1 = new byte[] { 0x00, 0x00, 0x08, 0x00, 0x01, 0x00, 0x00, 0x00, 
                                           0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 
                                           0x44, 0x58, 0x54, 0x31, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00 };
                    result.Write(header0, 0, header0.Length);
                    Helper.WriteU32(result, height);
                    Helper.WriteU32(result, width);
                    result.Write(header1, 0, header1.Length);
                    result.Write(data, 0, data.Length);
                    break;
                case "PF_BC5":
                    header0 = new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 
                                           0x07, 0x10, 0x0A, 0x00 };
                    header1 = new byte[] { 0x00, 0x00, 0x10, 0x00, 0x01, 0x00, 0x00, 0x00, 
                                           0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 
                                           0x42, 0x43, 0x35, 0x55, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                           0x00, 0x00, 0x00, 0x00 };
                    result.Write(header0, 0, header0.Length);
                    Helper.WriteU32(result, height);
                    Helper.WriteU32(result, width);
                    result.Write(header1, 0, header1.Length);
                    result.Write(data, 0, data.Length);
                    break;
                default:
                    return new byte[0];
            }
            return result.ToArray();
        }
    }
}
