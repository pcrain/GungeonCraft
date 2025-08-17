namespace CwaffingTheGungy;

public class Toothpaste : CwaffGun
{
    public static string ItemName         = "Toothpaste";
    public static string ShortDescription = "2 in 10 Dentists Recommend";
    public static string LongDescription  = "Fires globs of toothpaste that spread to consume all other adjacent goops. Reloading with a full clip swings a toothbrush that releases a damaging wave of foam that travels across nearby toothpaste goop.";
    public static string Lore             = "The de facto gold standard of tooth cleansing technology, packaged in a convenient easy-squeeze tube. You're truthfully not sure how basic dental hygiene equipment even remotely qualifies as weaponry, but your wishful thinking convinces you that with a bit of time and imagination, its utility will become apparent.";

    private const float _SWING_RATE = 0.65f;
    private const float _MASTERED_SWING_RATE = 0.4f;

    private static string _BrushAnim = null;
    internal static GameObject _ToothpasteSudsVFX = null;

    private float _nextSwing = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<Toothpaste>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.SILLY, reloadTime: 0.8f, ammo: 300, shootFps: 16, reloadFps: 16,
            smoothReload: 0.1f, fireAudio: "toothpaste_squirt_sound", reloadAudio: "toothpaste_squeeze_sound")
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "toothpaste_bullet", fps: 8, clipSize: 5, cooldown: 0.23f,
            angleVariance: 30f, damage: 5.0f, speed: 30f, range: 1000f, force: 4f, customClip: true))
          .Attach<GoopModifier>(g => {
            g.goopDefinition         = EasyGoopDefinitions.ToothpasteGoop;
            g.SpawnGoopInFlight      = true;
            g.InFlightSpawnRadius    = 0.25f;
            g.InFlightSpawnFrequency = 0.01f;})
          .Attach<ToothpasteProjectile>()
          .AttachTrail("toothpaste_trail", fps: 24, cascadeTimer: C.FRAME, softMaxLength: 1f);

        _BrushAnim = gun.QuickUpdateGunAnimation("brush", fps: 30, returnToIdle: true, audio: "toothbrush_swing_sound");

        _ToothpasteSudsVFX = VFX.Create("toothpaste_suds", fps: 20, loops: false);
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < this._nextSwing || player.IsDodgeRolling)
            return;

        gun.spriteAnimator.PlayIfNotPlaying(_BrushAnim);
        new GameObject().AddComponent<SudsWave>().DoTheWave(player, gun);
        this._nextSwing = now + (this.Mastered ? _MASTERED_SWING_RATE : _SWING_RATE);
    }

    private class GoopPropData
    {
        public IntVector2 pos;
        public int source;
        public int frame;
        public uint prop;
    }

    internal const float _PROP_RATE = 1f / 60f; // propagate 60 times per second
    internal const float _PROP_CHANCE = 0.1f; // propagate with 10% probability (any higher lags the game without GGV support)
    private const int _PROP_BITS = 30; // left shift 30 bits to get propagation
    private const uint _PROP_MAX = 3; // can track up to 3 propagations with two bits
    private const uint _PROP_MASK = _PROP_MAX << _PROP_BITS;  // highest two bits track propagation
    private const float _MIN_SUDS_LIFE = 1f;  // prevent suds from spreading to places where they just evaporated from
    private const float _HIT_CLEAR_RATE = 0.5f;  // how often HandleToothpasteGoopSpread() clears its list of hit enemies

    private static float _NextHitClearTime = 0f;
    private static float _NextUpdateTime = 0f;
    private static HashSet<IntVector2> _ConvertedGoops = new();
    private static HashSet<GoopPropData> _NewGoops = new();
    private static List<HealthHaver> _HitEnemies = new();

    //TODO: see if we can leverage GGV's optimizations here without a hard dependency
    public static void HandleToothpasteGoopSpread(DeadlyDeadlyGoopManager ddgm)
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < _NextUpdateTime)
            return;

        _NextUpdateTime = now + _PROP_RATE;

        // propagate all toothpaste goops
        bool anyNewGoops = false;
        foreach (GoopPositionData goop in ddgm.m_goopedCells.Values)
        {
            if (goop.NeighborsAsInt == 255)
            {
                goop.elecTriggerSemaphore |= (_PROP_MAX << _PROP_BITS); // cut off backpropagation if we're already surrounded by toothpaste
                continue; // completely surrounded by toothpaste already, so fast-track our way out of here
            }
            if (goop.remainingLifespan < _MIN_SUDS_LIFE)
                continue;
            if (UnityEngine.Random.value > _PROP_CHANCE)
                continue;
            anyNewGoops |= HandleSingleToothpasteGoopSpread(ddgm, goop);
        }
        if (anyNewGoops)
            ddgm.gameObject.Play("toothpaste_suds_spread_sound");

        if (_ConvertedGoops.Count > 0)
        {
            foreach (IntVector2 goopPos in _ConvertedGoops)
            {
                ddgm.AddGoopedPosition(goopPos);
                if (ddgm.m_goopedCells.TryGetValue(goopPos, out GoopPositionData newGoop))
                    DoToothpasteSudsAt(goopPos.GoopToWorldPosition());
            }
            if (_NextHitClearTime > now)
            {
                _HitEnemies.Clear();
                _NextHitClearTime = now + _HIT_CLEAR_RATE;
            }
            bool hitAnyoneThisFrame = Toothpaste.HandleNewlyGoopedEnemies(ref _ConvertedGoops, ref _HitEnemies);
            if (hitAnyoneThisFrame)
                ddgm.gameObject.Play("the_sound_of_getting_sudsed");
            _ConvertedGoops.Clear();
        }

        foreach (GoopPropData goopProp in _NewGoops)
        {
            ddgm.AddGoopedPosition(goopProp.pos, sourceId: goopProp.source, sourceFrameCount: goopProp.frame);
            if (ddgm.m_goopedCells.TryGetValue(goopProp.pos, out GoopPositionData newGoop))
            {
                DoToothpasteSudsAt(goopProp.pos.GoopToWorldPosition());
                newGoop.elecTriggerSemaphore |= ((goopProp.prop + 1) << _PROP_BITS); // track propagation status
            }
        }
        _NewGoops.Clear();
    }

    internal static bool HandleNewlyGoopedEnemies(ref HashSet<IntVector2> processedThisFrame, ref List<HealthHaver> hitEnemies)
    {
        const int _SUDS_DAMAGE = 20;

        if (!SudsWave._ToothpasteGooper)
            SudsWave._ToothpasteGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.ToothpasteGoop);

        List<AIActor> allEnemies = StaticReferenceManager.AllEnemies;
        bool hitAnyoneThisFrame = false;
        for (int i = 0; i < allEnemies.Count; i++)
        {
            AIActor aIActor = allEnemies[i];
            if (!aIActor.IsNormalEnemy || !aIActor.renderer.isVisible || aIActor.IsGone)
                continue;
            if (aIActor.healthHaver is not HealthHaver hh || !hh.IsAlive || !hh.IsVulnerable || hitEnemies.Contains(hh))
                continue;
            if (aIActor.sprite is not tk2dBaseSprite sprite)
                continue;

            IntVector2 goopBase = sprite.WorldCenter.WorldToGoopPosition();
            if (!processedThisFrame.Contains(goopBase) || !SudsWave._ToothpasteGooper.m_goopedPositions.Contains(goopBase))
                continue;

            hitAnyoneThisFrame = true;
            hitEnemies.Add(hh);
            hh.ApplyDamage(_SUDS_DAMAGE, Vector2.zero, "Minty Fresh Toothpaste Suds", CoreDamageTypes.Water, DamageCategory.Environment);
        }
        return hitAnyoneThisFrame;
    }

    internal static void DoToothpasteSudsAt(Vector2 position)
    {
        CwaffVFX.Spawn(prefab: Toothpaste._ToothpasteSudsVFX, position: position,
            rotation: Lazy.RandomEulerZ(), lifetime: 0.24f, fadeOutTime: 0.24f);
    }

    private static bool HandleSingleToothpasteGoopSpread(DeadlyDeadlyGoopManager ddgm, GoopPositionData goop)
    {
        bool anyNewGoops = false;
        IntVector2 pos = goop.goopPosition;
        uint prop = (goop.elecTriggerSemaphore & _PROP_MASK) >> _PROP_BITS;
        for (int i = 0; i < IntVector2.CardinalsAndOrdinals.Length; i++)
        {
            if (goop.neighborGoopData[i] != null)
                continue; // if our neighbor is toothpaste already, nothing to do
            IntVector2 neighbor = IntVector2.CardinalsAndOrdinals[i] + pos;
            //WARNING: this lookup is slow...see if we can leverage GGV later if we ever expose a public API
            if (DeadlyDeadlyGoopManager.allGoopPositionMap.TryGetValue(neighbor, out DeadlyDeadlyGoopManager oddgm))
                _ConvertedGoops.Add(neighbor); // if our neighbor isn't toothpaste, make it toothpaste
            else if (prop != _PROP_MAX)
                _NewGoops.Add(new(){pos = neighbor, source = goop.lastSourceID, frame = goop.frameGooped, prop = prop});
            else
                continue;
            anyNewGoops = true;
        }
        return anyNewGoops;
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

public class SudsWave : MonoBehaviour
{
    internal static DeadlyDeadlyGoopManager _ToothpasteGooper = null;

    public void DoTheWave(PlayerController player, Gun gun)
    {
        if (!_ToothpasteGooper)
            _ToothpasteGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.ToothpasteGoop);
        StartCoroutine(DoTheWave_CR(player, gun));
    }

    private IEnumerator DoTheWave_CR(PlayerController player, Gun gun)
    {
        const int ITERS_PER_FRAME = 4;

        Vector2 startPos = gun.barrelOffset.position;
        IntVector2 startGoopPos = startPos.WorldToGoopPosition();

        //TODO: pool these
        Queue<IntVector2> frontier = new();
        HashSet<IntVector2> processed = new();
        HashSet<IntVector2> processedThisFrame = new();
        List<HealthHaver> hitEnemies = new(); // list is faster than hashset here for small n, at least in theory
        frontier.Enqueue(startGoopPos);
        processed.Add(startGoopPos);

        while (frontier.Count > 0)
        {
            for (int n = 0; n < ITERS_PER_FRAME; ++n)
            {
                bool lastIter = n == ITERS_PER_FRAME - 1;
                int frontierSize = frontier.Count;
                for (int i = 0; i < frontierSize; ++i)
                {
                    IntVector2 nextPos = frontier.Dequeue();
                    if (!_ToothpasteGooper.m_goopedCells.TryGetValue(nextPos, out GoopPositionData goopData))
                        continue;
                    if (UnityEngine.Random.value > Toothpaste._PROP_CHANCE)
                    {
                        frontier.Enqueue(nextPos); // requeue and try again next cycle
                        continue;
                    }
                    if (lastIter)
                        Toothpaste.DoToothpasteSudsAt(nextPos.GoopToWorldPosition());
                    for (int j = 0; j < 8; ++j)
                    {
                        GoopPositionData neighbor = goopData.neighborGoopData[j];
                        if (neighbor != null && !processed.Contains(neighbor.goopPosition))
                        {
                            neighbor.remainingLifespan = EasyGoopDefinitions.ToothpasteGoop.lifespan; // reset lifespan
                            frontier.Enqueue(neighbor.goopPosition);
                            processed.Add(neighbor.goopPosition);
                            processedThisFrame.Add(neighbor.goopPosition);
                        }
                    }
                }
            }

            // check which enemies are in suds this frame
            bool hitAnyoneThisFrame = Toothpaste.HandleNewlyGoopedEnemies(ref processedThisFrame, ref hitEnemies);
            if (hitAnyoneThisFrame)
                base.gameObject.Play("the_sound_of_getting_sudsed");

            processedThisFrame.Clear();
            yield return new WaitForSeconds(Toothpaste._PROP_RATE);
        }

        UnityEngine.Object.Destroy(base.gameObject);
        yield break;
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
    private bool _mastered = false;

    private enum State
    {
        START,
        DECEL,
        HALT,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is PlayerController player)
            this._mastered = player.HasSynergy(Synergy.MASTERY_TOOTHPASTE);
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._startSpeed = this._projectile.baseData.speed;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore all motion module overrides, helix bullets doeesn't play well with speed changing projectiles
    }

    private void MintyFreshExplosion()
    {
        float goopRadius = _GOOP_SCALE * this._sudsTime * (this._mastered ? 2f : 1f);
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
                    this._projectile.DieInAir();
                break;
        }
    }
}
