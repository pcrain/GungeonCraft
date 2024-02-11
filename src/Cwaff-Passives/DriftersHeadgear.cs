namespace CwaffingTheGungy;

public class DriftersHeadgear : PassiveItem
{
    public static string ItemName         = "Drifter's Headgear";
    public static string ShortDescription = "Hyper Light Dodger";
    public static string LongDescription  = "Grants the user an extremely quick dash in place of their dodge roll, but leaves them vulnerable to bullets and enemies while dashing.";
    public static string Lore             = "A memento left behind by a former adventurer said to possess the ability to outrun everything except his inner demons. While he never elaborated on what exactly those demons were, it almost certainly wasn't the likes of the Gundead.";

    internal static GameObject _LinkVFXPrefab;
    internal static Projectile _LightningProjectile;

    private HLDRoll _dodgeRoller = null;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<DriftersHeadgear>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX)
            .RegisterPrefab(deactivate: false);

        var comp = item.gameObject.AddComponent<HLDRoll>();

        _LightningProjectile = Items.GunslingersAshes.CloneProjectile(new(damage: 5.0f, speed: 0.001f));
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner)
            return;

        this._dodgeRoller.isHyped =
            this.Owner.PlayerHasActiveSynergy(Synergy.HYPE_YOURSELF_UP);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if(!(this._dodgeRoller.isDodging && this._dodgeRoller.isHyped))  // reflect projectiles with hyped synergy
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
            this._dodgeRoller.owner = player;
        player.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
        this._dodgeRoller.AbortDodgeRoll();
        return base.Drop(player);
    }

    public override void OnDestroy()
    {
        if (this._dodgeRoller)
            this._dodgeRoller.AbortDodgeRoll();
        base.OnDestroy();
    }
}

public class HLDRoll : CustomDodgeRoll
{
    const float DASH_SPEED  = 55.0f; // Speed of our dash
    const float DASH_TIME   = 0.125f; // Time we spend dashing
    const float DISOWN_TIME = DASH_TIME+0.05f; // Amount of time after our dash starts before lightning is no longer connected
    const float FADE_TIME   = 0.5f; // Amount of time lightning persists after being disowned

    public bool isHyped = false;  // whether the hyped synergy is active

    public override void BeginDodgeRoll()
    {
        base.BeginDodgeRoll();
        if (!(this.isHyped && this.owner))
            return;
        Projectile p = SpawnManager.SpawnProjectile(
          DriftersHeadgear._LightningProjectile.gameObject,
          this.owner.sprite.WorldCenter,
          Quaternion.Euler(0f, 0f, this.owner.m_currentGunAngle),
          true).GetComponent<Projectile>();
            p.Owner = this.owner;
            p.Shooter = this.owner.specRigidbody;

            p.gameObject.AddComponent<FakeProjectileComponent>();
            p.gameObject.AddComponent<Expiration>().expirationTimer = DISOWN_TIME+FADE_TIME;

            OwnerConnectLightningModifier oclm = p.gameObject.AddComponent<OwnerConnectLightningModifier>();
                oclm.linkPrefab = DriftersHeadgear._LinkVFXPrefab;
                oclm.disownTimer = DISOWN_TIME;
                oclm.fadeTimer = FADE_TIME;
                oclm.MakeGlowy();
    }

    public override IEnumerator ContinueDodgeRoll()
    {
        float dashspeed = DASH_SPEED * (this.isHyped ? 1.2f : 1.0f);
        float dashtime = DASH_TIME;

        Vector2 vel = dashspeed * this.owner.m_lastNonzeroCommandedDirection.normalized;

        this.owner.gameObject.Play("teledasher");
        this.owner.SetInputOverride("hld");
        this.owner.SetIsFlying(true, "hld");

        DustUpVFX dusts = GameManager.Instance.Dungeon.dungeonDustups;
        for (int i = 0; i < 16; ++i)
        {
            float dir = UnityEngine.Random.Range(0.0f,360.0f);
            float rot = UnityEngine.Random.Range(0.0f,360.0f);
            float mag = UnityEngine.Random.Range(0.3f,1.25f);
            SpawnManager.SpawnVFX(
                dusts.rollLandDustup,
                this.owner.sprite.WorldCenter + BraveMathCollege.DegreesToVector(dir, mag),
                Quaternion.Euler(0f, 0f, rot));
        }

        bool interrupted = false;
        for (float timer = 0.0f; timer < dashtime; )
        {
            this.owner.PlayerAfterImage();
            timer += BraveTime.DeltaTime;
            this.owner.specRigidbody.Velocity = vel;
            GameManager.Instance.Dungeon.dungeonDustups.InstantiateLandDustup(this.owner.sprite.WorldCenter);
            yield return null;
            if (this.owner.IsFalling)
            {
                interrupted = true;
                break;
            }
        }
        if (!interrupted)
        {
            this.owner.PlayerAfterImage();
            for (int i = 0; i < 8; ++i)
            {
                float dir = UnityEngine.Random.Range(0.0f,360.0f);
                float rot = UnityEngine.Random.Range(0.0f,360.0f);
                float mag = UnityEngine.Random.Range(0.3f,1.0f);
                SpawnManager.SpawnVFX(
                    dusts.rollLandDustup,
                    this.owner.sprite.WorldCenter + BraveMathCollege.DegreesToVector(dir, mag),
                    Quaternion.Euler(0f, 0f, rot));
            }
        }
        this.owner.spriteAnimator.Stop();
        this.owner.SetIsFlying(false, "hld");
        this.owner.ClearInputOverride("hld");
    }
}
