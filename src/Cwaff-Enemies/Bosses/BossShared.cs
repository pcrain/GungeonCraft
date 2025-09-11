namespace CwaffingTheGungy;

internal static class BossShared
{
  private static GameObject _NapalmReticle = null;

  internal static void Init()
  {
    // Targeting reticle
    _NapalmReticle = ResourceManager.LoadAssetBundle("shared_auto_002").LoadAsset<GameObject>("NapalmStrikeReticle").ClonePrefab();
      _NapalmReticle.GetComponent<tk2dSlicedSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName("reticle_white"));
      UnityEngine.Object.Destroy(_NapalmReticle.GetComponent<ReticleRiserEffect>());  // delete risers for use with DoomZoneGrowth component later
  }

  // Creates a napalm-strike-esque danger zone
  internal static tk2dSlicedSprite DoomZone(Vector2 start, Vector2 target, float width, float lifetime = -1f, int growthTime = 1, string sprite = null, bool rise = true)
  {
    Vector2 delta         = target - start;
    GameObject reticle    = UnityEngine.Object.Instantiate(_NapalmReticle);
    reticle.SetLayerRecursively(LayerMask.NameToLayer("FG_Critical"));
    tk2dSlicedSprite quad = reticle.GetComponent<tk2dSlicedSprite>();
      if (sprite != null)
        quad.SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName(sprite));
      quad.dimensions              = C.PIXELS_PER_TILE * (new Vector2(delta.magnitude / growthTime, width));
      quad.transform.localRotation = delta.EulerZ();
      quad.transform.position      = start + (0.5f * width * delta.normalized.Rotate(-90f));
      quad.StartCoroutine(Lengthen(quad, delta.magnitude, growthTime, rise));
    if (lifetime > 0)
      reticle.ExpireIn(lifetime);
    return quad;

    static IEnumerator Lengthen(tk2dSlicedSprite quad, float targetLength, int numFrames, bool rise)
    {
      float scaleFactor = C.PIXELS_PER_TILE * targetLength / numFrames;
      for (int i = 1 ; i <= numFrames; ++i)
      {
        quad.dimensions = quad.dimensions.WithX(scaleFactor * i);
        quad.UpdateZDepth();
        yield return null;
      }
      if (rise)
        quad.gameObject.AddComponent<ReticleRiserEffect>().NumRisers = 3; // restore reticle riser settings
    }
  }

  /// <summary>Retarget a sliced sprite reticle.</summary>
  internal static void Retarget(this tk2dSlicedSprite quad, Vector2 start, Vector2 target)
  {
    bool overrideMat = quad.usesOverrideMaterial;
    Material mat = overrideMat ? quad.renderer.material : null;
    Vector2 delta                = target - start;
    quad.dimensions              = quad.dimensions.WithX(C.PIXELS_PER_TILE * delta.magnitude);
    quad.transform.localRotation = delta.EulerZ();
    float width                  = quad.dimensions.y / C.PIXELS_PER_TILE;
    quad.transform.position      = start + (0.5f * width * delta.normalized.Rotate(-90f));
    quad.UpdateZDepth();
    if (overrideMat) //NOTE: make sure emissive properties don't get reset
      quad.renderer.material = mat;
  }
}
