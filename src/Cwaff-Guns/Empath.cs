namespace CwaffingTheGungy;

public class Empath : CwaffGun
{
    public static string ItemName         = "Empath";
    public static string ShortDescription = "More than a Feeling";
    public static string LongDescription  = "Fires eyeballs that pass through enemies but collide with their projectiles, destroying them and damaging their owners in the process.";
    public static string Lore             = "Within the confines of the Gungeon, a small part of one's spirit is attached to each and every shot fired -- a link enabling the Gungeon's various treasures to impart their properties upon one's projectiles. By amplifying that attachment by several orders of magnitude, it's possible to inflict damage upon a projectile's owner simply by destroying the projectile. Whether treating a projectile to a day at the spa has positive effects on the owner has not yet been tested.";

    private const float _EMPATHY_DAMAGE_MULT = 4f;
    private const int _MAX_BULLETS_DESTROYED = 5;

    internal static GameObject _PsychicVFX = null;

    private bool _setupAnimator = false;

    public static void Init()
    {
        Lazy.SetupGun<Empath>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 2.1f, ammo: 480, shootFps: 14, reloadFps: 1,
            fireAudio: "empath_fire_sound", smoothReload: 0.0f, muzzleVFX: "muzzle_empath", muzzleFps: 30, muzzleAnchor: Anchor.MiddleCenter)
          .SetReloadAudio("empath_reload_sound_2", 7)
          .InitProjectile(GunData.New(sprite: "empath_projectile", clipSize: 15, cooldown: 0.13f, shootStyle: ShootStyle.SemiAutomatic, fps: 11,
            damage: 3.75f, speed: 18f, range: 999f, force: 12f, collidesWithProjectiles: true, bossDamageMult: 0.6f, scale: 2.0f, customClip: true))
          .SetAllImpactVFX(VFX.CreatePool("empath_impact_vfx", fps: 30, loops: false));

        _PsychicVFX = VFX.Create("empath_psychic_damage_vfx", fps: 30, loops: false);
    }

    public override void Update()
    {
        base.Update();
        if (!this._setupAnimator)
        {
          base.gameObject.GetComponent<tk2dSpriteAnimator>().AnimationEventTriggered += this.AnimationEventTriggered;
          this._setupAnimator = true;
        }
    }

    private void AnimationEventTriggered(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
        if (frame != 7 || !this.PlayerOwner || clip.name != "empath_reload")
            return;
        Vector2 eyePosition = this.gun.sprite.WorldCenter + (base.transform.rotation * (new Vector2(-6f/16f, -2f/16f))).XY();
        CwaffVFX.Spawn(_PsychicVFX, position: eyePosition, lifetime: 0.5f, startScale: 0.5f, endScale: 3.0f, fadeOutTime: 0.4f,
            anchorTransform: base.transform);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        projectile.specRigidbody.OnRigidbodyCollision += this.OnCollision;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody)
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.gameObject.GetComponent<Projectile>() is Projectile p && p.isActiveAndEnabled)
        {
            if (p.m_owner is PlayerController)
                PhysicsEngine.SkipCollision = true;
            return;
        }
        else if (this.Mastered && otherRigidbody.gameObject.GetComponent<AIActor>())
            return;
        PhysicsEngine.SkipCollision = true;
    }

    private void OnCollision(CollisionData collision)
    {
        if (!collision.OtherRigidbody || collision.OtherRigidbody.gameObject is not GameObject otherObject)
            return;
        if (this.Mastered && otherObject.GetComponent<AIActor>() is AIActor target)
        {
            HandleEnemyCollision(target);
            return;
        }
        if (otherObject.GetComponent<Projectile>() is not Projectile other)
            return;

        Projectile p = collision.MyRigidbody.gameObject.GetComponent<Projectile>();
        if (other.Owner is not AIActor enemy || !enemy.specRigidbody || !enemy.healthHaver || !enemy.healthHaver.IsAlive)
        {
            p.DieInAir();
            other.DieInAir();
            return;
        }

        Vector2 enemyPos = enemy.specRigidbody.UnitCenter;
        enemy.gameObject.Play("empath_collide_sound");
        CwaffVFX.SpawnBurst(prefab: _PsychicVFX, numToSpawn: 4, basePosition: enemyPos, positionVariance: 1f,
          minVelocity: 2f, uniform: true, velType: CwaffVFX.Vel.AwayRadial, lifetime: 0.4f);
        p.HandleHitEffectsMidair();  // play hit effects at position of projectile before moving
        p.specRigidbody.Position = new Position(enemyPos);
        p.specRigidbody.UpdateColliderPositions();
        LinearCastResult lcr = LinearCastResult.Pool.Allocate();
        p.baseData.damage *= _EMPATHY_DAMAGE_MULT;
        p.ForceCollision(enemy.specRigidbody, lcr);
        LinearCastResult.Pool.Free(ref lcr);
        other.DieInAir();
    }

    private static readonly List<Projectile> _ProjectilesToDestroy = new();
    private void HandleEnemyCollision(AIActor enemy)
    {
        if (!enemy || !enemy.healthHaver || !enemy.healthHaver.IsAlive)
            return;

        for (int i = StaticReferenceManager.AllProjectiles.Count - 1; i >=0; --i)
        {
            Projectile p = StaticReferenceManager.AllProjectiles[i];
            if (p && p.isActiveAndEnabled && !p.HasDiedInAir && p.Owner == enemy && p.sprite)
                _ProjectilesToDestroy.Add(p);
        }

        int numToDestroy = _MAX_BULLETS_DESTROYED;
        if (numToDestroy > _ProjectilesToDestroy.Count)
            numToDestroy = _ProjectilesToDestroy.Count;
        else if (numToDestroy < _ProjectilesToDestroy.Count)
            _ProjectilesToDestroy.Shuffle();

        for (int i = 0; i < numToDestroy; ++i)
        {
            Projectile p = _ProjectilesToDestroy[i];
            tk2dBaseSprite dupe = p.sprite.DuplicateInWorld();
            dupe.StartCoroutine(SchrodingersStat.PhaseOut(dupe, Vector2.right, 25f, 90f, 1.0f));
            p.DieInAir();
        }
        if (numToDestroy > 0)
            enemy.gameObject.Play("schrodinger_dead_sound");
        _ProjectilesToDestroy.Clear();
    }
}
