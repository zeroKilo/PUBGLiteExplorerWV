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

        public MemoryStream rawData;
        private float[] lastPos, lastRot, lastScale;
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
                    lastPos = Helper.ReadUnrealVector3(m, sb, p.name, true);
                    break;
                case "RelativeRotation":
                    m = new MemoryStream(((UStructProperty)p.prop).data);
                    lastRot = Helper.ReadUnrealVector3(m, sb, p.name, false);
                    break;
                case "RelativeScale":
                case "RelativeScale3D":
                    m = new MemoryStream(((UStructProperty)p.prop).data);
                    lastScale = Helper.ReadUnrealVector3(m, sb, p.name, false, true);
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
                if (!p._isValid || p.name == "None")
                    break;
                if (p.name == "RootComponent")
                    child = ((UObjectProperty)p.prop).value;
                if (p.name == "AttachParent")
                    parent = ((UObjectProperty)p.prop).value;
            }
        }


        public string GetDetails()
        {
            rawData = new MemoryStream();
            Helper.WriteCString(rawData, "WVM0");
            StringBuilder sb = new StringBuilder();
            foreach(int objID in objects)
                if(objID > 0)
                {
                    UExport exp = myAsset.exportTable[objID - 1];
                    string cls = myAsset.GetName(exp.classIdx);
                    sb.AppendLine(objID.ToString("X") + " " + cls + " " + exp._name);
                    lastPos = lastRot = new float[3];
                    lastScale = new float[] { 100, 100, 100 };
                    string name3 = "";
                    switch (cls)
                    {
                        case "StaticMeshActor":
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
                            {
                                name3 = myAsset.importTable[-staticMeshID - 1]._name;
                                sb.AppendLine("\tImport : " + name3);
                            }
                            else if (staticMeshID > 0)
                            {
                                name3 = myAsset.exportTable[staticMeshID - 1]._name;
                                sb.AppendLine("\tExport : " + name3);
                            }
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
                            staticMeshID = 0;
                            while (true)
                            {
                                UProperty p = new UProperty(m, myAsset);
                                if (p.name == "None")
                                    break;
                                if (p.name == "StaticMesh") 
                                    staticMeshID = ((UObjectProperty)p.prop).value;
                                else AddDetail(p, sb);
                            }
                            if (staticMeshID == 0)
                            {
                                sb.AppendLine("\t###STATIC MESH NOT FOUND!###");
                                continue;
                            }
                            if (staticMeshID < 0)
                            {
                                name3 = myAsset.importTable[-staticMeshID - 1]._name;
                                sb.AppendLine("\tImport : " + name3);
                            }
                            else if (staticMeshID > 0)
                            {
                                name3 = myAsset.exportTable[staticMeshID - 1]._name;
                                sb.AppendLine("\tExport : " + name3);
                            }
                            if (myAsset.GetName(expIMC.classIdx) == "HierarchicalInstancedStaticMeshComponent")
                            {
                                UHirarchicalInstancedStaticMeshComponent hismc = new UHirarchicalInstancedStaticMeshComponent(new MemoryStream(expIMC._data), myAsset, myBulk);
                                sb.AppendLine(hismc.GetDetails());
                                hismc.ProcessNodeForRaw(0, hismc.myLocation, rawData, name3, exp._name);
                                lastPos = new float[3];
                            }
                            break;
                        case "InstancedFoliageActor":
                            UInstancedFoliageActor ifa = new UInstancedFoliageActor(new MemoryStream(exp._data), myAsset, myBulk);
                            sb.AppendLine(ifa.GetDetails());
                            foreach(UInstancedFoliageActor.FoliageInfo info in ifa.foliageInfo)
                                if(info.hismc != null)
                                    info.hismc.ProcessNodeForRaw(0, info.hismc.myLocation, rawData, info.name, exp._name);
                            lastPos = new float[3];
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
                    if (lastPos[0] != 0 || lastPos[1] != 0 || lastPos[2] != 0)
                    {
                        Console.WriteLine(objID + " " + exp._name);
                        Console.WriteLine("Pos = " + lastPos[0] + " " + lastPos[1] + " " + lastPos[2]);
                        rawData.WriteByte(1);
                        Helper.WriteCString(rawData, cls);
                        rawData.WriteByte(0);
                        Helper.WriteCString(rawData, exp._name);
                        rawData.WriteByte(0);
                        Helper.WriteCString(rawData, name3);
                        rawData.WriteByte(0);
                        foreach (float f in lastPos)
                            Helper.WriteFloat(rawData, f);
                        foreach (float f in lastRot)
                            Helper.WriteFloat(rawData, f);
                        foreach (float f in lastScale)
                            Helper.WriteFloat(rawData, f);
                    }
                }
            rawData.WriteByte(0);
            return sb.ToString();
        }
    }
}
