using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UInstancedFoliageActor
    {
        public class FoliageInfo
        {
            public uint unk1;
            public uint exportIndex;
            public UHirarchicalInstancedStaticMeshComponent hismc;
            public string name = "Null";
            public FoliageInfo(Stream s, UAsset asset, MemoryStream ubulk)
            {
                int objIndex = 0;
                unk1 = Helper.ReadU32(s);
                exportIndex = Helper.ReadU32(s);
                if (exportIndex > 0)
                {
                    hismc = new UHirarchicalInstancedStaticMeshComponent(new MemoryStream(asset.exportTable[(int)exportIndex - 1]._data), asset, ubulk);
                    foreach(UProperty prop in hismc.props)
                        if(prop.name == "StaticMesh")
                        {
                            name = ((UObjectProperty)prop.prop).objName;
                            objIndex = -((UObjectProperty)prop.prop).value - 1;
                            break;
                        }
                    if(name == "Desert_Cover_Rock_Combine")
                    {
                        List<int> importIndicies = new List<int>();
                        for (int i = 0; i < asset.importCount; i++)
                        {
                            UImport imp = asset.importTable[i];
                            if (imp._name == "Desert_Cover_Rock_Combine")
                                importIndicies.Add(i);
                        }
                        name += "_" + (importIndicies.IndexOf(objIndex) + 1);
                    }
                }
            }
        }

        public List<UProperty> props;
        public List<FoliageInfo> foliageInfo;
        public UInstancedFoliageActor(Stream s, UAsset asset, MemoryStream ubulk)
        {
            props = new List<UProperty>();
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            uint count = Helper.ReadU32(s);
            foliageInfo = new List<FoliageInfo>();
            for (int i = 0; i < count; i++) 
                foliageInfo.Add(new FoliageInfo(s, asset, ubulk));
        }

        public string GetDetails(bool verbose = true)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (FoliageInfo info in foliageInfo)
            {
                sb.AppendLine("\t\tFoliage " + count++ + ": " + info.name);
                if (info.hismc != null && verbose)
                    sb.AppendLine(info.hismc.GetDetails());
            }
            return sb.ToString();
        }
    }
}
