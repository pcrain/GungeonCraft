namespace CwaffingTheGungy;

/* TODO:
    - [maybe] fix scattershot compatibility
*/

public class Gradius : CwaffGun
{
    public static string ItemName         = "Gradius";
    public static string ShortDescription = "Command and Conquer";
    public static string LongDescription  = "Grants command over a fleet of spaceships. Each ship type has its own projectile, and can be upgraded by picking up various collectibles:\n\n- Blue: blaster, upgraded by picking up ammo\n\n- Orange: beam, upgraded by picking up health or armor\n\n- Pink: missile, upgraded by picking up keys\n\n- Green: pulse, upgraded by picking up blanks";
    public static string Lore             = "A child's favorite toy spaceship, lost in the Gungeon like so many other toys before it. The vivid imagination of its previous owner has evidently manifested in ludicrous amounts of miniaturized firepower. Rather inconveniently, the child's prefontal cortex was developed *just* enough to imagine the ship running out of ammo during its battles, slightly disrupting an otherwise immaculate barrage of explosions.";

    private const float _MAX_ZIP_DIST = 20f;
    private const float _TOTAL_ZIP_TIME = 0.4f;
    private const float _LINEAR_ZIP_TIME = 0.2f;
    private const int _NORMAL_SHIPS = 7;
    private const int _MASTERED_SHIPS = 13;

    internal const int _MAX_SHIP_LEVEL = 5;
    internal const int _SHIP_TYPES = 4;

    internal static GameObject[] _ShipPrefab  = [null, null, null, null];
    internal static string[] _ShipNames  = ["gradius_falchion", "gradius_jade", "gradius_lord", "gradius_vic"];
    internal static Projectile _RoundLaserProjectile = null;
    internal static Projectile _WeakseekerProjectile = null;
    internal static GameObject _JadeRingImpactVFX = null;

    private List<GradiusShip> _extantShips = new();
    private Vector2 _basePos = default;
    private float _relativeBarrelY = 0f;
    private float _zipDist = 0f;
    private float _zipTime = 0f;
    private bool _spawningShips = false;

    internal float _lerpGunAngle = 0f;

    public int[] shipLevels = [1, 1, 1, 1];

