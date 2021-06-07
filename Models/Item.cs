using Decal.Adapter.Wrappers;
using System.Diagnostics;

namespace TidyBags.Models
{
    [DebuggerDisplay("{Slot}: {Name}")]
    public class Item
    {
        public Item(Item item)
        {
            Id = item.Id;
            Slot = item.Slot;
            Type = item.Type;
            Name = item.Name;
            Container = item.Container;
            ContainerSize = item.ContainerSize;
        }

        public Item(WorldObject item)
        {
            Id = item.Id;
            Slot = item.Values(LongValueKey.Slot, -1);
            Type = item.Type;
            Container = item.Container;
            Name = item.Name;
            ContainerSize = item.Values(LongValueKey.ItemSlots, 0);
        }

        public string Name;
        public int Id;
        public int Container;
        public int Slot;
        public long? Type;
        public int ContainerSize;
    }
}
