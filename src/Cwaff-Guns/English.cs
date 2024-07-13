namespace CwaffingTheGungy;

using System;
using static BilliardBall.State;

public class English : CwaffGun
{
    public static string ItemName         = "English";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _BilliardBallPhantom = null;
    internal static LinkedList<BilliardBall> _ExtantBilliards = new();
    internal static Projectile _BilliardBall = null;

    private RoomHandler _lastRoom = null;
    private List<GameObject> _phantoms = null;
    private bool _wasCharging = false;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<English>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox, fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound");

        _BilliardBall = gun.InitProjectile(GunData.New(clipSize: -1, cooldown: 0.25f, angleVariance: 5.0f, chargeTime: 0.5f,
          shootStyle: ShootStyle.Charged, damage: 3.0f, speed: 75.0f, range: 9999f, spawnSound: "tomislav_shoot",
          sprite: "billiard_ball", fps: 12, scale: 1.5f, anchor: Anchor.MiddleCenter)).Attach<BilliardBall>();
        _BilliardBall.collidesWithProjectiles = true;
        _BilliardBall.collidesOnlyWithPlayerProjectiles = true;

        _BilliardBallPhantom = VFX.Create("billiard_ball_vfx", fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 1.5f);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        this._lastRoom = player.CurrentRoom;
        UpdateBilliardCollision(this._lastRoom);
        EnsurePhantoms();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        DismissPhantoms();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        EnsurePhantoms();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        DismissPhantoms();
    }

    public override void OnDestroy()
    {
        if (this._phantoms != null)
            for (int i = this._phantoms.Count - 1; i >= 0; --i)
                this._phantoms[i].SafeDestroy();
        base.OnDestroy();
    }

    private void EnsurePhantoms()
    {
        if (this._phantoms == null)
            this._phantoms = new(10);
        for (int i = this._phantoms.Count; i < 10; ++i)
            this._phantoms.Add(null);
        for (int i = 0; i < 10; ++i)
        {
            if (this._phantoms[i])
                continue;
            this._phantoms[i] = SpawnManager.SpawnVFX(_BilliardBallPhantom, base.transform.position, Quaternion.identity, ignoresPools: true);
            this._phantoms[i].SetActive(false);
        }
    }

    private void DismissPhantoms()
    {
        if (this._phantoms == null)
            return;
        for (int i = 0; i < this._phantoms.Count; ++i)
            if (this._phantoms[i])
                this._phantoms[i].SetActive(false);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;

        if (this._lastRoom != player.CurrentRoom)
        {
            this._lastRoom = player.CurrentRoom;
            UpdateBilliardCollision(this._lastRoom);
        }
        if (this.gun.IsCharging)
            UpdatePhantomBilliards();
        else if (this._wasCharging)
            DismissPhantoms();
        this._wasCharging = this.gun.IsCharging;
    }

    private void UpdatePhantomBilliards()
    {
        if (this.PlayerOwner is not PlayerController player)
            return;
        bool charging = this.gun.IsCharging;
        Vector2 basePos = this.gun.barrelOffset.position;
        float baseAngle = this.gun.CurrentAngle;
        int i = 0;
        for (int d = 1; d <= 4; ++d)
        {
            Vector2 baseVec = baseAngle.ToVector(1.5f + 0.75f * d);
            for (int w = 0; w < d; ++w)
            {
                Vector2 perpVec = (baseAngle + 90f).ToVector(0.5f * (d - 1) - w);
                Vector2 pos = basePos + baseVec + perpVec;
                this._phantoms[i].SetActive(true);
                this._phantoms[i].GetComponent<tk2dSprite>().PlaceAtPositionByAnchor(pos, Anchor.MiddleCenter);
                ++i;
            }
        }
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this._wasCharging)
        {
            this._wasCharging = false;
            MaterializePhantoms();
            DismissPhantoms();
        }
    }

    private void MaterializePhantoms()
    {
        for (int i = 0; i < 10; ++i)
        {
            Vector2 pos = this._phantoms[i].GetComponent<tk2dSprite>().WorldCenter;
            GameObject projObj = SpawnManager.SpawnProjectile(_BilliardBall.gameObject, pos, Quaternion.identity, true);
            Projectile proj = projObj.GetComponent<Projectile>();
                proj.Owner = this.PlayerOwner;
                proj.collidesWithEnemies = true;
                proj.collidesWithPlayer = false;
                proj.baseData.speed = 0.01f;
            projObj.GetComponent<BilliardBall>().Setup(activate: false);
        }
    }

    private static void UpdateBilliardCollision(RoomHandler room)
    {
        LinkedListNode<BilliardBall> next = _ExtantBilliards.First;
        while (next != null)
        {
            if (!next.Value)
            {
                LinkedListNode<BilliardBall> dead = next;
                next = next.Next;
                _ExtantBilliards.Remove(dead);
                continue;
            }
            BilliardBall bball = next.Value;
            next = next.Next;
            bball.UpdateCollision(room);
        }
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        base.OnFullClipReload(player, gun);
        // Cycle through 1, 3, 6, and 10 balls variations
    }

    public static BilliardBall NearestBilliardWithinRadiusAndAngle(BilliardBall me, BilliardBall other, float radius, float angle)
    {
        float sqrRadius = radius * radius;
        Vector2 pos = me._projectile.SafeCenter;
        BilliardBall best = null;
        float bestAngle = BilliardBall._MAX_ADJUST;
        LinkedListNode<BilliardBall> next = _ExtantBilliards.First;
        while (next != null)
        {
            if (!next.Value)
            {
                LinkedListNode<BilliardBall> dead = next;
                next = next.Next;
                _ExtantBilliards.Remove(dead);
                continue;
            }
            BilliardBall bball = next.Value;
            next = next.Next;

            if (bball == me || bball == other)
                continue; // don't adjust towards ourselves or the ball we just collided with
            if (bball._state != RESTING)
                continue; // don't adjust towards moving balls
            Vector2 bpos = bball._projectile.SafeCenter;
            Vector2 delta = (bpos - pos);
            if (delta.sqrMagnitude > sqrRadius)
                continue; // don't adjust towards balls that are too far
            float deltaAngle = delta.ToAngle();
            float relAngle = deltaAngle.AbsAngleTo(angle);
            if (relAngle > bestAngle)
                continue; // don't adjust at weird angles
            if (!pos.HasLineOfSight(bpos))
                continue; // don't adjust to balls we can't see
            bestAngle = relAngle;
            best = bball;
        }
        return best;
    }
}


