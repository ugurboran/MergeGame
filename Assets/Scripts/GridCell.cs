// GridCell.cs
// Represents a single cell on the game board
// NOW WITH: Debug.Log statements, Mobile touch support, Visual feedback
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // DOTween for animations

public class GridCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int row;
    public int column;
    public Image backgroundImage;
    public BoardItem currentItem;

    private Color normalColor = new Color(0.4f, 0.8f, 0.9f, 0.3f);  // Light cyan/blue
    private Color highlightColor = new Color(0.6f, 0.9f, 1f, 0.8f); // Bright cyan
    private Color validDropColor = new Color(0.3f, 1f, 0.3f, 0.6f); // Green for valid merge
    private Color invalidDropColor = new Color(1f, 0.3f, 0.3f, 0.6f); // Red for swap

    void Start()
    {
        backgroundImage.color = normalColor;
        Debug.Log($"[GridCell] Cell ({row}, {column}) initialized");
    }

    public void SetItem(BoardItem item)
    {
        currentItem = item;
        if (item != null)
        {
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.currentCell = this;
            Debug.Log($"[GridCell] Cell ({row}, {column}) now contains {item.itemData.itemID}");
        }
        else
        {
            Debug.Log($"[GridCell] Cell ({row}, {column}) is now empty");
        }
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public void Highlight(bool highlight)
    {
        backgroundImage.DOKill(); // Stop any running animations

        if (highlight)
        {
            backgroundImage.DOColor(highlightColor, 0.2f);
            Debug.Log($"[GridCell] Cell ({row}, {column}) highlighted");
        }
        else
        {
            backgroundImage.DOColor(normalColor, 0.2f);
            Debug.Log($"[GridCell] Cell ({row}, {column}) unhighlighted");
        }
    }

    // Called when pointer enters the cell (works for touch and mouse)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only highlight if something is being dragged
        if (eventData.pointerDrag != null)
        {
            BoardItem draggedItem = eventData.pointerDrag.GetComponent<BoardItem>();
            if (draggedItem != null)
            {
                // Check if this would be a valid merge or swap
                if (currentItem != null && currentItem != draggedItem)
                {
                    bool canMerge = draggedItem.itemData.familyID == currentItem.itemData.familyID &&
                                   draggedItem.itemData.level == currentItem.itemData.level;

                    // Show different color based on action type
                    backgroundImage.DOKill();
                    backgroundImage.DOColor(canMerge ? validDropColor : invalidDropColor, 0.2f);

                    Debug.Log($"[GridCell] Dragging over ({row}, {column}) - Would {(canMerge ? "MERGE" : "SWAP")}");
                }
                else if (IsEmpty())
                {
                    // Empty cell - valid move
                    backgroundImage.DOKill();
                    backgroundImage.DOColor(highlightColor, 0.2f);
                    Debug.Log($"[GridCell] Dragging over empty cell ({row}, {column})");
                }
            }
        }
    }

    // Called when pointer exits the cell
    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset to normal color
        backgroundImage.DOKill();
        backgroundImage.DOColor(normalColor, 0.2f);
    }

    // Called when an item is dropped on this cell (works for touch and mouse)
    public void OnDrop(PointerEventData eventData)
    {
        BoardItem draggedItem = eventData.pointerDrag?.GetComponent<BoardItem>();
        if (draggedItem != null)
        {
            Debug.Log($"[GridCell] Item dropped on cell ({row}, {column})");
            GameBoard.Instance.HandleDrop(draggedItem, this);
        }
        else
        {
            Debug.LogWarning($"[GridCell] Drop event on ({row}, {column}) but no BoardItem found");
        }

        // Reset color after drop
        backgroundImage.DOKill();
        backgroundImage.DOColor(normalColor, 0.2f);
    }

    void OnDestroy()
    {
        // Clean up DOTween animations
        if (backgroundImage != null)
            backgroundImage.DOKill();
    }
}