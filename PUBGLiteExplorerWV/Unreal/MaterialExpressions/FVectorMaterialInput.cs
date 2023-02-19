using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class FVectorMaterialInput
    {
        public FExpressionInput baseData;
        public float x,y,z;
        public FVectorMaterialInput(Stream s, UAsset asset)
        {
            baseData = new FExpressionInput(s, asset);
            x = Helper.ReadFloat(s);
            y = Helper.ReadFloat(s);
            z = Helper.ReadFloat(s);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseData.ToString());
            sb.AppendLine(" Vector = (" + x + ";" + y + ";" + z + ")");
            return sb.ToString();
        }
    }
}
