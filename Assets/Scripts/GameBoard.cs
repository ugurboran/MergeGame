// GameBoard.cs
// Main board manager - handles grid, merging, and game logic
// NOW WITH: DOTween animations, Debug.Log statements, Mobile touch support
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // DOTween for animations

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance;

    [Header("Board Settings")]
    public int boardSize = 5; // 5x5 grid
    public GridLayoutGroup gridLayout;
    public GameObject cellPrefab;
    public GameObject itemPrefab;

    [Header("Starting Items")]
    [Tooltip("Add your starting items here (G1_1, G1_2, G1_3, G1_4)")]
    public List<ItemData> starterItems; // Items to spawn at game start

    [Header("Item Databases")]
    public List<ItemData> allItemData; // All item data for saving/loading

    [Header("Visual Effects")]
    public ParticleSystem mergeParticles; // Optional: assign a particle system prefab

    private GridCell[,] grid;
    private List<BoardItem> activeItems = new List<BoardItem>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameBoard] Instance created");
        }
        else
        {
            Debug.LogWarning("[GameBoard] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("[GameBoard] Starting initialization...");
        InitializeBoard();
        LoadGameState();
        Debug.Log($"[GameBoard] Initialization complete - {activeItems.Count} items on board");
    }

    void InitializeBoard()
    {
        Debug.Log($"[GameBoard] Creating {boardSize}x{boardSize} grid...");
        grid = new GridCell[boardSize, boardSize];

        // Create grid cells
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridLayout.transform);
                GridCell cell = cellObj.GetComponent<GridCell>();
                cell.row = row;
                cell.column = col;
                grid[row, col] = cell;
            }
        }

        Debug.Log($"[GameBoard] Grid created - {boardSize * boardSize} cells");

        // Create starting items in specific positions if no save exists
        if (!System.IO.File.Exists(Application.persistentDataPath + "/boardState.json"))
        {
            Debug.Log($"[GameBoard] No save file found - Creating starting items");

            // Check if starterItems list exists and has items
            if (starterItems == null)
            {
                Debug.LogError("[GameBoard] ? ERROR: starterItems list is NULL! Assign items in Inspector.");
                return;
            }

            if (starterItems.Count == 0)
            {
                Debug.LogError("[GameBoard] ? ERROR: starterItems list is EMPTY! Add items to 'Starter Items' list in GameBoard Inspector.");
                Debug.LogError("[GameBoard] ?? HOW TO FIX: Select GameManager ? GameBoard component ? Set 'Starter Items' Size to 5 ? Drag G1_1, G1_1, G1_2, G1_3, G1_4");
                return;
            }

            Debug.Log($"[GameBoard] ? Starter items list has {starterItems.Count} items");

            // Show what's in the list
            for (int i = 0; i < starterItems.Count; i++)
            {
                if (starterItems[i] != null)
                    Debug.Log($"[GameBoard]   Element {i}: {starterItems[i].itemID} (Level {starterItems[i].level})");
                else
                    Debug.LogWarning($"[GameBoard]   Element {i}: NULL ??");
            }

            // Define starting positions for up to 5 items
            Vector2Int[] startPositions = new Vector2Int[]
            {
                new Vector2Int(1, 2), // Position 0: Row 1, Col 2
                new Vector2Int(2, 2), // Position 1: Row 2, Col 2
                new Vector2Int(2, 3), // Position 2: Row 2, Col 3
                new Vector2Int(3, 3), // Position 3: Row 3, Col 3
                new Vector2Int(3, 4)  // Position 4: Row 3, Col 4
            };

            Debug.Log($"[GameBoard] ?? Starting item creation loop...");

            // Create items at their positions
            int successCount = 0;
            for (int i = 0; i < starterItems.Count && i < startPositions.Length; i++)
            {
                ItemData itemData = starterItems[i];

                if (itemData == null)
                {
                    Debug.LogWarning($"[GameBoard] ?? Starter item at index {i} is NULL - skipping this position");
                    continue;
                }

                Vector2Int pos = startPositions[i];
                Debug.Log($"[GameBoard] ?? Creating {itemData.itemID} (Level {itemData.level}) at position ({pos.x}, {pos.y})...");

                BoardItem createdItem = CreateItem(itemData, grid[pos.x, pos.y]);

                if (createdItem != null)
                {
                    successCount++;
                    Debug.Log($"[GameBoard] ? SUCCESS! Created {itemData.itemID} at ({pos.x}, {pos.y})");
                }
                else
                {
                    Debug.LogError($"[GameBoard] ? FAILED to create {itemData.itemID} at ({pos.x}, {pos.y})");
                }
            }

            Debug.Log($"[GameBoard] ?? Starting item creation complete!");
            Debug.Log($"[GameBoard] ?? Created {successCount} out of {starterItems.Count} items");
            Debug.Log($"[GameBoard] ?? Total items on board: {activeItems.Count}");

            if (successCount < starterItems.Count)
            {
                Debug.LogWarning($"[GameBoard] ?? Some items failed to create! Check above logs for details.");
            }
        }
        else
        {
            Debug.Log("[GameBoard] Save file found - Will load from save");
        }
    }

    public BoardItem CreateItem(ItemData data, GridCell cell)
    {
        if (data == null)
        {
            Debug.LogError("[GameBoard] Cannot create item - ItemData is null!");
            return null;
        }

        if (cell == null)
        {
            Debug.LogError("[GameBoard] Cannot create item - Target cell is null!");
            return null;
        }

        if (!cell.IsEmpty())
        {
            Debug.LogWarning($"[GameBoard] Cell ({cell.row}, {cell.column}) is not empty - Cannot create item");
            return null;
        }

        GameObject itemObj = Instantiate(itemPrefab, cell.transform);
        BoardItem item = itemObj.GetComponent<BoardItem>();
        item.Initialize(data, cell);
        cell.SetItem(item);
        activeItems.Add(item);

        Debug.Log($"[GameBoard] Created {data.itemID} at ({cell.row}, {cell.column}) - Total items: {activeItems.Count}");

        return item;
    }

    public void HandleDrop(BoardItem draggedItem, GridCell targetCell)
    {
        Debug.Log($"[GameBoard] HandleDrop - {draggedItem.itemData.itemID} dropped on cell ({targetCell.row}, {targetCell.column})");

        // Check if target cell has an item
        if (targetCell.currentItem != null && targetCell.currentItem != draggedItem)
        {
            BoardItem targetItem = targetCell.currentItem;
            Debug.Log($"[GameBoard] Target cell has {targetItem.itemData.itemID}");

            // Check if items can be merged
            if (CanMerge(draggedItem, targetItem))
            {
                Debug.Log($"[GameBoard] Items can merge! {draggedItem.itemData.itemID} + {targetItem.itemData.itemID}");
                MergeItems(draggedItem, targetItem, targetCell);
            }
            else
            {
                Debug.Log($"[GameBoard] Items cannot merge - Swapping positions");
                // Swap items
                SwapItems(draggedItem, targetItem);
            }
        }
        else if (targetCell.currentItem == null)
        {
            Debug.Log($"[GameBoard] Target cell is empty - Moving item");
            // Move to empty cell
            MoveItemToCell(draggedItem, targetCell);
        }
        else
        {
            Debug.Log($"[GameBoard] Item dropped on itself - Returning to original");
            // Dropped on itself, return to original position
            draggedItem.ReturnToOriginalPosition();
        }
    }

    bool CanMerge(BoardItem item1, BoardItem item2)
    {
        bool canMerge = item1.itemData.familyID == item2.itemData.familyID &&
                        item1.itemData.level == item2.itemData.level;

        Debug.Log($"[GameBoard] CanMerge Check: {item1.itemData.itemID} + {item2.itemData.itemID} = {canMerge}");
        Debug.Log($"[GameBoard]   - Family match: {item1.itemData.familyID} == {item2.itemData.familyID} = {item1.itemData.familyID == item2.itemData.familyID}");
        Debug.Log($"[GameBoard]   - Level match: {item1.itemData.level} == {item2.itemData.level} = {item1.itemData.level == item2.itemData.level}");

        return canMerge;
    }

    void MergeItems(BoardItem item1, BoardItem item2, GridCell targetCell)
    {
        // Find the next level item data
        string nextItemID = item1.itemData.familyID + "_" + (item1.itemData.level + 1);
        ItemData nextLevelData = allItemData.FirstOrDefault(data => data.itemID == nextItemID);

        Debug.Log($"[GameBoard] Merging to create: {nextItemID}");

        if (nextLevelData != null)
        {
            Debug.Log($"[GameBoard] Found next level data: {nextLevelData.itemID}");

            // Play merge animations for both items
            item1.PlayMergeAnimation(() => {
                // After item1 animation completes, destroy it
                activeItems.Remove(item1);
                item1.currentCell.currentItem = null;
                Destroy(item1.gameObject);
                Debug.Log($"[GameBoard] Destroyed first merge item");
            });

            item2.PlayMergeAnimation(() => {
                // After item2 animation completes, destroy it and create merged item
                activeItems.Remove(item2);
                Destroy(item2.gameObject);
                Debug.Log($"[GameBoard] Destroyed second merge item");

                // Create new merged item with delay for better visual effect
                DOVirtual.DelayedCall(0.1f, () => {
                    BoardItem newItem = CreateItem(nextLevelData, targetCell);
                    Debug.Log($"[GameBoard] ? MERGE SUCCESSFUL! Created {nextLevelData.itemID} at level {nextLevelData.level}");

                    // Play merge particles if available
                    if (mergeParticles != null)
                    {
                        ParticleSystem particles = Instantiate(mergeParticles, targetCell.transform.position, Quaternion.identity);
                        Destroy(particles.gameObject, 2f);
                    }
                });
            });
        }
        else
        {
            Debug.LogError($"[GameBoard] ? ERROR: Could not find next level item: {nextItemID}");
            Debug.LogError($"[GameBoard] Check that {nextItemID} exists in allItemData list!");

            // Return items to original positions
            item1.ReturnToOriginalPosition();
            item2.ReturnToOriginalPosition();
        }
    }

    void SwapItems(BoardItem item1, BoardItem item2)
    {
        GridCell cell1 = item1.currentCell;
        GridCell cell2 = item2.currentCell;

        Debug.Log($"[GameBoard] Swapping: {item1.itemData.itemID} at ({cell1.row},{cell1.column}) ? {item2.itemData.itemID} at ({cell2.row},{cell2.column})");

        // Animate the swap
        Vector3 pos1 = item1.transform.position;
        Vector3 pos2 = item2.transform.position;

        // Move item1 to cell2's position, then to cell2
        item1.transform.DOMove(pos2, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
            item1.MoveToCell(cell2);
        });

        // Move item2 to cell1's position, then to cell1
        item2.transform.DOMove(pos1, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
            item2.MoveToCell(cell1);
        });

        Debug.Log($"[GameBoard] Swap animation started");
    }

    void MoveItemToCell(BoardItem item, GridCell targetCell)
    {
        Debug.Log($"[GameBoard] Moving {item.itemData.itemID} from ({item.currentCell.row}, {item.currentCell.column}) to ({targetCell.row}, {targetCell.column})");

        // Clear the old cell
        if (item.currentCell != null)
        {
            item.currentCell.currentItem = null;
            Debug.Log($"[GameBoard] Cleared old cell ({item.currentCell.row}, {item.currentCell.column})");
        }

        // Move to new cell immediately (no animation during drag)
        item.transform.SetParent(targetCell.transform);
        item.transform.localPosition = Vector3.zero;
        item.currentCell = targetCell;
        targetCell.currentItem = item;

        Debug.Log($"[GameBoard] ? Item successfully moved to ({targetCell.row}, {targetCell.column})");
    }

    public GridCell FindEmptyAdjacentCell(GridCell sourceCell)
    {
        Debug.Log($"[GameBoard] Searching for empty cell adjacent to ({sourceCell.row}, {sourceCell.column})");

        // Check all 8 adjacent cells (and diagonals)
        int[] rowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] colOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

        List<GridCell> emptyCells = new List<GridCell>();

        for (int i = 0; i < 8; i++)
        {
            int newRow = sourceCell.row + rowOffsets[i];
            int newCol = sourceCell.column + colOffsets[i];

            if (IsValidCell(newRow, newCol) && grid[newRow, newCol].IsEmpty())
            {
                emptyCells.Add(grid[newRow, newCol]);
                Debug.Log($"[GameBoard]   - Found empty cell at ({newRow}, {newCol})");
            }
        }

        if (emptyCells.Count == 0)
        {
            Debug.Log($"[GameBoard]   - No empty adjacent cells found!");
            return null;
        }

        // Return random empty cell
        GridCell selectedCell = emptyCells[Random.Range(0, emptyCells.Count)];
        Debug.Log($"[GameBoard]   - Selected cell ({selectedCell.row}, {selectedCell.column}) from {emptyCells.Count} options");
        return selectedCell;
    }

    public bool HasEmptyCell()
    {
        // Check if there's ANY empty cell on the entire board
        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                if (grid[row, col].IsEmpty())
                {
                    Debug.Log($"[GameBoard] Board has empty cells - Found at ({row}, {col})");
                    return true;
                }
            }
        }

        Debug.Log($"[GameBoard] ?? BOARD IS COMPLETELY FULL!");
        return false;
    }

    bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < boardSize && col >= 0 && col < boardSize;
    }

    // ==================== SAVE/LOAD SYSTEM ====================

    void OnApplicationQuit()
    {
        Debug.Log("[GameBoard] Application quitting - Saving game state...");
        SaveGameState();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("[GameBoard] Application paused - Saving game state...");
            SaveGameState();
        }
        else
        {
            Debug.Log("[GameBoard] Application resumed");
        }
    }

    public void SaveGameState()
    {
        BoardState state = new BoardState();
        state.items = new List<ItemState>();

        foreach (var item in activeItems)
        {
            ItemState itemState = new ItemState
            {
                itemID = item.itemData.itemID,
                row = item.currentCell.row,
                column = item.currentCell.column
            };
            state.items.Add(itemState);
        }

        string json = JsonUtility.ToJson(state, true);
        string path = Application.persistentDataPath + "/boardState.json";
        System.IO.File.WriteAllText(path, json);

        Debug.Log($"[GameBoard] ?? Game state saved! ({activeItems.Count} items)");
        Debug.Log($"[GameBoard] Save location: {path}");
    }

    public void LoadGameState()
    {
        string path = Application.persistentDataPath + "/boardState.json";

        if (System.IO.File.Exists(path))
        {
            Debug.Log($"[GameBoard] ?? Loading game state from: {path}");

            string json = System.IO.File.ReadAllText(path);
            BoardState state = JsonUtility.FromJson<BoardState>(json);

            Debug.Log($"[GameBoard] Found {state.items.Count} items to load");

            foreach (var itemState in state.items)
            {
                ItemData data = allItemData.FirstOrDefault(d => d.itemID == itemState.itemID);
                if (data != null)
                {
                    GridCell cell = grid[itemState.row, itemState.column];
                    BoardItem item = CreateItem(data, cell);

                    Debug.Log($"[GameBoard]   - Loaded {itemState.itemID} at ({itemState.row}, {itemState.column})");
                }
                else
                {
                    Debug.LogWarning($"[GameBoard]   - Could not find ItemData for {itemState.itemID}");
                }
            }

            Debug.Log($"[GameBoard] ? Game state loaded successfully!");
        }
        else
        {
            Debug.Log($"[GameBoard] No save file found at: {path}");
        }
    }

    // Helper method to manually clear save (useful for testing)
    [ContextMenu("Clear Save Data")]
    public void ClearSaveData()
    {
        string path = Application.persistentDataPath + "/boardState.json";
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("[GameBoard] ??? Save data cleared!");
        }
        else
        {
            Debug.Log("[GameBoard] No save data to clear");
        }
    }
}

[System.Serializable]
public class BoardState
{
    public List<ItemState> items;
}

[System.Serializable]
public class ItemState
{
    public string itemID;
    public int row;
    public int column;
    // Production timer removed - no longer needed for click-based production
}