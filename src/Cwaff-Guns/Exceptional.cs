namespace CwaffingTheGungy;

public class Exceptional : CwaffGun
{
    public static string ItemName         = "Exceptional";
    public static string ShortDescription = "Exceptional";
    public static string LongDescription  = "Exceptional";
    public static string Lore             = "Exceptional";

    public static int _PickupId;
    public static int _ExceptionalPower;
    public static bool _Spawned = false;

    private const int _ERRORS_BEFORE_SPAWNING = 1337;
    private const int _BURST_SIZE = 7;

    private int _cachedPower = -1;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Exceptional>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true)
          .SetAttributes(quality: ItemQuality.SPECIAL, gunClass: CwaffGunClass.UTILITY, reloadTime: 0f, ammo: 80, shootFps: 30, reloadFps: 40,
            muzzleFrom: Items.Mailbox, fireAudio: "corruption_sound", banFromBlessedRuns: true, infiniteAmmo: true, modulesAreTiers: true);

        Projectile proj = gun.InitProjectile(GunData.New(sprite: "exceptional_projectile", clipSize: 32, cooldown: 0.33f, shootStyle: ShootStyle.Burst,
            angleVariance: 10f, damage: 4.0f, speed: 75f, range: 1000f, force: 12f, burstCooldown: 0.04f, hideAmmo: true)).Attach<ExceptionalProjectile>();

        //WARN: vanilla modulesAreTiers does NOT play nicely with burst weapons and continues firing sometimes long after you release the fire button.
        //      i think this has to do with the flag3 variable in HandleInitialGunShoot(), but i have no desire to muck with it
        //      as a workaround, i'm setting numberOfShotsInClip to -1, which prevents the glitch for some reason
        //TODO: fixed in Hotfixes, restore clip size later
        ProjectileModule mod = gun.DefaultModule;
        gun.Volley.projectiles = new(10);
        for (int i = 1; i <= 10; ++i)
        {
            ProjectileModule newMod = ProjectileModule.CreateClone(mod, inheritGuid: false);
            newMod.numberOfShotsInClip = -1;
            newMod.burstShotCount = i;
            newMod.projectiles = Enumerable.Repeat<Projectile>(proj, i).ToList();
            gun.Volley.projectiles.Add(newMod);
        } //REFACTOR: burst builder

        gun.gameObject.AddComponent<ExceptionalAmmoDisplay>();

        _PickupId = gun.PickupObjectId;

        Application.logMessageReceived += Exceptionalizationizer;
    }

    /// <summary>Manually initialize some Harmony patches at runtime if the gun ever gets instantiated, since they're a bit heavy-handed</summary>
    public static void InitRuntimePatches()
    {
        Harmony harmony = Initialisation._Harmony;
        BindingFlags anyFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        harmony.Patch(typeof(AmmonomiconPageRenderer).GetMethod(nameof(AmmonomiconPageRenderer.SetRightDataPageTexts), bindingAttr: anyFlags),
          postfix: new HarmonyMethod(typeof(CorruptAmmonomiconPatch).GetMethod("Postfix", bindingAttr: anyFlags)));

        harmony.Patch(typeof(EncounterTrackable).GetMethod(nameof(EncounterTrackable.GetModifiedDisplayName), bindingAttr: anyFlags),
          postfix: new HarmonyMethod(typeof(CorruptDisplayNamePatch).GetMethod("Postfix", bindingAttr: anyFlags)));

        harmony.Patch(
          original: AccessTools.EnumeratorMoveNext(
            typeof(UINotificationController).GetMethod(nameof(UINotificationController.HandleNotification), bindingAttr: anyFlags)),
          ilmanipulator: new HarmonyMethod(typeof(CorruptNotificationPatch).GetMethod("CorruptNotificationIL", bindingAttr: anyFlags)));
    }

    private static bool _DidRuntimePatches = false;
    private void Start()
    {
        if (!_DidRuntimePatches)
        {
            InitRuntimePatches();
            _DidRuntimePatches = true;
        }
        AdjustGunShader(true);
    }

    public override void Update()
    {
        base.Update();
        if (this._cachedPower == _ExceptionalPower)
            return;
        this._cachedPower = _ExceptionalPower;
        int newTier = 0;
        if (this._cachedPower >= 2) //BUG: due to the gun's spawn condition, newTier will only ever be 9
            newTier = Mathf.Min(9, Mathf.FloorToInt(Mathf.Log(this._cachedPower, 2)));
        if (this.gun.CurrentStrengthTier != newTier)
            this.gun.CurrentStrengthTier = newTier; //NOTE: expensive assignment since it recalculates stats, so only set if actually changed
    }

    public void AdjustGunShader(bool isOn)
    {
        this.gun.sprite.usesOverrideMaterial = isOn;
        this.gun.sprite.renderer.material.shader = isOn ? CwaffShaders.CorruptShader : ShaderCache.Acquire("Brave/PlayerShader");
    }

    public static void Exceptionalizationizer(string text, string stackTrace, LogType type)
    {
        if (type != LogType.Exception)
            return;
        if (++_ExceptionalPower < _ERRORS_BEFORE_SPAWNING)
            return;
        if (_Spawned)
            return;
        _Spawned = true;
        Lazy.SpawnChestWithSpecificItem(
          pickup: Lazy.Pickup<Exceptional>(),
          position: GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool success),
          overrideChestQuality: ItemQuality.S,
          overrideJunk: true);
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