    public static void Init()
    {
        Lazy.SetupGun<Gradius>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, ammo: 1985, shootFps: 14, reloadFps: 4,
            fireAudio: null, reloadAudio: null, carryOffset: new IntVector2(16, 0), doesScreenShake: false, infiniteAmmo: false/*, canReloadNoMatterAmmo: true*/)
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(baseProjectile: Items.HegemonyRifle.DefaultProjectile(), clipSize: -1, cooldown: 0.04f, damage: 10f,
            angleVariance: 0f, shootStyle: ShootStyle.Automatic, hideAmmo: true));
        gun.DefaultModule.projectiles = new(){ Lazy.NoProjectile() };

        _JadeRingImpactVFX = VFX.Create("jade_projectile_impact_vfx", fps: 60, loops: false, anchor: Anchor.MiddleCenter,
            emissivePower: 0.5f, emissiveColorPower: 1.5f, emissiveColour: ExtendedColours.vibrantOrange,
            lightColor: ExtendedColours.lime, lightRange: 1.5f, lightStrength: 7.0f);
        _RoundLaserProjectile = Items._38Special.DefaultProjectile().Clone(GunData.New(damage: 25f)).ConvertToSpecialtyType<RoundLaser>();

        GameObject dispersalPrefab = Items.FlashRay.DefaultProjectile().GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab.ClonePrefab();
        ParticleSystem ps = dispersalPrefab.GetComponent<ParticleSystem>().SetColor(ExtendedColours.vibrantOrange);
        _WeakseekerProjectile = Items._38Special.CloneProjectile(GunData.New(speed: 300f))
          .SetAllImpactVFX(VFX.CreatePool("lord_projectile_impact_vfx", fps: 60, loops: false, anchor: Anchor.MiddleCenter,
            emissivePower: 0.5f, emissiveColorPower: 1.5f, emissiveColour: ExtendedColours.vibrantOrange, scale: 0.75f,
            lightColor: ExtendedColours.vibrantOrange, lightRange: 1.5f, lightStrength: 7.0f))
          .AttachTrail("weakseeker_trail", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 0.25f, dispersalPrefab: dispersalPrefab);

        for (int i = 0; i < _ShipNames.Length; ++i)
            _ShipPrefab[i] = VFX.Create(_ShipNames[i], emissivePower: 3f, scale: 0.5f).Attach<GradiusShip>();
    }

    #if DEBUG
    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!manualReload)
            return;
        for (int i = 0; i < shipLevels.Length; ++i)
            UpgradeShip((GradiusShip.Ship)i);
    }
    #endif

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        CreateShips();
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        for (int i = this._extantShips.Count - 1; i >= 0; --i)
            if (this._extantShips[i])
                this._extantShips[i].AttemptToFire();
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        CwaffEvents.OnWillPickUpMinorInteractible += DoPickupChecks;
        GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        CwaffEvents.OnWillPickUpMinorInteractible -= DoPickupChecks;
        base.OnDroppedByPlayer(player);
    }

    private static GradiusShip.Ship[] ships = [
        GradiusShip.Ship.Vic,
        GradiusShip.Ship.Vic,
        GradiusShip.Ship.Falchion,
        GradiusShip.Ship.Falchion,
        GradiusShip.Ship.Lord,
        GradiusShip.Ship.Lord,
        GradiusShip.Ship.Jade,
        // for mastery
        GradiusShip.Ship.Vic,
        GradiusShip.Ship.Vic,
        GradiusShip.Ship.Falchion,
        GradiusShip.Ship.Falchion,
        GradiusShip.Ship.Lord,
        GradiusShip.Ship.Lord,
    ];
    private static Vector2[] shipOffsets = [
        new Vector2(0.5f, -0.375f),
        new Vector2(0.5f, 0.375f),
        new Vector2(-0.125f, -2.0f),
        new Vector2(-0.125f, 2.0f),
        new Vector2(-1.5f, -1.125f),
        new Vector2(-1.5f, 1.125f),
        new Vector2(-2.25f, 0.0f),
        // for mastery
        new Vector2(0.875f, -0.875f),
        new Vector2(0.875f, 0.875f),
        new Vector2(-0.125f, -2.75f),
        new Vector2(-0.125f, 2.75f),
        new Vector2(-1.9375f, -1.625f),
        new Vector2(-1.9375f, 1.625f),
    ];
    private void CreateShips()
    {
        this._lerpGunAngle = BraveMathCollege.QuantizeFloat(this.gun.gunAngle, 90f);
        if (this._extantShips.Count > 0)
            return;

        int numShips = this.Mastered ? _MASTERED_SHIPS : _NORMAL_SHIPS;
        for (int i = 0; i < numShips; ++i)
        {
            GradiusShip grad = UnityEngine.Object.Instantiate(_ShipPrefab[(int)ships[i]]).GetComponent<GradiusShip>();
            grad.Setup(this.PlayerOwner, this, shipOffsets[i], ships[i], i < _NORMAL_SHIPS);
            this._extantShips.Add(grad);
        }

        this._zipDist = _MAX_ZIP_DIST;
        this._zipTime = _TOTAL_ZIP_TIME;
        base.gameObject.PlayUnique("gradius_ship_spawn_sound");
        SmoothlyMoveShips();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyShips();
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (player && player.CurrentGun == this.gun && this._zipTime > 0f)
            player.SuppressThisClick = true; // can't fire when our guns are fading in
    }

    private void DestroyShips()
    {
        int nguns = this._extantShips.Count;
        bool anythingDestroyed = false;
        for (int i = nguns - 1; i >= 0; --i)
            if (this._extantShips[i])
            {
                UnityEngine.Object.Destroy(this._extantShips[i].gameObject);
                anythingDestroyed = true;
            }
        if (anythingDestroyed)
            base.gameObject.PlayUnique("gradius_ship_destroy_sound");
        this._extantShips.Clear();
    }

    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = 90f;
        this._lerpGunAngle = this._lerpGunAngle.SmoothRotateTo(this.gun.gunAngle, 12f);
    }

    private void LateUpdate()
    {
        SmoothlyMoveShips();
    }

    private void SmoothlyMoveShips()
    {
        // smoothly relocate ship positions when our gun switches hands
        const float ABS_DIFF = 1.125f;  // absolute horizontal distance between barrel world coordinates when facing right vs left
        const float GUN_HEIGHT = 0.75f; // height of gun sprite in game units (<height in pixels> / 16)
        Transform btrans = this.gun.barrelOffset.transform;
        Vector2 bpos = btrans.position.XY();
        float localY = btrans.localPosition.y;
        this._relativeBarrelY = Lazy.SmoothestLerp(this._relativeBarrelY, localY, 12f);
        Vector2 effectiveBarrelPos = new Vector2(bpos.x + ABS_DIFF * (this._relativeBarrelY - localY) / GUN_HEIGHT, bpos.y);
        this._basePos = effectiveBarrelPos;

        // smoothly transition ships in when switching to this gun
        if (this._zipTime <= 0)
            return;
        this._zipTime = Mathf.Max(this._zipTime - BraveTime.DeltaTime, 0f);
        this._zipDist = Lazy.SmoothestLerp(this._zipDist, 0f, 16f);
        if (this._zipTime <= _LINEAR_ZIP_TIME)
        {
            float maxDist = _MAX_ZIP_DIST * (this._zipTime / _TOTAL_ZIP_TIME);
            this._zipDist = Mathf.Min(this._zipDist, maxDist);
        }
        this._basePos += new Vector2(0f, this._zipDist);
    }

    internal Vector2 GetBasePos() => this._basePos;

    public override void OnDestroy()
    {
        DestroyShips();
        StopAllCoroutines();
        CwaffEvents.OnWillPickUpMinorInteractible -= DoPickupChecks;
        GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        if (this.PlayerOwner is PlayerController player)
            player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDestroy();
    }

    private void OnNewFloor()
    {
        if (!this)
            return;
        DestroyShips();
        if (this.PlayerOwner && this.PlayerOwner.CurrentGun == this.gun)
            this.PlayerOwner.StartCoroutine(SpawnShipsOnceWeCanMove());
    }

    private IEnumerator SpawnShipsOnceWeCanMove()
    {
        if (this._spawningShips)
            yield break;
        this._spawningShips = true;
        while (this.PlayerOwner)
        {
            if (this.PlayerOwner.AcceptingNonMotionInput)
                break;
            yield return null;
        }
        if (!this.PlayerOwner)
            yield break;

        CreateShips();
        this._spawningShips = false;
    }

    private void DoPickupChecks(PlayerController player, PickupObject pickup)
    {
        if (GameManager.Instance.IsLoadingLevel)
            return;
        if (pickup is KeyBulletPickup)
            UpgradeShip(GradiusShip.Ship.Falchion);
        else if (pickup is SilencerItem)
            UpgradeShip(GradiusShip.Ship.Jade);
        else if (pickup is HealthPickup)
            UpgradeShip(GradiusShip.Ship.Lord);
        else if (pickup is AmmoPickup)
            UpgradeShip(GradiusShip.Ship.Vic);
    }

    private void UpgradeShip(GradiusShip.Ship shipType)
    {
        int idx = (int)shipType;
        if (this.shipLevels[idx] >= Gradius._MAX_SHIP_LEVEL)
            return;
        ++this.shipLevels[idx];
        PlayerController player = this.PlayerOwner;
        if (this.gun == player.CurrentGun)
        {
            foreach (GradiusShip ship in this._extantShips)
            {
                if (ship && ship._shipType == shipType)
                    ship.DoUpgrade();

            }
        }
        else
        {
            CwaffVFX.SpawnBurst(
                prefab           : VFX.SinglePixel,
                numToSpawn       : 10 * this.shipLevels[idx],
                anchorTransform  : player.transform,
                basePosition     : player.CenterPosition,
                positionVariance : 5f,
                velType          : CwaffVFX.Vel.InwardToCenter,
                lifetime         : 0.5f,
                emissivePower    : 200f,
                overrideColor    : GradiusShip._ShipColors[(int)shipType],
                emitColorPower   : 8f
              );
        }
        base.gameObject.PlayUnique("gradius_powerup_sound");
    }

    internal void DelayMastered(GradiusShip.Ship shipType, bool mastered, float delay)
    {
        foreach (GradiusShip ship in this._extantShips)
            if (ship && ship._shipType == shipType && ship._mastered == mastered)
                ship._cooldown = delay;
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        for (int n = 0; n < _SHIP_TYPES; ++n)
            data.Add(this.shipLevels[n]);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        for (int n = 0; n < _SHIP_TYPES; ++n)
            this.shipLevels[n] = (int)data[i++];
    }

    [HarmonyPatch]
    private class GradiusBlankPatch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ForceBlank))]
        [HarmonyILManipulator]
        private static void PlayerControllerForceBlankPatchIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("Play_OBJ_silenceblank_use_01")))
                return;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.CallPrivate(typeof(GradiusBlankPatch), nameof(ReplaceBlankSound));
        }

        private static string ReplaceBlankSound(string origString, PlayerController player)
        {
            if (player && player.CurrentGun is Gun gun && gun.gameObject.GetComponent<Gradius>())
                return "gradius_blank_sound";
            return origString;
        }
    }
}

