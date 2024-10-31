namespace CwaffingTheGungy;

public class Sunderbuss : CwaffGun
{
    public static string ItemName         = "Sunderbuss";
    public static string ShortDescription = "Smashed to Oblivion";
    public static string LongDescription  = "Smashes the ground with extreme force, releasing projectiles in all directions. Slows the user down immensely while charging. User receives double damage from all sources while this gun is equipped.";
    public static string Lore             = "TBD";

    internal static readonly string[] _ColorNames = ["red", "yellow", "green", "cyan", "blue", "magenta", "gray"];
    internal static GameObject _ScorchMark = null;
    internal static GameObject _BlunderbussProjectile = null;
    internal static GameObject[] _ShatterDebris = new GameObject[7];

    private const int  _IDLE_FPS = 6;
    private const float _RUN_SPEED_WHEN_CHARGING = 0.35f;
    private const int _CHARGE_FPS = 12;
    private const float _CHARGE_TIME = 1.5f;
    private const float _COOLDOWN = 1.0f;

    private bool _hasLichguard = false;

    public static void Init()
    {
        Lazy.SetupGun<Sunderbuss>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.0f, ammo: 100, shootFps: 60, idleFps: _IDLE_FPS,
            chargeFps: _CHARGE_FPS, loopChargeAt: 18, fireAudio: "sunderbuss_fire", infiniteAmmo: true, attacksThroughWalls: true,
            autoPlay: false, percentSpeedWhileCharging: _RUN_SPEED_WHEN_CHARGING, preventRollingWhenCharging: true)
          .IncreaseLootChance(typeof(Lichguard), 20f)
          .InitSpecialProjectile<SunderbussProjectile>(GunData.New(clipSize: -1, cooldown: _COOLDOWN, angleVariance: 0.0f,
            shootStyle: ShootStyle.Charged, damage: 50.0f, speed: 1.0f, range: 0.01f, sprite: "sunderbuss_projectile", fps: 30,
            anchor: Anchor.MiddleCenter, chargeTime: _CHARGE_TIME, hideAmmo: true));

        _ScorchMark = Explosions.EmergencyCrate.effect.transform.Find("scorch").gameObject;
        for (int i = 0; i < 7; ++i)
            _ShatterDebris[i] = BreakableAPIToolbox.GenerateDebrisObject(
                shardSpritePath         : $"sunderbuss_debris_{_ColorNames[i]}",
                debrisObjectsCanRotate  : true,
                LifeSpanMin             : 1,
                LifeSpanMax             : 1,
                AngularVelocity         : 0,
                AngularVelocityVariance : 1080,
                DebrisBounceCount       : 2).gameObject;

        _BlunderbussProjectile = Items.Blunderbuss.AsGun().rawVolley.projectiles[0].chargeProjectiles[0].Projectile.gameObject.ClonePrefab();
        UnityEngine.Object.Destroy(_BlunderbussProjectile.GetComponent<BounceProjModifier>());
        Projectile proj = _BlunderbussProjectile.GetComponent<Projectile>();
            proj.baseData.speed = 20f;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS);
        gun.spriteAnimator.Play();
        player.healthHaver.ModifyDamage += this.OnTakeDamage;
        CwaffEvents.OnStatsRecalculated += this.CheckForLichguard;
        CheckForLichguard(player);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        gun.SetAnimationFPS(gun.idleAnimation, 0);
        gun.spriteAnimator.StopAndResetFrameToDefault();
        player.healthHaver.ModifyDamage -= this.OnTakeDamage;
        CwaffEvents.OnStatsRecalculated += this.CheckForLichguard;
        CheckForLichguard(player);
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!this._hasLichguard && this.gun.CurrentOwner is PlayerController player && player.CurrentGun == this.gun)
            data.ModifiedDamage *= 2f;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.healthHaver.ModifyDamage -= this.OnTakeDamage;
            CwaffEvents.OnStatsRecalculated -= this.CheckForLichguard;
        }
        base.OnDestroy();
    }

    private void CheckForLichguard(PlayerController player)
    {
        this._hasLichguard = player.HasPassive<Lichguard>();
        this.percentSpeedWhileCharging = this._hasLichguard ? 1.0f : _RUN_SPEED_WHEN_CHARGING;
        this.gun.SetAnimationFPS(this.gun.chargeAnimation, (this._hasLichguard ? 2 : 1) * _CHARGE_FPS);
        this.gun.DefaultModule.chargeProjectiles[0].ChargeTime = (this._hasLichguard ? 0.5f : 1.0f) * _CHARGE_TIME;
        this.gun.DefaultModule.cooldownTime = (this._hasLichguard ? 0.5f : 1.0f) * _COOLDOWN;
    }
}

