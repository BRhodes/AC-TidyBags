using Decal.Adapter.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TidyBags.Models
{
    class VirtualInventory
    {
        public VirtualInventory(List<Pack> packs, int characterId) {
            Packs = packs;
            packs.Add(new Pack(characterId, -1, 101));
        }

        public List<Pack> Packs = new List<Pack>();
        public List<SortedItem> Finalize()
        {
            var rv = new List<SortedItem>();

            foreach (var pack in Packs)
            {
                var x = pack.Items;

                for (int i = 0; i < x.Count(); i++)
                {
                    rv.Add(new SortedItem(x[i], pack.Id, i));
                }
            }

            return rv;
        }
    }

    class SortedItem : Item
    {
        public SortedItem(Item item, int pack, int slot) : base(item)
        {
            DestinationPack = pack;
            DestinationSlot = slot;
        } 
        public int DestinationPack;
        public int DestinationSlot;
    }

    [DebuggerDisplay("{Slot}: {Size} -- {Id}")]
    class Pack
    {
        public Pack(int id, int slot, int size)
        {
            Id = id;
            Slot = slot;
            Size = size;
            TopItems = new List<Item>();
            BottomItems = new List<Item>();
        }

        public int Id;
        public long Slot;
        public int Size;
        public List<Item> TopItems;
        public List<Item> BottomItems;
        public bool IsFull()
        {
            return Size >= TopItems.Count + BottomItems.Count;
        }

        public List<Item> Items { get { return TopItems.Concat(BottomItems).ToList(); } }

        public bool AddItem(Item item, bool putOnBottom)
        {
            if (TopItems.Count + BottomItems.Count >= Size)
                return false;

            if (putOnBottom)
                BottomItems.Add(item);
            else
                TopItems.Add(item);

            return true;
        }

    }
}
