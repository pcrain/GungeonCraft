namespace CwaffingTheGungy;

public class Exceptional : CwaffGun
{
    public static string ItemName         = "Exceptional";
    public static string ShortDescription = "Exceptional";
    public static string LongDescription  = "Exceptional";
    public static string Lore             = "Exceptional";

    public static int _PickupId;
    public static int _ExceptionalPower;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Exceptional>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true);
            gun.SetAttributes(quality: ItemQuality.SPECIAL, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.2f, ammo: 80, shootFps: 30, reloadFps: 40,
                muzzleFrom: Items.Mailbox, fireAudio: "blowgun_fire_sound", reloadAudio: "blowgun_reload_sound", banFromBlessedRuns: true);

        gun.gameObject.AddComponent<ExceptionalAmmoDisplay>();

        _PickupId = gun.PickupObjectId;

        Application.logMessageReceived += Exceptionalizationizer;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        var d = player.GetComponent<Exceptional>().debris; // throws a null reference exception
    }

    public static void Exceptionalizationizer(string text, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
            ++_ExceptionalPower;
    }

    public class ExceptionalAmmoDisplay : CustomAmmoDisplay
    {
        private PlayerController _owner;
        private void Start()
        {
            this._owner = base.GetComponent<Gun>().CurrentOwner as PlayerController;
        }

        public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
        {
            if (!this._owner)
                return false;

            uic.SetAmmoCountLabelColor(Color.red);
            uic.GunAmmoCountLabel.Text = $"{_ExceptionalPower}";
            return true;
        }
    }
}

[HarmonyPatch(typeof(UINotificationController), nameof(UINotificationController.HandleNotification), MethodType.Enumerator)]
static class CorruptNotificationPatch
{
    [HarmonyILManipulator]
    private static void PatchNameIL(ILContext il, MethodBase original)
    {
        ILCursor cursor = new ILCursor(il);
        Type ot = original.DeclaringType;
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall("BraveTime", "get_DeltaTime")))
            return;
        cursor.Emit(OpCodes.Ldarg_0); // load enumerator type
        cursor.Emit(OpCodes.Ldfld, ot.GetEnumeratorField("notifyParams"));
        cursor.Emit(OpCodes.Ldarg_0); // load enumerator type
        cursor.Emit(OpCodes.Ldfld, ot.GetEnumeratorField("$this")); // load actual "$this" field
        cursor.Emit(OpCodes.Call, typeof(CorruptNotificationPatch).GetMethod(nameof(CorruptNotificationPatch.Corrupt), BindingFlags.Static | BindingFlags.NonPublic));
        return;
    }

    private static StringBuilder _SB = new StringBuilder("", 1000);
    private static void Corrupt(NotificationParams notifyParams, UINotificationController uinc)
    {
        if (notifyParams.pickupId != Exceptional._PickupId)
            return;
        _SB.Length = 0;
        _SB.Append("[color #dd6666]");
        _SB.Append(Lazy.GenRandomCorruptedString());
        _SB.Append("[/color]");
        uinc.NameLabel.ProcessMarkup = true;
        uinc.NameLabel.Text = _SB.ToString();

        _SB.Length = 0;
        _SB.Append("[color #dd6666]");
        _SB.Append(Lazy.GenRandomCorruptedString());
        _SB.Append(Lazy.GenRandomCorruptedString());
        _SB.Append("[/color]");
        uinc.DescriptionLabel.ProcessMarkup = true;
        uinc.DescriptionLabel.Text = _SB.ToString();
    }
}

[HarmonyPatch(typeof(AmmonomiconPageRenderer), nameof(AmmonomiconPageRenderer.SetRightDataPageTexts))]
static class CorruptAmmonomiconPatch
{
    static void Postfix(AmmonomiconPageRenderer __instance, tk2dBaseSprite sourceSprite, EncounterDatabaseEntry linkedTrackable)
    {
        if (linkedTrackable.pickupObjectId != Exceptional._PickupId)
            return;
        AmmonomiconPageRenderer ammonomiconPageRenderer = ((!(AmmonomiconController.Instance.ImpendingRightPageRenderer != null)) ? AmmonomiconController.Instance.CurrentRightPageRenderer : AmmonomiconController.Instance.ImpendingRightPageRenderer);
        dfScrollPanel component = ammonomiconPageRenderer.guiManager.transform.Find("Scroll Panel").GetComponent<dfScrollPanel>();
        Transform transform = component.transform.Find("Header");
        if (!transform)
            return;

        dfLabel itemLabel = transform.Find("Label").GetComponent<dfLabel>();
        itemLabel.ProcessMarkup = true;
        itemLabel.Text = Lazy.GenRandomCorruptedString();
        itemLabel.PerformLayout();

        dfLabel firstTapeLabel = component.transform.Find("Tape Line One").Find("Label").GetComponent<dfLabel>();
        firstTapeLabel.ProcessMarkup = true;
        firstTapeLabel.Text = Lazy.GenRandomCorruptedString();
        firstTapeLabel.PerformLayout();
        component.transform.Find("Tape Line One").GetComponentInChildren<dfSlicedSprite>().Width = firstTapeLabel.GetAutosizeWidth() / 4f + 12f;

        dfLabel secondTapeLabel = component.transform.Find("Tape Line Two").Find("Label").GetComponent<dfLabel>();
        secondTapeLabel.ProcessMarkup = true;
        secondTapeLabel.Text = Lazy.GenRandomCorruptedString();
        secondTapeLabel.PerformLayout();
        component.transform.Find("Tape Line Two").GetComponentInChildren<dfSlicedSprite>().Width = secondTapeLabel.GetAutosizeWidth() / 4f + 12f;

        dfLabel descLabel = component.transform.Find("Scroll Panel").Find("Panel").Find("Label").GetComponent<dfLabel>();
        __instance.CheckLanguageFonts(descLabel);
        descLabel.ProcessMarkup = true;
        descLabel.Text = $"{Lazy.GenRandomCorruptedString()}\n{Lazy.GenRandomCorruptedString()}\n{Lazy.GenRandomCorruptedString()}\n{Lazy.GenRandomCorruptedString()}";
        descLabel.transform.parent.GetComponent<dfPanel>().Height = descLabel.Height;
        descLabel.PerformLayout();
        descLabel.Update();
    }
}

[HarmonyPatch(typeof(EncounterTrackable), nameof(EncounterTrackable.GetModifiedDisplayName))]
static class CorruptDisplayNamePatch
{
    static void Postfix(EncounterTrackable __instance, ref string __result)
    {
        if (__instance.m_pickup is not PickupObject pickup)
          return;
        if (pickup.PickupObjectId != Exceptional._PickupId)
          return;
        __result = Lazy.GenRandomCorruptedString();
    }
}