public class SunderbussProjectile : Projectile
{
    public override void Move()
    {
        const float RADIUS = 3f;

        Projectile proj = base.gameObject.GetComponent<Projectile>();
        Vector2 pos = base.transform.position.XY();
        if (base.transform.position.GetAbsoluteRoom() is RoomHandler absoluteRoom)
            absoluteRoom.ApplyActionToNearbyEnemies(pos, RADIUS + 0.5f, ProcessEnemy);
        Exploder.DoRadialMinorBreakableBreak(pos, RADIUS);
        Exploder.DoRadialMajorBreakableDamage(proj.baseData.damage, pos, RADIUS);
        Exploder.DoRadialPush(pos, 100f, RADIUS);
        ScorchGroundAt(pos);
        GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f,6f,0.5f,0f), pos);
        for (int i = 1; i <= RADIUS; ++i)
            CwaffVFX.SpawnBurst(
              prefab           : Groundhog._EarthClod,
              numToSpawn       : i * 10,
              basePosition     : pos,
              positionVariance : i,
              velType          : CwaffVFX.Vel.AwayRadial,
              velocityVariance : 4f,
              rotType          : CwaffVFX.Rot.Random,
              lifetime         : 0.5f,
              fadeOutTime      : 0.5f,
              startScale       : 1.0f,
              endScale         : 0.1f,
              uniform          : true,
              randomFrame      : true
              );

        // spawn Blunderbuss projectiles
        for (int i = 0; i < 16; ++i)
        {
            float angle = (360f / 16f) * i;
            Vector2 avec = angle.ToVector();
            Projectile p = SpawnManager.SpawnProjectile(
              Sunderbuss._BlunderbussProjectile, pos + RADIUS * avec, angle.EulerZ()).GetComponent<Projectile>();
            p.Owner = base.Owner;
            p.Shooter = base.Shooter;
        }

        DieInAir();
    }

    private static void ScorchGroundAt(Vector2 pos)
    {
        UnityEngine.Object.Instantiate(Sunderbuss._ScorchMark, pos, Quaternion.identity)
          .SetLayerRecursively(LayerMask.NameToLayer("BG_Critical"));
    }

    private void ProcessEnemy(AIActor a, float b)
    {
        if (!a || !a.IsNormalEnemy || !a.healthHaver || a.IsGone)
            return;
        if (a.gameObject.GetComponent<SpawnEnemyOnDeath>() is SpawnEnemyOnDeath spawn)
            spawn.chanceToSpawn = 0.0f;
        a.healthHaver.ApplyDamage(base.ModifiedDamage, Vector2.zero, base.Owner ? base.OwnerName : "projectile", damageTypes);
        if (a.healthHaver.IsDead && !a.healthHaver.IsBoss && !a.healthHaver.IsSubboss)
        {
            ShatterViolentlyIntoAMillionPieces(a);
            a.EraseFromExistenceWithRewards(suppressDeathSounds: true);
            return;
        }
        if (a.behaviorSpeculator && !a.behaviorSpeculator.ImmuneToStun)
            a.behaviorSpeculator.Stun(2f);
    }

    // Elements 0-5: pixels corresponding to each hue
    // Element 6: pixels corresponding to gray
    // Element 7: total pixels counted
    private static readonly Dictionary<string, float[]> _HueLookupDict = new();

    private static void ShatterViolentlyIntoAMillionPieces(AIActor enemy)
    {
        string guid = enemy.EnemyGuid;
        if (!_HueLookupDict.TryGetValue(guid, out float[] hues))
            hues = _HueLookupDict[guid] = ComputeHuesForEnemy(guid);
        int numDebris = Mathf.CeilToInt(hues[7] / 20f);
        for (int i = 0; i < numDebris; ++i)
        {
            int hue = hues.FirstLE(UnityEngine.Random.value);  // get weighted debris hue based on enemy's colors
            DebrisObject debris = UnityEngine.Object.Instantiate(
              Sunderbuss._ShatterDebris[hue], enemy.CenterPosition, Lazy.RandomEulerZ()).GetComponent<DebrisObject>();
            debris.Trigger(Lazy.RandomVector(10f * UnityEngine.Random.value).ToVector3ZUp(6f), 0.25f);
        }
    }

    private static float[] ComputeHuesForEnemy(string guid)
    {
        float[] hues = new float[8];
        Color[] pixels = Lazy.GetPixelColorsForEnemy(guid);
        AIActor prefab = EnemyDatabase.GetOrLoadByGuid(guid);
        Texture2D paletteTex = prefab.optionalPalette ? prefab.optionalPalette.GetRW() : null;
        int npixels = pixels.Length;
        int validPixels = 0;
        for (int i = 0; i < npixels; ++i)
        {
            Color pixel = pixels[i];
            if (pixel.a < 0.5f)
                continue; // mostly-transparent pixels don't count
            if (paletteTex)
                pixel = Lazy.GetPaletteColor(paletteTex, pixel.r);
            Color.RGBToHSV(pixel, out float h, out float s, out float v);
            hues[(s < 0.25f) ? 6 : Mathf.RoundToInt(h * 6.0f) % 6] += 1.0f;
            ++validPixels;
        }
        for (int i = 0; i < 7; ++i)
        {
            hues[i] /= validPixels;
            if (i > 0)
                hues[i] += hues[i - 1]; // cumulative sum of hues
        }
        hues[7] = validPixels;

        return hues;
    }
}
