namespace CwaffingTheGungy;

/// <summary>Class for managing multiple sources vying for the main camera controller.</summary>
[HarmonyPatch]
public static class CameraOverrideController
{
  private static GameObject _CameraOwner = null;
  private static Action _OnRelinquishedCameraControl;

  public static bool HasControlOverCamera(this GameObject obj)
  {
    return obj == _CameraOwner;
  }

  /// <summary>Returns true if we successfully claim ownership of the camera.</summary>
  public static bool RequestCameraControl(this GameObject obj, bool lerp = true, Action relinquishAction = null)
  {
    if (GameManager.Instance.MainCameraController.ManualControl)
      return false;

    GameManager.Instance.MainCameraController.SetManualControl(true, shouldLerp: lerp);
    _CameraOwner = obj;
    _OnRelinquishedCameraControl = null;
    if (relinquishAction != null)
      _OnRelinquishedCameraControl += relinquishAction;
    return true;
  }

  /// <summary>Returns true if we successfully relinquish ownership of the camera</summary>
  public static bool RelinquishCameraControl(this GameObject obj, bool lerp = true)
  {
    if (obj != _CameraOwner)
      return false;

    if (_OnRelinquishedCameraControl != null)
      _OnRelinquishedCameraControl();
    _OnRelinquishedCameraControl = null;
    _CameraOwner = null;
    GameManager.Instance.MainCameraController.SetManualControl(false, shouldLerp: lerp);
    return true;
  }

  /// <summary>Patch to make sure control over the camera is restored when something else requests manual control.</summary>
  [HarmonyPatch(typeof(CameraController), nameof(CameraController.SetManualControl))]
  [HarmonyPrefix]
  private static void CameraControllerSetManualControlPatch(CameraController __instance, bool manualControl, bool shouldLerp)
  {
    if (manualControl && _CameraOwner)
      _CameraOwner.RelinquishCameraControl();
  }

  // /// <summary>Patch to make sure we relinquish camera control when, e.g., fighting Gatling Gull.</summary>
  // [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ForceWalkInDirectionWhilePaused))]
  // [HarmonyPostfix]
  // private static void PlayerControllerForceWalkInDirectionWhilePausedPatch(PlayerController __instance, DungeonData.Direction direction, float thresholdValue)
  // {
  //   if (DeathNoteHUD._ActiveInstance is DeathNoteHUD hud)
  //     hud.Dismiss();
  // }
}
