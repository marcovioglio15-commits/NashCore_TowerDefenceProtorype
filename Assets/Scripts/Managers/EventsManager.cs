using System;
using System.Collections.Generic;
using Player.Inventory;
using Scriptables.Turrets;
using UnityEngine;

public static class EventsManager
{
    #region Actions
    #region Inputs
    public static Action<Vector2> Drag;
    public static Action<Vector2> Swipe;
    public static Action Hold;
    public static Action<Vector2> Tap;
    public static Action<Vector2> PinchIn;
    public static Action<Vector2> PinchOut;
    #endregion

    #region Buildables
    public static Action<IReadOnlyList<TurretClassDefinition>> BuildablesCatalogChanged;
    public static Action<TurretClassDefinition, Vector2> BuildableDragBegan;
    public static Action<Vector2> BuildableDragUpdated;
    public static Action<Vector2> BuildableDragEnded;
    public static Action<BuildPreviewData> BuildablePreviewUpdated;
    public static Action<BuildPlacementResult> BuildablePlacementResolved;
    public static Action<PooledTurret> TurretPerspectiveRequested;
    public static Action<PooledTurret> TurretFreeAimStarted;
    public static Action<PooledTurret> TurretFreeAimEnded;
    public static Action TurretFreeAimExitRequested;
    #endregion
    #endregion

    #region Invokes
    #region Inputs
    public static void InvokeDrag(Vector2 delta) => Drag?.Invoke(delta);
    public static void InvokeSwipe(Vector2 delta) => Swipe?.Invoke(delta);
    public static void InvokeHold() => Hold?.Invoke();
    public static void InvokeTap(Vector2 screenPosition) => Tap?.Invoke(screenPosition);
    public static void InvokePinchIn(Vector2 delta)=> PinchIn?.Invoke(delta);
    public static void InvokePinchOut(Vector2 delta)=> PinchOut?.Invoke(delta);
    public static void InvokeBuildablesCatalogChanged(IReadOnlyList<TurretClassDefinition> catalog)=> BuildablesCatalogChanged?.Invoke(catalog);
    public static void InvokeBuildableDragBegan(TurretClassDefinition definition, Vector2 screenPosition)=> BuildableDragBegan?.Invoke(definition, screenPosition);
    public static void InvokeBuildableDragUpdated(Vector2 screenPosition)=> BuildableDragUpdated?.Invoke(screenPosition);
    public static void InvokeBuildableDragEnded(Vector2 screenPosition)=> BuildableDragEnded?.Invoke(screenPosition);
    public static void InvokeBuildablePreviewUpdated(BuildPreviewData preview)=> BuildablePreviewUpdated?.Invoke(preview);
    public static void InvokeBuildablePlacementResolved(BuildPlacementResult result)=> BuildablePlacementResolved?.Invoke(result);
    public static void InvokeTurretPerspectiveRequested(PooledTurret turret)=> TurretPerspectiveRequested?.Invoke(turret);
    public static void InvokeTurretFreeAimStarted(PooledTurret turret)=> TurretFreeAimStarted?.Invoke(turret);
    public static void InvokeTurretFreeAimEnded(PooledTurret turret)=> TurretFreeAimEnded?.Invoke(turret);
    public static void InvokeTurretFreeAimExitRequested()=> TurretFreeAimExitRequested?.Invoke();
    #endregion
    #endregion

}
