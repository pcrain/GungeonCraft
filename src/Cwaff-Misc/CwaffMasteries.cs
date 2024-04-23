namespace CwaffingTheGungy;

public static class CwaffMasteries
{
    private static Transform CreateEmptySprite(tk2dBaseSprite target)
    {
        GameObject gameObject = new GameObject("suck image");
        gameObject.layer = target.gameObject.layer;
        tk2dSprite tk2dSprite2 = gameObject.AddComponent<tk2dSprite>();
        gameObject.transform.parent = SpawnManager.Instance.VFX;
        tk2dSprite2.SetSprite(target.sprite.Collection, target.sprite.spriteId);
        tk2dSprite2.transform.position = target.sprite.transform.position;
        GameObject gameObject2 = new GameObject("image parent");
        gameObject2.transform.position = tk2dSprite2.WorldCenter;
        tk2dSprite2.transform.parent = gameObject2.transform;
        // if (target.optionalPalette != null)
        //     tk2dSprite2.renderer.material.SetTexture("_PaletteTex", target.optionalPalette);
        return gameObject2.transform;
    }

    private static IEnumerator Evaporate(tk2dBaseSprite target, Vector2 motionDirection, bool useBase = false, float duration = 10f)
    {
        float elapsed = 0f;

        Transform copyTransform = useBase ? target.gameObject.transform : CreateEmptySprite(target);
        tk2dBaseSprite copySprite = useBase ? target : copyTransform.GetComponentInChildren<tk2dSprite>();
        target.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUber");
        target.usesOverrideMaterial = true;
        SpriteOutlineManager.RemoveOutlineFromSprite(target);
        // GameObject ParticleSystemToSpawn = (ItemHelper.Get(Items.CombinedRifle) as Gun).alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
        // GameObject gameObject = UnityEngine.Object.Instantiate(ParticleSystemToSpawn, copySprite.WorldCenter.ToVector3ZisY(), Quaternion.identity);
        // ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
        // Dissect.DumpFieldsAndProperties<ParticleSystem>(component);
        // var main = component.main;
        // main.duration = duration;
        // gameObject.transform.parent = copyTransform;
        // if ((bool)copySprite)
        // {
        //     gameObject.transform.position = copySprite.WorldCenter;
        //     Bounds bounds = copySprite.GetBounds();
        //     ParticleSystem.ShapeModule shape = component.shape;
        //     shape.scale = new Vector3(bounds.extents.x * 2f, bounds.extents.y * 2f, 0.125f);
        // }
        copySprite.renderer.material.DisableKeyword("TINTING_OFF");
        copySprite.renderer.material.EnableKeyword("TINTING_ON");
        copySprite.renderer.material.DisableKeyword("EMISSIVE_OFF");
        copySprite.renderer.material.EnableKeyword("EMISSIVE_ON");
        copySprite.renderer.material.DisableKeyword("BRIGHTNESS_CLAMP_ON");
        copySprite.renderer.material.EnableKeyword("BRIGHTNESS_CLAMP_OFF");
        copySprite.renderer.material.SetFloat("_EmissiveThresholdSensitivity", 5f);
        copySprite.renderer.material.SetFloat("_EmissiveColorPower", 1f);
        int emId = Shader.PropertyToID("_EmissivePower");
        copySprite.renderer.material.SetFloat(emId, 0f);
        while (elapsed < duration)
        {
            elapsed += BraveTime.DeltaTime;
            // float t = elapsed / duration;
            // copySprite.renderer.material.SetFloat(emId, Mathf.Lerp(0f, 10f, 1f - t));
            copySprite.renderer.material.SetFloat("_BurnAmount", Mathf.Abs(Mathf.Sin(3f * elapsed)));
            copyTransform.position += motionDirection.ToVector3ZisY().normalized * BraveTime.DeltaTime * 1f;
            yield return null;
        }
        if (!useBase)
            UnityEngine.Object.Destroy(copyTransform.gameObject);
    }
}

public class MasteryRitualComponent : MonoBehaviour
{
  DebrisObject _pickup = null;

  public static void PrepareDroppedItemForMasteryRitual(DebrisObject pickup)
  {
    if (pickup && !pickup.GetComponent<MasteryRitualComponent>())
      pickup.OnGrounded += OnPickupGrounded;
  }

  private static void OnPickupGrounded(DebrisObject pickup)
  {
    MasteryRitualComponent ritComp = pickup.AddComponent<MasteryRitualComponent>();
  }

  private static Light _Lights = null;
  private static Light GetMasteryRitualLights()
  {
    if (_Lights)
      return _Lights;

    _Lights = new GameObject().AddComponent<Light>().RegisterPrefab();
    _Lights.enabled = true;
    _Lights.type = LightType.Point;
    _Lights.range = 200f;
    _Lights.shadows = LightShadows.None;
    _Lights.color = Color.white;
    _Lights.intensity = 200f;
    _Lights.renderMode = LightRenderMode.Auto;
    return _Lights;
  }

