// BoardItem.cs
// Represents an item on the board (generator or product)
// NOW WITH: DOTween animations, Debug.Log statements, Mobile touch support
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // DOTween for animations

public class BoardItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public ItemData itemData;
    public Image itemImage;
    //public Text levelText;
    public GridCell currentCell;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Transform originalParent;

    // Generator-specific
    private bool canProduce = true; // Can this generator produce now?

    // Animation control
    private Sequence productionPulse;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

        Debug.Log($"[BoardItem] Awake - Item initialized");
    }

    public void Initialize(ItemData data, GridCell cell)
    {
        itemData = data;
        currentCell = cell;
        itemImage.sprite = data.sprite;
        //levelText.text = data.level.ToString();

        Debug.Log($"[BoardItem] Initialize - Created {data.itemID} at cell ({cell.row}, {cell.column})");

        // Play spawn animation
        PlaySpawnAnimation();

        // Visual feedback for generators (no automatic production)
        if (data.isGenerator)
        {
            Debug.Log($"[BoardItem] {data.itemID} is a generator - Click to produce!");
            PlayGeneratorIdlePulse();
        }
    }

    void Update()
    {
        // Generator production is now manual (click-based), so Update is not needed for production
        // Can be used for other animations or effects if needed
    }

    void PlayGeneratorIdlePulse()
    {
        // Subtle breathing animation for generators
        if (productionPulse != null)
            productionPulse.Kill();

        productionPulse = DOTween.Sequence();
        productionPulse.Append(transform.DOScale(1.05f, 1f).SetEase(Ease.InOutSine));
        productionPulse.Append(transform.DOScale(1f, 1f).SetEase(Ease.InOutSine));
        productionPulse.SetLoops(-1, LoopType.Restart);

        Debug.Log($"[BoardItem] Generator idle pulse started for {itemData.itemID}");
    }

    // Called when player clicks on a generator
    public void OnGeneratorClicked()
    {
        if (!itemData.isGenerator)
        {
            Debug.Log($"[BoardItem] {itemData.itemID} is not a generator - Click ignored");
            return;
        }

        if (!canProduce)
        {
            Debug.Log($"[BoardItem] {itemData.itemID} is on cooldown - Cannot produce yet");
            return;
        }

        Debug.Log($"[BoardItem] Generator {itemData.itemID} clicked - Attempting to produce");
        ProduceProduct();
    }

    void ProduceProduct()
    {
        // Check if board has any empty cells at all
        if (!GameBoard.Instance.HasEmptyCell())
        {
            Debug.Log($"[BoardItem] Board is FULL! {itemData.itemID} cannot produce");
            // Play "board full" feedback animation
            PlayBoardFullFeedback();
            return;
        }

        // Find an empty adjacent cell
        GridCell emptyCell = GameBoard.Instance.FindEmptyAdjacentCell(currentCell);

        if (emptyCell != null)
        {
            // Determine which product to create based on chances
            ItemData productToCreate = DetermineProduct();

            if (productToCreate != null)
            {
                Debug.Log($"[BoardItem] ? {itemData.itemID} producing {productToCreate.itemID} at cell ({emptyCell.row}, {emptyCell.column})");
                GameBoard.Instance.CreateItem(productToCreate, emptyCell);

                // Production flash effect
                PlayProductionFlash();

                canProduce = true; // Can produce again immediately after successful production
            }
            else
            {
                Debug.LogWarning($"[BoardItem] {itemData.itemID} failed to determine product to create");
            }
        }
        else
        {
            Debug.Log($"[BoardItem] {itemData.itemID} cannot produce - No empty adjacent cells (generator is surrounded)");
            // Play "blocked" feedback animation
            PlayBlockedFeedback();
        }
    }

    ItemData DetermineProduct()
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulativeChance = 0f;

        Debug.Log($"[BoardItem] Rolling for product: Random value = {randomValue:F2}");

        foreach (var chance in itemData.productionChances)
        {
            cumulativeChance += chance.chance;
            if (randomValue <= cumulativeChance)
            {
                Debug.Log($"[BoardItem] Selected {chance.productData.itemID} (chance: {chance.chance}%, cumulative: {cumulativeChance}%)");
                return chance.productData;
            }
        }

        Debug.LogError($"[BoardItem] No product selected - Check production chances setup!");
        return null;
    }

    // ==================== TOUCH & DRAG HANDLERS ====================

    // IPointerDownHandler - Supports both mouse and touch
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[BoardItem] Pointer down on {itemData.itemID}");

        // If this is a generator, try to produce
        if (itemData.isGenerator)
        {
            OnGeneratorClicked();

            // Quick scale feedback for generators
            transform.DOKill();
            transform.DOScale(0.9f, 0.1f).OnComplete(() => {
                transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            });
        }
        else
        {
            // Regular item - just visual feedback
            transform.DOKill();
            transform.DOScale(0.95f, 0.1f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[BoardItem] ?? Begin drag {itemData.itemID} from cell ({currentCell.row}, {currentCell.column})");

        originalPosition = rectTransform.position;
        originalParent = transform.parent;

        // Make the item semi-transparent and allow raycasts to pass through
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to root canvas so it renders on top
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling(); // Ensure it's on top

        // Scale up slightly and add rotation for drag feedback
        transform.DOKill();
        transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
        transform.DORotate(new Vector3(0, 0, 5f), 0.2f);

        Debug.Log($"[BoardItem] Dragging started - Parent: {transform.parent.name}, Alpha: {canvasGroup.alpha}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Works for both mouse and touch
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[BoardItem] ?? End drag {itemData.itemID} at position {eventData.position}");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Reset scale and rotation
        transform.DOKill();
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        transform.DORotate(Vector3.zero, 0.2f);

        // Check if we're still under Canvas (no valid drop occurred)
        // The drop handler in GridCell should have already handled the drop if valid
        if (transform.parent == canvas.transform)
        {
            Debug.Log($"[BoardItem] No valid drop target detected - Returning to original position");
            ReturnToOriginalPosition();
        }
        else
        {
            Debug.Log($"[BoardItem] Drop handled - Now under: {transform.parent.name}");
        }
    }

    public void ReturnToOriginalPosition()
    {
        Debug.Log($"[BoardItem] {itemData.itemID} returning to original position at ({currentCell.row}, {currentCell.column})");

        // Animate back to original position with a "bounce back" effect
        Sequence returnSeq = DOTween.Sequence();

        // First move to original position
        returnSeq.Append(transform.DOMove(originalPosition, 0.25f).SetEase(Ease.OutQuad));

        // Then settle into the cell
        returnSeq.OnComplete(() => {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            Debug.Log($"[BoardItem] {itemData.itemID} returned to cell ({currentCell.row}, {currentCell.column})");
        });
    }

    public void MoveToCell(GridCell targetCell)
    {
        Debug.Log($"[BoardItem] MoveToCell: {itemData.itemID} from ({currentCell.row}, {currentCell.column}) to ({targetCell.row}, {targetCell.column})");

        if (currentCell != null)
        {
            currentCell.currentItem = null;
            Debug.Log($"[BoardItem] Cleared old cell ({currentCell.row}, {currentCell.column})");
        }

        currentCell = targetCell;
        targetCell.currentItem = this;

        // Make sure item is child of target cell
        transform.SetParent(targetCell.transform);
        transform.localPosition = Vector3.zero;

        // Reset scale
        transform.DOKill();
        transform.localScale = Vector3.one;

        Debug.Log($"[BoardItem] Now in cell ({targetCell.row}, {targetCell.column}), parent: {transform.parent.name}");
    }

    // ==================== ANIMATION EFFECTS ====================

    void PlaySpawnAnimation()
    {
        // Start small and scale up with bounce
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        Debug.Log($"[BoardItem] Playing spawn animation for {itemData.itemID}");
    }

    void PlayProductionFlash()
    {
        // Quick flash effect when producing
        itemImage.DOKill();
        itemImage.DOColor(Color.yellow, 0.1f).OnComplete(() => {
            itemImage.DOColor(Color.white, 0.3f);
        });

        // Scale bump
        transform.DOKill();
        transform.DOScale(1.15f, 0.15f).OnComplete(() => {
            transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        });
    }

    void PlayBlockedFeedback()
    {
        // Shake animation when generator is blocked (surrounded by items)
        transform.DOKill();
        Sequence shakeSeq = DOTween.Sequence();
        shakeSeq.Append(transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.3f, 10, 1f));
        shakeSeq.Join(itemImage.DOColor(Color.red, 0.15f));
        shakeSeq.Append(itemImage.DOColor(Color.white, 0.15f));

        Debug.Log($"[BoardItem] Playing blocked feedback for {itemData.itemID}");
    }

    void PlayBoardFullFeedback()
    {
        // Different animation when entire board is full
        transform.DOKill();
        itemImage.DOKill();

        Sequence fullSeq = DOTween.Sequence();
        fullSeq.Append(itemImage.DOColor(new Color(1f, 0.5f, 0f), 0.15f)); // Orange
        fullSeq.Append(itemImage.DOColor(Color.white, 0.15f));
        fullSeq.SetLoops(2);

        Debug.Log($"[BoardItem] Playing board full feedback for {itemData.itemID}");
    }

    public void PlayCannotMergeFeedback()
    {
        // Shake animation when items can't merge
        transform.DOKill();
        itemImage.DOKill();

        Sequence cannotMergeSeq = DOTween.Sequence();

        // Red flash
        cannotMergeSeq.Append(itemImage.DOColor(Color.red, 0.1f));
        cannotMergeSeq.Append(itemImage.DOColor(Color.white, 0.1f));

        // Small shake
        cannotMergeSeq.Join(transform.DOShakePosition(0.2f, strength: 10f, vibrato: 10, randomness: 90f));

        Debug.Log($"[BoardItem] Playing cannot merge feedback for {itemData.itemID}");
    }

    public void PlayMergeAnimation(System.Action onComplete)
    {
        Debug.Log($"[BoardItem] Playing merge animation for {itemData.itemID}");

        // Stop production pulse if active
        if (productionPulse != null)
        {
            productionPulse.Kill();
        }

        // Merge animation: scale up, rotate, then scale down
        Sequence mergeSeq = DOTween.Sequence();
        mergeSeq.Append(transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
        mergeSeq.Join(transform.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360));
        mergeSeq.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        mergeSeq.OnComplete(() => {
            onComplete?.Invoke();
        });
    }

    void OnDestroy()
    {
        // Clean up DOTween animations
        transform.DOKill();
        if (itemImage != null)
            itemImage.DOKill();
        if (productionPulse != null)
            productionPulse.Kill();

        Debug.Log($"[BoardItem] Destroyed {itemData?.itemID ?? "Unknown"}");
    }
}