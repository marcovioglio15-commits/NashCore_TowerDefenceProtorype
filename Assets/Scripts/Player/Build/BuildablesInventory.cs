using System.Collections.Generic;
using Grid;
using Scriptables.Turrets;
using UnityEngine;

namespace Player.Inventory
{
    /// <summary>
    /// Central authority containing the list of buildable turrets and serving placement requests driven by the UI.
    /// </summary>
    [RequireComponent(typeof(TurretPlacementLogic))]
    public class BuildablesInventory : MonoBehaviour
    {
        #region Variables And Properties
        #region Serialized Fields
        [Header("Catalog")]
        [Tooltip("Turret classes available to the player at runtime.")]
        [SerializeField] private List<TurretClassDefinition> buildableTurrets = new List<TurretClassDefinition>();

        [Header("Placement")]
        [Tooltip("Camera used to project drag positions onto the grid plane.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Maximum distance in meters allowed between the drop point and the snapped cell center.")]
        [SerializeField] private float snapRadius = 1.5f;
        [Tooltip("Additional cells scanned around the pointer when resolving a snap target.")]
        [SerializeField] private int snapCellExpansion = 1;

        [Header("Debug")]
        [Tooltip("Visual color used for valid previews.")] 
        [SerializeField] private Color validPreviewColor = new Color(0.3f, 0.9f, 0.4f, 0.65f);
        [Tooltip("Visual color used for invalid previews.")]
        [SerializeField] private Color invalidPreviewColor = new Color(0.9f, 0.3f, 0.3f, 0.5f);
        [Tooltip("Draws gizmos for the last evaluated preview cell when selected.")]
        [SerializeField] private bool drawPreviewGizmos = true;
        #endregion

        #region Runtime State
        private TurretPlacementLogic placementService;
        private Grid3D grid;
        private readonly List<TurretClassDefinition> catalogBuffer = new List<TurretClassDefinition>();
        private TurretClassDefinition activeDefinition;
        private bool dragActive;
        private bool hasPreview;
        private Vector3 previewWorldPosition;
        private Vector2Int previewCell;
        private string lastFailureReason = "Placement unavailable";
        private bool buildPhaseActive = true;
        #endregion
        #endregion

        #region Methods
        #region Unity
        /// <summary>
        /// Caches dependencies required for placement validation.
        /// </summary>
        private void Awake()
        {
            placementService = GetComponent<TurretPlacementLogic>();
            if (placementService != null)
                grid = placementService.Grid;
        }

        /// <summary>
        /// Subscribes to build-related events to drive placement logic.
        /// </summary>
        private void OnEnable()
        {
            EventsManager.BuildableDragBegan += HandleDragBegan;
            EventsManager.BuildableDragUpdated += HandleDragUpdated;
            EventsManager.BuildableDragEnded += HandleDragEnded;
            EventsManager.GamePhaseChanged += HandleGamePhaseChanged;
            SyncPhaseState();
            BroadcastCatalog();
        }

        /// <summary>
        /// Cleans up listeners when the component is disabled or destroyed.
        /// </summary>
        private void OnDisable()
        {
            EventsManager.BuildableDragBegan -= HandleDragBegan;
            EventsManager.BuildableDragUpdated -= HandleDragUpdated;
            EventsManager.BuildableDragEnded -= HandleDragEnded;
            EventsManager.GamePhaseChanged -= HandleGamePhaseChanged;
        }

        /// <summary>
        /// Clamps serialized data whenever values change in the inspector.
        /// </summary>
        private void OnValidate()
        {
            if (snapRadius < 0.05f)
                snapRadius = 0.05f;

            if (snapCellExpansion < 0)
                snapCellExpansion = 0;
        }
        #endregion

        #region Public 
        /// <summary>
        /// Forces a catalog broadcast so late subscribers can catch up.
        /// </summary>
        public void RequestCatalogBroadcast()
        {
            BroadcastCatalog();
        }

