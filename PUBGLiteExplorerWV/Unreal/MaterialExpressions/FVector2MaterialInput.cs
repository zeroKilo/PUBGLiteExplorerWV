using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class FVector2MaterialInput
    {
        public FExpressionInput baseData;
        public float x, y;
        public FVector2MaterialInput(Stream s, UAsset asset)
        {
            baseData = new FExpressionInput(s, asset);
            x = Helper.ReadFloat(s);
            y = Helper.ReadFloat(s);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseData.ToString());
            sb.AppendLine(" Vector = (" + x + ";" + y + ")");
            return sb.ToString();
        }
    }
}
