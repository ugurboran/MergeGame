// ItemData.cs
// Base Scriptable Object for all items (generators and products)
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "MergeGame/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemID;           // Unique identifier (e.g., "G1_5", "P1_3")
    public int level;               // Current level of the item
    public Sprite sprite;           // Visual representation
    public ItemType itemType;       // Generator or Product
    public string familyID;         // Family identifier (e.g., "G1", "P1", "P2")

    [Header("Generator Settings")]
    public bool isGenerator;        // True if this item can produce products
    public float productionTime;    // Time in seconds to produce a product
    public ProductionChance[] productionChances; // What products can be generated
}

[System.Serializable]
public class ProductionChance
{
    public ItemData productData;
    public float chance;  // Percentage (0-100)
}

public enum ItemType
{
    Generator,
    Product
}