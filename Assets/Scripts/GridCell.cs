// GridCell.cs
// Represents a single cell on the game board
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridCell : MonoBehaviour, IDropHandler
{
    public int row;
    public int column;
    public Image backgroundImage;
    public BoardItem currentItem;

    private Color normalColor = new Color(0.4f, 0.8f, 0.9f, 0.3f);  // Light cyan/blue
    private Color highlightColor = new Color(0.6f, 0.9f, 1f, 0.8f); // Bright cyan

    void Start()
    {
        backgroundImage.color = normalColor;
    }

    public void SetItem(BoardItem item)
    {
        currentItem = item;
        if (item != null)
        {
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.currentCell = this;
        }
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public void Highlight(bool highlight)
    {
        backgroundImage.color = highlight ? highlightColor : normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        BoardItem draggedItem = eventData.pointerDrag?.GetComponent<BoardItem>();
        if (draggedItem != null)
        {
            GameBoard.Instance.HandleDrop(draggedItem, this);
        }
    }
}