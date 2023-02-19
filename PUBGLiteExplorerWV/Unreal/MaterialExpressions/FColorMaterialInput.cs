using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class FColorMaterialInput
    {
        public FExpressionInput baseData;
        public uint color;
        public FColorMaterialInput(Stream s, UAsset asset)
        {
            baseData = new FExpressionInput(s, asset);
            color = Helper.ReadU32(s);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseData.ToString());
            sb.AppendLine(" Color = 0x" + color.ToString("X8"));
            return sb.ToString();
        }
    }
}