        /// <summary>
        /// Enables or disables placement actions based on the current phase.
        /// </summary>
        public void SetBuildPhaseActive(bool enabled)
        {
            if (buildPhaseActive == enabled)
                return;

            buildPhaseActive = enabled;
            if (!buildPhaseActive)
                ResetDragState();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Called when the UI begins dragging a turret icon.
        /// </summary>
        private void HandleDragBegan(TurretClassDefinition definition, Vector2 screenPosition)
        {
            if (!buildPhaseActive)
                return;

            if (definition == null)
                return;

            if (!buildableTurrets.Contains(definition))
                return;

            activeDefinition = definition;
            dragActive = true;
            EvaluatePreview(screenPosition);
        }

        /// <summary>
        /// Processes drag updates and refreshes the preview.
        /// </summary>
        private void HandleDragUpdated(Vector2 screenPosition)
        {
            if (!buildPhaseActive)
                return;

            if (!dragActive)
                return;

            EvaluatePreview(screenPosition);
        }

        /// <summary>
        /// Finalizes placement attempts when the drag gesture ends.
        /// </summary>
        private void HandleDragEnded(Vector2 screenPosition)
        {
            if (!buildPhaseActive)
                return;

            if (!dragActive)
                return;

            TryCommitPlacement();
            ResetDragState();
        }

        /// <summary>
        /// Updates internal flags when the global game phase changes.
        /// </summary>
        private void HandleGamePhaseChanged(GamePhase phase)
        {
            SetBuildPhaseActive(phase == GamePhase.Building);
        }
        #endregion

        #region Catalog
        /// <summary>
        /// Broadcasts the catalog to UI subscribers.
        /// </summary>
        private void BroadcastCatalog()
        {
            catalogBuffer.Clear();
            int count = buildableTurrets != null ? buildableTurrets.Count : 0;
            for (int i = 0; i < count; i++)
            {
                TurretClassDefinition definition = buildableTurrets[i];
                if (definition == null)
                    continue;

                if (catalogBuffer.Contains(definition))
                    continue;

                catalogBuffer.Add(definition);
            }

            EventsManager.InvokeBuildablesCatalogChanged(catalogBuffer);
        }
        #endregion

        #region Preview And Placement
        /// <summary>
        /// Evaluates the closest valid cell to the drag position and notifies the UI.
        /// </summary>
        private void EvaluatePreview(Vector2 screenPosition)
        {
            if (activeDefinition == null || placementService == null || grid == null)
            {
                hasPreview = false;
                lastFailureReason = "Missing placement dependencies";
                RaisePreviewEvent();
                return;
            }

            Vector3 worldPoint;
            if (!TryProjectToGridPlane(screenPosition, out worldPoint))
            {
                hasPreview = false;
                lastFailureReason = "Cannot reach grid plane";
                RaisePreviewEvent();
                return;
            }

            Vector2Int snappedCell;
            Vector3 snappedWorld;
            string failureMessage;
            if (!FindSnapCell(worldPoint, activeDefinition, out snappedCell, out snappedWorld, out failureMessage))
            {
                hasPreview = false;
                lastFailureReason = failureMessage;
                RaisePreviewEvent();
                return;
            }

            hasPreview = true;
            previewCell = snappedCell;
            previewWorldPosition = snappedWorld;
            lastFailureReason = string.Empty;
            RaisePreviewEvent();
        }

        /// <summary>
        /// Projects the provided screen position onto the grid plane.
        /// </summary>
        private bool TryProjectToGridPlane(Vector2 screenPosition, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (grid == null)
                return false;

            Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null)
                return false;

            Plane gridPlane = new Plane(Vector3.up, grid.Origin);
            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            float hitDistance;
            if (!gridPlane.Raycast(ray, out hitDistance))
                return false;

            worldPosition = ray.GetPoint(hitDistance);
            return true;
        }