public class RoundLaser : Projectile
{
    private const float _LIFETIME = 1.0f;
    private const float _BASE_MAX_RADIUS = 3f;
    private const float _LVL_RADIUS = 1.5f;
    private const float _THICKNESS = 0.25f;
    private static readonly Color _StartColor = new Color(0.5f, 1.0f, 0.6f, 1.0f);
    private static readonly Color _EndColor = new Color(0.5f, 1.0f, 0.6f, 0.0f);

    private Geometry _laserRing = null;
    private float _lifetime = 0f;

    public tk2dBaseSprite ship = null;
    public int level = 1;

    private List<AIActor> _hitEnemies = new();

    public override void Start()
    {
        base.Start();
        this.m_usesNormalMoveRegardless = true; // ignore Helix Bullets, etc.
        this.collidesWithEnemies = false;
        this.collidesWithPlayer = false;
        this.collidesWithProjectiles = false;
        if (this.sprite is tk2dBaseSprite sprite)
            sprite.renderer.enabled = false;
        this.UpdateCollisionMask();

        this._laserRing = new GameObject().AddComponent<Geometry>();
    }

    public override void Move()
    {
        this._lifetime += BraveTime.DeltaTime;
        if (this._lifetime > _LIFETIME)
        {
            if (this._laserRing)
            {
                UnityEngine.Object.Destroy(this._laserRing.gameObject);
                this._laserRing = null;
            }
            this.DieInAir(suppressInAirEffects: true);
            return;
        }

        float t = this._lifetime / _LIFETIME;
        float r = t * (_BASE_MAX_RADIUS + _LVL_RADIUS * this.level);
        if (ship)
            base.transform.position = ship.WorldCenter;
        Vector2 pos = base.transform.position;
        this._laserRing.Setup(shape: Geometry.Shape.RING, color: Color.Lerp(_StartColor, _EndColor, t),
          pos: pos, radius: r + _THICKNESS, radiusInner: r);

        foreach (AIActor enemy in Lazy.GetAllNearbyEnemies(center: pos, radius: r, ignoreWalls: true))
        {
            if (_hitEnemies.Contains(enemy))
                continue;
            _hitEnemies.Add(enemy);
            if (enemy.healthHaver is not HealthHaver hh || !hh.IsVulnerable)
                continue;
            hh.ApplyDamage(base.baseData.damage, (enemy.CenterPosition - pos).normalized, Gradius.ItemName);
            CwaffVFX.Spawn(Gradius._JadeRingImpactVFX, enemy.CenterPosition);
            enemy.gameObject.Play("gradius_round_laser_impact_sound");
        }
    }

