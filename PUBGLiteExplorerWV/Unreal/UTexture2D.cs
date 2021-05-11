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
                    mips.Add(new UTexture2DMipMap(s, asset, offset, ubulk));
            }
            else
            {
                mips = new List<UTexture2DMipMap>();
                for (int i = 0; i < numMips; i++)
                    mips.Add(new UTexture2DMipMap(s, asset, offset, null));
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

        public UTexture2DMipMap(Stream s, UAsset asset, uint off, MemoryStream ubulk)
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
    }
}
