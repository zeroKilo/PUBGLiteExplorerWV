using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class ULandscapeMaterialInstanceConstant
    {
        public List<UProperty> props;

        public ULandscapeMaterialInstanceConstant(Stream s, UAsset asset)
        {
            props = new List<UProperty>();
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
        }

        public int FindChannelIndex(string layerName)
        {
            int[] remap = { 1, 2, 3, 0 };
            layerName = layerName.Replace("_LayerInfo", "");
            UArrayProperty vpv = (UArrayProperty)Helper.FindPropByName(props, "VectorParameterValues");
            int layerCount = vpv.subProps.Count;
            for (int i = 0; i < layerCount; i++)
            {
                UStructProperty struc = (UStructProperty)vpv.subProps[i].prop;
                UNameProperty name = (UNameProperty)struc.subProps[0].prop;
                if(name.value.Replace("LayerMask_", "") == layerName)
                {
                    UStructProperty vector = (UStructProperty)struc.subProps[1].prop;
                    for (int j = 0; j < 4; j++)
                        if (BitConverter.ToSingle(vector.data, j * 4) != 0)
                            return remap[j];
                }
            }
            return -1;
        }
    }
}
