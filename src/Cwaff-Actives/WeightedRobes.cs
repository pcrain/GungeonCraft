namespace CwaffingTheGungy;

using static WeightedRobes.EnemyType;

public class WeightedRobes : CwaffActive, ILabelItem
{
    public static string ItemName         = "Weighted Robes";
    public static string ShortDescription = "Hide Your Power Level";
    public static string LongDescription  = "Toggles weighted training when used. Each room cleared while weighted training is active increases the player's training level. Fire rate, reload speed, and movement speed are reduced proportional to training level while training is active, and increased proportional to training level while training is inactive. Bosses and minibosses increase training level more than normal enemies.";
    public static string Lore             = "These garments once belonged to King Triggolo, a nemesis-turned-ally of the great Gunsoku. Triggolo would often wear these robes during lesser gunfights to make them more physically difficult, allowing him to build speed and strength in preparation for more serious gunfights. He also had a habit of forgetting to remove them before said serious gunfights, leading to several injuries that could have been easily avoided.";

    private const int   _MAX_TRAINING = 50;
    private const float _FIRERATE     = 0.5f;
    private const float _RELOAD       = -0.5f;
    private const float _MOVEMENT     = 2.0f;

    private static int _InactiveId;
    private static int _ActiveId;

    internal static GameObject _TrainingVFX;
    internal static GameObject _TrainedVFX;

    private PlayerController _owner        = null;
    private bool _active                   = false;
    private StatModifier[] weightedStats   = null;
    private int[] _trainingDone            = [0, 0];
    private StatModifier[][] trainingStats = [null, null];
    private EnemyType _strongestKilled     = MOOK;

    internal enum EnemyType { MOOK, MINIBOSS, BOSS }

    public static void Init()
    {
        PlayerItem item   = Lazy.SetupActive<WeightedRobes>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.C;
        item.consumable   = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 0.5f);

