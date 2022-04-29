using System;
using System.Collections.Generic;
using System.IO;

namespace PUBGLiteExplorerWV.Model
{
    public class Export
    {
        public UExport uexport;
        public int objID;
        
        public NIEObject classIdx;
        public NIEObject nameIdx;
        public NIEObject packageIdx;
        public NIEObject templateIdx;

        public List<Property> properties = new List<Property>();

        public Export(UAsset asset, int objID)
        {
            this.uexport = asset.exportTable[objID - 1];
            this.objID = objID;

            this.classIdx = uexport.classIdx != 0 ? new NIEObject(asset, uexport.classIdx, false) : null;
            this.nameIdx = uexport.nameIdx != 0 ? new NIEObject(asset, uexport.nameIdx, false) : null;
            this.packageIdx = uexport.packageIdx != 0 ? new NIEObject(asset, uexport.packageIdx, false) : null;
            this.templateIdx = uexport.templateIdx != 0 ? new NIEObject(asset, uexport.templateIdx, false) : null;
            
            MemoryStream m = new MemoryStream(uexport._data);
            while ((ulong)m.Position < uexport.dataSize)
            {
                UProperty p = new UProperty(m, asset);
                if (p.name == "None" || !p._isValid)
                    break;
                
                properties.Add(new Property(asset, p));
            }
        }
    }
}