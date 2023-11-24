namespace CwaffingTheGungy;

/* TODO:
    - Look into logic in ConsumableStealthItem
*/

public class DeadRinger : PassiveItem
{
    public static string ItemName         = "Dead Ringer";
    public static string SpritePath       = "dead_ringer_icon";
    public static string ShortDescription = "Tactical Defeat";
    public static string LongDescription  = "Feigh death and become stealthed upon taking damage. Shooting while stealthed deals 10x damage and removes stealth.";
    public static string Lore             = "Developed by the French government for use by their elite secret agents in case of their inevitable failure, this marvelous gadget takes making lemonade out of lemons to the next level.";

    internal const float _DEAD_RINGER_DAMAGE_MULT = 10.0f;
    internal const float _LENIENCE = 0.75f; // minimum time after getting hit we can decloak (to prevent instantly losing cloak in panic)

    internal static GameObject _CorpsePrefab;

    private float _lastActivationTime = 0.0f;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<DeadRinger>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        _CorpsePrefab     = BraveResources.Load("Global Prefabs/PlayerCorpse") as GameObject;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnReceivedDamage += this.OnReceivedDamage;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.OnReceivedDamage -= this.OnReceivedDamage;
        BreakStealth(player);
        return base.Drop(player);
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
        AkSoundEngine.PostEvent("spy_uncloak_feigndeath", this.Owner.gameObject);
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
        this.Owner.CurrentGun?.CeaseAttack(false);
        this.Owner.OnDidUnstealthyAction += BreakStealth;
        this.Owner.PostProcessProjectile += SneakAttackProcessor;
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

        this.Owner.ClearOverrideShader();
        this.Owner.OnDidUnstealthyAction -= BreakStealth;
        this.Owner.PostProcessProjectile -= SneakAttackProcessor;
        this.Owner.SetIsStealthed(false, "DeadRinger");
        this.Owner.SetCapableOfStealing(false, "DeadRinger");
        AkSoundEngine.PostEvent("medigun_heal_detach", this.Owner.gameObject);
        DoSmokeAroundPlayer(8);
    }

    private bool JustBecameStealthy()
    {
        return BraveTime.ScaledTimeSinceStartup < this._lastActivationTime + _LENIENCE;
    }

    private void SneakAttackProcessor(Projectile proj, float _)
    {
        if (JustBecameStealthy())
            return; // don't get sneak attack bonus immediately unless we become unstealthed
        if (this.Owner?.IsStealthed ?? false)
            proj.baseData.damage *= _DEAD_RINGER_DAMAGE_MULT;
    }

    private void DoSmokeAroundPlayer(int amount)
    {
        GameObject smokePrefab = ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject;
        Vector2 ppos = this.Owner.sprite.WorldCenter;
        for (int i = 0; i < amount; ++i)
        {
            GameObject smoke = UnityEngine.Object.Instantiate(smokePrefab);
            tk2dBaseSprite sprite = smoke.GetComponent<tk2dBaseSprite>();
            sprite.PlaceAtPositionByAnchor((ppos + Lazy.RandomVector(
                UnityEngine.Random.Range(0f,0.5f))).ToVector3ZisY(), Anchor.MiddleCenter);
            sprite.transform.position = sprite.transform.position.Quantize(0.0625f);
        }
    }
}