public class BilliardBall : MonoBehaviour
{
    internal enum State
    {
        FIRED,
        RESTING,
        ACTIVATED,
    }

    private const float _FRICTION = 0.965f;
    private const float _COLLISON_DAMPING = 0.9f;
    private const float _MIN_SPEED = 1.0f;
    private const int _SUPER_BOUNCES = 1; // number of times we can bounce and keep more velocity than normal
    internal const float _MAX_ADJUST = 30f; // adjust our angle by at most 30 degress to an actual enemy

    internal Projectile _projectile   = null;
    private PlayerController _owner  = null;
    private List<BilliardBall> _collisionsThisFrame = new();
    internal State _state             = FIRED;
    private int _superBounces; // times we can bounce and keep some velocity
    private float _baseDamage;
    private SpeculativeRigidbody _body;
    private bool _setup = false;

    private void Start()
    {
        Setup(activate: true);
    }

    public void Setup(bool activate)
    {
        if (this._setup)
            return;

        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._baseDamage = this._projectile.baseData.damage;
        this._body = this._projectile.specRigidbody;

        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;

        this._body.OnRigidbodyCollision += this.OnRigidbodyCollision;
        this._body.OnTileCollision += this.OnTileCollision;

        if (activate)
            this.Activate();
        else
            this.Deactivate();
        this._state = FIRED;
        English._ExtantBilliards.AddLast(this);

        this._setup = true;
    }

    private void OnTileCollision(CollisionData collision)
    {
        // TODO: refactor since this is used by a lot of projectiles at this point
        SpeculativeRigidbody body = collision.MyRigidbody;
        Vector2 newVel            = body.Velocity;
        if (collision.CollidedX)
          newVel = newVel.WithX(-newVel.x);
        if (collision.CollidedY)
          newVel = newVel.WithY(-newVel.y);
        newVel *= _COLLISON_DAMPING;
        this._projectile.m_currentSpeed = this._projectile.baseData.speed = newVel.magnitude;
        newVel = AdjustTowardsNearbyEnemiesOrBalls(this, this, newVel);
        this._projectile.SendInDirection(newVel, true);
        PhysicsEngine.PostSliceVelocity = newVel;
    }

    private void OnRigidbodyCollision(CollisionData collision)
    {
        if (collision.OtherRigidbody is SpeculativeRigidbody other && other.gameObject.GetComponent<BilliardBall>() is BilliardBall bb)
        {
            DoElasticCollision(collision, bb);
            return;
        }
        if (this._state == RESTING)
            return;

        // TODO: refactor since this is used by a lot of projectiles at this point
        SpeculativeRigidbody body = collision.MyRigidbody;
        Vector2 newVel            = body.Velocity;
        if (collision.CollidedX)
          newVel = newVel.WithX(-newVel.x);
        if (collision.CollidedY)
          newVel = newVel.WithY(-newVel.y);
        newVel *= _COLLISON_DAMPING;
        this._projectile.m_currentSpeed = this._projectile.baseData.speed = newVel.magnitude;
        this._projectile.SendInDirection(newVel, true);
        PhysicsEngine.PostSliceVelocity = newVel;

        base.gameObject.Play("billiard_collide_sound");
    }

