using System;
using Unity.Profiling;
namespace Scripts.GridSystem.Model
{
    public class MinHeap
    {
        public interface IHeapItem<T> : IComparable<T>
        {
            int HeapIndex { get; set; }
        }
        
        private readonly PathNode[] _items;
        private int _count;

        public MinHeap(int maxHeapSize)
        {
            _items = new PathNode[maxHeapSize];
            _count = 0;
        }

        public int Count => _count;

        public void Add(PathNode item)
        {
            item.HeapIndex = _count;
            _items[_count] = item;
            SortUp(item);
            _count++;
        }

        public PathNode RemoveFirst()
        {
            var firstItem = _items[0];
            _count--;
            _items[0] = _items[_count];
            _items[0].HeapIndex = 0;
            SortDown(_items[0]);
            return firstItem;
        }

        public void UpdateItem(PathNode item)
        {
            SortUp(item);
        }

        public void Clear()
        {
            _count = 0;
        }

        public bool Contains(PathNode item)
        {
            if (item.HeapIndex >= _count || item.HeapIndex < 0)
                return false;
            return Equals(_items[item.HeapIndex], item);
        }

        private void SortDown(PathNode item)
        {
            using (new ProfilerMarker("MinHeap.SortDown").Auto())
            {
                while (true)
                {
                    int childIndexLeft = item.HeapIndex * 2 + 1;
                    int childIndexRight = item.HeapIndex * 2 + 2;
                    int swapIndex = item.HeapIndex;
                    int itemHeapIndex = item.HeapIndex;

                    if (childIndexLeft < _count && _items[childIndexLeft].CompareTo(_items[swapIndex]) < 0)
                    {
                        swapIndex = childIndexLeft;
                    }

                    if (childIndexRight < _count && _items[childIndexRight].CompareTo(_items[swapIndex]) < 0)
                    {
                        swapIndex = childIndexRight;
                    }

                    if (swapIndex != item.HeapIndex)
                    {
                        Swap(item, itemHeapIndex, _items[swapIndex], swapIndex);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        private void SortUp(PathNode item)
        {
            using (new ProfilerMarker("MinHeap.SortUp").Auto())
            {
                while (true)
                {
                    var itemHeapIndex = item.HeapIndex;
                    if (itemHeapIndex == 0)
                    {
                        return;
                    }
                    
                    int parentIndex = (itemHeapIndex - 1) / 2;
                    var parentItem = _items[parentIndex];

                    if (item.CompareTo(parentItem) < 0)
                    {
                        Swap(item, itemHeapIndex, parentItem, parentIndex);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        private void Swap(PathNode itemA, int indexA, PathNode itemB, int indexB)
        {
            _items[indexA] = itemB;
            _items[indexB] = itemA;

            itemA.HeapIndex = indexB;
            itemB.HeapIndex = indexA;
        }
    }
}
