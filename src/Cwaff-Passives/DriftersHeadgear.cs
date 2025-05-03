namespace CwaffingTheGungy;

public class DriftersHeadgear : CwaffDodgeRollItem
{
    public static string ItemName         = "Drifter's Headgear";
    public static string ShortDescription = "Hyper Light Dodger";
    public static string LongDescription  = "Grants the player an extremely quick dash in place of their dodge roll, but leaves them vulnerable to bullets and enemies while dashing.";
    public static string Lore             = "A memento left behind by a former adventurer said to possess the ability to outrun everything except his inner demons. While he never elaborated on what exactly those demons were, it almost certainly wasn't the likes of the Gundead.";

    internal static GameObject _LinkVFXPrefab;
    internal static Projectile _LightningProjectile;

    private HLDRoll _dodgeRoller = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<DriftersHeadgear>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.gameObject.AddComponent<HLDRoll>();

        _LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX)
            .RegisterPrefab(deactivate: false);

        _LightningProjectile = Items.GunslingersAshes.CloneProjectile(GunData.New(damage: 5.0f, speed: 0.001f));
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;

        this._dodgeRoller.isHyped =
            this.Owner.HasSynergy(Synergy.HYPE_YOURSELF_UP);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if(!(this._dodgeRoller._isDodging && this._dodgeRoller.isHyped))  // reflect projectiles with hyped synergy
            return;
        Projectile component = otherRigidbody.GetComponent<Projectile>();
        if (component != null && !(component.Owner is PlayerController))
        {
            PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
            PhysicsEngine.SkipCollision = true;
        }
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._dodgeRoller = this.gameObject.GetComponent<HLDRoll>();
        player.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
        if (this._dodgeRoller)
            this._dodgeRoller.AbortDodgeRoll();
    }

    public override CustomDodgeRoll CustomDodgeRoll()
    {
        if (!this._dodgeRoller)
            this._dodgeRoller = this.gameObject.GetComponent<HLDRoll>();
        return this._dodgeRoller;
    }
}

public class HLDRoll : CustomDodgeRoll
{
    const float DASH_SPEED  = 55.0f; // Speed of our dash
    const float DASH_TIME   = 0.125f; // Time we spend dashing
    const float DISOWN_TIME = DASH_TIME+0.05f; // Amount of time after our dash starts before lightning is no longer connected
    const float FADE_TIME   = 0.5f; // Amount of time lightning persists after being disowned

    public bool isHyped = false;  // whether the hyped synergy is active

    private Vector2 _dashDir;

    public override float bufferWindow       => 0.5f;
    public override bool  canUseWeapon       => true;
    public override bool  dodgesProjectiles  => false; // we have our own projectile collision handling
    public override bool  takesContactDamage => false;

    protected override void BeginDodgeRoll(Vector2 direction, bool buffered, bool wasAlreadyDodging)
    {
        this._dashDir = (direction != Vector2.zero) ? direction : this._owner.m_lastNonzeroCommandedDirection.normalized;

        if (!this.isHyped)
            return;

        Projectile p = SpawnManager.SpawnProjectile(
          DriftersHeadgear._LightningProjectile.gameObject,
          this._owner.CenterPosition,
          Quaternion.Euler(0f, 0f, this._owner.m_currentGunAngle),
          true).GetComponent<Projectile>();
            p.Owner = this._owner;
            p.Shooter = this._owner.specRigidbody;

            p.gameObject.AddComponent<FakeProjectileComponent>();
            p.gameObject.AddComponent<ProjectileExpiration>().expirationTimer = DISOWN_TIME+FADE_TIME;

            OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                oclm.linkPrefab = DriftersHeadgear._LinkVFXPrefab;
                oclm.disownTimer = DISOWN_TIME;
                oclm.fadeTimer = FADE_TIME;
                oclm.MakeGlowy();
    }

    protected override IEnumerator ContinueDodgeRoll()
    {
        float dashspeed = DASH_SPEED * (this.isHyped ? 1.2f : 1.0f);

        Vector2 vel = dashspeed * this._dashDir;

        this._owner.gameObject.Play("teledasher");

        DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
        for (int i = 0; i < 16; ++i)
        {
            float dir = UnityEngine.Random.Range(0.0f,360.0f);
            float rot = UnityEngine.Random.Range(0.0f,360.0f);
            float mag = UnityEngine.Random.Range(0.3f,1.25f);
            SpawnManager.SpawnVFX(
                dusts.rollLandDustup,
                this._owner.CenterPosition + BraveMathCollege.DegreesToVector(dir, mag),
                Quaternion.Euler(0f, 0f, rot));
        }

        bool interrupted = false;
        for (float timer = 0.0f; timer < DASH_TIME; timer += BraveTime.DeltaTime)
        {
            this._owner.PlayerAfterImage();
            this._owner.specRigidbody.Velocity = vel;
            GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(this._owner.CenterPosition);
            yield return null;
            if (this._owner.IsFalling)
            {
                interrupted = true;
                break;
            }
        }
        if (!interrupted)
        {
            this._owner.PlayerAfterImage();
            for (int i = 0; i < 8; ++i)
            {
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float mag = UnityEngine.Random.Range(0.3f,1.0f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    this._owner.CenterPosition + BraveMathCollege.DegreesToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }
        }
        this._owner.spriteAnimator.Stop();
    }
}
