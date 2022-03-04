using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class URandomPositionPlayerStart
    {
        public List<UProperty> props = new List<UProperty>();
        public float[] location = new float[3];
        public URandomPositionPlayerStart(Stream s, UAsset asset, MemoryStream ubulk)
        {
            int capsuleObject = 0;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                if (p.name == "CapsuleComponent")
                    capsuleObject = ((UObjectProperty)p.prop).value;
                props.Add(p);
            }
            if (capsuleObject > 0)
            {
                UExport exp = asset.exportTable[capsuleObject - 1];
                MemoryStream m = new MemoryStream(exp._data);
                while (true)
                {
                    UProperty p = new UProperty(m, asset);
                    if (p.name == "None")
                        break;
                    if (p.name == "RelativeLocation")
                    {
                        m = new MemoryStream(((UStructProperty)p.prop).data);
                        location = Helper.swapYZ(new float[] { Helper.ReadFloat(m) * 0.01f, Helper.ReadFloat(m) * 0.01f, Helper.ReadFloat(m) * 0.01f });
                        break;
                    }
                }
            }
        }

        public string GetDetails()
        {
            return "\tLocation = " + Helper.MakeVector(location);
        }
    }
}
