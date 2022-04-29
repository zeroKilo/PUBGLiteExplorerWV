namespace PUBGLiteExplorerWV.Model
{
    public class Property
    {
        public UProperty property;
        
        public int refObject;
        
        public Property(UAsset asset, UProperty property)
        {
            this.property = property;
            
            if (property.prop is UObjectProperty)
                refObject = ((UObjectProperty) property.prop).value;
        }
    }
}