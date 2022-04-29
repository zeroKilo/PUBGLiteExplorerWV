using System;

namespace PUBGLiteExplorerWV.Model
{
    public class NIEObject
    {
        private UAsset asset;
        
        public int export;
        public int import;
        public string name;

        public NIEObject(UAsset asset, int objID, bool canBeExport)
        {
            this.asset = asset;
            
            if (objID > 0)
            {
                if (canBeExport)
                    export = objID <= asset.exportCount ? objID : 0;
                else
                    name = objID <= asset.nameCount ? asset.nameTable[objID - 1] : "";
            }
            else
            {
                import = -objID <= asset.importCount ? objID : 0;
            }

            if (export == 0 && import == 0 && name == null)
            {
                throw new ArgumentException("Cannot find object!");
            }
        }

        public string GetObjectName()
        {
            if (IsExport())
                return new Export(asset, export).uexport._name;
            else if (IsImport())
                return new Import(asset, import).uimport._name;
            else if (IsName())
                return name;
            else
                throw new ArgumentException("Cannot find object!");
        }

        public bool IsExport()
        {
            return export != 0;
        }

        public bool IsImport()
        {
            return import != 0;
        }

        public bool IsName()
        {
            return name != null;
        }
    }
}