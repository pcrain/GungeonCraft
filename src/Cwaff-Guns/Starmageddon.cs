﻿namespace CwaffingTheGungy;

public class Starmageddon : CwaffGun
{
    public static string ItemName         = "Starmageddon";
    public static string ShortDescription = "Heavenly Wrath";
    public static string LongDescription  = "Fires projectiles that orbit the player while fire is held. Projectiles ascend when fire is released and fall upon semi-random enemies after a short delay. Enemies closer to the player are more likely to be targeted.";
    public static string Lore             = "A mythical weapon feared and revered by many as the 'Gun That Shall Fire the Final Shot', it is said to launch projectiles heavenward and rain meteoric destruction upon the lands. Fortunately for humanity, the weapon's projectiles only get to ascend about 50 feet before hitting the Gungeon's ceiling, making it a rare example of a gun that is actually *weakened* by the Gungeon's magic.";

    internal static TrailController _StarmageddonTrailPrefab = null;

    private int _nextIndex = 0;
    private int _curBatch  = 0;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Starmageddon>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.S, gunClass: GunClass.FULLAUTO, reloadTime: 1.0f, ammo: 900, shootFps: 20, chargeFps: 20, reloadFps: 30);
            gun.LoopAnimation(gun.reloadAnimation);

        gun.InitProjectile(GunData.New(clipSize: 30, cooldown: 0.125f, angleVariance: 15.0f,
          shootStyle: ShootStyle.Automatic, damage: 6.0f, speed: 60.0f, range: 999999f, spawnSound: "starmageddon_fire_sound",
          sprite: "starmageddon_bullet", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter, useDummyChargeModule: true,
          // overrideColliderPixelSizes: new IntVector2(128, 128), //BUG: large hitboxes apparently lag the game????
          shrapnelVFX: VFX.Create("starmageddon_shrapnel"), shrapnelCount: 5
          )
        ).Attach<StarmageddonProjectile>();

        _StarmageddonTrailPrefab = VFX.CreateTrailObject("starmageddon_trail", fps: 60, cascadeTimer: C.FRAME, softMaxLength: 1f, destroyOnEmpty: true);
    }

    public override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (this.GenericOwner is not PlayerController)
            return;
        if (this.gun.IsCharging)
            return;

        if (!this.gun.IsReloading && this.gun.ClipShotsRemaining < Mathf.Min(this.gun.ClipCapacity, this.gun.CurrentAmmo))
        {
            base.gameObject.PlayOnce("starmageddon_reload_sound");
            this.gun.Reload(); // force reload while we're not at max clip capacity
        }

        Reset();
        // Synchronize ammo clips between projectile modules as necessary
        // (don't do while charging or bullets will all be forcibly released)
        this.gun.SynchronizeReloadAcrossAllModules();
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        Reset();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        Reset();
    }

    public override void OnDropped()
    {
        base.OnDropped();
        Reset();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        Reset();
    }

    private void Reset()
    {
        if (this._nextIndex == 0)
            return;

        this._nextIndex = 0;
        ++this._curBatch;
    }

    public int GetNextIndex() => this._nextIndex++;
    public int GetBatch() => this._curBatch;
}

public class StarmageddonProjectile : MonoBehaviour
{
    private const float ROT_PER_SEC = 2f;    // rotation speed of star sprites
    private const float REV_PER_SEC = 0.44f; // revolution speed of stars around player
    private const float _SPREAD = 2.5f;  // spread for falling stars

    private PlayerController _owner                 = null;
    private Projectile       _projectile            = null;
    private int              _index                 = 0;
    private int              _batch                 = 0;
    private bool             _naturalSpawn          = false;
    private float            _spawnTime             = 0f;

    private State _state = State.NEUTRAL;

    private enum State {
        NEUTRAL,
        ORBITING,
        RISING,
        HANGING,
        FALLING,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._spawnTime = BraveTime.ScaledTimeSinceStartup;

        this._projectile.OnDestruction += this.OnProjectileDestruction;

        if (!this._owner)
            return; // shouldn't happen, but just be safe

        StartCoroutine(ShootForTheStars());
    }

    private void OnProjectileDestruction(Projectile p)
    {
        p.gameObject.Play("starmageddon_bullet_impact_sound_1");
        p.gameObject.Play("starmageddon_bullet_impact_sound_2");
    }

    private void Update()
    {
        if (!this._projectile)
            return;
        this._projectile.transform.localRotation = (-ROT_PER_SEC * 360f * (BraveTime.ScaledTimeSinceStartup - this._spawnTime)).EulerZ();
    }