  private void Start()
  {
      this._pickup = base.GetComponent<DebrisObject>();
      if (!this._pickup)
        return;

      tk2dBaseSprite sprite = this._pickup.sprite;
      if (!sprite)
      {
        ETGModConsole.Log($"no sprite");
        return;
      }

      // sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitCutoutUber");
      // sprite.usesOverrideMaterial = true;
      // SpriteOutlineManager.RemoveOutlineFromSprite(sprite);
      GameObject psPrefab = (ItemHelper.Get(Items.CombinedRifle) as Gun).alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
      if (!psPrefab)
      {
        ETGModConsole.Log($"no prefab");
        return;
      }
      GameObject psObj = UnityEngine.Object.Instantiate(psPrefab);
      //NOTE: look at CombineSparks.prefab for reference
      //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
      ParticleSystem ps = psObj.GetComponent<ParticleSystem>();
      // ETGModConsole.Log($"was using shader {psObj.GetComponent<ParticleSystemRenderer>().material.shader.name}");
      // psObj.GetComponent<ParticleSystemRenderer>().material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
      // psObj.GetComponent<ParticleSystemRenderer>().material.SetFloat("_EmissivePower", 15f);

      ParticleSystem.MainModule main = ps.main;
      main.duration = 3600f;
      main.startLifetime = 1f;
      main.startSpeed = 0.25f;
      main.scalingMode = ParticleSystemScalingMode.Local;
      main.startRotation = 0f;
      main.maxParticles = 200;
      Color particleColor = new Color(1.0f, 0.5f, 0.5f);
      main.startColor = particleColor;
      ParticleSystem.MinMaxGradient grad = main.startColor;
      // grad.mode = ParticleSystemGradientMode.Color;
      ParticleSystem.ColorOverLifetimeModule colm = ps.colorOverLifetime;
      // colm.color = main.startColor; // looks good except fadeout
      Gradient g = new Gradient();
      g.SetKeys(
          new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 1.0f) },
          new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1.0f) }
      );
      colm.color = new ParticleSystem.MinMaxGradient(g); // looks jank

      ParticleSystem.EmissionModule em = ps.emission;
      em.rateOverTime = 100f;

      ParticleSystemRenderer psr = psObj.GetComponent<ParticleSystemRenderer>();
      psr.material.SetFloat("_InvFade", 3.0f);
      psr.material.SetFloat("_EmissionGain", 0.8f);
      psr.material.SetColor("_EmissionColor", particleColor);
      psr.material.SetColor("_DiffuseColor", particleColor);
      psr.sortingLayerName = "Foreground";

      // ParticleSystem.LightsModule lights = ps.lights;
      // lights.enabled = true;
      // lights.ratio = 1.0f;
      // lights.intensity = 100.0f;
      // lights.intensityMultiplier = 100.0f;
      // lights.light = (ItemHelper.Get(Items.BundleOfWands) as Gun).singleModule.projectiles[0].GetComponentInChildren<Light>();
      // lights.light = GetMasteryRitualLights();
      // if (!lights.light)
      //   ETGModConsole.Log($"no lighting ):");

      // psObj.transform.position = sprite.WorldCenter;
      // psObj.transform.parent = base.gameObject.transform;
      psObj.transform.position = sprite.WorldCenter.ToVector3ZisY(10f);
      ParticleSystem.ShapeModule shape = ps.shape;
      shape.randomDirectionAmount = 0f;
      shape.alignToDirection = true;
      // shape.arc = 0.0f;
      // shape.scale = new Vector3(1f, 0.1f, 1.0f);

      bool experimental = true;
      if (!experimental) // working version
      {
        shape.shapeType = ParticleSystemShapeType.Box;
        Bounds bounds = sprite.GetBounds();
        shape.scale = new Vector3(bounds.extents.x * 2f, bounds.extents.y * 2f, 0.125f);
      }
      else // experimental version
      {
        // shape.scale = new Vector3(1f, 1f, 0.0625f);
        // shape.scale = new Vector3(1f, 0.0625f, 1f);
        // shape.scale = new Vector3(41f, 0.0625f, 41f);
        shape.scale = new Vector3(1f, 1f, 1f);
        shape.shapeType = ParticleSystemShapeType.Donut;
        // shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radiusThickness = 0.0625f;
        // shape.radiusMode = ParticleSystemShapeMultiModeValue.BurstSpread;
        shape.radiusMode = ParticleSystemShapeMultiModeValue.Loop;
        // shape.radius = 3f;
        shape.radius = 2.25f;
        // shape.radius = 0.25f;
        shape.donutRadius = 0.25f;
        shape.rotation = new Vector3(0f, 0f, 0f);
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
        // shape.rotation = new Vector3(45f, 0f, 45f);
        // shape.rotation = new Vector3(90f, 0f, 0f);
        // shape.rotation = new Vector3(0f, 90f, 0f);
        // shape.rotation = new Vector3(0f, 0f, 90f);  // in degrees, around each respective axis
      }
  }
}