        _InactiveId  = item.sprite.spriteId;
        _ActiveId    = item.sprite.collection.GetSpriteIdByName("weighted_robes_active_icon");
        _TrainingVFX = VFX.Create("status_arrow_down", anchor: Anchor.LowerCenter, emissivePower: 100f);;
        _TrainedVFX  = VFX.Create("status_arrow_up", anchor: Anchor.LowerCenter, emissivePower: 100f);;
    }

    public string GetLabel()
    {
        if (!this._owner)
            return string.Empty;
        Transform t = GameUIRoot.Instance.itemControllers[this._owner.PlayerIDX].ItemCountLabel.gameObject.transform;
        t.localPosition = t.localPosition.WithY(0.27f);
        return $"[color #dd6666]{(int)((100f * this._trainingDone[this._owner.PlayerIDX]) / _MAX_TRAINING)}%[/color]";
    }

    [HarmonyPatch(typeof(GameUIItemController), nameof(GameUIItemController.UpdateItem))]
    private class RestoreSensibleLabelPatch
    {
        static void Postfix(GameUIItemController __instance, PlayerItem current, List<PlayerItem> items)
        {
            if (current is ILabelItem)
                return;
            Transform t = __instance.ItemCountLabel.gameObject.transform;
            t.localPosition = t.localPosition.WithY(0.0148148155f); //HACK: hardcoded default, integrate better into Alexandria later
        }
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        this._owner = player;
        player.OnEnteredCombat += this.OnEnteredCombat;
        player.OnRoomClearEvent += this.DidSomeTraining;
        player.OnKilledEnemyContext += this.MaybeKilledBoss;
        this.weightedStats ??= [
            StatType.RateOfFire.Add(0f),
            StatType.ReloadSpeed.Add(0f),
            StatType.MovementSpeed.Add(0f),
        ];
        int pid = player.PlayerIDX;
        if (this.trainingStats[pid] != null)
            return;
        this.trainingStats[pid] = [
            StatType.RateOfFire.Add(0f),
            StatType.ReloadSpeed.Add(0f),
            StatType.MovementSpeed.Add(0f),
        ];
        player.ownerlessStatModifiers.AddRange(this.trainingStats[pid]);
        player.stats.RecalculateStats(player);
    }

    private void OnEnteredCombat()
    {
        if (this._active && this._owner)
            this._owner.StartCoroutine(DoTraining_CR(this._owner, true));
    }

    private void MaybeKilledBoss(PlayerController player, HealthHaver enemy)
    {
        if (enemy.IsBoss)
            this._strongestKilled = BOSS;
        else if (enemy.IsSubboss && this._strongestKilled != BOSS)
            this._strongestKilled = MINIBOSS;
    }

    public override void OnPreDrop(PlayerController player)
    {
        this._owner = null;
        player.OnRoomClearEvent -= this.DidSomeTraining;
        player.OnKilledEnemyContext -= this.MaybeKilledBoss;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        if (this._owner)
        {
            this._owner.OnRoomClearEvent -= this.DidSomeTraining;
            this._owner.OnKilledEnemyContext -= this.MaybeKilledBoss;
        }
        base.OnDestroy();
    }

    private void Train(PlayerController player, int trainingDone)
    {
        if (!this._active)
            return;
        int pid = player.PlayerIDX;
        this._trainingDone[pid] = Mathf.Min(this._trainingDone[pid] + trainingDone, _MAX_TRAINING);
        player.StartCoroutine(DoTraining_CR(player, false));
        UpdateTrainingStats(player);
    }

    private void DidSomeTraining(PlayerController player)
    {
        Train(player, this._strongestKilled switch {BOSS => 10, MINIBOSS => 5, _ => 1});
        this._strongestKilled = MOOK;
    }

    private void UpdateTrainingStats(PlayerController player)
    {
        StatModifier[] trainingStats = this.trainingStats[player.PlayerIDX];
        float training = (float)this._trainingDone[player.PlayerIDX] / _MAX_TRAINING;
        trainingStats[0].amount = training * _FIRERATE;
        trainingStats[1].amount = training * _RELOAD;
        trainingStats[2].amount = training * _MOVEMENT;

        // training gets harder as stats go up
        this.weightedStats[0].amount = -2f * trainingStats[0].amount;
        this.weightedStats[1].amount = -2f * trainingStats[1].amount;
        this.weightedStats[2].amount = -2f * trainingStats[2].amount;
        player.stats.RecalculateStats(player);
    }

    public override bool CanBeUsed(PlayerController player)
    {
        return !player.IsInCombat && base.CanBeUsed(player);
    }

    public override void DoEffect(PlayerController player)
    {
        if (player != this._owner)
            return;

        this._active = !this._active;
        this.CanBeDropped = !this._active;
        this.passiveStatModifiers = this._active ? this.weightedStats : null;
        base.sprite.SetSprite(this._active ? _ActiveId : _InactiveId);

        player.StartCoroutine(DoTraining_CR(player, this._active));

        player.stats.RecalculateStats(player);
    }

    private static readonly Color _TrainingGreen = new Color(30f / 255f, 255f / 255f, 30f / 255f);
    private static readonly Color _TrainingRed = new Color(255f / 255f, 30f / 255f, 30f / 255f);
    public static IEnumerator DoTraining_CR(PlayerController player, bool active)
    {
        if (active)
            player.FlashVFXAbovePlayer(_TrainingVFX, sound: "statdown", glowAndFade: true, time: 0.0f);
        else
            player.FlashVFXAbovePlayer(_TrainedVFX, sound: "statup", glowAndFade: true, time: 0.0f);

        SpriteOutlineManager.RemoveOutlineFromSprite(player.sprite);
        Material[] mats = player.SetOverrideShader(CwaffShaders.UnlitDigitizeShader);
        foreach (Material mat in mats)
        {
            mat.SetTexture(CwaffVFX._BinaryTexId, active ? CwaffShaders.PowerdownTexture : CwaffShaders.PowerupTexture);
            mat.SetColor(CwaffVFX._ColorId, active ? _TrainingRed : _TrainingGreen);
            mat.SetFloat(CwaffVFX._BinarizeProgressId, 0.5f);
            mat.SetFloat(CwaffVFX._ColorizeProgressId, 1.0f);
            mat.SetFloat(CwaffVFX._FadeProgressId, 0.0f);
            mat.SetFloat(CwaffVFX._ScrollSpeedId, active ? 3.0f : -3.0f);
        }
        yield return new WaitForSeconds(1.0f);
        player.ClearOverrideShader();
        SpriteOutlineManager.AddOutlineToSprite(player.sprite, Color.black);
    }
}
