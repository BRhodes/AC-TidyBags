using System.Text.RegularExpressions;

namespace TidyBags.Models
{
    public class ItemComparer
    {
        public string Name;
        public long? Type;
        public long? Id;
        public string Regex;

        public bool Match(Item item)
        {
            if (Type != null)
            {
                return item.Type == Type;
            }
            
            if (Id != null) {
                return item.Id == Id;
            }

            if (Regex != null)
            {
                var r = new Regex(Regex);
                return r.IsMatch(item.Name);
            }

            return false;
        }
    }
}