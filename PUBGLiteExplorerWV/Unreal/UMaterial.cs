using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UMaterial
    {
        public List<UProperty> props = new List<UProperty>();
        public UAsset myAsset;

        public List<uint> expressionIDs = new List<uint>();
        public UMaterial(Stream s, UAsset asset)
        {
            myAsset = asset;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            UArrayProperty expressions = (UArrayProperty)Helper.FindPropByName(props, "Expressions");
            if (expressions == null)
                return;
            expressionIDs = new List<uint>();
            MemoryStream m = new MemoryStream(expressions.data);
            uint count = Helper.ReadU32(m);
            for (int i = 0; i < count; i++)
            {
                uint ID = Helper.ReadU32(m);
                if (ID > 0)
                    expressionIDs.Add(ID - 1);
            }
        }

        public string MakeDotGraph()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("graph Material {");
            foreach (uint ID in expressionIDs)
                DumpMaterialExpressionNodes(sb, myAsset, ID);
            foreach (uint ID in expressionIDs)
                DumpMaterialExpressionEdges(sb, myAsset, ID);
            sb.AppendLine("}");
            return sb.ToString();
        }
        public void DumpMaterialExpressionNodes(StringBuilder sb, UAsset asset, uint exportID)
        {
            sb.Append("N" + exportID + "[shape=box label=\"");
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch (cname)
            {
                case "MaterialExpressionTextureSampleParameter2D":
                    UMaterialExpressionTextureSampleParameter2D metsp2d = new UMaterialExpressionTextureSampleParameter2D(new MemoryStream(export._data), asset);
                    metsp2d.DumpNodeText(sb);
                    break;
                default:
                    sb.Append(cname);
                    break;
            }
            sb.AppendLine("\"]");
        }
        public void DumpMaterialExpressionEdges(StringBuilder sb, UAsset asset, uint exportID)
        {
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch (cname)
            {
                case "MaterialExpressionTextureSampleParameter2D":
                    UMaterialExpressionTextureSampleParameter2D metsp2d = new UMaterialExpressionTextureSampleParameter2D(new MemoryStream(export._data), asset);
                    metsp2d.DumpEdgeText(sb);
                    break;
                default:
                    break;
            }
        }
    }
}
