using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class FExpressionInput
    {
        public uint outputIndex;
        public string inputName;
        public uint mask;
        public uint[] maskRGBA = new uint[4];
        public string expressionName;
        public uint index;
        public bool useConstant = false;
        public FExpressionInput(Stream s, UAsset asset)
        {
            outputIndex = Helper.ReadU32(s);
            int len = (int)Helper.ReadU32(s);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
                sb.Append((char)s.ReadByte());
            inputName = sb.ToString().Trim();
            mask = Helper.ReadU32(s);
            for (int i = 0; i < 4; i++)
                maskRGBA[i] = Helper.ReadU32(s);
            expressionName = asset.GetName((int)Helper.ReadU32(s));
            index = Helper.ReadU32(s);
            if (s.Position < s.Length)
                useConstant = Helper.ReadU32(s) == 1;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" Output Index = 0x" + outputIndex.ToString("X"));
            sb.AppendLine(" Input Name = \"" + inputName + "\"");
            sb.AppendLine(" Mask = 0x" + mask.ToString("X"));
            sb.AppendLine(" Mask RGBA = (" + maskRGBA[0].ToString("X") + ";" + maskRGBA[1].ToString("X") + ";" + maskRGBA[2].ToString("X") + ";" + maskRGBA[3].ToString("X") + ") ");
            sb.AppendLine(" Expression Name = \"" + expressionName + "\"");
            sb.AppendLine(" Index = 0x" + index.ToString("X"));
            sb.AppendLine(" Use Constant = " + useConstant);
            return sb.ToString();
        }
    }
}
