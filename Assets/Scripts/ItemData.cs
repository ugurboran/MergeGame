// ItemData.cs
// Base Scriptable Object for all items (generators and products)
// This defines the data structure for all items in the game
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "MergeGame/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemID;           // Unique identifier (e.g., "G1_5", "P1_3")
    public int level;               // Current level of the item (1-10)
    public Sprite sprite;           // Visual representation
    public ItemType itemType;       // Generator or Product
    public string familyID;         // Family identifier (e.g., "G1", "P1", "P2")

    [Header("Generator Settings")]
    public bool isGenerator;        // True if this item can produce products
    public float productionTime;    // Time in seconds to produce a product
    public ProductionChance[] productionChances; // What products can be generated and their probabilities
}

// This class defines what products a generator can create and with what probability
[System.Serializable]
public class ProductionChance
{
    public ItemData productData;    // Reference to the product ItemData
    public float chance;            // Percentage chance (0-100)
}

// Enum to distinguish between generators and products
public enum ItemType
{
    Generator,
    Product
}