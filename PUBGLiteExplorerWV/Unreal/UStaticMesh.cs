using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UStaticMesh
    {
        public List<UProperty> props = new List<UProperty>();
        public List<byte[]> lodRawData = new List<byte[]>();

        public UStaticMesh(Stream s, UAsset asset, MemoryStream ubulk)
        {
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            if (ubulk == null)
                return;
            s.Seek(0x22, SeekOrigin.Current);
            uint count = Helper.ReadU32(s);
            for (int i = 0; i < count; i++)
            {
                uint unk1 = Helper.ReadU32(s);
                uint size1 = Helper.ReadU32(s);
                uint size2 = Helper.ReadU32(s);
                if (size1 != size2)
                    return;
                long offset = (long)Helper.ReadU64(s);
                offset += (long)asset.bulkDataStartOffset;
                ubulk.Seek(offset, 0);
                byte[] buff = new byte[size1];
                ubulk.Read(buff, 0, (int)size1);
                lodRawData.Add(buff);
            }
        }
    }
}
