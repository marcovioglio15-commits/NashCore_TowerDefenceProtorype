using Scriptables.Turrets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Managers.UI
{
    /// <summary>
    /// UI widget responsible for exposing a turret entry inside the build bar and forwarding drag gestures.
    /// </summary>
    public class BuildableIconView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        #region Variables And Properties
        #region Serialized Fields
        [Tooltip("Image displaying the turret icon.")]
        [SerializeField] private Image iconImage;
        [Tooltip("Optional label displaying the turret display name.")]
        [SerializeField] private TextMeshProUGUI nameLabel;
        [Tooltip("Canvas group toggled to indicate drag state.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Label showing the gold cost to build this turret.")]
        [SerializeField] private TextMeshProUGUI costLabel;
        #endregion

        #region Runtime
        private TurretClassDefinition definition;
        private bool dragActive;
        #endregion
        #endregion

        #region Methods
        #region Binding
        /// <summary>
        /// Associates the view with a turret definition and refreshes visuals.
        /// </summary>
        public void Bind(TurretClassDefinition targetDefinition)
        {
            definition = targetDefinition;
            if (iconImage != null)
                iconImage.sprite = definition != null ? definition.Icon : null;

            if (nameLabel != null)
                nameLabel.text = definition != null ? definition.DisplayName : string.Empty;

            if (costLabel != null)
                costLabel.text = definition != null ? $"COST : {definition.Economy.BuildCost}" : string.Empty;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        #endregion

        #region Economy
        /// <summary>
        /// Returns true when the player can afford this turret or no resource tracker is available.
        /// </summary>
        private bool HasAvailableGold()
        {
            PlayerResourcesManager resources = PlayerResourcesManager.Instance;
            if (resources == null || definition == null)
                return true;

            int cost = Mathf.Max(0, definition.Economy.BuildCost);
            return resources.CanAfford(cost);
        }

        /// <summary>
        /// Emits a global notification when the drag cannot start due to insufficient funds.
        /// </summary>
        private void NotifyInsufficientGold()
        {
            int cost = definition != null ? Mathf.Max(0, definition.Economy.BuildCost) : 0;
            PlayerResourcesManager resources = PlayerResourcesManager.Instance;
            int currentGold = resources != null ? resources.CurrentGold : 0;
            EventsManager.InvokePlayerGoldInsufficient(currentGold, cost);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        #endregion

        #region EventSystem
        /// <summary>
        /// Invoked by the event system when the drag gesture begins.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (definition == null)
                return;

            if (!HasAvailableGold())
            {
                NotifyInsufficientGold();
                return;
            }

            dragActive = true;
            if (canvasGroup != null)
                canvasGroup.alpha = 0.75f;

            EventsManager.InvokeBuildableDragBegan(definition, eventData.position);
        }

        /// <summary>
        /// Invoked while the user drags the icon across the screen.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!dragActive)
                return;

            EventsManager.InvokeBuildableDragUpdated(eventData.position);
        }

        /// <summary>
        /// Invoked when the icon is pressed without starting a drag to surface affordability feedback.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (HasAvailableGold())
                return;

            NotifyInsufficientGold();
        }

        /// <summary>
        /// Invoked when the drag gesture ends.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragActive)
                return;

            dragActive = false;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            EventsManager.InvokeBuildableDragEnded(eventData.position);
        }
        #endregion

        #region Unity
        /// <summary>
        /// Ensures drag state is reset if the object is disabled mid-gesture.
        /// </summary>
        private void OnDisable()
        {
            if (!dragActive)
                return;

            dragActive = false;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
        #endregion
        #endregion
    }
}
