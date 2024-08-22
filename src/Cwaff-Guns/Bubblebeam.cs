namespace CwaffingTheGungy;

public class Bubblebeam : CwaffGun
{
    public static string ItemName         = "Bubblebeam";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _BubbleVFX = null;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Bubblebeam>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        gun.InitProjectile(GunData.New(sprite: null, clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, collidesWithProjectiles: true,
            damage: 0.0f, speed: 25f, range: 18f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound", hitWallSound: "paintball_impact_wall_sound"))
          .Attach<BubblebeamProjectile>();

        _BubbleVFX = VFX.Create("capture_bubble", fps: 6);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        //
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        //
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        //
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        //
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        //
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        //
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            //
        }
        base.OnDestroy();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner || !this.PlayerOwner.AcceptingNonMotionInput)
            return;

        //
    }
}

public class BubblebeamProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Bubblebeam _gun;
    private float _force = 0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        if (this._owner.CurrentGun is Gun gun)
            this._gun = gun.gameObject.GetComponent<Bubblebeam>();

        this._force = this._projectile.baseData.force;
        this._projectile.baseData.force = 0f; // don't want to apply normal knockback
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!this._projectile)
            return;
        if (otherRigidbody.gameObject.GetComponent<Projectile>() is Projectile p)
        {
            if (p.Owner is not AIActor enemy)
                return;
            EnbubbleProjectile(p);
            Pop();
            PhysicsEngine.SkipCollision = true;
            return;
        }
    }

    private void Pop()
    {
        this._projectile.DieInAir();
    }

    private const float MIN_BUBBLE_SPEED = 5.0f;
    private void EnbubbleProjectile(Projectile p)
    {
        float newSpeed = p.Speed - this._projectile.Speed;
        if (newSpeed > MIN_BUBBLE_SPEED)
        {
            p.SetSpeed(newSpeed);
            return;
        }
        //TODO: don't play sound effect, maybe use reverse patch?
        PassiveReflectItem.ReflectBullet(p, true, this._projectile.Owner, MIN_BUBBLE_SPEED);
        //TODO: draw bubble around projectile
        p.SetSpeed(0.01f);
    }

    // private const float ENBUBBLE_ENEMY_CHANCE = 0.1f;
    private const float ENBUBBLE_ENEMY_CHANCE = 1f;
    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool _)
    {
        if (UnityEngine.Random.value > ENBUBBLE_ENEMY_CHANCE)
            return;
        if (enemy.gameObject.GetComponent<AIActor>() is not AIActor actor)
            return;
        EnbubbleEnemy(actor, p);
    }

    private void EnbubbleEnemy(AIActor actor, Projectile p)
    {
        if (actor.gameObject.GetComponent<BehaviorSpeculator>() is not BehaviorSpeculator bs)
            return;
        if (bs.ImmuneToStun)
            return;
        actor.gameObject.GetOrAddComponent<EnbubbledEnemyBehaviour>().PushBubble(p);
    }

    private void Update()
    {
      // enter update code here
    }

    private void OnDestroy()
    {
      // enter destroy code here
    }
}

public class EnbubbledEnemyBehaviour : MonoBehaviour
{
    private GameObject _vfx = null;
    private AIActor _target = null;
    private tk2dSprite _bubble = null;
    private float _lifetime = 0.0f;
    private bool _setup = false;

