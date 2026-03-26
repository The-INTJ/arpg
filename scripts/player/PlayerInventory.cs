using System;
using System.Collections.Generic;

namespace ARPG;

public partial class PlayerInventory
{
    private readonly List<InventoryItem> _slots = new();

    public int Capacity => _slots.Count;
    public int OccupiedSlotCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null)
                    count++;
            }

            return count;
        }
    }
    public int MinimumRequiredCapacity => HighestOccupiedIndex() + 1;

    public PlayerInventory(int capacity)
    {
        SetCapacity(capacity);
    }

    public void SetCapacity(int requestedCapacity)
    {
        int safeRequestedCapacity = Math.Max(1, requestedCapacity);
        int requiredCapacity = Math.Max(safeRequestedCapacity, MinimumRequiredCapacity);

        while (_slots.Count < requiredCapacity)
            _slots.Add(null);

        while (_slots.Count > requiredCapacity)
            _slots.RemoveAt(_slots.Count - 1);
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
            return null;

        return _slots[slotIndex];
    }

    public bool TryAdd(InventoryItem item, out int slotIndex)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
                continue;

            _slots[i] = item;
            slotIndex = i;
            return true;
        }

        slotIndex = -1;
        return false;
    }

    public InventoryItem RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
            return null;

        var item = _slots[slotIndex];
        _slots[slotIndex] = null;
        return item;
    }

    private int HighestOccupiedIndex()
    {
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            if (_slots[i] != null)
                return i;
        }

        return -1;
    }
}
