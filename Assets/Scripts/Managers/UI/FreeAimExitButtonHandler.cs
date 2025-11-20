using UnityEngine;
using UnityEngine.EventSystems;

namespace Managers.UI
{
    /// <summary>
    /// Forwards pointer presses on the free-aim exit control to the main scene UI manager.
    /// </summary>
    public class FreeAimExitButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        #region Variables And Properties
        #region Serialized Fields
        [Tooltip("UI manager coordinating the free-aim exit hold feedback.")] [SerializeField] private UIManager_MainScene uiManager;
        #endregion
        #endregion

        #region Methods
        #region EventSystem
        /// <summary>
        /// Begins the hold countdown when the control is pressed.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            UIManager_MainScene manager = uiManager != null ? uiManager : UIManager_MainScene.Instance;
            if (manager != null)
                manager.BeginFreeAimExitHold();
        }

        /// <summary>
        /// Cancels the exit hold when the control is released.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            UIManager_MainScene manager = uiManager != null ? uiManager : UIManager_MainScene.Instance;
            if (manager != null)
                manager.CancelFreeAimExitHold();
        }

        /// <summary>
        /// Cancels the exit hold when the pointer leaves the control.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            UIManager_MainScene manager = uiManager != null ? uiManager : UIManager_MainScene.Instance;
            if (manager != null)
                manager.CancelFreeAimExitHold();
        }
        #endregion
        #endregion
    }
}