    public override void OnDestroy()
    {
        if (this._laserRing)
        {
            UnityEngine.Object.Destroy(this._laserRing.gameObject);
            this._laserRing = null;
        }
        base.OnDestroy();
    }
}

public class GradiusShip : MonoBehaviour
{
    private const float _FREQ = 5f;
    private const float _AMP  = 0.0625f;

    private static VFXPool _MuzzleVFX = null;

    private bool _didSetup = false;
    private Vector2 _relPos = default;
    private PlayerController _owner = null;
    private Gradius _gun = null;
    private tk2dBaseSprite _sprite = null;
    private float _phase = 0f;
    private ParticleSystem _ps = null;
    private int _level = 1;
    private Geometry[] _levelBlips = [null, null, null, null, null];

    internal bool _mastered = false;
    internal float _cooldown = 0f;
    internal Ship _shipType = default;

    public enum Ship
    {
        Falchion, // flanks, homing missiles
        Jade, // back, round laser, AOE centered around player
        Lord, // side, ripple laser, seeks out weakest visible enemy
        Vic, // front, normal blaster lasers dead ahead, fires as faster as the player can tap the button
    }

    internal static Color[] _ShipColors = [
        ExtendedColours.pink,
        ExtendedColours.lime,
        ExtendedColours.vibrantOrange,
        Color.cyan,
    ];
    private static GameObject[] _ShipParticleSystem = [
        MakeShipParticleSystem(_ShipColors[0]), // Falchion
        MakeShipParticleSystem(_ShipColors[1]), // Jade
        MakeShipParticleSystem(_ShipColors[2]), // Lord
        MakeShipParticleSystem(_ShipColors[3]), // Vic
    ];

