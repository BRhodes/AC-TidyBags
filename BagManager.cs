using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TidyBags.Models;

namespace TidyBags
{
    static public class BagManager
    {
        static public MoveItemAction GetNextAction(IEnumerable<Item> items, int characterId)
        {
            // get all items and current pack
            //IEnumerable<Item> items = new List<Item>();
            //var core = Decal.Adapter.CoreManager.Current;
            //var mainPack = core.WorldFilter.GetByContainer(core.CharacterFilter.Id).Where(x => x.ObjectClass != ObjectClass.Container && x.Values(LongValueKey.Slot, -1) != -1);
            //var packs = core.WorldFilter.GetByContainer(core.CharacterFilter.Id)
            //    .Where(x => x.ObjectClass == ObjectClass.Container);
            //foreach (var pack in packs)
            //{
            //    var packItems = core.WorldFilter.GetByContainer(pack.Id);
            //    items.Concat(packItems.Select(x => new Item(x)));
            //}

            //.SelectMany(x => new SortableItem(core.WorldFilter.GetByContainer(x.Id), x.Id)).ToList();
            //todo problems here

            var vi = new VirtualInventory(items.Where(x => x.ContainerSize > 0).Select(x => new Pack(x.Id, x.Slot, x.ContainerSize)).ToList(), characterId);

            var unsortedItems = items.Where(x => x.Slot != -1 && x.ContainerSize == 0);

            var overflow = new List<Item>();
            // for each filter group
            var filters = GetFilters();
            foreach (var filter in filters)
            {
                var filteredItems = filter.Split(unsortedItems, out unsortedItems);
                var sortedItems = filter.Sort(filteredItems);
                foreach (var item in sortedItems)
                {
                    var placed = false;
                    foreach (var destination in filter.destinations)
                    {
                        var pack = vi.Packs.Single(x => destination.Slot == x.Slot);

                        if (pack.AddItem(item, destination.Bottom))
                        {
                            placed = true;
                            break;
                        };
                    }
                    if (!placed)
                    {
                        overflow.Add(item);
                    }
                }
            }

            var defaultPack = vi.Packs.Single(x => x.Slot == -1);
            foreach (var item in unsortedItems.OrderBy(x => x.Type).ThenBy(x => x.Id))
            {
                if (!defaultPack.AddItem(item, false))
                {
                    foreach (var pack in vi.Packs.OrderBy(x => x.Slot))
                    {
                        if (pack.AddItem(item, false))
                            break;
                    }
                }
            }
            //   sort
            //   stuff into packs

            var finalizedItems = vi.Finalize();

            // stack like items

            // internally sort each actual pack
            foreach (var pack in vi.Packs)
            {

                var allItems = pack.Items.Where(x => x.Container == pack.Id).ToList();
                var orderedItems = Util.LongestIncreasingSubsequence(allItems);

                Dictionary<Item, int> position = new Dictionary<Item, int>();

                for (int i = 0; i < allItems.Count; i++)
                    position[allItems[i]] = i;

                var unorderedItem = allItems.FirstOrDefault(x => !orderedItems.Contains(x));
                if (unorderedItem != null)
                {
                    int slot;
                    if (position[unorderedItem] < position[orderedItems.First()])
                    {
                        slot = 0;
                        Util.WriteToChat($"Moving {unorderedItem.Name}({unorderedItem.Id}) before {orderedItems.First().Name}({orderedItems.First().Id}) in pack {pack.Slot}.");
                    }
                    else
                    {
                        var orderedItem = orderedItems.Last(x => position[x] < position[unorderedItem]);

                        slot = orderedItem.Slot + ((unorderedItem.Slot < orderedItem.Slot) ? 0 : 1);
                        Util.WriteToChat($"Moving {unorderedItem.Name}({unorderedItem.Id}) after {orderedItem.Name}({orderedItem.Id}) in pack {pack.Slot}.");
                    }

                    return new MoveItemAction
                    {
                        ObjectId = unorderedItem.Id,
                        PackId = pack.Id,
                        Slot = slot
                    };
                }
            }
            // move items that are in the wrong home to there rightful location

            //var available_packs = vi.Packs.Where(x => !x.IsFull());
            var availablePacks = vi.Packs.Where(x => x.Size > finalizedItems.Count(y => y.Container == x.Id)).Select(x => x.Id);
            var itemToSort = finalizedItems.FirstOrDefault(x => availablePacks.Contains(x.DestinationPack) && x.DestinationPack != x.Container);

            if (itemToSort != null)
            {
                int slot = 0;
                var destPack = vi.Packs.Single(x => x.Id == itemToSort.DestinationPack);
                var destItems = finalizedItems.Where(x => x.DestinationPack == x.Container);
                if (itemToSort.DestinationSlot > destItems.First().DestinationSlot)
                {
                    var orderedItem = destItems.Last(x => x.DestinationSlot < itemToSort.DestinationSlot);

                    slot = orderedItem.Slot + 1;
                    Util.WriteToChat($"Moving {itemToSort.Name}({itemToSort.Id}) after {orderedItem.Name}({orderedItem.Id}) to pack {itemToSort.DestinationPack}.");
                }

                return new MoveItemAction
                {
                    ObjectId = itemToSort.Id,
                    PackId = itemToSort.DestinationPack,
                    Slot = slot
                };
            }



            //var unordered_Item = vi.Packs.SelectMany(x => x.Items).FirstOrDefault(x => x.;



            // di
            //            Decal.Adapter.CoreManager.Current.Actions.MoveItem()
            //var core = Decal.Adapter.CoreManager.Current;
            //var i = 0;
            //var misplacedDistance = 0;
            //var placePosition = -1;
            //WorldObject misplacedItem = null;
            //var items = core.WorldFilter.GetByContainer(core.CharacterFilter.Id).OrderBy(x => x.Values(LongValueKey.Slot, -1)).Where(x => x.ObjectClass != ObjectClass.Container && x.Values(LongValueKey.Slot, -1) != -1);
            //var sortedItems = items.Select(x => new KeyValuePair<int, WorldObject>(i++, x)).OrderBy(x => x.Value.Name).ThenBy(x => x.Value.Id).ToList();

            //for (int x = 0; x < sortedItems.Count; x++)
            //{
            //    if (Math.Abs(sortedItems[x].Value.Values(LongValueKey.Slot) - x) > misplacedDistance)
            //    {
            //        misplacedDistance = Math.Abs(sortedItems[x].Value.Values(LongValueKey.Slot) - x);
            //        misplacedItem = sortedItems[x].Value;
            //        placePosition = x;
            //    }
            //}

            //if (misplacedItem != null)
            //    Host.Actions.MoveItem(misplacedItem.Id, core.CharacterFilter.Id, placePosition, false);

            //Util.WriteToChat("Nothing to move!");
            return null;
        }
        static private IEnumerable<Filter> GetFilters()
        {
            TextReader tr = new StreamReader(@"C:\AC\TidyBags\Profile\Vehn.json");
            var data = tr.ReadToEnd();

            return JsonConvert.DeserializeObject<List<Filter>>(data);
        }
    }
}
