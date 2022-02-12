using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UHirarchicalInstancedStaticMeshComponent
    {
        public class NodeData
        {
            public float[] min;
            public int firstChild;
            public float[] max;
            public int lastChild;
            public int firstInstance;
            public int lastInstance;
            public NodeData(Stream s)
            {
                min = new float[] { Helper.ReadFloat(s), Helper.ReadFloat(s), Helper.ReadFloat(s) };
                firstChild = (int)Helper.ReadU32(s);
                max = new float[] { Helper.ReadFloat(s), Helper.ReadFloat(s), Helper.ReadFloat(s) };
                lastChild = (int)Helper.ReadU32(s);
                firstInstance = (int)Helper.ReadU32(s);
                lastInstance = (int)Helper.ReadU32(s);
            }
        }
        public List<UProperty> props = new List<UProperty>();
        public UAsset myAsset;
        public float[] myLocation;
        public List<float[]> transforms;
        public List<NodeData> nodes;

        public UHirarchicalInstancedStaticMeshComponent(Stream s, UAsset asset, MemoryStream ubulk)
        {
            myAsset = asset;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                if (p.name == "RelativeLocation")
                {
                    MemoryStream m = new MemoryStream(((UStructProperty)p.prop).data);
                    myLocation = new float[] { Helper.ReadFloat(m), Helper.ReadFloat(m), Helper.ReadFloat(m) };
                }
                props.Add(p);
            }
            s.Seek(-4, SeekOrigin.Current);
            uint test = Helper.ReadU32(s);
            if (test != 0)
                s.Seek(test * 16, SeekOrigin.Current);
            Helper.ReadU32(s);
            Helper.ReadU32(s);
            uint count = Helper.ReadU32(s);
            transforms = new List<float[]>();
            for (int i = 0; i < count; i++)
            {
                List<float> temp = new List<float>();
                for (int j = 0; j < 16; j++)
                    temp.Add(Helper.ReadFloat(s));
                transforms.Add(temp.ToArray());
            }
            Helper.ReadU32(s);
            count = Helper.ReadU32(s);
            nodes = new List<NodeData>();
            for (int i = 0; i < count; i++)
                nodes.Add(new NodeData(s));
        }       
        
        public float[] AddVec3(float[]v1, float[] v2)
        {
            float[] res = new float[3];
            for (int i = 0; i < 3; i++)
                res[i] = v1[i] + v2[i];
            return res;
        }

        public string ProcessNode(int index, float[] loc)
        {
            StringBuilder sb = new StringBuilder();
            NodeData node = nodes[index];
            float[] center = new float[3];
            for (int i = 0; i < 3; i++)
                center[i] = (node.min[i] + node.max[i]) * 0.5f;
            if (node.firstChild != -1 && node.lastChild != -1)
                for (int i = node.firstChild; i <= node.lastChild; i++)
                    sb.Append(ProcessNode(i, AddVec3(loc, center)));
            else
            {
                float[] mat = transforms[node.firstInstance];
                float[] pos = Helper.GetPosFromMatrix(mat);
                float[] apos = AddVec3(pos, myLocation);
                float[] scale = Helper.GetScaleFromMatrix(mat);
                float[] rot = Helper.GetRotFromMatrix(mat);
                apos = Helper.swapYZ(apos);
                apos[2] *= -1f;
                for (int i = 0; i < 3; i++)
                {
                    apos[i] *= 0.01f;
                    scale[i] *= 100f;
                }
                scale = Helper.swapYZ(scale);
                sb.AppendLine("g = Instantiate(instance, new " + Helper.MakeVector(apos) + ", Quaternion.identity);");
                sb.AppendLine("g.transform.localScale = new " + Helper.MakeVector(scale) + ";");
            }
            return sb.ToString();
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\t\tInstances:");
            sb.Append(ProcessNode(0, myLocation));
            return sb.ToString();
        }
    }
}
