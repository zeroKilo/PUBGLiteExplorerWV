using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class ULevel
    {
        public List<UProperty> props = new List<UProperty>();
        public List<int> objects = new List<int>();
        public UAsset myAsset;
        public MemoryStream myBulk;
        public ULevel(Stream s, UAsset asset, MemoryStream ubulk)
        {
            myAsset = asset;
            myBulk = ubulk;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            uint count = Helper.ReadU32(s);
            objects = new List<int>();
            for (int i = 0; i < count; i++)
                objects.Add((int)Helper.ReadU32(s));
        }        

        private void AddDetail(UProperty p, StringBuilder sb)
        {
            MemoryStream m;
            switch (p.name)
            {
                case "RelativeLocation":
                    m = new MemoryStream(((UStructProperty)p.prop).data);
                    Helper.ReadUnrealVector3(m, sb, p.name, true);
                    break;
                case "RelativeRotation":
                    m = new MemoryStream(((UStructProperty)p.prop).data);
                    Helper.ReadUnrealVector3(m, sb, p.name, false);
                    break;
                case "RelativeScale":
                case "RelativeScale3D":
                    m = new MemoryStream(((UStructProperty)p.prop).data);
                    Helper.ReadUnrealVector3(m, sb, p.name, false, true);
                    break;
            }
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            foreach(int objID in objects)
                if(objID > 0)
                {
                    UExport exp = myAsset.exportTable[objID - 1];
                    string cls = myAsset.GetName(myAsset.exportTable[objID - 1].classIdx);
                    sb.AppendLine(objID.ToString("X") + " " + cls + " " + exp._name);
                    switch(cls)
                    {
                        case "StaticMeshActor":
                            Console.WriteLine(objID + " " + exp._name);
                            MemoryStream m = new MemoryStream(exp._data);
                            int staticMeshComponentID = -1;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "StaticMeshComponent")
                                    staticMeshComponentID = ((UObjectProperty)p.prop).value;
                                else AddDetail(p, sb);
                            }
                            if (staticMeshComponentID < 1)
                                break;
                            UExport expSMC = myAsset.exportTable[staticMeshComponentID - 1];
                            m = new MemoryStream(expSMC._data);
                            int staticMeshID = -1;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if(p.name == "StaticMesh")
                                    staticMeshID = ((UObjectProperty)p.prop).value;
                                else AddDetail(p, sb);
                            }
                            if (staticMeshID < 0)
                                sb.AppendLine("\tImport : " + myAsset.importTable[-staticMeshID - 1]._name);
                            else if (staticMeshID > 0)
                                sb.AppendLine("\tExport : " + myAsset.exportTable[staticMeshID - 1]._name);
                            break;
                        case "StaticMeshActorFM":
                            m = new MemoryStream(exp._data);
                            int instancedMeshComponentID = -1;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "InstancedMeshComponent")
                                    instancedMeshComponentID = ((UObjectProperty)p.prop).value;
                            }
                            if (instancedMeshComponentID < 1)
                                break;
                            UExport expIMC = myAsset.exportTable[instancedMeshComponentID - 1];
                            m = new MemoryStream(expIMC._data);
                            staticMeshID = -1;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "StaticMesh")
                                    staticMeshID = ((UObjectProperty)p.prop).value;
                                else AddDetail(p, sb);
                            }
                            if (staticMeshID < 0)
                                sb.AppendLine("\tImport : " + myAsset.importTable[-staticMeshID - 1]._name);
                            else if (staticMeshID > 0)
                                sb.AppendLine("\tExport : " + myAsset.exportTable[staticMeshID - 1]._name);
                            if(myAsset.GetName( expIMC.classIdx) == "HierarchicalInstancedStaticMeshComponent")
                            {
                                UHirarchicalInstancedStaticMeshComponent hismc = new UHirarchicalInstancedStaticMeshComponent(new MemoryStream(expIMC._data), myAsset, myBulk);
                                sb.AppendLine(hismc.GetDetails());
                            }
                            break;
                    }
                }
            return sb.ToString();
        }
    }
}