    private static GameObject MakeShipParticleSystem(Color particleColor)
    {
        GameObject psBasePrefab = Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
        GameObject psnewPrefab = UnityEngine.Object.Instantiate(psBasePrefab).RegisterPrefab();
        //NOTE: look at CombineSparks.prefab for reference
        //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
        ParticleSystem ps = psnewPrefab.GetComponent<ParticleSystem>();
        // ETGModConsole.Log($"was using shader {psObj.GetComponent<ParticleSystemRenderer>().material.shader.name}");

        float arcSpeed = 2f;

        ParticleSystem.MainModule main = ps.main;
        main.duration                = 3600f;
        main.startLifetime           = 1.0f; // slightly higher than one rotation
        // main.startSpeed              = 6.0f;
        main.startSize               = 0.0625f;
        main.scalingMode             = ParticleSystemScalingMode.Local;
        main.startRotation           = 0f;
        main.startRotation3D         = false;
        main.startRotationMultiplier = 0f;
        main.maxParticles            = 200;
        main.startColor              = particleColor;
        main.emitterVelocityMode     = ParticleSystemEmitterVelocityMode.Transform;

        ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
        force.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        AnimationCurve vcurve = new AnimationCurve();
        vcurve.AddKey(0.0f, 6.0f);
        vcurve.AddKey(0.5f, 0.5f);
        vcurve.AddKey(1.0f, 0.0f);
        vel.x = vel.y = vel.z = new ParticleSystem.MinMaxCurve(1.0f, vcurve);
        vel.xMultiplier = vel.yMultiplier = vel.zMultiplier = 1.0f;
        vel.xMultiplier = 0.0f;

        ParticleSystem.RotationOverLifetimeModule rotl = ps.rotationOverLifetime;
        rotl.enabled = false;

        ParticleSystem.RotationBySpeedModule rots = ps.rotationBySpeed;
        rots.enabled = false;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0.5f, 0.25f), new GradientAlphaKey(0.15f, 0.5f),  new GradientAlphaKey(0.01f, 0.75f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        ParticleSystem.ColorOverLifetimeModule colm = ps.colorOverLifetime;
        colm.color = new ParticleSystem.MinMaxGradient(g); // looks jank

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 14f;

        ParticleSystemRenderer psr = psnewPrefab.GetComponent<ParticleSystemRenderer>();
        psr.material.SetFloat("_InvFade", 3.0f);
        psr.material.SetFloat("_EmissionGain", 0.1f);
        psr.material.SetColor("_EmissionColor", particleColor);
        psr.material.SetColor("_DiffuseColor", particleColor);
        psr.sortingLayerName = "Foreground";

        ParticleSystem.SizeOverLifetimeModule psz = ps.sizeOverLifetime;
        psz.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);
        sizeCurve.AddKey(0.9f, 0.0f);
        psz.size = new ParticleSystem.MinMaxCurve(1.5f, sizeCurve);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.randomDirectionAmount = 0f;
        shape.alignToDirection = false;
        shape.scale           = Vector3.one;
        shape.radiusThickness = 1.0f;
        shape.radiusMode      = ParticleSystemShapeMultiModeValue.Random;
        shape.length          = 2f;
        shape.radius          = 0.25f;
        shape.rotation        = Vector3.up;
        shape.arc             = 360f;
        shape.arcMode         = ParticleSystemShapeMultiModeValue.Random;
        shape.arcSpeed        = arcSpeed;
        shape.meshShapeType   = ParticleSystemMeshShapeType.Vertex;

