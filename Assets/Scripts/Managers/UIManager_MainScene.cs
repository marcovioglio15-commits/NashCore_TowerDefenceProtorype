using System.Collections.Generic;
using Managers.UI;
using Player.Inventory;
using Scriptables.Turrets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinates the build UI by reacting to catalog, preview and drag events exposed through the EventsManager.
/// </summary>
public class UIManager_MainScene : Singleton<UIManager_MainScene>
{
    #region Variables And Properties
    #region Serialized Fields
    [Header("Inventory")]
    [Tooltip("Inventory provider used to refresh the buildable catalog when the UI becomes active.")]
    [SerializeField] private BuildablesInventory buildablesInventory;

    [Header("Build Bar")]
    [Tooltip("Horizontal layout container hosting turret icons.")]
    [SerializeField] private RectTransform buildablesContainer;
    [Tooltip("Prefab instantiated for each turret entry in the build bar.")]
    [SerializeField] private BuildableIconView iconPrefab;

    [Header("Drag Preview")]
    [Tooltip("Canvas hosting the build UI, required to convert screen positions into anchored coordinates.")]
    [SerializeField] private Canvas uiCanvas;
    [Tooltip("Layer used to display the drag preview sprite.")]
    [SerializeField] private RectTransform dragLayer;
    [Tooltip("Image shown while dragging to mirror the selected turret.")]
    [SerializeField] private Image dragPreviewImage;
    [Tooltip("Color applied to the drag preview when a cell is valid.")]
    [SerializeField] private Color validDragColor = Color.white;
    [Tooltip("Color applied to the drag preview when no valid cell is available.")]
    [SerializeField] private Color invalidDragColor = new Color(1f, 0.45f, 0.45f, 0.95f);
    #endregion

    #region Runtime
    private readonly List<BuildableIconView> activeIcons = new List<BuildableIconView>();
    private bool dragActive;
    #endregion
    #endregion

    #region Methods
    #region Unity
    /// <summary>
    /// Subscribes to catalog, drag and placement events as soon as the manager is enabled.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EventsManager.BuildablesCatalogChanged += HandleCatalogChanged;
        EventsManager.BuildableDragBegan += HandleDragBegan;
        EventsManager.BuildableDragUpdated += HandleDragUpdated;
        EventsManager.BuildableDragEnded += HandleDragEnded;
        EventsManager.BuildablePreviewUpdated += HandlePreviewUpdated;
        EventsManager.BuildablePlacementResolved += HandlePlacementResolved;
        if (buildablesInventory != null)
            buildablesInventory.RequestCatalogBroadcast();

        HideDragPreview();
    }

    /// <summary>
    /// Cleans up subscriptions when the manager is disabled.
    /// </summary>
    private void OnDisable()
    {
        EventsManager.BuildablesCatalogChanged -= HandleCatalogChanged;
        EventsManager.BuildableDragBegan -= HandleDragBegan;
        EventsManager.BuildableDragUpdated -= HandleDragUpdated;
        EventsManager.BuildableDragEnded -= HandleDragEnded;
        EventsManager.BuildablePreviewUpdated -= HandlePreviewUpdated;
        EventsManager.BuildablePlacementResolved -= HandlePlacementResolved;
    }
    #endregion

    #region Catalog
    /// <summary>
    /// Rebuilds the build bar when the catalog changes.
    /// </summary>
    private void HandleCatalogChanged(IReadOnlyList<TurretClassDefinition> catalog)
    {
        if (catalog == null || buildablesContainer == null || iconPrefab == null)
            return;

        for (int i = 0; i < activeIcons.Count; i++)
        {
            BuildableIconView icon = activeIcons[i];
            if (icon != null)
                Destroy(icon.gameObject);
        }

        activeIcons.Clear();

        for (int i = 0; i < catalog.Count; i++)
        {
            TurretClassDefinition definition = catalog[i];
            if (definition == null)
                continue;

            BuildableIconView instance = Instantiate(iconPrefab, buildablesContainer);
            instance.Bind(definition);
            activeIcons.Add(instance);
        }
    }
    #endregion

    #region Drag Handling
    /// <summary>
    /// Displays the drag preview when the user starts dragging a turret icon.
    /// </summary>
    private void HandleDragBegan(TurretClassDefinition definition, Vector2 screenPosition)
    {
        dragActive = true;
        UpdateDragPreviewSprite(definition, screenPosition);
    }

    /// <summary>
    /// Moves the drag preview while the drag gesture progresses.
    /// </summary>
    private void HandleDragUpdated(Vector2 screenPosition)
    {
        if (!dragActive)
            return;

        UpdateDragPreviewPosition(screenPosition);
    }

    /// <summary>
    /// Hides the drag preview when the gesture completes.
    /// </summary>
    private void HandleDragEnded(Vector2 screenPosition)
    {
        if (!dragActive)
            return;

        dragActive = false;
        HideDragPreview();
    }

    /// <summary>
    /// Adjusts drag tint based on preview validation results.
    /// </summary>
    private void HandlePreviewUpdated(BuildPreviewData preview)
    {
        if (!dragActive || dragPreviewImage == null)
            return;

        dragPreviewImage.color = preview.HasValidCell ? validDragColor : invalidDragColor;
    }

    /// <summary>
    /// Reports placement results in the console for early debug purposes.
    /// </summary>
    private void HandlePlacementResolved(BuildPlacementResult result)
    {
        if (!result.Success)
        {
            Debug.LogWarning($"Turret placement failed: {result.FailureReason}", this);
            return;
        }

        Debug.Log($"Placed turret {result.Definition.DisplayName} at cell {result.Cell}", this);
    }
    #endregion

    #region Drag Helpers
    /// <summary>
    /// Updates drag image sprite and resets tint when a new drag begins.
    /// </summary>
    private void UpdateDragPreviewSprite(TurretClassDefinition definition, Vector2 screenPosition)
    {
        if (dragPreviewImage == null)
            return;

        dragPreviewImage.sprite = definition != null ? definition.Icon : null;
        dragPreviewImage.color = validDragColor;
        dragPreviewImage.enabled = dragPreviewImage.sprite != null;
        UpdateDragPreviewPosition(screenPosition);
    }

    /// <summary>
    /// Converts a screen position into anchored coordinates relative to the drag layer.
    /// </summary>
    private void UpdateDragPreviewPosition(Vector2 screenPosition)
    {
        if (dragPreviewImage == null || dragLayer == null)
            return;

        Camera eventCamera = uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera ? uiCanvas.worldCamera : null;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPosition, eventCamera, out local))
            return;

        dragPreviewImage.rectTransform.anchoredPosition = local;
    }

    /// <summary>
    /// Hides the drag preview image.
    /// </summary>
    private void HideDragPreview()
    {
        if (dragPreviewImage == null)
            return;

        dragPreviewImage.enabled = false;
    }
    #endregion
    #endregion
}
