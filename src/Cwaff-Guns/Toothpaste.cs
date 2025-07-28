namespace CwaffingTheGungy;


/* TODO:
    - make toothpaste goop consume and transform other adjacent goop
        - can use elecTriggerSemaphore to curb overspreading
        - can use two highest bits of elecTriggerSemaphore to track spread potential
    - make toothpaste goop inflict damage over time to enemies, including flying enemies
*/

public class Toothpaste : CwaffGun
{
    public static string ItemName         = "Toothpaste";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject[] _ToothpasteSudsVFX = [null, null];

    public static void Init()
    {
        Lazy.SetupGun<Toothpaste>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.RIFLE, reloadTime: 1.2f, ammo: 800, shootFps: 16, reloadFps: 16,
            smoothReload: 0.1f, fireAudio: "toothpaste_squirt_sound", reloadAudio: "toothpaste_squeeze_sound")
          .InitProjectile(GunData.New(sprite: "toothpaste_bullet", fps: 8, clipSize: 5, cooldown: 0.23f,
            angleVariance: 30f, damage: 5.0f, speed: 20f, range: 1000f, force: 4f))
          .Attach<ToothpasteProjectile>()
          .AttachTrail("toothpaste_trail", fps: 24, cascadeTimer: C.FRAME, softMaxLength: 1f);

        // _ToothpasteSudsVFX[0] = VFX.Create("toothpaste_bubbles", fps: 24, loops: false);
        // _ToothpasteSudsVFX[1] = VFX.Create("toothpaste_suds", fps: 1, loops: false);
    }

    private const float _PROP_RATE = 0.05f; // propagate 20 times per second
    private const float _PROP_CHANCE = 0.5f; // propagate with 50% probability
    private static float _NextUpdateTime = 0f;

    //TODO: see if we can leverage GGV's optimizations here without a hard dependency
    public static void HandleToothpasteGoopSpread(DeadlyDeadlyGoopManager ddgm)
    {
        const int PROP_BITS = 30; // left shift 30 bits to get propagation
        const uint PROP_MAX = 3; // can track up to 3 propagations with two bits
        const uint PROP_MASK = PROP_MAX << PROP_BITS;  // highest two bits track propagation
        const float MIN_SUDS_LIFE = 1f;  // prevent suds from spreading to places where they just evaporated from

        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < _NextUpdateTime)
            return;

        _NextUpdateTime = now + _PROP_RATE;

        //TODO: optimize this later
        List<GoopPositionData> goops = ddgm.m_goopedCells.Values.ToList();

        // propagate all toothpaste goops
        foreach (GoopPositionData goop in goops)
        {
            if (goop.remainingLifespan < MIN_SUDS_LIFE)
                continue;
            if (UnityEngine.Random.value > _PROP_CHANCE)
                continue;
            IntVector2 pos = goop.goopPosition;
            uint prop = (goop.elecTriggerSemaphore & PROP_MASK) >> PROP_BITS;
            for (int i = 0; i < IntVector2.CardinalsAndOrdinals.Length; i++)
            {
                IntVector2 neighbor = IntVector2.CardinalsAndOrdinals[i] + pos;
                if (DeadlyDeadlyGoopManager.allGoopPositionMap.TryGetValue(neighbor, out DeadlyDeadlyGoopManager oddgm))
                {
                    if (oddgm != ddgm) // if our neighbor isn't toothpaste, make it toothpaste
                        ddgm.AddGoopedPosition(neighbor/*, sourceId: goop.lastSourceID, sourceFrameCount: goop.frameGooped*/);
                    continue; // if our neighbor is non-empty, we continue whether it's toothpaste or not
                }
                if (prop == PROP_MAX)
                    continue; // can't propagate to any more empty cells
                ddgm.AddGoopedPosition(neighbor, sourceId: goop.lastSourceID, sourceFrameCount: goop.frameGooped);
                if (ddgm.m_goopedCells.TryGetValue(neighbor, out GoopPositionData newGoop))
                    newGoop.elecTriggerSemaphore |= ((prop + 1) << PROP_BITS); // track propagation status
            }
        }
    }

    [HarmonyPatch]
    private static class ToothpasteGoopSudsDoer
    {
        [HarmonyPatch(typeof(DeadlyDeadlyGoopManager), nameof(DeadlyDeadlyGoopManager.LateUpdate))]
        [HarmonyPostfix]
        private static void DeadlyDeadlyGoopManagerLateUpdatePatch(DeadlyDeadlyGoopManager __instance)
        {
            if (__instance.goopDefinition == EasyGoopDefinitions.ToothpasteGoop)
                Toothpaste.HandleToothpasteGoopSpread(__instance);
        }
    }
}