    private IEnumerator ShootForTheStars()
    {
        // Setup
        this._projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        this._projectile.sprite.SetGlowiness(glowAmount: 70f, glowColor: Color.yellow);

        // Phase 1 -- initial fire
        if (this._owner.CurrentGun.GetComponent<Starmageddon>() is Starmageddon sm)
        {
            this._index = sm.GetNextIndex();
            this._batch = sm.GetBatch();
            this._naturalSpawn = true;
        }
        else
            sm = null;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.Reinitialize();
        yield return new WaitForSeconds(0.125f);

        // Phase 2 -- orbiting the player
        this._state = State.ORBITING;
        this._projectile.SetSpeed(0.01f);
        float radius = this._projectile.DistanceToOwner() * UnityEngine.Random.Range(0.92f, 1.08f);
        float angle = this._projectile.AngleFromOwner();
        if (this._naturalSpawn)
            for (float elapsed = 0f; this._state == State.ORBITING; elapsed += BraveTime.DeltaTime)
            {
                if (!this._owner)
                {
                    this._projectile.DieInAir();
                    yield break;
                }
                if (!sm || sm.GetBatch() != this._batch)
                {
                    this._state = State.RISING;
                    break;
                }

                Vector2 oldPos = this._projectile.specRigidbody.Position.GetPixelVector2();
                Vector2 newPos = this._owner.CenterPosition + (angle + 360f * REV_PER_SEC * elapsed).Clamp360().ToVector(radius);
                this._projectile.specRigidbody.Position = new Position(newPos);
                Vector2 vel = (newPos - oldPos);
                this._projectile.specRigidbody.Velocity = vel;
                this._projectile.specRigidbody.UpdateColliderPositions();
                yield return null;
            }

        // Phase 3 -- launching to the stars
        this._state = State.RISING;
        yield return new WaitForSeconds(0.25f * UnityEngine.Random.value);
        this._projectile.sprite.HeightOffGround = 10f; // max, 100 doesn't render
        this._projectile.sprite.UpdateZDepth();
        this._projectile.sprite.renderLayer = 3; // 2 is same as Mourning Star laser, 3 is Gatling Gull outro doer
        DepthLookupManager.ProcessRenderer(
            this._projectile.sprite.renderer, DepthLookupManager.GungeonSortingLayer.FOREGROUND);

        this._projectile.SetSpeed(200f);
        this._projectile.SendInDirection(Vector2.up, true);
        this._projectile.baseData.range = float.MaxValue;
        TrailController tc = this._projectile.AddTrailToProjectileInstance(Starmageddon._StarmageddonTrailPrefab);
        tc.gameObject.SetGlowiness(10f);
        yield return null; // wait a frame so we can properly set the trails to unoccluded without being overwritten
        tc.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        tc.gameObject.transform.position = tc.gameObject.transform.position.XY().ToVector3ZisY(100f);
        DepthLookupManager.ProcessRenderer(
            tc.sprite.renderer, DepthLookupManager.GungeonSortingLayer.FOREGROUND);

        // Phase 4 -- hang time
        this._projectile.gameObject.Play("starmageddon_bullet_launch_sound");
        yield return new WaitForSeconds(0.25f);
        this._projectile.gameObject.Play("starmageddon_bullet_fall_sound");
        yield return new WaitForSeconds(0.25f);

        // Phase 5 -- falling on enemies
        this._state = State.FALLING;
        GameActor target = FindTarget();
        Vector2 targetPos = target.CenterPosition + Lazy.RandomVector(_SPREAD * UnityEngine.Random.value);
        float fallSpeed = 150f.AddRandomSpread(10f);
        float fallAngle = 270f.AddRandomSpread(24f);
        float fallTime = 0.35f.AddRandomSpread(0.25f);
        Vector2 fallFromPos = targetPos - fallAngle.ToVector(fallTime * fallSpeed);
        this._projectile.specRigidbody.Position = new Position(fallFromPos);
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.SetSpeed(fallSpeed);
        this._projectile.SendInDirection(fallAngle.ToVector(), true);
        for (float elapsed = 0f; elapsed < (fallTime - BraveTime.DeltaTime); elapsed += BraveTime.DeltaTime)
        {
            if (target)
            {
                Vector2 delta = target.CenterPosition - this._projectile.specRigidbody.Position.GetPixelVector2();
                this._projectile.SetSpeed(delta.magnitude / (fallTime - elapsed));
                this._projectile.SendInDirection(delta, true);
            }
            yield return null;
        }
        // this._projectile.specRigidbody.Position = new Position(targetPos);
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.specRigidbody.Reinitialize();
        yield return null;

        this._projectile.DieInAir();
    }

    /// <summary>Select a random target, weighted by inverse square distance to player</summary>
    private GameActor FindTarget()
    {
        RoomHandler room = GameManager.Instance.PrimaryPlayer.CurrentRoom;
        if (room == null || room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> enemies)
            return this._owner;
        List<AIActor> livingEnemies = new();
        List<Vector2> weights = new();
        int i = 0;
        foreach(AIActor enemy in enemies)
            if (enemy && enemy.healthHaver && enemy.healthHaver.IsAlive)
            {
                livingEnemies.Add(enemy);
                weights.Add(new(i++, 1f / (enemy.CenterPosition - this._owner.CenterPosition).sqrMagnitude));
            }
        return (livingEnemies.Count == 0) ? this._owner :  livingEnemies[weights.WeightedRandom()];
    }
}