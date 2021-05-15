using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PUBGLiteExplorerWV
{
    public class UStaticMeshLOD
    {
        public bool _valid = false;
        public List<float[]> vertices;
        public List<float[]> uvs;
        public List<ushort[]> sections;

        public UStaticMeshLOD(Stream s)
        {
            if (Helper.ReadU16(s) != 1)
                return;
            if (Helper.ReadU32(s) != 1)
                return;
            s.Seek(0x14, SeekOrigin.Current);
            if (Helper.ReadU32(s) != 1 || Helper.ReadU32(s) != 1 || Helper.ReadU32(s) != 0)
                return;
            uint size1 = Helper.ReadU32(s);
            uint count1 = Helper.ReadU32(s);
            uint size2 = Helper.ReadU32(s);
            uint count2 = Helper.ReadU32(s);
            if (count1 != count2 || size1 != size2 || size1 != 0xC)
                return;
            vertices = new List<float[]>();
            for (int i = 0; i < count1; i++)
            {
                float[] vec = new float[3];
                vec[0] = Helper.ReadFloat(s);
                vec[1] = Helper.ReadFloat(s);
                vec[2] = Helper.ReadFloat(s);
                vertices.Add(vec);
            }
            if (Helper.ReadU16(s) != 1)
                return;
            if (Helper.ReadU32(s) != 2)
                return;
            size1 = Helper.ReadU32(s);
            count1 = Helper.ReadU32(s);
            s.Seek(8, SeekOrigin.Current);
            size2 = Helper.ReadU32(s);
            count2 = Helper.ReadU32(s);
            if (count1 != count2 || size1 != size2 || size1 != 0x10)
                return;
            uvs = new List<float[]>();
            for (int i = 0; i < count1; i++)
            {
                float[] vec = new float[8];
                for (int j = 0; j < 8; j++)
                    vec[j] = Helper.Half2Float(Helper.ReadU16(s));
                uvs.Add(vec);
            }
            if (uvs.Count != vertices.Count)
                return;
            if (Helper.ReadU16(s) != 1)
                return;
            if (Helper.ReadU32(s) != 0 || Helper.ReadU32(s) != 0)
                return;
            sections = new List<ushort[]>();
            long size = s.Length;
            while (s.Position < size)
            {
                if (Helper.ReadU32(s) != 0 || Helper.ReadU32(s) != 1)
                    break;
                size1 = Helper.ReadU32(s);
                count1 = size1 / 2;
                ushort[] buf = new ushort[count1];
                for (int i = 0; i < count1; i++)
                    buf[i] = Helper.ReadU16(s);
                sections.Add(buf);
            }
            _valid = true;
        }

        public byte[] MakePSK(int uvset)
        {
            if(!_valid)
                return new byte[0];
            MemoryStream result = new MemoryStream();
            Helper.WriteCString(result, "ACTRHEAD");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteCString(result, "PNTS0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0xC);
            Helper.WriteU32(result, (uint)vertices.Count);
            foreach (float[] vec in vertices)
                foreach (float f in vec)
                    Helper.WriteFloat(result, f);
            Helper.WriteCString(result, "VTXW0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0x10);
            Helper.WriteU32(result, (uint)uvs.Count);
            uint index = 0;
            foreach (float[] uv in uvs)
            {
                Helper.WriteU32(result, index++);
                Helper.WriteFloat(result, uv[uvset * 2]);
                Helper.WriteFloat(result, uv[uvset * 2 + 1]);
                Helper.WriteU32(result, 0);
            }
            Helper.WriteCString(result, "FACE0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0xC);
            uint count = 0;
            foreach (ushort[] sec in sections)
                count += (uint)(sec.Length / 3);
            Helper.WriteU32(result, count);
            byte count2 = 0;
            foreach (ushort[] sec in sections)
            {
                count = (uint)sec.Length / 3;
                for (int i = 0; i < count; i++)
                {
                    Helper.WriteU16(result, sec[i * 3]);
                    Helper.WriteU16(result, sec[i * 3 + 1]);
                    Helper.WriteU16(result, sec[i * 3 + 2]);
                    result.WriteByte(count2);
                    result.WriteByte(0);
                    Helper.WriteU32(result, 1);
                }
                count2++;
            }
            count2 = 0;
            Helper.WriteCString(result, "MATT0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0x58);
            Helper.WriteU32(result, (uint)sections.Count);
            foreach (ushort[] sec in sections)
            {
                string matName = "material_"+ count2;
                byte[] buff = new byte[0x58];
                for (int i = 0; i < matName.Length; i++)
                    buff[i] = (byte)matName[i];
                result.Write(buff, 0, 0x58);
                count2++;
            }
            Helper.WriteCString(result, "REFSKELT");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0x78);
            Helper.WriteCString(result, "RAWWEIGH");
            Helper.WriteU64(result, 0x5354);
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0xC);
            return result.ToArray();
        }
    }
}