        ParticleSystem.InheritVelocityModule iv = ps.inheritVelocity;
        iv.enabled = true;
        iv.mode = ParticleSystemInheritVelocityMode.Current;
        iv.curveMultiplier = 1f;
        AnimationCurve ivcurve = new AnimationCurve();
        ivcurve.AddKey(0.0f, 1.0f);
        ivcurve.AddKey(1.0f, 1.0f);
        iv.curve = new ParticleSystem.MinMaxCurve(1.0f, ivcurve);

        return psnewPrefab;
    }

    private static AIActor GetWeakestVisibleEnemy(Vector2 pos, Quaternion rot)
    {
        const float ARC = 90f;
        AIActor target = null;
        float minHealth = float.MaxValue;
        foreach (AIActor a in Lazy.AllEnemiesWithinConeOfVision(pos, rot.eulerAngles.z, ARC))
        {
            if (a.healthHaver is not HealthHaver hh)
                continue;
            if (!hh.IsVulnerable)
                continue;
            if (hh.currentHealth >= minHealth)
                continue;
            minHealth = hh.currentHealth;
            target = a;
        }
        return target;
    }

    public void AttemptToFire()
    {
        const float MAX_MISALIGN = 8f;
        if (this._cooldown > 0 || !this._owner)
            return;
        Quaternion rot = base.transform.localRotation;
        float zRot = rot.eulerAngles.z.Clamp360();
        float qzRot = zRot.Quantize(90f, VectorConversions.Round);
        if (zRot.AbsAngleTo(qzRot) > MAX_MISALIGN)
            return; // NOTE: prevent firing when not axis-aligned
        rot = qzRot.EulerZ();

        this._cooldown = GetCooldown() / this._owner.FireRateMult();
        // NOTE: delay cooldown of ships with the opposite mastery status by half of our cooldown so we alternate fire
        this._gun.DelayMastered(this._shipType, !this._mastered, 0.5f * this._cooldown);

        Vector2 pos = this._sprite.WorldCenterRight();
        GameObject prefab = this._shipType switch {
            Ship.Falchion => Lazy.GunDefaultProjectile((int)Items.Com4nd0).gameObject,
            Ship.Jade     => Gradius._RoundLaserProjectile.gameObject,
            Ship.Lord     => Gradius._WeakseekerProjectile.gameObject,
            Ship.Vic      => Lazy.GunDefaultProjectile((int)Items.HegemonyRifle).gameObject,
            _             => Lazy.GunDefaultProjectile(Lazy.PickupId<OmnidirectionalLaser>()).gameObject,
        };
        if (this._shipType == Ship.Lord && GetWeakestVisibleEnemy(pos, rot) is AIActor target)
            rot = (target.CenterPosition - pos).EulerZ(); // seek towards weakest enemy
        GameObject po = SpawnManager.SpawnProjectile(prefab, pos, rot);
        Projectile proj = po.GetComponent<Projectile>();
        proj.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(proj);

        switch (this._shipType)
        {
            case Ship.Falchion:
                proj.RuntimeUpdateScale(0.5f); // mini missiles
                proj.baseData.speed *= (0.65f + 0.35f * this._level);
                base.gameObject.PlayUnique("gradius_missile_sound");
                break;
            case Ship.Jade:
                base.gameObject.PlayUnique("gradius_shield_sound");
                RoundLaser rl = proj.gameObject.GetComponent<RoundLaser>();
                rl.ship = this._sprite;
                rl.level = this._level;
                break;
            case Ship.Lord:
                base.gameObject.PlayUnique("gradius_beam_sound");
                break;
            case Ship.Vic:
                base.gameObject.PlayUnique("gradius_blaster_sound");
                break;
        }

        if (_MuzzleVFX == null)
            _MuzzleVFX = Items.Mailbox.AsGun().muzzleFlashEffects;
        _MuzzleVFX.SpawnAtPosition(pos, zRotation: zRot);
    }

    private static readonly float[] _FalchionCooldowns = [2.50f, 1.80f, 1.20f, 0.70f, 0.50f];
    private static readonly float[] _JadeCooldowns     = [2.10f, 1.90f, 1.70f, 1.50f, 1.40f];
    private static readonly float[] _LordCooldowns     = [3.00f, 2.00f, 1.50f, 1.10f, 0.90f];
    private static readonly float[] _VicCooldowns      = [0.20f, 0.15f, 0.12f, 0.10f, 0.08f];
    private float GetCooldown()
    {
        switch (this._shipType)
        {
            case Ship.Falchion: return _FalchionCooldowns[this._level - 1];
            case Ship.Jade:     return _JadeCooldowns[this._level - 1];
            case Ship.Lord:     return _LordCooldowns[this._level - 1];
            case Ship.Vic:      return _VicCooldowns[this._level - 1];
        }
        return 0.5f + UnityEngine.Random.value;
    }

    public void Setup(PlayerController owner, Gradius gun, Vector2 relPos, Ship ship, bool mastered)
    {
        this._shipType = ship;
        this._relPos = relPos;
        this._owner = owner;
        this._gun = gun;
        this._mastered = mastered;
        this._sprite = base.gameObject.GetComponent<tk2dBaseSprite>();

        this._level = this._gun.shipLevels[(int)this._shipType];
        this._cooldown = this._owner.FireRateMult() * GetCooldown();

        this._phase = Mathf.PI * UnityEngine.Random.value;
        base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unpixelated"));

        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, 2.0f, 0f);
        this._sprite.UpdateZDepth();

        GameObject psObj = UnityEngine.Object.Instantiate(_ShipParticleSystem[(int)this._shipType]);
        psObj.transform.position = this._sprite.WorldCenterLeft();
        psObj.transform.parent   = base.gameObject.transform;
        psObj.transform.localRotation = 90f.EulerZ();
        this._ps = psObj.GetComponent<ParticleSystem>();

        this._didSetup = true;
    }

    internal void DoUpgrade()
    {
        UpdateLevelIndicators();
        CwaffVFX.SpawnBurst(
            prefab           : VFX.SinglePixel,
            numToSpawn       : 10 * this._level,
            anchorTransform  : base.transform,
            basePosition     : base.transform.position,
            positionVariance : 5f,
            velType          : CwaffVFX.Vel.InwardToCenter,
            lifetime         : 0.5f,
            emissivePower    : 200f,
            overrideColor    : _ShipColors[(int)this._shipType],
            emitColorPower   : 8f
          );
    }

    private void LateUpdate()
    {
        if (!this._didSetup || !this._owner || !this._sprite || !this._gun || this._owner.CurrentGun != this._gun.gun)
            return;

        float gunAngle = this._gun._lerpGunAngle;
        Vector2 offset = this._relPos.Rotate(gunAngle);
        Vector2 hover = new Vector2(0f, _AMP * Mathf.Sin(_FREQ * (BraveTime.ScaledTimeSinceStartup + this._phase)));
        base.transform.localRotation = gunAngle.EulerZ();
        base.transform.position = this._gun.GetBasePos() + offset + hover;

        UpdateLevelIndicators();

        if (this._cooldown > 0)
            this._cooldown -= BraveTime.DeltaTime;
    }

    private void UpdateLevelIndicators()
    {
        if (!this._owner || this._owner.IsInCombat)
        {
            for (int i = 0; i < Gradius._MAX_SHIP_LEVEL; ++i)
            {
                if (this._levelBlips[i])
                    this._levelBlips[i]._meshRenderer.enabled = false;
            }
            return;
        }

        int t = (int)this._shipType;
        int level = this._level = this._gun.shipLevels[t];
        Vector3 basePos = base.transform.position;
        Quaternion baseRot = base.transform.localRotation;

        // if (!this._levelBlips[0])
        //     this._levelBlips[0] = new GameObject().AddComponent<Geometry>();
        // this._levelBlips[0].Setup(shape: Geometry.Shape.CIRCLE, color: _ShipColors[t], pos: this._sprite ? this._sprite.WorldCenter : basePos,
        //   radius: 0.625f, radiusInner: 0.5f, arc: 90f * (level - 1), angle: baseRot.eulerAngles.z + 180f);

        Vector3 baseOff = new Vector3(0.5625f, -0.25f, 0f);
        for (int i = 0; i < level; ++i)
        {
            if (!this._levelBlips[i])
                this._levelBlips[i] = new GameObject().AddComponent<Geometry>();
            this._levelBlips[i].Setup(shape: Geometry.Shape.RING, color: _ShipColors[t], pos: basePos + (baseRot * new Vector2(baseOff.x, baseOff.y + 0.125f * i)), radius: 0.03125f, radiusInner: 0f);
        }
    }

    private void OnDestroy()
    {
        CwaffVFX.SpawnBurst(
            prefab           : VFX.SinglePixel,
            numToSpawn       : 10,
            anchorTransform  : base.transform,
            basePosition     : base.transform.position,
            positionVariance : 1f,
            velocityVariance : 10f,
            velType          : CwaffVFX.Vel.AwayRadial,
            lifetime         : 0.35f,
            lifetimeVariance : 0.15f,
            emissivePower    : 200f,
            overrideColor    : _ShipColors[(int)this._shipType],
            emitColorPower   : 8f
          );
        for (int i = 0; i < Gradius._MAX_SHIP_LEVEL; ++i) //TODO: magic number
        {
            if (this._levelBlips[i])
                UnityEngine.Object.Destroy(this._levelBlips[i].gameObject);
        }
        if (this._ps)
        {
          this._ps.Stop(true);
          this._ps.gameObject.transform.parent = null;
          this._ps.gameObject.ExpireIn(0.1f);
          this._ps = null;
        }
    }
}
