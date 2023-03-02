using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGLiteExplorerWV
{
    public class UMaterial
    {
        public class FPlatformTypeLayoutParameters
        {
            public uint maxFieldAlignment;
            public uint flags;
            public FPlatformTypeLayoutParameters(Stream s)
            {
                maxFieldAlignment = Helper.ReadU32(s);
                flags = Helper.ReadU32(s);
            }
        }
        public class FShaderTypeDependency
        {
            public ulong shaderTypeName;
            public byte[] SourceHash = new byte[20];

            public FShaderTypeDependency(Stream s)
            {
                shaderTypeName = Helper.ReadU64(s);
                s.Read(SourceHash, 0, 20);
            }
        }
        public class FMaterialShaderMapId
        {
            public uint usageInt;
            public byte[] baseMaterialId = new byte[16];
            public uint qualityLevel;
            public uint featureLevel;
            public uint numStaticSwitchParameters;
            public uint numStaticComponentMaskParameters;
            public uint numTerrainLayerWeightParameters;
            public uint numReferencedFunctions;
            public uint numReferencedParameterCollections;
            public uint numShaderTypeDependencies;
            public FShaderTypeDependency[] shaderTypeDependencies;
            public uint numVertexFactoryTypeDependencies;
            public uint unknown1;
            public uint unknown2;
            public uint unknown3;
            public byte[] textureReferencesHash = new byte[20];
            public byte[] basePropertyOverridesHash = new byte[20];
            public FPlatformTypeLayoutParameters layoutParams;
            public FMaterialShaderMapId(Stream s)
            {
                usageInt = Helper.ReadU32(s);
                s.Read(baseMaterialId, 0, 16);
                qualityLevel = Helper.ReadU32(s);
                featureLevel = Helper.ReadU32(s);
                numStaticSwitchParameters = Helper.ReadU32(s);
                numStaticComponentMaskParameters = Helper.ReadU32(s);
                numTerrainLayerWeightParameters = Helper.ReadU32(s);
                numReferencedFunctions = Helper.ReadU32(s);
                numReferencedParameterCollections = Helper.ReadU32(s);
                numShaderTypeDependencies = Helper.ReadU32(s);
                shaderTypeDependencies = new FShaderTypeDependency[numShaderTypeDependencies];
                for (int i = 0; i < numShaderTypeDependencies; i++)
                    shaderTypeDependencies[i] = new FShaderTypeDependency(s);
                numVertexFactoryTypeDependencies = Helper.ReadU32(s);
                unknown1 = Helper.ReadU32(s);
                unknown2 = Helper.ReadU32(s);
                unknown3 = Helper.ReadU32(s);
                s.Read(textureReferencesHash, 0, 20);
                s.Read(basePropertyOverridesHash, 0, 20);
                layoutParams = new FPlatformTypeLayoutParameters(s);
            }
        }

        public class FMaterialShaderMap
        {
            public FMaterialShaderMapId shaderMapId;
            public FMaterialShaderMap(Stream s)
            {
                shaderMapId = new FMaterialShaderMapId(s);
            }
        }

        public class FMaterialResource
        {
            public bool bCooked;
            public bool bValid;
            public FMaterialShaderMap gameThreadShaderMap;
            public FMaterialResource(Stream s)
            {
                bCooked = Helper.ReadU32(s) == 1;
                bValid = Helper.ReadU32(s) == 1;
                gameThreadShaderMap = new FMaterialShaderMap(s);
            }
        }

        public List<UProperty> props = new List<UProperty>();
        public UAsset myAsset;

        public List<FMaterialResource> resources = new List<FMaterialResource>();
        public List<uint> expressionIDs = new List<uint>();
        public UMaterial(Stream s, UAsset asset)
        {
            myAsset = asset;
            while (true)
            {
                UProperty p = new UProperty(s, asset);
                if (p.name == "None")
                    break;
                props.Add(p);
            }
            uint NumResources = Helper.ReadU32(s);
            for(uint i = 0; i < NumResources; i++)
            {
                resources.Add(new FMaterialResource(s));
                break;
            }
            UArrayProperty expressions = (UArrayProperty)Helper.FindPropByName(props, "Expressions");
            if (expressions == null)
                return;
            expressionIDs = new List<uint>();
            MemoryStream m = new MemoryStream(expressions.data);
            uint count = Helper.ReadU32(m);
            for (int i = 0; i < count; i++)
            {
                uint ID = Helper.ReadU32(m);
                if (ID > 0)
                    expressionIDs.Add(ID - 1);
            }
        }

        public string MakeDotGraph()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("graph Material {");
            foreach (uint ID in expressionIDs)
                DumpMaterialExpressionNodes(sb, myAsset, ID);
            foreach (uint ID in expressionIDs)
                DumpMaterialExpressionEdges(sb, myAsset, ID);
            sb.AppendLine("}");
            return sb.ToString();
        }
        public void DumpMaterialExpressionNodes(StringBuilder sb, UAsset asset, uint exportID)
        {
            sb.Append("N" + exportID + "[shape=box label=\"");
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch (cname)
            {
                case "MaterialExpressionTextureSampleParameter2D":
                    UMaterialExpressionTextureSampleParameter2D metsp2d = new UMaterialExpressionTextureSampleParameter2D(new MemoryStream(export._data), asset);
                    metsp2d.DumpNodeText(sb);
                    break;
                default:
                    sb.Append(cname);
                    break;
            }
            sb.AppendLine("\"]");
        }
        public void DumpMaterialExpressionEdges(StringBuilder sb, UAsset asset, uint exportID)
        {
            UExport export = asset.exportTable[(int)exportID];
            string cname = asset.GetClassName(export.classIdx);
            switch (cname)
            {
                case "MaterialExpressionTextureSampleParameter2D":
                    UMaterialExpressionTextureSampleParameter2D metsp2d = new UMaterialExpressionTextureSampleParameter2D(new MemoryStream(export._data), asset);
                    metsp2d.DumpEdgeText(sb);
                    break;
                default:
                    break;
            }
        }
    }
}
