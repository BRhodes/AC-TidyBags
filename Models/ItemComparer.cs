namespace TidyBags.Models
{
    public class ItemComparer
    {
        public string Name;
        public long? Type;
        public long? Id;

        public bool Match(Item item)
        {
            if (Type != null)
            {
                return item.Type == Type;
            }
            
            if (Id != null) {
                return item.Id == Id;
            }

            return false;
        }
    }
}