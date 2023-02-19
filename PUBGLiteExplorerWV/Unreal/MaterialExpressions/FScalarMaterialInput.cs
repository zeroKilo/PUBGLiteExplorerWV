using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class FScalarMaterialInput
    {
        public FExpressionInput baseData;
        public float scalar;
        public FScalarMaterialInput(Stream s, UAsset asset)
        {
            baseData = new FExpressionInput(s, asset);
            scalar = Helper.ReadFloat(s);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseData.ToString());
            sb.AppendLine(" Scalar = " + scalar);
            return sb.ToString();
        }
    }
}