public class ExceptionalProjectile : MonoBehaviour
{
    private const float _DECEL_START = 0.05f;
    private const float _HALT_START  = 0.25f;
    private const float _RELAUNCH_START  = 0.5f;

    private Projectile _projectile;
    private PlayerController _owner;
    private float _lifetime = 0f;
    private State _state = State.START;

    private enum State
    {
        START,
        DECEL,
        HALT,
        RELAUNCH,
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore all motion module overrides, helix bullets doeesn't play well with speed changing projectiles

        this._projectile.sprite.usesOverrideMaterial = true;
        this._projectile.sprite.renderer.material.shader = CwaffShaders.CorruptShader;
    }

    private static void ApplyCorruptionShader(tk2dBaseSprite sprite)
    {
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = CwaffShaders.CorruptShader;
    }

    private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool killed)
    {
        enemy.gameObject.PlayUnique("corruption_sound");
        if (enemy.aiActor)
            enemy.aiActor.ApplyShader(ApplyCorruptionShader, true, true);
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        switch (this._state)
        {
            case State.START:
                if (this._lifetime >= _DECEL_START)
                    this._state = State.DECEL;
                break;
            case State.DECEL:
                this._projectile.baseData.speed = Lazy.SmoothestLerp(this._projectile.baseData.speed, 0f, 10f);
                this._projectile.UpdateSpeed();
                if (this._lifetime >= _HALT_START)
                {
                    this._projectile.baseData.speed = 0.01f;
                    this._projectile.UpdateSpeed();
                    this._state = State.HALT;
                }
                break;
            case State.HALT:
                if (this._lifetime >= _RELAUNCH_START)
                {
                    if (Lazy.NearestEnemyPos(this._projectile.SafeCenter) is Vector2 v)
                        this._projectile.SendInDirection(v - this._projectile.SafeCenter, true);
                    this._projectile.baseData.speed = 100f * (this._owner ? this._owner.ProjSpeedMult() : 1f);
                    this._projectile.UpdateSpeed();
                    base.gameObject.PlayUnique("corruption_sound");
                    this._state = State.RELAUNCH;
                }
                break;
            case State.RELAUNCH:
                break;
        }
    }
}

static class CorruptNotificationPatch
{
    private static void CorruptNotificationIL(ILContext il, MethodBase original)
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
