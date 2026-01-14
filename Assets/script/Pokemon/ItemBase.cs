using UnityEngine;

public enum ItemType { Healing, Pokeball, StatusHeal, Revive, KeyItem }

[CreateAssetMenu(fileName = "New Item", menuName = "Pokemon/Item")]

public class ItemBase : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;
    public int price;
    public bool consumable = true;

    [Header("Healing")]
    public int healAmount;          // e.g., 20 for Potion
    public bool healToFull;         // e.g., Full Restore

    [Header("Revive")]
    public bool isRevive;
    public int revivePercent;       // e.g., 50 for Revive, 100 for Max Revive

    [Header("Status Heal")]
    public bool curesAllStatus;     // Full Heal
    public StatusEffect[] curesSpecific; // e.g., { Poison } for Antidote

    [Header("Pokeball")]
    public float catchRateMultiplier; // 1.0 Poké Ball, 1.5 Great Ball, 2.0 Ultra Ball

}