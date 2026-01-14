[System.Serializable]
public class Item
{
    public ItemBase Base;
    public int Quantity;

    public Item(ItemBase baseItem, int quantity)
    {
        Base = baseItem;
        Quantity = quantity;
    }
}