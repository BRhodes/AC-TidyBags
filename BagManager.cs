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
            // Get Finalized items
            var vi = new VirtualInventory(items.Where(x => x.ContainerSize > 0).Select(x => new Pack(x.Id, x.Slot, x.ContainerSize)).ToList(), characterId);

            var unsortedItems = items.Where(x => x.Slot != -1 && x.ContainerSize == 0 && !x.Name.Contains("Foci"));

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
                        var pack = vi.Packs.SingleOrDefault(x => destination.Slot == x.Slot);
                        if (pack == null) continue;

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

            int j = 0;
            foreach (var pack in vi.Packs.OrderBy(x => x.Slot))
            {
                for (; j < overflow.Count; j++)
                {
                    if (!pack.AddItem(overflow[j], false))
                    {
                        j--;
                        break;
                    }
                }
            }


            var defaultPack = vi.Packs.Single(x => x.Slot == -1);
            //foreach (var item in unsortedItems.OrderBy(x => vi.Packs.Single(y => y.Id == x.Container)).ThenBy(x => x.Slot))
            foreach (var item in unsortedItems.OrderBy(x => x.Name).ThenBy(x => x.Type))
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

            // In sort /////////////////////////////////////////////////////////////////////////////////////////

            var itemsInCorrectPack = finalizedItems.Where(x => x.DestinationPack == x.Container).GroupBy(x => x.Container);

            foreach (var pack in itemsInCorrectPack)
            {
                var orderedItems = Util.LongestIncreasingSubsequence(pack.OrderBy(x => x.Slot), x => x.DestinationSlot);
                var unsortedItem = pack.Except(orderedItems).FirstOrDefault();
                if (unsortedItem != null)
                    return MoveAction(unsortedItem, orderedItems);
            }

            // Exterior sort ///////////////////////////////////////////////////////////////////////////////////

            var availablePacks = vi.Packs.Where(x => x.Size > finalizedItems.Count(y => y.Container == x.Id)).Select(x => x.Id);
            if (availablePacks.Count() == 0) return null;
            var sortabledItems = finalizedItems.Where(x => availablePacks.Contains(x.DestinationPack) && x.DestinationPack != x.Container);
            var pendingDeliveriesByPack = finalizedItems
                .GroupBy(x => x.DestinationPack)
                .Select(x => new KeyValuePair<int, int>(x.Key, x.Count(y => y.DestinationPack != y.Container)))
                .ToDictionary(x => x.Key, x => x.Value);

            var misplacedExterior = sortabledItems.OrderByDescending(x => pendingDeliveriesByPack.GetValueOrDefault(x.Container, -1)).FirstOrDefault();

            if (misplacedExterior != null)
                return MoveAction(misplacedExterior, finalizedItems.Where(x => x.DestinationPack == misplacedExterior.DestinationPack && x.Container == x.DestinationPack).ToList());

            // McJigger items //////////////////////////////////////////////////////////////////////////////////

            var misplacedItems = finalizedItems.Where(x => x.DestinationPack != x.Container);
            var blocker = misplacedItems.OrderByDescending(x => pendingDeliveriesByPack.GetValueOrDefault(x.Container, -1)).FirstOrDefault();
            if (blocker != null)
            {
                blocker.DestinationPack = availablePacks.OrderByDescending(x => pendingDeliveriesByPack.GetValueOrDefault(x, -1)).First();
                blocker.DestinationSlot = 0;
                return new MoveItemAction()
                {
                    ObjectId = blocker.Id,
                    PackId = blocker.DestinationPack,
                    Slot = blocker.DestinationSlot
                };
            }

            return null;
        }

        private static MoveItemAction MoveAction(SortedItem unsortedItem, List<SortedItem> items)
        {
            SortedItem sortedItem = null;
            int slot;

            foreach (var item in items.OrderBy(x => x.Slot))
            {
                if (unsortedItem.DestinationSlot < item.DestinationSlot)
                    break;

                sortedItem = item;
            }

            if (sortedItem != null)
            {
                var samePackOffset = unsortedItem.Slot < sortedItem.Slot && unsortedItem.Container == sortedItem.Container ? 0 : 1;
                slot = sortedItem.Slot + samePackOffset;
            }
            else
            {
                slot = 0;
            }

            return new MoveItemAction
            {
                ObjectId = unsortedItem.Id,
                PackId = unsortedItem.DestinationPack,
                Slot = slot
            };
        }

        static private IEnumerable<Filter> GetFilters()
        {
            TextReader tr = new StreamReader(@"C:\AC\TidyBags\Profile\Vehn.json");
            var data = tr.ReadToEnd();

            return JsonConvert.DeserializeObject<List<Filter>>(data);
        }
    }
}
