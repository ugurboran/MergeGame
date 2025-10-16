// GameBoard.cs
// Main board manager - handles grid, merging, and game logic
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance;

    [Header("Board Settings")]
    public int boardSize = 5; // 5x5 grid
    public GridLayoutGroup gridLayout;
    public GameObject cellPrefab;
    public GameObject itemPrefab;

    [Header("Starting Items")]
    public ItemData startingGenerator; // G1_1

    [Header("Item Databases")]
    public List<ItemData> allItemData; // All item data for saving/loading

    private GridCell[,] grid;
    private List<BoardItem> activeItems = new List<BoardItem>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        InitializeBoard();
        LoadGameState();
    }

    void InitializeBoard()
    {
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

        // Create starting generator at center position (2,2) if no save exists
        if (!System.IO.File.Exists(Application.persistentDataPath + "/boardState.json"))
        {
            CreateItem(startingGenerator, grid[2, 2]);
        }
    }

    public BoardItem CreateItem(ItemData data, GridCell cell)
    {
        GameObject itemObj = Instantiate(itemPrefab, cell.transform);
        BoardItem item = itemObj.GetComponent<BoardItem>();
        item.Initialize(data, cell);
        cell.SetItem(item);
        activeItems.Add(item);
        return item;
    }

    public void HandleDrop(BoardItem draggedItem, GridCell targetCell)
    {
        // Check if target cell has an item
        if (targetCell.currentItem != null && targetCell.currentItem != draggedItem)
        {
            BoardItem targetItem = targetCell.currentItem;

            // Check if items can be merged
            if (CanMerge(draggedItem, targetItem))
            {
                MergeItems(draggedItem, targetItem, targetCell);
            }
            else
            {
                // Swap items
                SwapItems(draggedItem, targetItem);
            }
        }
        else if (targetCell.currentItem == null)
        {
            // Move to empty cell
            draggedItem.MoveToCell(targetCell);
        }
        else
        {
            // Dropped on itself, return to original position
            draggedItem.ReturnToOriginalPosition();
        }
    }

    bool CanMerge(BoardItem item1, BoardItem item2)
    {
        // Items can merge if they are from the same family and same level
        return item1.itemData.familyID == item2.itemData.familyID &&
               item1.itemData.level == item2.itemData.level;
    }

    void MergeItems(BoardItem item1, BoardItem item2, GridCell targetCell)
    {
        // Find the next level item data
        string nextItemID = item1.itemData.familyID + "_" + (item1.itemData.level + 1);
        ItemData nextLevelData = allItemData.FirstOrDefault(data => data.itemID == nextItemID);

        if (nextLevelData != null)
        {
            // Remove both items
            activeItems.Remove(item1);
            activeItems.Remove(item2);
            item1.currentCell.currentItem = null;
            Destroy(item1.gameObject);
            Destroy(item2.gameObject);

            // Create new merged item
            CreateItem(nextLevelData, targetCell);

            // Optional: Play merge effect here
        }
    }

    void SwapItems(BoardItem item1, BoardItem item2)
    {
        GridCell cell1 = item1.currentCell;
        GridCell cell2 = item2.currentCell;

        // Swap the items
        item1.MoveToCell(cell2);
        item2.MoveToCell(cell1);
    }

    public GridCell FindEmptyAdjacentCell(GridCell sourceCell)
    {
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
            }
        }

        // Return random empty cell or null if none found
        return emptyCells.Count > 0 ? emptyCells[Random.Range(0, emptyCells.Count)] : null;
    }

    bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < boardSize && col >= 0 && col < boardSize;
    }

    // ==================== SAVE/LOAD SYSTEM ====================

    void OnApplicationQuit()
    {
        SaveGameState();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveGameState();
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
                column = item.currentCell.column,
                productionTimer = item.GetComponent<BoardItem>().GetType()
                    .GetField("productionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(item) as float? ?? 0f
            };
            state.items.Add(itemState);
        }

        string json = JsonUtility.ToJson(state, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/boardState.json", json);
        Debug.Log("Game state saved!");
    }

    public void LoadGameState()
    {
        string path = Application.persistentDataPath + "/boardState.json";

        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            BoardState state = JsonUtility.FromJson<BoardState>(json);

            foreach (var itemState in state.items)
            {
                ItemData data = allItemData.FirstOrDefault(d => d.itemID == itemState.itemID);
                if (data != null)
                {
                    GridCell cell = grid[itemState.row, itemState.column];
                    BoardItem item = CreateItem(data, cell);

                    // Restore production timer if it's a generator
                    if (data.isGenerator)
                    {
                        var field = item.GetType().GetField("productionTimer",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        field.SetValue(item, itemState.productionTimer);
                    }
                }
            }

            Debug.Log("Game state loaded!");
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
    public float productionTimer;
}