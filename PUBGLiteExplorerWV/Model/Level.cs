using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PUBGLiteExplorerWV.Model
{
    public class Level
    {
        public List<Export> objects = new List<Export>();
        
        public Level(ULevel level)
        {
            foreach (int objID in level.objects)
            {
                if (objID <= 0)
                    continue;
                
                objects.Add(new Export(level.myAsset, objID));
            }
        }
    }
}