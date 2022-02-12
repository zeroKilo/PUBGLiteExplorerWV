using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace PUBGLiteExplorerWV
{

    public static class Helper
    {
        public static ushort ReadU16(Stream s)
        {
            byte[] buff = new byte[2];
            s.Read(buff, 0, 2);
            return BitConverter.ToUInt16(buff, 0);
        }

        public static uint ReadU32(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToUInt32(buff, 0);
        }

        public static ulong ReadU64(Stream s)
        {
            byte[] buff = new byte[8];
            s.Read(buff, 0, 8);
            return BitConverter.ToUInt64(buff, 0);
        }

        public static float ReadFloat(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToSingle(buff, 0);
        }

        public static string ReadUString(Stream s)
        {
            int len = (int)ReadU32(s);
            StringBuilder sb = new StringBuilder();
            if (len > 0)
            {
                for (int i = 0; i < len - 1; i++)
                    sb.Append((char)s.ReadByte());
                s.ReadByte();
            }
            else
            {
                for (int i = 0; i < -len - 1; i++)
                    sb.Append((char)ReadU16(s));
                ReadU16(s);
            }
            return sb.ToString();
        }

        public static void WriteU16(Stream s, ushort u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 2);
        }

        public static void WriteU32(Stream s, uint u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 4);
        }

        public static void WriteU64(Stream s, ulong u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 8);
        }

        public static void WriteFloat(Stream s, float f)
        {
            s.Write(BitConverter.GetBytes(f), 0, 4);
        }

        public static void WriteCString(Stream s, string str)
        {
            foreach(char c in str)
                s.WriteByte((byte)c);
        }

        public static float Half2Float(ushort h)
        {   
	        int sign = (h >> 15) & 0x00000001;
	        int exp  = (h >> 10) & 0x0000001F;
	        int mant =  h        & 0x000003FF;
	        exp  = exp + (127 - 15);
	        uint tmp = (uint)((sign << 31) | (exp << 23) | (mant << 13));
            byte[] buff = BitConverter.GetBytes(tmp);
            return BitConverter.ToSingle(buff, 0);
        }

        public static byte[] Decompress(byte[] data)
        {
            byte[] result = null;
            using (InflaterInputStream inf = new InflaterInputStream(new MemoryStream(data)))
            {
                MemoryStream m = new MemoryStream();
                inf.CopyTo(m);
                result = m.ToArray();
            }
            return result;
        }

        public static float[] UnrealToUnity(float[] vec)
        {
            return new float[] { vec[0] * 0.01f, vec[2] * 0.01f, -vec[1] * 0.01f };
        }

        public static string comma2dot(float f)
        {
            return f.ToString().Replace(",", ".");
        }

        public static void ReadUnrealVector3(MemoryStream m, StringBuilder sb, string name, bool convert)
        {
            float[] vec = new float[] { Helper.ReadFloat(m), Helper.ReadFloat(m), Helper.ReadFloat(m) };
            if (convert)
                vec = UnrealToUnity(vec);
            sb.AppendLine("\t" + name + " : " + MakeVector(vec));
        }
        public static float[] GetPosFromMatrix(float[] mat)
        {
            float[] pos = new float[3];
            for (int i = 0; i < 3; i++)
                pos[i] = mat[i + 12];
            return pos;
        }

        public static float[] GetScaleFromMatrix(float[] mat)
        {
            float[] scale = new float[3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    scale[i] += mat[j + 4 * i] * mat[j + 4 * i];
                scale[i] = (float)Math.Sqrt(scale[i]);
            }
            return scale;
        }

        public static float[] GetRotFromMatrix(float[] mat)
        {
            double[,] rotMat = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                rotMat[0, i] = mat[4 * i];
                rotMat[1, i] = mat[4 * i + 1];
                rotMat[2, i] = mat[4 * i + 2];
            }
            double[] rotd = Helper.RotM2Eul(rotMat, Helper.AxisSequence.ZYX, Helper.AngleUnit.Degrees);
            float[] rot = new float[3];
            for (int i = 0; i < 3; i++)
            {
                int n = (int)rotd[i] / 360;
                rot[i] = (float)rotd[i] - n * 360f;
            }
            return rot;
        }

        public static float[] swapYZ(float[] v)
        {
            float t = v[1];
            v[1] = v[2];
            v[2] = t;
            return v;
        }

        public static float[] transform(float[] mat, float[]v)
        {
            float[] res = new float[3];
            for(int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    res[i] += v[j] * mat[i + j * 4];
                res[i] += mat[i + 12];
            }
            return res;
        }

        public static string MakeVector(float[] f)
        {
            return "Vector3(" + Helper.comma2dot(f[0]) + "f, " + Helper.comma2dot(f[1]) + "f, " + Helper.comma2dot(f[2]) + "f)";
        }

        public enum AngleUnit
        {
            Radiant,
            Degrees
        }

        public enum AxisSequence
        {
            ZYX,
            ZYZ,
            XYZ
        }

        public static double[] RotM2Eul(double[,] R, AxisSequence sequence = AxisSequence.ZYX, AngleUnit angleUnit = AngleUnit.Radiant)
        {
            if (R.GetLength(0) != 3 && R.GetLength(1) != 3)
                throw new ArgumentOutOfRangeException("The rotation matrix R must have 3x3 elements.");

            double[] eul = new double[3];

            int firstAxis = 0;
            bool repetition = false;
            int parity = 0;

            int i = 0;
            int j = 0;
            int k = 0;

            int[] nextAxis = { 2, 3, 1, 2 };

            switch (sequence)
            {
                case AxisSequence.ZYX:
                    firstAxis = 1;
                    repetition = false;
                    parity = 0;
                    break;
                case AxisSequence.XYZ:
                    firstAxis = 3;
                    repetition = false;
                    parity = 1;
                    break;
                case AxisSequence.ZYZ:
                    firstAxis = 3;
                    repetition = true;
                    parity = 1;
                    break;
                default:
                    break;
            }

            i = firstAxis - 1;
            j = nextAxis[i + parity] - 1;
            k = nextAxis[i - parity + 1] - 1;

            if (repetition)
            {
                double sy = Math.Sqrt(R[i, j] * R[i, j] + R[i, k] * R[i, k]);
                bool singular = sy < 10 * Double.Epsilon;

                eul[0] = Math.Atan2(R[i, j], R[i, k]);
                eul[1] = Math.Atan2(sy, R[i, i]);
                eul[2] = Math.Atan2(R[j, i], -R[k, i]);

                if (singular)
                {
                    eul[0] = Math.Atan2(-R[j, k], R[j, j]);
                    eul[1] = Math.Atan2(sy, R[i, i]);
                    eul[2] = 0;
                }
            }
            else
            {
                double sy = Math.Sqrt(R[i, i] * R[i, i] + R[j, i] * R[j, i]);
                bool singular = sy < 10 * double.Epsilon;

                eul[0] = Math.Atan2(R[k, j], R[k, k]);
                eul[1] = Math.Atan2(-R[k, i], sy);
                eul[2] = Math.Atan2(R[j, i], R[i, i]);

                if (singular)
                {
                    eul[0] = Math.Atan2(-R[j, k], R[j, j]);
                    eul[1] = Math.Atan2(-R[k, i], sy);
                    eul[2] = 0;
                }
            }

            if (parity == 1)
            {
                eul[0] = -eul[0];
                eul[1] = -eul[1];
                eul[2] = -eul[2];
            }


            double value0 = eul[0];
            double value2 = eul[2];

            eul[0] = value2;
            eul[2] = value0;

            if (angleUnit == AngleUnit.Degrees)
            {
                eul[0] *= (180 / Math.PI);
                eul[1] *= (180 / Math.PI);
                eul[2] *= (180 / Math.PI);
            }

            return eul;
        }
    }
}
