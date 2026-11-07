namespace CwaffingTheGungy;

public class SubMachineGun : CwaffGun
{
    public static string ItemName         = "Sub Machine Gun";
    public static string ShortDescription = "The Hero You Deserve";
    public static string LongDescription  = "Fires harmless sandwiches that may fully heal and permanently charm enemies. Charm chance is equal to twice the percent of health the target has lost. Charmed allies teleport to the player when entering combat. Can be eaten when out of ammo to restore 1.5 hearts.";
    public static string Lore             = "Calling this object a gun stretches the definition of the word to its absolute limits, but it evidently scrapes by just enough to get a pass from both the legal system and the Breach's marketing division. Even so, this 'gun' doesn't seem to be particularly useful as a conventional firearm.";

    private const float _HEAL_AMOUNT = 1.5f;

    internal static GameObject _NourishVFX;
    internal static GameObject _NourishSelfVFX;

    private List<NourishedFollower> _followers = new();

    public static void Init()
    {
        Lazy.SetupGun<SubMachineGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.CHARM, reloadTime: 1.5f, ammo: 200, shootFps: 20, reloadFps: 10,
            muzzleVFX: "muzzle_sub_machine_gun", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter, smoothReload: 0.1f,
            fireAudio: "sub_machine_gun_fire_sound", reloadAudio: "sub_machine_gun_reload_sound", banFromBlessedRuns: true)
          .InitProjectile(GunData.New(sprite: "sandwich_projectile", clipSize: 5, cooldown: 0.2f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 0.0f, shouldRotate: false, customClip: true))
          .Attach<NourishingProjectile>();

        _NourishVFX = VFX.Create("nourish_vfx", fps: 18, emissivePower: 1, emissiveColour: Color.Lerp(Color.green, Color.white, 0.5f));
        _NourishSelfVFX = Items.Ration.AsActive().gameObject.GetComponent<RationItem>().healVFX;
    }

    public override bool OnAttemptedGunThrow()
    {
        base.OnAttemptedGunThrow();
        if (this.PlayerOwner is not PlayerController pc)
            return true;

        Consume(pc);
        return false;
    }

    private void Consume(PlayerController pc, bool preventedDeath = false)
    {
        if (preventedDeath)
            NourishingProjectile.DoActorNourishVFX(pc);
        else
            pc.gameObject.Play("nourished_sound");
        pc.gameObject.Play("sub_machine_gun_reload_sound");
        pc.healthHaver.ApplyHealing(this.Mastered ? 999f :  _HEAL_AMOUNT);
        pc.PlayEffectOnActor(_NourishSelfVFX, Vector3.zero);
        UnityEngine.Object.Destroy(pc.ForceDropGun(this.gun).gameObject);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        if (!player.ForceZeroHealthState)
            player.healthHaver.ModifyDamage += this.ModifyDamage;
        player.OnEnteredCombat += this.OnEnteredCombat;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        DoCleanup(player);
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        DoCleanup(this.PlayerOwner);
        base.OnDestroy();
    }

    private void DoCleanup(PlayerController player)
    {
        if (player)
        {
          player.OnEnteredCombat -= this.OnEnteredCombat;
          player.healthHaver.ModifyDamage -= this.ModifyDamage;
        }
    }

    private void OnEnteredCombat()
    {
        if (this._followers == null)
          return;
        if (this.PlayerOwner is not PlayerController player || player.CurrentRoom is not RoomHandler room)
          return;
        foreach (NourishedFollower follower in this._followers)
        {
          if (!follower || follower._actor is not AIActor actor)
            continue;
          actor.RelocateToNewRoom(room, forceReparent: true);
          // TODO: teleport VFX?
        }
    }

    public void AddFolower(AIActor follower)
    {
        NourishedFollower nf = follower.gameObject.AddComponent<NourishedFollower>();
        nf.Setup(this);
        this._followers.AddUnique(nf);
    }

    public void RemoveFollower(NourishedFollower follower)
    {
        this._followers.Remove(follower);
    }

    private void ModifyDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!this.Mastered)
            return;
        if (!hh.PlayerWillDieFromHit(data))
            return; // if we're not going to die, we don't need to activate

        data.ModifiedDamage = 0f;
        hh.ModifyDamage -= this.ModifyDamage;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        Consume(this.PlayerOwner, preventedDeath: true);
    }
}

