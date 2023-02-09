using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UMaterialExpressionTextureSampleParameter2D
    {
        public List<UProperty> props = new List<UProperty>();
        public UAsset myAsset;
        public UMaterialExpressionTextureSampleParameter2D(Stream s, UAsset asset)
        {
            myAsset = asset;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
        }

        public void DumpNodeText(StringBuilder sb)
        {
            sb.Append("MaterialExpressionTextureSampleParameter2D\\n");
            foreach (UProperty p in props)
                sb.Append(p.name + "\\n");
        }

        public void DumpEdgeText(StringBuilder sb)
        {

        }
    }
}
