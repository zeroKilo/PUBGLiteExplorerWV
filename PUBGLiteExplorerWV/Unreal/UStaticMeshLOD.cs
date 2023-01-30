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
        public class MaterialInfo
        {
            public uint matIndex;
            public uint unk2;
            public uint unk3;
            public uint start;
            public uint end;
            public uint unk4;
            public uint unk5;
            public MaterialInfo(Stream s)
            {
                matIndex = Helper.ReadU32(s);
                unk2 = Helper.ReadU32(s);
                unk3 = Helper.ReadU32(s);
                start = Helper.ReadU32(s);
                end = Helper.ReadU32(s);
                unk4 = Helper.ReadU32(s);
                unk5 = Helper.ReadU32(s);
            }
        }

        public bool _valid = false;
        public List<MaterialInfo> matInfo;
        public List<float[]> vertices;
        public List<float[]> uvs;
        public List<uint> colors;
        public List<ushort[]> sections;
        public UStaticMesh parent;

        public UStaticMeshLOD(Stream s, UStaticMesh p)
        {
            parent = p;
            if (Helper.ReadU16(s) != 1)
                return;
            uint matCount = Helper.ReadU32(s);
            if (matCount > 10)
                return;
            matInfo = new List<MaterialInfo>();
            for (int i = 0; i < matCount; i++)
                matInfo.Add(new MaterialInfo(s));
            if (Helper.ReadU32(s) != 0)
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
            uint unk1 = Helper.ReadU32(s);
            size1 = Helper.ReadU32(s);
            count1 = Helper.ReadU32(s);
            s.Seek(8, SeekOrigin.Current);
            size2 = Helper.ReadU32(s);
            count2 = Helper.ReadU32(s);
            if (count1 != count2 || size1 != size2)
                return;
            uvs = new List<float[]>();
            for (int i = 0; i < count1; i++)
            {
                float[] vec = new float[size1 / 2];
                ushort[] tmp = new ushort[size1 / 2];
                for (int j = 0; j < size1 / 2; j++)
                    tmp[j] = Helper.ReadU16(s);
                    for (int j = 0; j < 4; j++)
                        vec[j] = (tmp[j]) / (float)0xFFFF;
                    for (int j = 4; j < size1 / 2; j++)
                        vec[j] = Helper.Half2Float(tmp[j]);
                uvs.Add(vec);
            }
            if (Helper.ReadU16(s) != 1)
                return;
            size1 = Helper.ReadU32(s);
            count1 = Helper.ReadU32(s);
            if (size1 != 0 && count1 != 0)
            {
                size2 = Helper.ReadU32(s);
                count2 = Helper.ReadU32(s);
                if (count1 != count2 || size1 != size2)
                    return;
                if(size1 == 4)
                {
                    colors = new List<uint>();
                    for (int i = 0; i < count1; i++)
                        colors.Add(Helper.ReadU32(s));
                }
                else
                    s.Seek(size1 * count1, SeekOrigin.Current);
            }
            if (uvs.Count != vertices.Count)
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

        public byte[] MakePSK()
        {
            if(!_valid)
                return new byte[0];
            List<float[]> tmpVerts = new List<float[]>();
            List<float[]> tmpUVs = new List<float[]>();
            List<byte> tmpMatIndex = new List<byte>();
            foreach (ushort u in sections[0])
            {
                tmpVerts.Add(vertices[u]);
                tmpUVs.Add(uvs[u]);
                for (byte i = 0; i < matInfo.Count; i++)
                    if (u >= matInfo[i].start && u <= matInfo[i].end)
                    {
                        tmpMatIndex.Add(i);
                        break;
                    }
            }
            if (tmpVerts.Count != tmpMatIndex.Count)
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
            Helper.WriteU32(result, (uint)tmpVerts.Count);
            foreach (float[] vec in tmpVerts)
                foreach (float f in vec)
                    Helper.WriteFloat(result, f);
            Helper.WriteCString(result, "VTXW0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0x10);
            Helper.WriteU32(result, (uint)tmpUVs.Count);
            uint index = 0;
            foreach (float[] uv in tmpUVs)
            {
                Helper.WriteU32(result, index);
                Helper.WriteFloat(result, uv[0]);
                Helper.WriteFloat(result, uv[1]);
                Helper.WriteU32(result, tmpMatIndex[(int)index++]);
            }
            Helper.WriteCString(result, "FACE0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0xC);
            Helper.WriteU32(result, (uint)(tmpVerts.Count / 3));
            ushort currIdx = 0;
            for (int i = 0; i < sections[0].Length / 3; i++)
            {
                byte matIdx = tmpMatIndex[currIdx];
                Helper.WriteU16(result, currIdx++);
                Helper.WriteU16(result, currIdx++);
                Helper.WriteU16(result, currIdx++);
                result.WriteByte(matIdx);
                result.WriteByte(0);
                Helper.WriteU32(result, 1);
            }
            Helper.WriteCString(result, "MATT0000");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU32(result, 0x58);
            Helper.WriteU32(result, (uint)matInfo.Count);
            currIdx = 0;
            foreach (MaterialInfo info in matInfo)
            {
                string matName;
                if (currIdx < parent.materialNames.Count)
                    matName = parent.materialNames[(int)matInfo[currIdx].matIndex];
                else
                    matName = "material_" + currIdx;
                byte[] buff = new byte[0x58];
                for (int i = 0; i < matName.Length; i++)
                    buff[i] = (byte)matName[i];
                result.Write(buff, 0, 0x58);
                currIdx++;
            }
            Helper.WriteCString(result, "REFSKELT");
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0x78);
            Helper.WriteCString(result, "RAWWEIGH");
            Helper.WriteU64(result, 0x5354);
            Helper.WriteU64(result, 0);
            Helper.WriteU64(result, 0xC);
            int extraCount = 0;
            if (tmpUVs.Count > 0)
                extraCount = tmpUVs[0].Length / 2 - 1;
            for (int i = 0; i < extraCount; i++)
            {
                Helper.WriteCString(result, "EXTRAUVS" + i);
                for (int j = 0; j < 7; j++)
                    result.WriteByte(0);
                Helper.WriteU64(result, 0);
                Helper.WriteU32(result, 0x8);
                Helper.WriteU32(result, (uint)tmpUVs.Count);
                foreach (float[] uv in tmpUVs)
                {
                    Helper.WriteFloat(result, uv[2 + i * 2]);
                    Helper.WriteFloat(result, uv[3 + i * 2]);
                }
            }
            return result.ToArray();
        }
    }
}
