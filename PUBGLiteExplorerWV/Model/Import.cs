using System;

namespace PUBGLiteExplorerWV.Model
{
    public class Import
    {
        public UImport uimport;
        public int objID;

        public NIEObject className;
        public NIEObject classPackage;
        public NIEObject objectName;
        public NIEObject packageIdx;

        public Import(UAsset asset, int objID)
        {
            this.uimport = asset.importTable[-objID - 1];
            this.objID = objID;

            this.className = uimport.className != 0 ? new NIEObject(asset, (int) uimport.className, false) : null;
            this.classPackage = uimport.classPackage != 0 ? new NIEObject(asset, (int) uimport.classPackage, false) : null;
            this.objectName = uimport.objectName != 0 ? new NIEObject(asset, (int) uimport.objectName, false) : null;
            this.packageIdx = uimport.packageIdx != 0 ? new NIEObject(asset, uimport.packageIdx, true) : null;
        }
    }
}