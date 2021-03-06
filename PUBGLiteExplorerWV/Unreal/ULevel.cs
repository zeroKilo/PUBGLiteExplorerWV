using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public void GetTree(TreeView tv, UAsset asset)
        {
            tv.Nodes.Clear();
            TreeNode t = new TreeNode("root");
            foreach (int obj in objects)
                if (obj > 0)
                    AddChild(asset, obj - 1, t);
            tv.Nodes.Add(t);
        }

        public void AddChild(UAsset asset, int idx, TreeNode root)
        {
            TreeNode t = new TreeNode(idx.ToString("X") + " " + asset.exportTable[idx]._name);
            int child, parent;
            GetChildAndParent(asset, idx, out child, out parent);
            if (child > 0)
                AddChild(asset, child - 1, t);
            for(int i = 0; i < asset.exportCount; i++)
                if(i != idx)
                {
                    GetChildAndParent(asset, i, out child, out parent);
                    if(parent -1 == idx)
                        AddChild(asset, i, t);
                }
            root.Nodes.Add(t);
        }

        public void GetChildAndParent(UAsset asset, int idx, out int child, out int parent)
        {
            UExport ex = asset.exportTable[idx];
            MemoryStream s = new MemoryStream(ex._data);
            child = 0;
            parent = 0;
            while (s.Position < s.Length)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                if (p.name == "RootComponent")
                    child = ((UObjectProperty)p.prop).value;
                if (p.name == "AttachParent")
                    parent = ((UObjectProperty)p.prop).value;
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
                        case "TslSpecificLocationMarker":
                            m = new MemoryStream(exp._data);
                            int sphereComponent = -1;
                            string locName = null;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "SphereComponent")
                                    sphereComponent = ((UObjectProperty)p.prop).value;
                                if (p.name == "LocationName")
                                    locName = ((UStrProperty)p.prop).value;
                            }
                            if (sphereComponent < 1 || locName == null)
                                break;
                            sb.AppendLine("\tLocation Name : " + locName);
                            UExport expSC = myAsset.exportTable[sphereComponent - 1];
                            m = new MemoryStream(expSC._data);
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                else AddDetail(p, sb);
                            }
                            break;
                        default:
                            m = new MemoryStream(exp._data);
                            int defaultSceneRoot= -1;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "DefaultSceneRoot")
                                    defaultSceneRoot = ((UObjectProperty)p.prop).value;
                                else AddDetail(p, sb);
                            }
                            if (defaultSceneRoot < 1)
                                break;
                            sb.AppendLine(" ->DefaultSceneRoot");
                            UExport expDSR = myAsset.exportTable[defaultSceneRoot - 1];
                            m = new MemoryStream(expDSR._data);
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                else AddDetail(p, sb);
                            }
                            break;
                    }
                }
            return sb.ToString();
        }
    }
}