        /// <summary>
        /// Searches nearby cells for a valid placement candidate.
        /// </summary>
        private bool FindSnapCell(Vector3 worldPoint, TurretClassDefinition definition, out Vector2Int snappedCell, out Vector3 snappedWorld, out string failureReason)
        {
            snappedCell = Vector2Int.zero;
            snappedWorld = worldPoint;
            failureReason = "No buildable cell within snap radius";

            if (grid == null || definition == null)
                return false;

            Vector2Int baseCell;
            if (!grid.TryWorldToGrid(worldPoint, out baseCell))
            {
                failureReason = "Pointer outside grid";
                return false;
            }

            float closestDistance = float.MaxValue;
            Vector2Int bestCell = baseCell;
            Vector3 bestWorld = worldPoint;
            bool found = false;
            int expansion = Mathf.Max(0, snapCellExpansion);

            for (int dz = -expansion; dz <= expansion; dz++)
            {
                for (int dx = -expansion; dx <= expansion; dx++)
                {
                    Vector2Int candidate = new Vector2Int(baseCell.x + dx, baseCell.y + dz);
                    Vector3 candidateWorld;
                    string validationMessage;
                    if (!ValidateCandidate(definition, candidate, out candidateWorld, out validationMessage))
                    {
                        failureReason = validationMessage;
                        continue;
                    }

                    Vector2 worldPointXZ = new Vector2(worldPoint.x, worldPoint.z);
                    Vector2 candidateXZ = new Vector2(candidateWorld.x, candidateWorld.z);
                    float distance = Vector2.Distance(worldPointXZ, candidateXZ);
                    if (distance > snapRadius || distance >= closestDistance)
                        continue;

                    closestDistance = distance;
                    bestCell = candidate;
                    bestWorld = candidateWorld;
                    found = true;
                }
            }

            if (!found)
                return false;

            snappedCell = bestCell;
            snappedWorld = bestWorld;
            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Validates a single candidate cell through the placement service.
        /// </summary>
        private bool ValidateCandidate(TurretClassDefinition definition, Vector2Int cell, out Vector3 worldPosition, out string failureReason)
        {
            worldPosition = Vector3.zero;
            failureReason = "Invalid cell";

            if (placementService == null)
                return false;

            Quaternion rotation = ResolvePlacementRotation(definition);
            bool canPlace = placementService.CanPlace(definition, cell, rotation, out worldPosition, out failureReason);
            return canPlace;
        }

        /// <summary>
        /// Attempts to place the turret on the last valid preview cell.
        /// </summary>
        private void TryCommitPlacement()
        {
            if (!hasPreview || activeDefinition == null)
            {
                BuildPlacementResult fail = new BuildPlacementResult(activeDefinition, false, lastFailureReason, previewWorldPosition, previewCell);
                EventsManager.InvokeBuildablePlacementResolved(fail);
                return;
            }

            Vector3 validationPosition;
            string validationMessage;
            Quaternion rotation = ResolvePlacementRotation(activeDefinition);
            if (!placementService.CanPlace(activeDefinition, previewCell, rotation, out validationPosition, out validationMessage))
            {
                BuildPlacementResult fail = new BuildPlacementResult(activeDefinition, false, validationMessage, validationPosition, previewCell);
                EventsManager.InvokeBuildablePlacementResolved(fail);
                return;
            }

            PooledTurret spawned = placementService.PlaceTurret(activeDefinition, previewCell, rotation);
            bool success = spawned != null;
            string resultMessage = success ? string.Empty : "Pool returned null turret";
            BuildPlacementResult result = new BuildPlacementResult(activeDefinition, success, resultMessage, validationPosition, previewCell);
            EventsManager.InvokeBuildablePlacementResolved(result);
        }

        /// <summary>
        /// Calculates the rotation applied to newly placed turrets.
        /// </summary>
        private Quaternion ResolvePlacementRotation(TurretClassDefinition definition)
        {
            if (definition == null)
                return Quaternion.identity;

            if (definition.Placement.AlignWithGrid && grid != null)
                return Quaternion.LookRotation(grid.transform.forward, Vector3.up);

            if (worldCamera != null)
            {
                Vector3 forward = worldCamera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0f)
                    return Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Dispatches preview data to any interested UI listeners.
        /// </summary>
        private void RaisePreviewEvent()
        {
            BuildPreviewData preview = new BuildPreviewData(activeDefinition, previewWorldPosition, previewCell, hasPreview);
            EventsManager.InvokeBuildablePreviewUpdated(preview);
        }

        /// <summary>
        /// Clears drag state after a placement attempt.
        /// </summary>
        private void ResetDragState()
        {
            dragActive = false;
            activeDefinition = null;
            hasPreview = false;
            previewCell = Vector2Int.zero;
            previewWorldPosition = Vector3.zero;
            lastFailureReason = string.Empty;
            RaisePreviewEvent();
        }

        /// <summary>
        /// Syncs the runtime build gate with the current GameManager phase when available.
        /// </summary>
        private void SyncPhaseState()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null)
                return;

            SetBuildPhaseActive(manager.CurrentPhase == GamePhase.Building);
        }
        #endregion

        #region Gizmos
        /// <summary>
        /// Draws preview gizmos in the editor to visualize snap results.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!drawPreviewGizmos || !hasPreview)
                return;

            Gizmos.color = validPreviewColor;
            Gizmos.DrawSphere(previewWorldPosition, 0.15f);
            Gizmos.color = invalidPreviewColor;
            Gizmos.DrawWireSphere(previewWorldPosition, snapRadius);
        }
        #endregion
        #endregion
    }
}
