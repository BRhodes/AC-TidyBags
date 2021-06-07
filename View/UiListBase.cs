using System.Collections.Generic;
using VirindiViewService.Controls;

namespace TidyBags.View
{
    abstract class UiListBase<Item, ViewRow> where ViewRow : IEmbedded<Item>
    {
        protected HudList _hudList { get; set; }
        protected readonly List<ViewRow> DisplayedRows = new List<ViewRow>();

        protected UiListBase(HudList hudList)
        {
            _hudList = hudList;
        }

        public void RemoveItem(int index)
        {
            RemoveHandlers(_hudList[index], DisplayedRows[index]);

            DisplayedRows.RemoveAt(index);
            _hudList.RemoveRow(index);
        }

        abstract protected void RemoveHandlers(HudList.HudListRowAccessor row, ViewRow quest);

        abstract protected List<Item> GetItems();

        abstract protected void DrawItem(HudList.HudListRowAccessor row, ViewRow item);
        internal void RedrawItems()
        {
            var itemList = GetItems();

            RefreshDrawList(itemList, DisplayedRows);

            for (int x = 0; x < DisplayedRows.Count; x++)
            {
                HudList.HudListRowAccessor row = _hudList[x];
                DrawItem(row, DisplayedRows[x]);
            }
        }

        abstract protected int Compare(Item i, Item row);
        abstract protected List<Item> Filter(List<Item> items);
        private void Sort(List<Item> items)
        {
            items.Sort((a, b) => Compare(a, b));
        }


        private void RefreshDrawList(List<Item> newList, List<ViewRow> old)
        {
            // Todo: Adjust scrollbar Afterwards
            var filteredItems = Filter(newList);
            Sort(filteredItems);

            for (int i = 0; i < filteredItems.Count; i++)
            {
                var comp = old.Count > i ? Compare(filteredItems[i], old[i].Item) : int.MinValue;
                if (comp == 0)
                {
                    // do nothing
                }
                else if (comp < 0)
                {
                    if (old.Count > i)
                        Util.WriteToChat($"Adding: {filteredItems[i]} in front of {old[i]}");
                    else
                        Util.WriteToChat($"Adding: {filteredItems[i]}");
                    AddItemAt(i, filteredItems[i]);
                }
                else if (comp > 0)
                {
                    Util.WriteToChat($"Removing: {old[i]}");
                    RemoveItem(i);
                    i--;
                }
            }
            while (old.Count > filteredItems.Count)
            {
                Util.WriteToChat($"Removing excess: {old[filteredItems.Count]}");
                RemoveItem(filteredItems.Count);
            }
        }

        public void AddItemAt(int index, Item item)
        {
            var viewRow = CreateViewRow(item);

            DisplayedRows.Insert(index, viewRow);
            var row = _hudList.InsertRow(index);
            SetupRow(row, viewRow);
        }

        abstract protected void SetupRow(HudList.HudListRowAccessor row, ViewRow viewRow);
        abstract protected ViewRow CreateViewRow(Item item);

    }

    internal interface IEmbedded<T>
    {
        T Item { get; }
    }
}