public class ToothpasteProjectile : MonoBehaviour
{
    private const float _DECEL_START = 0.05f;
    private const float _HALT_START  = 0.25f;
    private const float _RELAUNCH_START  = 0.5f;
    private const float _LERP_RATE = 13f;
    private const float _SUDS_RATE = 0.04f;
    private const float _GOOP_SCALE = 1.25f;
    private const float _MAX_GROWTH = 3.0f;

    private Projectile _projectile;
    private float _lifetime = 0f;
    private float _sudsTime = 0f;
    private State _state = State.START;
    private float _startSpeed;
    private float _nextSuds;
    private float _nextGoop = 0.5f;

    private enum State
    {
        START,
        DECEL,
        HALT,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._startSpeed = this._projectile.baseData.speed;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore all motion module overrides, helix bullets doeesn't play well with speed changing projectiles
    }

    private void MintyFreshExplosion()
    {
        float goopRadius = _GOOP_SCALE * this._sudsTime;
        if (goopRadius < this._nextGoop)
            return;
        if (DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.ToothpasteGoop) is DeadlyDeadlyGoopManager gooper)
            gooper.AddGoopCircle(this._projectile.SafeCenter, goopRadius);
        this._nextGoop += 0.5f;
    }

    private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
    {
        MintyFreshExplosion();
    }

    private void Update()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        float dtime = BraveTime.DeltaTime;
        this._lifetime += dtime;
        switch (this._state)
        {
            case State.START:
                if (this._lifetime >= _DECEL_START)
                    this._state = State.DECEL;
                break;
            case State.DECEL:
                if (this._lifetime >= _HALT_START)
                {
                    this._projectile.baseData.speed = 0.01f;
                    this._nextSuds = BraveTime.ScaledTimeSinceStartup + _SUDS_RATE;
                    this._state = State.HALT;
                }
                else
                  this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, 0f, _LERP_RATE);
                this._projectile.UpdateSpeed();
                break;
            case State.HALT:
                this._sudsTime += dtime;
                MintyFreshExplosion();
                if (this._sudsTime >= _MAX_GROWTH)
                {
                    this._projectile.DieInAir();
                    break;
                }
                if (now < this._nextSuds)
                    break;

                // this._nextSuds = now + _SUDS_RATE;
                // float goopRadius = _GOOP_SCALE * this._sudsTime;
                // for (int i = 0; i < 1; ++i)
                //     CwaffVFX.SpawnBurst(
                //         prefab           : Toothpaste._ToothpasteSudsVFX[i],
                //         numToSpawn       : Mathf.FloorToInt(4 * (1 + goopRadius)),
                //         basePosition     : this._projectile.SafeCenter,
                //         positionVariance : goopRadius,
                //         rotType          : CwaffVFX.Rot.Random,
                //         lifetime         : (i == 1) ? 0.15f : 0.35f,
                //         fadeOutTime      : (i == 1) ? 0.15f : 0.35f,
                //         randomFrame      : i == 1,
                //         startScale       : 1.0f,
                //         endScale         : (i == 1) ? 0.2f : 1.0f
                //       );
                break;
        }
    }
}
