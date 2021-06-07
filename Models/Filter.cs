using System.Collections.Generic;
using System.Linq;

namespace TidyBags.Models
{
    class Filter
    {
        public Filter()
        {
        }


        public string Name;
        public List<ItemComparer> itemComparers = new List<ItemComparer>();
        public List<Destination> destinations = new List<Destination>();

        public IEnumerable<Item> Split(IEnumerable<Item> items, out IEnumerable<Item> filteredItems)
        {
            var matches = items.Where(x => itemComparers.Any(y => y.Match(x)));
            filteredItems = items.Where(x => !itemComparers.Any(y => y.Match(x)));

            return matches;
        }

        internal IEnumerable<Item> Sort(IEnumerable<Item> items)
        {
            List<KeyValuePair<int, Item>> list = new List<KeyValuePair<int, Item>>();
            foreach (var item in items)
            {
                for (int i = 0; i < itemComparers.Count; i++)
                {
                    if (itemComparers[i].Match(item))
                    {
                        list.Add(new KeyValuePair<int, Item>(i, item));
                        break;
                    }
                }
            }

            return list.OrderBy(x => x.Key)
                .ThenBy(x => x.Value.Id)
                //.ThenBy(x => $"{x.Value.Item.Values(LongValueKey.Material, 0)} {x.Value.Item.Name}")
                .Select(x => x.Value);
        }
    }

}
