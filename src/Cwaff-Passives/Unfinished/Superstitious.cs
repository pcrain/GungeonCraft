namespace CwaffingTheGungy;

public class Superstitious : CwaffPassive
{
    public static string ItemName         = "Superstitious";
    // public static string SpritePath       = "88888888_icon";
    public static string ShortDescription = "Writings on the HUD";
    public static string LongDescription  = "(6s and 7s)";
    public static string Lore             = "TBD";

    private static HUDController hud => HUDController.Instance;
    private static List<HUDElement> els = new List<HUDElement>();

    private StatModifier superstitionBuff = null;
    private int sixes, sevens;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Superstitious>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;

        els.Add(new HUDElement("Coolness","",$"{C.MOD_INT_NAME}/Resources/HUD/Coolness.png"));
        // els.Add(new HUDElement("Curse","",$"{C.MOD_INT_NAME}/Resources/HUD/Curse.png"));
        // els.Add(new HUDElement("Basic2","basic text",null));
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        foreach (HUDElement el in els)
        {
            el.updater = HUDUpdater;
            // el.updateFreq = 0.5f;
            el.Activate();
        }
        player.OnReloadedGun += this.HandleGunReloaded;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        foreach (HUDElement el in els)
        {
            el.updater = null;
            el.Deactivate();
        }
        player.OnReloadedGun -= this.HandleGunReloaded;
        return base.Drop(player);
    }

    private void HandleGunReloaded(PlayerController player, Gun playerGun)
    {
        int statboost = this.sevens - this.sixes;

        player.ownerlessStatModifiers.TryRemove(superstitionBuff);

        superstitionBuff = StatType.Damage.Mult((statboost < 0)
            ? (1f / Mathf.Pow(1.5f, -statboost))
            : Mathf.Pow(1.5f, statboost));

        player.ownerlessStatModifiers.Add(superstitionBuff);
        player.stats.RecalculateStats(player);
    }

    public override void Update()
    {
        base.Update();
        string shells = this.Owner.carriedConsumables.Currency.ToString();
        string ammo = this.Owner.CurrentGun.InfiniteAmmo
            ? ""
            : this.Owner.CurrentGun.CurrentAmmo.ToString()+this.Owner.CurrentGun.AdjustedMaxAmmo.ToString();
        string keys = this.Owner.carriedConsumables.KeyBullets.ToString();
        string blanks = this.Owner.Blanks.ToString();
        string numbers = shells+ammo+keys+blanks;
        this.sixes = numbers.Count(x => x == '6');
        this.sevens = numbers.Count(x => x == '7');
    }

    private bool HUDUpdater(HUDElement self)
    {
        if (this.Owner)
            self.text.Text = sixes.ToString()+","+sevens.ToString();
        return true;
    }
}
