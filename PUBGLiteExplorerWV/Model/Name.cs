using System;

namespace PUBGLiteExplorerWV.Model
{
    public class Name
    {
        public string name;
        public int objID;
        
        public Name(UAsset asset, int objID)
        {
            this.name = asset.nameTable[objID - 1];
            this.objID = objID;
        }
    }
}