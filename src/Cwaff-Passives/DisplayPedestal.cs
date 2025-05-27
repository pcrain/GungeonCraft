namespace CwaffingTheGungy;

public class DisplayPedestal : CwaffPassive
{
    public static string ItemName         = "Display Pedestal";
    public static string ShortDescription = "Professionally Graded";
    public static string LongDescription  = "Picking up any new gun preserves it in Mint Condition until fired or dropped. Each Mint Condition gun increases coolness by one, and can be sold to the Sell Creep at its full purchase price.";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item          = Lazy.SetupPassive<DisplayPedestal>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality              = ItemQuality.C;
        item.passiveStatModifiers = [ StatType.Coolness.Add(0f) ];
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        PassiveItem.IncrementFlag(player, typeof(DisplayPedestal));
        this.passiveStatModifiers[0].amount = 0;
        player.stats.RecalculateStats(player);
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
            return;

        PassiveItem.DecrementFlag(player, typeof(DisplayPedestal));
        if (PassiveItem.IsFlagSetForCharacter(player, typeof(DisplayPedestal)))
            return;

        foreach (Gun gun in player.inventory.AllGuns)
            if (gun && gun.gameObject.GetComponent<PristineGun>() is PristineGun pg)
                pg.NoLongerPristine();
    }

    public void UpdateStats()
    {
        if (!this.m_owner)
            return;
        this.passiveStatModifiers[0].amount = this.m_owner.GetFlagCount(typeof(PristineGun));
        this.m_owner.stats.RecalculateStats(this.m_owner);
    }

    [HarmonyPatch]
    private class DisplayCasePatches
    {
        private static bool _NextGunIsPristine = false;

        [HarmonyPatch(typeof(Gun), nameof(Gun.Pickup))]
        [HarmonyPrefix]
        private static void CheckIfPristine(Gun __instance, PlayerController player)
        {
            if (__instance.gameObject.GetComponent<PristineGun>() is PristineGun pg)
                pg.NoLongerPristine();
            if (!__instance.HasEverBeenAcquiredByPlayer && PassiveItem.IsFlagSetForCharacter(player, typeof(DisplayPedestal)))
                _NextGunIsPristine = true;
        }

        [HarmonyPatch(typeof(GunInventory), nameof(GunInventory.AddGunToInventory))]
        [HarmonyPostfix]
        private static void AddGunToInventory(GunInventory __instance, Gun gun, bool makeActive, ref Gun __result)
        {
            if (!_NextGunIsPristine)
                return;
            _NextGunIsPristine = false;
            __result.gameObject.AddComponent<PristineGun>();
        }

        [HarmonyPatch(typeof(SellCellController), nameof(SellCellController.HandleSoldItem), MethodType.Enumerator)]
        [HarmonyILManipulator]
        private static void SellCellControllerHandleSoldItemPatchIL(ILContext il, MethodBase original)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<SellCellController>("SellValueModifier")))
                return;

            cursor.Emit(OpCodes.Ldarg_0);  // load enumerator type
            cursor.Emit(OpCodes.Ldfld, original.DeclaringType.GetEnumeratorField("targetItem"));
            cursor.CallPrivate(typeof(DisplayCasePatches), nameof(MintConditionMult));
            return;
        }

        private static float MintConditionMult(float origMult, PickupObject targetItem)
        {
            return targetItem.gameObject.GetComponent<PristineGun>() ? 1f : origMult;
        }
    }
}

public class PristineGun : MonoBehaviour
{
    private Gun _gun;
    private PlayerController _player;
    private bool _inInventory = false;

    private void Start()
    {
        this._gun = base.gameObject.GetComponent<Gun>();
        this._player = this._gun.GunPlayerOwner();
        if (!this._player) // probably paranoid but oh well
        {
            UnityEngine.Object.Destroy(this);
            return;
        }

        this._inInventory = true;
        this._gun.OnDropped += Drop;
        this._gun.OnPostFired += PostFired;
        PassiveItem.IncrementFlag(this._player, typeof(PristineGun));
        if (this._player.GetPassive<DisplayPedestal>() is DisplayPedestal dp)
            dp.UpdateStats();
    }

    private void PostFired(PlayerController player, Gun gun)
    {
        NoLongerPristine();
    }

    private void Drop()
    {
        this._gun.OnDropped -= Drop;
        this._gun.OnPostFired -= PostFired;
        this._inInventory = false;
        if (!this._player)
            return;

        PassiveItem.DecrementFlag(this._player, typeof(PristineGun));
        if (this._player.GetPassive<DisplayPedestal>() is DisplayPedestal dp)
            dp.UpdateStats();
    }

    internal void NoLongerPristine()
    {
        this._gun.OnDropped -= Drop;
        this._gun.OnPostFired -= PostFired;
        if (this._inInventory)
        {
            PassiveItem.DecrementFlag(this._player, typeof(PristineGun));
            if (this._player.GetPassive<DisplayPedestal>() is DisplayPedestal dp)
                dp.UpdateStats();
        }
        UnityEngine.Object.Destroy(this);
    }
}
