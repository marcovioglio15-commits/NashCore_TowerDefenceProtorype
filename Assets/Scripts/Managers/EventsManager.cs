using System;
using UnityEngine;

public static class EventsManager
{
    #region Actions
    #region Inputs
    public static Action<Vector2> Drag;
    public static Action<Vector2> Swipe;
    public static Action Hold;
    public static Action Tap;
    public static Action<Vector2> PinchIn;
    public static Action<Vector2> PinchOut;
    #endregion
    #endregion

    #region Invokes
    public static void InvokeDrag(Vector2 delta) => Drag?.Invoke(delta);
    public static void InvokeSwipe(Vector2 delta) => Swipe?.Invoke(delta);
    public static void InvokeHold() => Hold?.Invoke();
    public static void InvokeTap() => Tap?.Invoke();
    public static void InvokePinchIn(Vector2 delta)=> PinchIn?.Invoke(delta);
    public static void InvokePinchOut(Vector2 delta)=> PinchOut?.Invoke(delta);
    #region Inputs
    #endregion
    #endregion

}
