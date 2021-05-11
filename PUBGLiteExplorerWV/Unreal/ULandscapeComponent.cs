using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class ULandscapeComponent
    {
        public List<UProperty> props;

        public byte[] data;
        public ULandscapeComponent(Stream s, UAsset asset)
        {
            props = new List<UProperty>();
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            s.Seek(16, SeekOrigin.Current);
            uint size = Helper.ReadU32(s);
            uint count = Helper.ReadU32(s);
            if(size != 2)
            {
                data = new byte[0];
                return;
            }
            data = new byte[size * count];
            s.Read(data, 0, (int)(size * count));
        }
    }
}