public class NourishingProjectile : MonoBehaviour
{
    private Projectile _proj = null;

    private void Start()
    {
        this._proj = base.GetComponent<Projectile>();
        this._proj.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile p, SpeculativeRigidbody body, bool killed)
    {
        if ((body.aiActor is not AIActor enemy) || (body.healthHaver is not HealthHaver hh) || !hh.IsAlive || killed)
            return; // already dead
        if (this._proj.Owner is not PlayerController player)
            return; // no leader
        if (player.GetGun<SubMachineGun>() is not SubMachineGun smg)
            return; // no sub machine gun
        if (enemy.gameObject.GetComponent<NourishedFollower>() is not NourishedFollower nf)
        {
          if ((1f - hh.GetCurrentHealthPercentage()) <= (0.5f * UnityEngine.Random.value))
              return; // percent chance to charm is equal to twice the percent of depleted health

          enemy.ApplyEffect((ItemHelper.Get(Items.YellowChamber) as YellowChamberItem).CharmEffect);
          if (enemy.CanTargetPlayers || !enemy.CanTargetEnemies)
              return; // failed to apply charm

          smg.AddFolower(enemy);
        }

        hh.FullHeal();
        DoActorNourishVFX(enemy);
    }

    internal static void DoActorNourishVFX(GameActor actor)
    {
        GameObject vfx = SpawnManager.SpawnVFX(SubMachineGun._NourishVFX, actor.sprite.WorldTopCenter + new Vector2(0f, 1f), Quaternion.identity, ignoresPools: true);
        vfx.GetComponent<tk2dSprite>().HeightOffGround = 1f;
        vfx.transform.parent = actor.sprite.transform;
        vfx.AddComponent<GlowAndFadeOut>().Setup(
            fadeInTime: 0.15f, glowInTime: 0.20f, holdTime: 0.0f, glowOutTime: 0.20f, fadeOutTime: 0.15f, maxEmit: 5f, destroy: true);
        actor.gameObject.Play("nourished_sound");
    }
}

public class NourishedFollower : MonoBehaviour
{
  internal AIActor _actor;
  internal HealthHaver _hh;

  private SpeculativeRigidbody _body;
  private PlayerController _leader;
  private SubMachineGun _smg;

  public void Setup(SubMachineGun smg)
  {
    this._smg    = smg;
    this._leader = this._smg.PlayerOwner;
    this._actor  = base.gameObject.GetComponent<AIActor>();
    this._hh     = base.gameObject.GetComponent<HealthHaver>();
    this._body   = base.gameObject.GetComponent<SpeculativeRigidbody>();
    if (base.gameObject.GetComponent<ObjectVisibilityManager>() is ObjectVisibilityManager ovm)
      UnityEngine.Object.Destroy(ovm); // prevent rendering shenanigans related to the old room
    if (!this._actor || !this._hh || !this._body)
    {
      UnityEngine.Object.Destroy(this);
      return;
    }

    if (this._leader.HasSynergy(Synergy.I_NEED_A_HERO))
      this._actor.ReplaceGun(Items.Heroine);

    this._actor.IgnoreForRoomClear = true;
    this._hh.OnPreDeath += this.OnPreDeath;
  }

  private void DoCleanup()
  {
    if (this._smg)
      this._smg.RemoveFollower(this);
    this._smg = null;
  }

  private void OnPreDeath(Vector2 vector)
  {
    DoCleanup();
    UnityEngine.Object.Destroy(this);
  }

  private void OnDestroy()
  {
    DoCleanup();
  }
}
