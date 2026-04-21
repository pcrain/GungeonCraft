namespace CwaffingTheGungy;

/* TODO:
    - make entire HUD
      - orange visor
      - enemy diorama
      - health bar readout
      - range readout
      - enemy type readout
      - obstruction readout
    - gun animations
    - impact splash damage / explosion / goop
    - better dispersal particles
    - targeting sightlines
    - custom clip

    - no impact vfx on enemies
    - shading on impact vfx is bad
*/

public class Retina : CwaffGun
{
    public static string ItemName         = "Retina";
    public static string ShortDescription = "Breach of Covenant";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        Lazy.SetupGun<Retina>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 1.25f, ammo: 20, idleFps: 10, shootFps: 24, reloadFps: 30,
            smoothReload: 0.1f, reloadAudio: "retina_reload_sound", fireAudio: "retina_fire_sound")
          .InitProjectile(GunData.New(sprite: "retina_projectile", clipSize: 2, cooldown: 1.25f, angleVariance: 1.0f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 300.0f, speed: 300.0f, force: 10.0f, range: 1000.0f, pierceBreakables: true, hitSound: "retina_impact_sound"))
          .SetAllImpactVFX(VFX.CreatePool("retina_impact_vfx", fps: 30, loops: false, emissivePower: 1f/*, lightColor: ExtendedColours.vibrantOrange, lightRange: 7.0f, lightStrength: 20.0f*/))
          .AttachTrail("retina_beam", fps: 60, timeTillAnimStart: 0.00f,
            destroyOnEmpty: true, dispersalPrefab: Lazy.DispersalParticles(ExtendedColours.vibrantOrange))
          .Attach<RetinaProjectile>();
    }
}

// [HarmonyPatch]
public class RetinaProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private bool _killedEnemy;
    private Vector2? collisionPoint;

    // [HarmonyPatch(typeof(VFXPool), nameof(VFXPool.SpawnAtPosition), typeof(Vector3), typeof(float), typeof(Transform), typeof(Vector2?), typeof(Vector2?), typeof(float?), typeof(bool), typeof(VFXComplex.SpawnMethod), typeof(tk2dBaseSprite), typeof(bool))]
    // [HarmonyPrefix]
    // private static void ProjectileHandleHitEffectsEnemyPatch(VFXPool __instance)
    // {
    //   if (__instance.effects.Length > 0 && __instance.effects[0].effects.Length > 0)
    //     System.Console.WriteLine($"spawning {__instance.effects[0].effects[0].effect.name}");
    // }

    // [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.Spawn), typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(bool))]
    // [HarmonyPrefix]
    // private static void SpawnManagerSpawnPatch(SpawnManager __instance, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool ignoresPools)
    // {
    //     if (!prefab || !prefab.name.Contains("retina"))
    //       return;
    //     System.Console.WriteLine($"spawning object {prefab.name} at {position.x},{position.y},{position.z} with ignorePools {ignoresPools} and parent {(parent == null ? "null" : parent.gameObject.name)}");
    // }

    private void Start()
    {
      this._projectile = base.GetComponent<Projectile>();
      this._projectile.OnWillKillEnemy += this.OnWillKillEnemy;
      this._projectile.specRigidbody.OnRigidbodyCollision += this.OnRigidbodyCollision;
      this._projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
      this._owner = this._projectile.Owner as PlayerController;
      if (base.GetComponentInChildren<CwaffTrailController>() is CwaffTrailController tc)
        tc.gameObject.GetComponent<tk2dBaseSprite>().SetGlowiness(100f);
    }

    private void OnWillKillEnemy(Projectile projectile, SpeculativeRigidbody enemy)
    {
      if (enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
        return;
      if (enemy.aiActor is not AIActor actor || actor.sprite is not tk2dBaseSprite sprite)
        return;
      sprite.DuplicateInWorldAsMesh(optionalPalette: actor.optionalPalette)
        .Dissipate(time: 1.5f, amplitudeStart: 0.0625f, amplitudeEnd: 2f, emissionEnd: 50f,
          easeEmit: RetinaEmit, easeFade: RetinaFade, easeAmp: RetinaFade);
      actor.EraseFromExistenceWithRewards(true); // NOTE: this suppresses hit effects, so we need to spawn them manually
      this._killedEnemy = true;
    }

    private void OnRigidbodyCollision(CollisionData collision)
    {
      collisionPoint = collision.Contact;
      if (this._killedEnemy) //NOTE: need to spawn hit effects manually due to EraseFromExistenceWithRewards
        SpawnManager.SpawnVFX(this._projectile.hitEffects.enemy.effects[0].effects[0].effect, collision.Contact, Quaternion.identity, ignoresPools: true);
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
      collisionPoint = tileCollision.Contact;
    }

    private static void DoLightBurst(Vector2 lightPoint)
    {
      EasyLight.Create(pos: lightPoint, color: ExtendedColours.vibrantOrange, radius: 4f, grownIn: true, brightness: 10.0f, fadeInTime: 0.2f, fadeOutTime: 0.2f, maxLifeTime: 0.5f);
    }

    private void OnDestroy()
    {
      Vector2 lightPoint = collisionPoint ?? (this._projectile ? this._projectile.SafeCenter : base.transform.position);
      DoLightBurst(lightPoint);
    }

    private static float RetinaEmit(float t)
    {
      if (t < 0.4f)
        return 0f;
      if (t < 0.5f)
        return 10f * (t - 0.4f);
      return 1f;
    }

    private static float RetinaFade(float t)
    {
      if (t < 0.5f)
        return t * 0.1f;
      return 0.05f + 2.0f * (t - 0.5f);
    }
}
