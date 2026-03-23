namespace ARPG;

public partial class PlayerInventory
{
    private readonly InventoryItem[] _slots;

    public int Capacity => _slots.Length;

    public PlayerInventory(int capacity)
    {
        _slots = new InventoryItem[capacity];
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length)
            return null;

        return _slots[slotIndex];
    }

    public bool TryAdd(InventoryItem item, out int slotIndex)
    {
        for (int i = 0; i < _slots.Length; i++)
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
        if (slotIndex < 0 || slotIndex >= _slots.Length)
            return null;

        var item = _slots[slotIndex];
        _slots[slotIndex] = null;
        return item;
    }
}
