// BoardItem.cs
// Represents an item on the board (generator or product)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class BoardItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public Image itemImage;
    public TextMeshProUGUI levelText;
    public GridCell currentCell;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Transform originalParent;

    // Generator-specific
    private float productionTimer;
    private bool isProducing;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(ItemData data, GridCell cell)
    {
        itemData = data;
        currentCell = cell;
        itemImage.sprite = data.sprite;
        levelText.text = data.level.ToString();

        // Start production if this is a generator
        if (data.isGenerator)
        {
            StartProduction();
        }
    }

    void Update()
    {
        // Handle generator production
        if (isProducing && itemData.isGenerator)
        {
            productionTimer += Time.deltaTime;

            if (productionTimer >= itemData.productionTime)
            {
                ProduceProduct();
                productionTimer = 0f;
            }
        }
    }

    void StartProduction()
    {
        isProducing = true;
        productionTimer = 0f;
    }

    void ProduceProduct()
    {
        // Find an empty adjacent cell
        GridCell emptyCell = GameBoard.Instance.FindEmptyAdjacentCell(currentCell);

        if (emptyCell != null)
        {
            // Determine which product to create based on chances
            ItemData productToCreate = DetermineProduct();

            if (productToCreate != null)
            {
                GameBoard.Instance.CreateItem(productToCreate, emptyCell);
            }
        }
    }

    ItemData DetermineProduct()
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulativeChance = 0f;

        foreach (var chance in itemData.productionChances)
        {
            cumulativeChance += chance.chance;
            if (randomValue <= cumulativeChance)
            {
                return chance.productData;
            }
        }

        return null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.position;
        originalParent = transform.parent;

        // Make the item semi-transparent and allow raycasts to pass through
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to root canvas so it renders on top
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // The drop will be handled by GridCell.OnDrop
        // If no valid drop occurred, return to original position
        if (transform.parent == canvas.transform)
        {
            ReturnToOriginalPosition();
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.localPosition = Vector3.zero;
    }

    public void MoveToCell(GridCell targetCell)
    {
        if (currentCell != null)
        {
            currentCell.currentItem = null;
        }

        currentCell = targetCell;
        targetCell.SetItem(this);
    }
}