    public void PushBubble(Projectile p)
    {
        Setup();
        if (!this._target || !this._target.specRigidbody || !this._target.knockbackDoer)
            return;

        Vector2 projMomentum = 0.2f * p.Speed * p.Direction;
        Vector2 newMomentum = projMomentum + (this._target.specRigidbody.Velocity * 0.1f * this._target.knockbackDoer.weight);
        this._target.knockbackDoer.ClearContinuousKnockbacks();
        this._target.knockbackDoer.ApplyContinuousKnockback(newMomentum, newMomentum.magnitude);
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (this._setup)
            return;

        this._setup = true;
        this._target = base.gameObject.GetComponent<AIActor>();
        if (!this._target || !this._target.sprite || !this._target.specRigidbody)
            return;

        this._target.SetIsFlying(true, "enbubbled", true, true);
        this._target.specRigidbody.OnCollision += this.OnAnyCollision;
        if (this._target.behaviorSpeculator is BehaviorSpeculator bs && !bs.ImmuneToStun)
            bs.Stun(36000f, false);

        this._vfx = SpawnManager.SpawnVFX(Bubblebeam._BubbleVFX, this._target.transform.position, Quaternion.identity, ignoresPools: true);
        this._vfx.transform.parent = this._target.transform;
        this._bubble = this._vfx.GetComponent<tk2dSprite>();
        this._bubble.PlaceAtScaledPositionByAnchor(this._target.CenterPosition, Anchor.MiddleCenter);
        this._bubble.SetAlphaImmediate(0.25f);

        tk2dSpriteDefinition enemyDef = this._target.sprite.GetCurrentSpriteDef();
        tk2dSpriteDefinition bubbleDef = this._bubble.GetCurrentSpriteDef();
        float scale = 0.25f + Mathf.Max(enemyDef.boundsDataExtents.x, enemyDef.boundsDataExtents.y) / bubbleDef.boundsDataExtents.y;
        this._bubble.scale = new Vector3(scale, scale, 1f);
    }

    private void OnAnyCollision(CollisionData collision)
    {
      SpeculativeRigidbody body = collision.MyRigidbody;
      Vector2 newVel            = body.Velocity;
      Vector2 kbVel             = this._target.KnockbackVelocity;
      if (collision.CollidedX)
      {
          newVel = newVel.WithX(-body.Velocity.x);
          kbVel  = kbVel.WithX(-kbVel.x);
      }
      else
      {
          newVel = newVel.WithY(-body.Velocity.y);
          kbVel  = kbVel.WithY(-kbVel.y);
      }
      PhysicsEngine.PostSliceVelocity = newVel;

      if (this._target.knockbackDoer)
      {
          this._target.knockbackDoer.ClearContinuousKnockbacks();
          this._target.knockbackDoer.ApplyContinuousKnockback(kbVel, kbVel.magnitude * 0.1f * this._target.knockbackDoer.weight);
      }
    }

    private void UpdateKnockback()
    {
        if (!this._target || !this._target.specRigidbody)
            return;
        this._target.KnockbackVelocity = this._target.specRigidbody.Velocity;
    }

    private const float MIN_DRIFT = 1f;
    private const float BUBBLE_DRIFT_DECAY = 1f;
    private void Update()
    {
        if (!this._target || !this._target.specRigidbody || this._target.knockbackDoer is not KnockbackDoer kb)
            return;
        if (kb.m_activeContinuousKnockbacks == null)
            return;
        for (int i = 0; i < kb.m_activeContinuousKnockbacks.Count; ++i)
        {
            float newMag = (kb.m_activeContinuousKnockbacks[i].magnitude - BUBBLE_DRIFT_DECAY * BraveTime.DeltaTime);
            if (newMag > MIN_DRIFT)
                kb.m_activeContinuousKnockbacks[i] = newMag * kb.m_activeContinuousKnockbacks[i].normalized;
        }
    }

    private void LateUpdate()
    {
        this._bubble.HeightOffGround = 4f;
        this._bubble.UpdateZDepth();
    }

    private void OnDestroy()
    {
        if (this._vfx)
            UnityEngine.Object.Destroy(this._vfx);
    }

    // /// <summary></summary>
    // [HarmonyPatch(typeof(KnockbackDoer), nameof(KnockbackDoer.Update))]
    // private class KnockbackDoerUpdateForBubblePatch
    // {
    //     static bool Prefix(KnockbackDoer __instance, object arg, ref ReturnType __result)
    //     {
    //         if (__instance.gameObject.GetComponent<EnbubbledEnemyBehaviour>() is not EnbubbledEnemyBehaviour eeb)
    //             return true; // call the original method
    //         eeb.UpdateKnockback();
    //         return false; // skip the original method
    //     }
    // }
}
