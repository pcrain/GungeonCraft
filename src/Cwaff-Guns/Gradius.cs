namespace CwaffingTheGungy;

/* TODO:
    - add mechanics / visuals for upgrading ships
    - add ammo display for current levels

    - add mastery
    - fix scattershot compatibility
    - [maybe] animate main gun
    - [maybe] add custom sprite for mini missiles
    - [maybe] add custom sprite for blasters
*/

public class Gradius : CwaffGun
{
    public static string ItemName         = "Gradius";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject[] _ShipPrefab  = [null, null, null, null];
    internal static string[] _ShipNames  = ["gradius_falchion", "gradius_jade", "gradius_lord", "gradius_vic"];
    internal static Projectile _RoundLaserProjectile = null;
    internal static Projectile _WeakseekerProjectile = null;

    private List<GradiusHoveringGun> _extantGuns = new();

    internal float _lerpGunAngle = 0f;

    public int[] shipLevels = [1, 1, 1, 1];

    public static void Init()
    {
        Lazy.SetupGun<Gradius>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            fireAudio: null, reloadAudio: null, carryOffset: new IntVector2(8, 0), doesScreenShake: false, infiniteAmmo: true, canReloadNoMatterAmmo: true)
          .Attach<GradiusAmmoDisplay>()
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(baseProjectile: Items.HegemonyRifle.DefaultProjectile(), clipSize: -1, cooldown: 0.02f, damage: 2f,
            angleVariance: 0f, shootStyle: ShootStyle.Automatic, hideAmmo: true));
        gun.DefaultModule.projectiles = new(){ Lazy.NoProjectile() };

        _RoundLaserProjectile = Items._38Special.DefaultProjectile().Clone(GunData.New(damage: 25f)).ConvertToSpecialtyType<RoundLaser>();

        GameObject dispersalPrefab = Items.FlashRay.DefaultProjectile().GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab.ClonePrefab();
        ParticleSystem ps = dispersalPrefab.GetComponent<ParticleSystem>().SetColor(ExtendedColours.vibrantOrange);
        _WeakseekerProjectile = Items._38Special.CloneProjectile(GunData.New(speed: 300f))
          .AttachTrail("weakseeker_trail", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 0.25f, dispersalPrefab: dispersalPrefab);

        int i = 0;
        foreach (string s in _ShipNames)
        {
            _ShipPrefab[i] = VFX.Create(s, emissivePower: 3f, scale: 0.5f);
            _ShipPrefab[i].AddComponent<GradiusHoveringGun>();
            ++i;
        }
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        #if DEBUG
            if (!manualReload || this.shipLevels[0] >= 4)
                return;

            for (int i = 0; i < 4; ++i)
                ++this.shipLevels[i];
            base.gameObject.PlayOnce("gradius_powerup_sound");
        #endif
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        CreateShips();
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        for (int i = this._extantGuns.Count - 1; i >= 0; --i)
            if (this._extantGuns[i])
                this._extantGuns[i].AttemptToFire();
    }

    private static GradiusHoveringGun.Ship[] ships = [
        GradiusHoveringGun.Ship.Vic,
        GradiusHoveringGun.Ship.Vic,
        GradiusHoveringGun.Ship.Falchion,
        GradiusHoveringGun.Ship.Falchion,
        GradiusHoveringGun.Ship.Lord,
        GradiusHoveringGun.Ship.Lord,
        GradiusHoveringGun.Ship.Jade,
    ];
    private static Vector2[] shipOffsets = [
        new Vector2(1.5f, -0.375f),
        new Vector2(1.5f, 0.375f),
        new Vector2(0.875f, -2.0f),
        new Vector2(0.875f, 2.0f),
        new Vector2(-0.5f, -1.125f),
        new Vector2(-0.5f, 1.125f),
        new Vector2(-1.25f, 0.0f),
    ];
    private void CreateShips()
    {
        this._lerpGunAngle = BraveMathCollege.QuantizeFloat(this.gun.gunAngle, 90f);
        if (this._extantGuns.Count > 0)
            return;
        for (int i = 0; i < ships.Length; ++i)
        {
            GradiusHoveringGun grad = UnityEngine.Object.Instantiate(_ShipPrefab[(int)ships[i]]).GetComponent<GradiusHoveringGun>();
            grad.Setup(this.PlayerOwner, this, shipOffsets[i], ships[i]);
            this._extantGuns.Add(grad);
        }
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyShips();
    }

    private void DestroyShips()
    {
        int nguns = this._extantGuns.Count;
        for (int i = nguns - 1; i >= 0; --i)
            if (this._extantGuns[i])
                UnityEngine.Object.Destroy(this._extantGuns[i].gameObject);
        this._extantGuns.Clear();
    }

    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = 90f;
        this._lerpGunAngle = this._lerpGunAngle.SmoothRotateTo(this.gun.gunAngle, 12f);
    }

    public override void OnDestroy()
    {
        DestroyShips();
        StopAllCoroutines();
        base.OnDestroy();
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        for (int n = 0; n < 4; ++n)
            data.Add(this.shipLevels[n]);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        for (int n = 0; n < 4; ++n)
            this.shipLevels[n] = (int)data[i++];
    }

    private class GradiusAmmoDisplay : CustomAmmoDisplay
    {
        private Gun _gun;
        private Gradius _Gradius;
        private PlayerController _owner;

        private void Start()
        {
            this._gun = base.GetComponent<Gun>();
            this._Gradius = this._gun.GetComponent<Gradius>();
            this._owner = this._gun.CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            return false;

            // if (!this._owner || !this._Gradius || !this._Gradius.Mastered)
            //     return false;

            // if (this._Gradius.autotarget)
            //     uic.GunAmmoCountLabel.Text = $"[color #ff44ff]Autotarget On[/color]\n{this._owner.VanillaAmmoDisplay()}";
            // else
            //     uic.GunAmmoCountLabel.Text = $"[color #444444]Autotarget Off[/color]\n{this._owner.VanillaAmmoDisplay()}";
            // return true;
        }
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
    private const float _LVL_RADIUS = 2f;
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
            this.DieInAir();
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
            if (enemy.healthHaver is HealthHaver hh && hh.IsVulnerable)
                hh.ApplyDamage(base.baseData.damage, (enemy.CenterPosition - pos).normalized, Gradius.ItemName);
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

public class GradiusHoveringGun : MonoBehaviour
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
    private Ship _ship = default;
    private int _level = 1;
    private float _cooldown = 0f;
    private Geometry[] _levelBlips = [null, null, null, null];

    public enum Ship
    {
        Falchion, // flanks, homing missiles
        Jade, // back, round laser, AOE centered around player
        Lord, // side, ripple laser, seeks out weakest visible enemy
        Vic, // front, normal blaster lasers dead ahead, fires as faster as the player can tap the button
    }

    private static Color[] _ShipColors = [
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

        this._cooldown = this._owner.FireRateMult() * GetCooldown();

        Vector2 pos = this._sprite.WorldCenterRight();
        GameObject prefab = this._ship switch {
            Ship.Falchion => Lazy.GunDefaultProjectile((int)Items.Com4nd0).gameObject,
            Ship.Jade     => Gradius._RoundLaserProjectile.gameObject,
            Ship.Lord     => Gradius._WeakseekerProjectile.gameObject,
            Ship.Vic      => Lazy.GunDefaultProjectile((int)Items.HegemonyRifle).gameObject,
            _             => Lazy.GunDefaultProjectile(Lazy.PickupId<OmnidirectionalLaser>()).gameObject,
        };
        if (this._ship == Ship.Lord && GetWeakestVisibleEnemy(pos, rot) is AIActor target)
            rot = (target.CenterPosition - pos).EulerZ(); // seek towards weakest enemy
        GameObject po = SpawnManager.SpawnProjectile(prefab, pos, rot);
        Projectile proj = po.GetComponent<Projectile>();
        proj.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(proj);

        switch (this._ship)
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

    private static readonly float[] _FalchionCooldowns = [2.50f, 1.80f, 1.20f, 0.70f];
    private static readonly float[] _JadeCooldowns     = [2.10f, 1.90f, 1.70f, 1.50f];
    private static readonly float[] _LordCooldowns     = [3.00f, 2.00f, 1.50f, 1.10f];
    private static readonly float[] _VicCooldowns      = [0.20f, 0.15f, 0.12f, 0.10f];
    private float GetCooldown()
    {
        switch (this._ship)
        {
            case Ship.Falchion: return _FalchionCooldowns[this._level - 1];
            case Ship.Jade:     return _JadeCooldowns[this._level - 1];
            case Ship.Lord:     return _LordCooldowns[this._level - 1];
            case Ship.Vic:      return _VicCooldowns[this._level - 1];
        }
        return 0.5f + UnityEngine.Random.value;
    }

    public void Setup(PlayerController owner, Gradius gun, Vector2 relPos, Ship ship)
    {
        this._ship = ship;
        this._relPos = relPos;
        this._owner = owner;
        this._gun = gun;
        this._sprite = base.gameObject.GetComponent<tk2dBaseSprite>();

        this._level = this._gun.shipLevels[(int)this._ship];
        this._cooldown = this._owner.FireRateMult() * GetCooldown();

        this._phase = Mathf.PI * UnityEngine.Random.value;
        base.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unpixelated"));

        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, 2.0f, 0f);
        this._sprite.UpdateZDepth();

        GameObject psObj = UnityEngine.Object.Instantiate(_ShipParticleSystem[(int)this._ship]);
        psObj.transform.position = this._sprite.WorldCenterLeft();
        psObj.transform.parent   = base.gameObject.transform;
        psObj.transform.localRotation = 90f.EulerZ();
        this._ps = psObj.GetComponent<ParticleSystem>();

        this._didSetup = true;
    }

    private void Update()
    {
        if (!this._didSetup || !this._owner || !this._sprite || !this._gun || this._owner.CurrentGun != this._gun.gun)
            return;

        UpdateLevelIndicators();

        float gunAngle = this._gun._lerpGunAngle;
        Vector2 offset = this._relPos.Rotate(gunAngle);
        Vector2 hover = new Vector2(0f, _AMP * Mathf.Sin(_FREQ * (BraveTime.ScaledTimeSinceStartup + this._phase)));
        base.transform.localRotation = gunAngle.EulerZ();
        base.transform.position = this._gun.gun.barrelOffset.position.XY() + offset + hover;

        if (this._cooldown > 0)
            this._cooldown -= BraveTime.DeltaTime;
    }

    private void UpdateLevelIndicators()
    {
        int t = (int)this._ship;
        int level = this._level = this._gun.shipLevels[t];
        Vector3 basePos = base.transform.position;
        Quaternion baseRot = base.transform.localRotation;
        Vector3 baseOff = new Vector3(0.5625f, -0.1875f, 0f);
        for (int i = 0; i < level; ++i)
        {
            if (!this._levelBlips[i])
                this._levelBlips[i] = new GameObject().AddComponent<Geometry>();
            this._levelBlips[i].Setup(shape: Geometry.Shape.RING, color: _ShipColors[t], pos: basePos + (baseRot * new Vector2(baseOff.x, baseOff.y + 0.125f * i)), radius: 0.03125f, radiusInner: 0f);
        }
        for (int i = level; i < 4; ++i)
        {
            if (this._levelBlips[i])
                this._levelBlips[i]._meshRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 4; ++i) //TODO: magic number
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
