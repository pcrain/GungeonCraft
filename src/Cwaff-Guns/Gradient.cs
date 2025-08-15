namespace CwaffingTheGungy;

public class Gradius : CwaffGun
{
    public static string ItemName         = "Gradius";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject[] _ShipPrefab  = [null, null, null, null];

    private List<GradiusHoveringGun> _extantGuns = new();

    internal float _lerpGunAngle = 0f;

    public static void Init()
    {
        Lazy.SetupGun<Gradius>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.S, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            fireAudio: null, reloadAudio: null)
          .Attach<GradiusAmmoDisplay>()
          .InitProjectile(GunData.New(clipSize: -1, cooldown: 0.18f, damage: 2f, angleVariance: 0f));

        int i = 0;
        foreach (string s in new string[]{ "gradius_falchion", "gradius_jade", "gradius_lord", "gradius_vic" })
        {
            _ShipPrefab[i] = VFX.Create(s, emissivePower: 3f, scale: 0.5f);
            _ShipPrefab[i].AddComponent<GradiusHoveringGun>();
            ++i;
        }
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        // if (!this.Mastered || this.gun.IsFiring)
        //     return;
        // this.autotarget = !this.autotarget;
        // base.gameObject.PlayOnce("xelsior_autofire_toggle");
        // if (this.autotarget)
        //     return;

        // int numGuns = this._extantGuns.Count;
        // for (int i = 0; i < numGuns; ++i)
        //     this._extantGuns[i].SetTarget(null);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        CreateShips();
        // DestroyExtantGuns();
        // if (this.maxGuns == 0)
        //     this.maxGuns = 1; // make sure we always have at least one gun
        // StartCoroutine(SpawnGunsOnceWeCanMove());

        // #if DEBUG
        // Commands._OnDebugKeyPressed -= AddNewGun;
        // Commands._OnDebugKeyPressed += AddNewGun;
        // #endif
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        for (int i = this._extantGuns.Count - 1; i >= 0; --i)
            if (this._extantGuns[i])
                this._extantGuns[i].AttemptToFire();
    }

    private void CreateShips()
    {
        this._lerpGunAngle = BraveMathCollege.QuantizeFloat(this.gun.gunAngle, 90f);
        if (this._extantGuns.Count > 0)
            return;
        for (int i = 0; i < 4; ++i)
        {
            GradiusHoveringGun grad = UnityEngine.Object.Instantiate(_ShipPrefab[i]).GetComponent<GradiusHoveringGun>();
            grad.Setup(this.PlayerOwner, new Vector2(0, i + 1.5f), (GradiusHoveringGun.Ship)i);
            this._extantGuns.Add(grad);
        }
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DestroyShips();
        // DestroyExtantGuns();
        // this._target = null;
        // if (this._reticle)
        //     this._reticle.GetComponent<tk2dSprite>().renderer.enabled = this._target;
    }

    private void DestroyShips()
    {
        return;
        // int nguns = this._extantGuns.Count;
        // for (int i = nguns - 1; i >= 0; --i)
        //     if (this._extantGuns[i])
        //         UnityEngine.Object.Destroy(this._extantGuns[i].gameObject);
    }

    public override void Update()
    {
        base.Update();
        this.gun.OverrideAngleSnap = 90f;
        this._lerpGunAngle = this._lerpGunAngle.SmoothRotateTo(this.gun.gunAngle, 10f);
    }

    public override void OnDestroy()
    {
        // GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
        DestroyShips();
        StopAllCoroutines();
        base.OnDestroy();

        // #if DEBUG
        // Commands._OnDebugKeyPressed -= AddNewGun;
        // #endif
    }

    public override void MidGameSerialize(List<object> data, int i)
    {
        base.MidGameSerialize(data, i);
        // data.Add(this.maxGuns);
    }

    public override void MidGameDeserialize(List<object> data, ref int i)
    {
        base.MidGameDeserialize(data, ref i);
        // this.maxGuns = (int)data[i++];
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

    public enum Ship
    {
        Falchion, // flanks, homing missiles
        Jade, // back, round laser, AOE centered around player
        Lord, // side, ripple laser, seeks out weakest visible enemy
        Vic, // front, normal blaster lasers dead ahead, fires as faster as the player can tap the button
    }

    private static GameObject[] _ShipParticleSystem = [
        MakeShipParticleSystem(ExtendedColours.pink, 2f), // Falchion
        MakeShipParticleSystem(ExtendedColours.lime, 2f), // Jade
        MakeShipParticleSystem(ExtendedColours.vibrantOrange, 2f), // Lord
        MakeShipParticleSystem(Color.cyan, 2f), // Vic
    ];

    private static GameObject MakeShipParticleSystem(Color particleColor, float arcSpeed)
    {
        GameObject psBasePrefab = Items.CombinedRifle.AsGun().alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>().ParticleSystemToSpawn;
        GameObject psnewPrefab = UnityEngine.Object.Instantiate(psBasePrefab).RegisterPrefab();
        //NOTE: look at CombineSparks.prefab for reference
        //NOTE: uses shader https://github.com/googlearchive/soundstagevr/blob/master/Assets/third_party/Sonic%20Ether/Shaders/SEParticlesAdditive.shader
        ParticleSystem ps = psnewPrefab.GetComponent<ParticleSystem>();
        // ETGModConsole.Log($"was using shader {psObj.GetComponent<ParticleSystemRenderer>().material.shader.name}");

        float absSpeed = Mathf.Abs(arcSpeed);

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
        // force.y = 6f;
        // force.z = 15f;

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
        // shape.position        = new Vector3(-0.5f, 0.0f, 0.0f);
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

    public void AttemptToFire()
    {
        if (this._cooldown > 0)
            return;
        this._cooldown = GetCooldown();

        Quaternion rot = base.transform.localRotation;
        Vector2 pos = this._sprite.WorldCenterRight();
        GameObject po = SpawnManager.SpawnProjectile(Xelsior._HoverProjectile, pos, rot);
        Projectile proj = po.GetComponent<Projectile>();
        proj.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(proj);
        base.gameObject.PlayUnique("xelsior_shoot_sound_short");
        // proj.AddTrail(ChekhovsGun._ChekhovTrailPrefab).gameObject.SetGlowiness(10.0f);

        if (_MuzzleVFX == null)
            _MuzzleVFX = Items.Mailbox.AsGun().muzzleFlashEffects;
        _MuzzleVFX.SpawnAtPosition(pos, zRotation: rot.eulerAngles.z);
    }

    // TODO: maybe respect stat modifiers?
    private float GetCooldown()
    {
        return 0.5f;
    }

    public void Setup(PlayerController owner, Vector2 relPos, Ship ship)
    {
        this._ship = ship;
        this._relPos = relPos;
        this._owner = owner;
        this._gun = owner.GetGun<Gradius>();
        this._sprite = base.gameObject.GetComponent<tk2dBaseSprite>();

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

        float gunAngle = this._gun._lerpGunAngle;
        Vector2 offset = this._relPos.Rotate(gunAngle);
        Vector2 hover = new Vector2(0f, _AMP * Mathf.Sin(_FREQ * (BraveTime.ScaledTimeSinceStartup + this._phase)));
        base.transform.localRotation = gunAngle.EulerZ();
        base.transform.position = this._owner.CenterPosition + offset + hover;

        if (this._cooldown > 0)
            this._cooldown -= BraveTime.DeltaTime;
    }

    private void OnDestroy()
    {
        if (this._ps)
        {
          this._ps.Stop(true);
          this._ps.gameObject.transform.parent = null;
          this._ps.gameObject.ExpireIn(3f);
          this._ps = null;
        }
    }
}
