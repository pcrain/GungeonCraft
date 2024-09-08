namespace CwaffingTheGungy;

/* TODO:
    - Look into logic in ConsumableStealthItem
*/

public class DeadRinger : CwaffPassive
{
    public static string ItemName         = "Dead Ringer";
    public static string ShortDescription = "Tactical Defeat";
    public static string LongDescription  = "Feign death and become stealthed upon taking damage. Shooting while stealthed deals 10x damage and removes stealth. Projectiles deal 5x damage for 2 seconds after breaking stealth.";
    public static string Lore             = "Developed by the French government for use by their elite secret agents in case of their inevitable failure, this marvelous gadget takes making lemonade out of lemons to the next level.";

    internal const float _DEAD_RINGER_STEALTH_MULT   = 10.0f;
    internal const float _DEAD_RINGER_SURPRISE_MULT  = 5.0f;
    internal const float _DEAD_RINGER_SURPRISE_TIMER = 2.0f;
    internal const float _LENIENCE                   = 0.75f; // minimum time after getting hit we can decloak (to prevent instantly losing cloak in panic)
    internal const float _DECOY_LIFE                 = 3f;

    internal static GameObject _CorpsePrefab;
    internal static GameObject _DecoyPrefab;
    internal static GameObject _ExplosiveDecoyPrefab;

    private float _lastActivationTime = 0.0f;
    private float _lastDecloakTime = 0.0f;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<DeadRinger>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        _CorpsePrefab     = BraveResources.Load("Global Prefabs/PlayerCorpse") as GameObject;

        _DecoyPrefab = ItemHelper.Get(Items.Decoy).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.ClonePrefab();
        _DecoyPrefab.GetComponent<Decoy>().DeathExplosionTimer = _DECOY_LIFE;

        _ExplosiveDecoyPrefab = ItemHelper.Get(Items.ExplosiveDecoy).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.ClonePrefab();
        _ExplosiveDecoyPrefab.GetComponent<Decoy>().DeathExplosionTimer = _DECOY_LIFE;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnReceivedDamage += this.OnReceivedDamage;
        this.Owner.PostProcessProjectile += this.SneakAttackProcessor;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        player.OnReceivedDamage -= this.OnReceivedDamage;
        player.PostProcessProjectile -= this.SneakAttackProcessor;
        BreakStealth(player);
    }

    private void OnReceivedDamage(PlayerController player)
    {
        if (this.Owner != player || player.IsStealthed)
            return;
        FeignDeath();
        BecomeInvisible();
    }

    private void FeignDeath()
    {
        this.Owner.gameObject.Play("spy_uncloak_feigndeath");
        if (this.Owner.HasSynergy(Synergy.DEAD_MAN_EXPANDING))
        {
            GameObject decoy = DeadRinger._ExplosiveDecoyPrefab.Instantiate(position: this.Owner.SpriteBottomCenter, anchor: Anchor.LowerCenter);
            if (decoy.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
                body.RegisterGhostCollisionException(this.Owner.specRigidbody);
        }
        else if (this.Owner.HasSynergy(Synergy.DEAD_MAN_STANDING))
        {
            GameObject decoy = DeadRinger._DecoyPrefab.Instantiate(position: this.Owner.SpriteBottomCenter, anchor: Anchor.LowerCenter);
            if (decoy.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
                body.RegisterGhostCollisionException(this.Owner.specRigidbody);
        }
        StartCoroutine(AnimateTheCorpse(this.Owner));
    }

    private IEnumerator AnimateTheCorpse(PlayerController pc)
    {
        tk2dBaseSprite deathSprite = pc.sprite;
        Vector3 deathScale         = pc.sprite.scale;
        Vector3 deathPosition      = pc.sprite.transform.position;

        GameObject corpse          = SpawnManager.SpawnDebris(_CorpsePrefab, pc.transform.position, Quaternion.identity);
        tk2dSprite corpseSprite    = corpse.GetComponent<tk2dSprite>();
        corpseSprite.SetSprite(pc.sprite.Collection, pc.sprite.spriteId);
        tk2dSpriteAnimator animator = corpseSprite.gameObject.GetOrAddComponent<tk2dSpriteAnimator>();
        string feignDeathAnimation = ((!pc.UseArmorlessAnim) ? "death_coop" : "death_coop_armorless");
        // string feignDeathAnimation = "spinfall";
        tk2dSpriteAnimationClip deathClip = pc.spriteAnimator.GetClipByName(feignDeathAnimation);

        animator.Play(deathClip, clipStartTime: 0f, overrideFps: 8f, skipEvents: true);
        yield return null;
        while (animator.IsPlaying(deathClip))
            yield return null;
        corpseSprite.scale = deathScale;
        corpse.transform.position = deathPosition;
        corpseSprite.HeightOffGround = -3.5f;
        corpseSprite.UpdateZDepth();
    }

    // copied and simplified from DoEffect() of CardboardBoxItem.cs
    private void BecomeInvisible()
    {
        if (this.Owner.CurrentGun)
            this.Owner.CurrentGun.CeaseAttack(false);
        this.Owner.OnDidUnstealthyAction += BreakStealth;
        // if (!CanAnyBossOrNPCSee(this.Owner)) // don't need this check, we can feign death in front of them
        this.Owner.SetIsStealthed(true, "DeadRinger");
        this.Owner.SetCapableOfStealing(true, "DeadRinger");
        this._lastActivationTime = BraveTime.ScaledTimeSinceStartup;

        // Apply a shadowy shader
        foreach (Material m in this.Owner.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/HighPriestAfterImage")))
        {
            m.SetFloat("_EmissivePower", 0f);
            m.SetFloat("_Opacity", 0.5f);
            m.SetColor("_DashColor", Color.gray);
        }

        DoSmokeAroundPlayer(8);
    }

    private void BreakStealth(PlayerController pc)
    {
        if (this.Owner != pc)
            return;
        if (JustBecameStealthy())
            return; // don't lose stealth immediately if we shoot right when we get shot

        this._lastDecloakTime = BraveTime.ScaledTimeSinceStartup;
        this.Owner.ClearOverrideShader();
        this.Owner.OnDidUnstealthyAction -= BreakStealth;
        this.Owner.SetIsStealthed(false, "DeadRinger");
        this.Owner.SetCapableOfStealing(false, "DeadRinger");
        this.Owner.gameObject.Play("medigun_heal_detach");
        DoSmokeAroundPlayer(8);
    }

    private bool JustBecameStealthy()
    {
        return BraveTime.ScaledTimeSinceStartup < this._lastActivationTime + _LENIENCE;
    }

    private void SneakAttackProcessor(Projectile proj, float _)
    {
        if (JustBecameStealthy() || !this.Owner)
            return; // don't get sneak attack bonus immediately unless we become unstealthed
        if (this.Owner.IsStealthed)
            proj.baseData.damage *= _DEAD_RINGER_STEALTH_MULT;
        else if ((BraveTime.ScaledTimeSinceStartup - this._lastDecloakTime) < _DEAD_RINGER_SURPRISE_TIMER)
            proj.baseData.damage *= _DEAD_RINGER_SURPRISE_MULT;
        else
            return;

        proj.AdjustPlayerProjectileTint(Color.gray, 2);
    }

    private void DoSmokeAroundPlayer(int amount)
    {
        GameObject smokePrefab = ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject;
        Vector2 ppos = this.Owner.CenterPosition;
        for (int i = 0; i < amount; ++i)
            smokePrefab.Instantiate(position: (ppos + Lazy.RandomVector( UnityEngine.Random.Range(0f,0.5f))), anchor: Anchor.MiddleCenter);
    }
}