    private void DoElasticCollision(CollisionData collision, BilliardBall other)
    {
        if (this._collisionsThisFrame.Contains(other))
            return;

        bool isSuperBounce = (this._superBounces--) > 0;

        Projectile otherProj = other.GetComponent<Projectile>();

        Vector2 x1 = collision.MyRigidbody.UnitCenter;
        Vector2 x2 = collision.OtherRigidbody.UnitCenter;
        Vector2 v1 = collision.MyRigidbody.Velocity;
        Vector2 v2 = collision.OtherRigidbody.Velocity;
        float distNorm = Mathf.Max(0.1f,(x1-x2).sqrMagnitude);
        Vector2 newv1 = v1 - (Vector2.Dot(v1-v2,x1-x2) / distNorm) * (x1-x2);
        Vector2 newv2 = v2 - (Vector2.Dot(v2-v1,x2-x1) / distNorm) * (x2-x1);

        float myMag    = isSuperBounce ? Mathf.Max(newv1.magnitude, newv2.magnitude) : newv1.magnitude;
        float otherMag = isSuperBounce ? myMag : newv2.magnitude;

        PhysicsEngine.PostSliceVelocity = newv1;
        this._projectile.baseData.speed = myMag;
        this._projectile.UpdateSpeed();
        this._projectile.SendInDirection(newv1, true);
        this.Activate();

        otherProj.baseData.speed = otherMag;
        otherProj.UpdateSpeed();
        otherProj.SendInDirection(AdjustTowardsNearbyEnemiesOrBalls(this, other, newv2), true);
        other.Activate();

        this._collisionsThisFrame.Add(other);
        other._collisionsThisFrame.Add(this);

        base.gameObject.Play("billiard_collide_sound");
    }

    private static Vector2 AdjustTowardsNearbyEnemiesOrBalls(BilliardBall self, BilliardBall other, Vector2 origDir)
    {
        const float _MAX_RADIUS = 10f;
        Vector2 pos = self._projectile.SafeCenter;
        float origAngle = origDir.ToAngle();
        Vector2? nearest = Lazy.NearestEnemyPosWithinConeOfVision(start: pos, coneAngle: origAngle, maxDeviation: _MAX_ADJUST, maxDistance: _MAX_RADIUS);
        if (nearest.HasValue)
            return (nearest.Value - pos);
        if (English.NearestBilliardWithinRadiusAndAngle(self, other, _MAX_RADIUS, origAngle) is BilliardBall bb)
            return (bb._projectile.SafeCenter - pos);
        return origDir;
    }

    private void Update()
    {
        switch (this._state)
        {
            case RESTING:   UpdateResting();   break;
            case FIRED:     UpdateActivated(); break;
            case ACTIVATED: UpdateActivated(); break;
        }
        this._collisionsThisFrame.Clear();
    }

    private void UpdateResting()
    {
        // disable collision while the player is in a different room
    }

    private void UpdateActivated()
    {
        this._projectile.ApplyFriction(_FRICTION);
        float speed = this._projectile.baseData.speed;
        if (speed < _MIN_SPEED)
        {
            this.Deactivate();
            return;
        }

        if (this._state == ACTIVATED)
            this._projectile.baseData.damage = this._baseDamage * Mathf.Log(speed, 2);
        else if (this._state == FIRED)
            this._projectile.baseData.damage = this._baseDamage;
    }

    internal void UpdateCollision(RoomHandler room)
    {
        if (!this._body)
            return;
        this._body.enabled = this._projectile
            ? (room == this._projectile.SafeCenter.GetAbsoluteRoom())
            : false;
    }

    private void Activate()
    {
        if (this._state == ACTIVATED)
            return;
        this._projectile.collidesWithEnemies = true;
        this._projectile.ManualControl       = false;
        this._projectile.UpdateCollisionMask();
        this._superBounces                   = _SUPER_BOUNCES;
        this._state                          = ACTIVATED;
    }

    private void Deactivate()
    {
        if (this._state == RESTING)
            return;
        this._projectile.specRigidbody.Velocity = Vector2.zero;
        this._projectile.collidesWithEnemies    = false;
        this._projectile.ManualControl          = true;
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric);
        this._projectile.UpdateCollisionMask();
        this._state                             = RESTING;
    }
}
