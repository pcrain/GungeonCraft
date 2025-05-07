namespace CwaffingTheGungy;

public class AdrenalineShot : CwaffPassive
{
    public static string ItemName         = "Adrenaline Shot";
    public static string ShortDescription = "Just a Little Longer";
    public static string LongDescription  = "Upon taking fatal damage, the player is put in a critical 0-health state and has 60 seconds to restore at least half a heart or exit the current floor. Taking any damage in this state decreases the countdown by 4 seconds. This item cannot be used by the Robot or other 0-health characters, and gives 20 casings instead.";
    public static string Lore             = "This otherwise normal-looking epinephrine injector has approximately 5 times the doctor-approved amount of adrenaline deemed necessary for your everyday anaphylaxis. While there's no telling what kinds of long-term effects that much adrenaline might have on your health, it's reasonable to assume it probably won't be worse than the short-term effects of having your vital organs punctured by bullets...probably.";

    internal const float _MAX_RUSH_TIME = 90f;
    internal const float _MAX_ADRENALINE_TIME = 60f;
    internal const float _MAX_ADRENALINE_LOSS = 4f; // loss from taking damage while under effects of adrenaline
    internal const int   _CASINGS_FOR_ROBOT   = 20;
    internal static float _LastHeartbeatTime = 0f;

    internal bool  _adrenalineActive = false;
    private float _adrenalineTimer  = _MAX_ADRENALINE_TIME;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<AdrenalineShot>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
    }

    public override void OnFirstPickup(PlayerController player)
    {
        if (player.ForceZeroHealthState) // Robot + 0-health characters can't use this item, so just give them some money
            LootEngine.SpawnCurrency(player.CenterPosition, _CASINGS_FOR_ROBOT);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        if (!player.ForceZeroHealthState)
            player.healthHaver.ModifyDamage += this.OnTakeDamage;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;
        if (this._adrenalineActive)
            DeactivateAdrenaline(); // shouldn't ever be able to happen, but just in case
        if (!player.ForceZeroHealthState)
            player.healthHaver.ModifyDamage -= this.OnTakeDamage;
    }

    public override void Update()
    {
        base.Update();
        if (!this.Owner || !this._adrenalineActive)
            return; // nothing to do if we don't have active adrenaline
        if (GameManager.Instance.IsLoadingLevel)
        {
            DeactivateAdrenaline();
            return;
        }
        if (GameManager.Instance.IsPaused || !this.Owner.AcceptingNonMotionInput)
            return; // nothing to do if we're not in control of our character

        this._adrenalineTimer -= BraveTime.DeltaTime;
        float heartRate = this._adrenalineTimer switch
        {
            > 30 => 2f,
            > 10 => 1f,
            > 3  => 0.5f,
            _    => 0.25f,
        };
        if (BraveTime.ScaledTimeSinceStartup - _LastHeartbeatTime > heartRate)
        {
            _LastHeartbeatTime = BraveTime.ScaledTimeSinceStartup;
            this.Owner.gameObject.Play("heartbeat");
        }

        if (this._adrenalineTimer > 0f)
            return; // clock's still ticking

        // ded D:
        this.Owner.healthHaver.ModifyDamage -= this.OnTakeDamage;
        this.Owner.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        this.Owner.healthHaver.ApplyDamage(999f, Vector2.zero, ItemName, CoreDamageTypes.None, DamageCategory.Unstoppable, ignoreInvulnerabilityFrames: true);
    }

    private void OnTakeDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
        if (!hh.PlayerWillDieFromHit(data))
            return; // if we're not going to die, we don't need to activate

        if (this._adrenalineActive)
        {
            if (!this.Owner.HasSynergy(Synergy.ADRENALINE_RUSH))
                this._adrenalineTimer -= _MAX_ADRENALINE_LOSS;
            if (this._adrenalineTimer <= 0)
                return;
            hh.gameObject.Play("adrenaline_tank_damage_sound");
        }

        data.ModifiedDamage = 0f;
        hh.TriggerInvulnerabilityPeriod();
        Lazy.DoDamagedFlash(hh);
        if (!this._adrenalineActive)
            ActivateAdrenaline();
    }

    private void ActivateAdrenaline()
    {
        this._adrenalineActive = true;
        this.CanBeDropped      = false;
        this.CanBeSold         = false;
        this.Owner.gameObject.AddComponent<UnderAdrenalineEffects>();
        this.Owner.healthHaver.ForceSetCurrentHealth(0.5f);
        this.Owner.healthHaver.OnHealthChanged += this.OnHealthChanged;
        Color faded = Color.Lerp(Color.gray, Color.clear, 0.25f);
        this.Owner.FlatColorOverridden = true;
        this.Owner.baseFlatColorOverride = faded;
        this.Owner.ChangeFlatColorOverride(faded);
        this.Owner.DoGenericItemActivation(this.sprite, "minecraft_totem_pop_sound");

        this._adrenalineTimer  = this.Owner.HasSynergy(Synergy.ADRENALINE_RUSH) ? _MAX_RUSH_TIME : _MAX_ADRENALINE_TIME;
    }

    private void OnHealthChanged(float resultValue, float maxValue)
    {
        if (resultValue > 0.5f)
            DeactivateAdrenaline();
    }

    private void DeactivateAdrenaline()
    {
        this._adrenalineActive = false;
        this.CanBeDropped      = true;
        this.CanBeSold         = true;
        this.Owner.healthHaver.OnHealthChanged -= this.OnHealthChanged;
        this.Owner.healthHaver.ForceSetCurrentHealth(Mathf.Max(this.Owner.healthHaver.currentHealth - 0.5f, 0.5f));
        this.Owner.baseFlatColorOverride = Color.clear;
        this.Owner.ChangeFlatColorOverride(Color.clear);
        this.Owner.gameObject.Play("adrenaline_deactivate_sound");

        UnityEngine.Object.Destroy(this.Owner.gameObject.GetComponent<UnderAdrenalineEffects>());
    }
}

public class UnderAdrenalineEffects : MonoBehaviour {}
