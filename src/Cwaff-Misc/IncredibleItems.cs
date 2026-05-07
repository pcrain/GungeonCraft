namespace CwaffingTheGungy;

public static class IncredibleItems
{
    internal static Chest _PaperChestPrefab = null;

    internal static readonly List<int> _IncredibleItemIDs = new();

    public static void Init()
    {
        Chest baseChest = GameManager.Instance.RewardManager.GetTargetChestPrefab(ItemQuality.A);
        _PaperChestPrefab = baseChest.gameObject.ClonePrefab().GetComponent<Chest>();

            _PaperChestPrefab.ShadowSprite = null;
            if (_PaperChestPrefab.gameObject.transform.Find("Shadow") is Transform shadow)
              UnityEngine.Object.Destroy(shadow.gameObject);
            if (_PaperChestPrefab.gameObject.transform.Find("SpawnTransform") is Transform spawnTransform)
              UnityEngine.Object.Destroy(spawnTransform.gameObject);
            _PaperChestPrefab.spawnTransform = null;
            //WARNING: if spawnAnimName is set to null, the first one will work okay, but subsequent runs will cause chests to appear as their original variants
            //         however, if it's NOT set to null, then the chest spawns in with a disabled SpeculativeRigidBody due to SpawnBehavior_CR()
            _PaperChestPrefab.spawnAnimName = "chest_paper_idle";
            _PaperChestPrefab.openAnimName  = _PaperChestPrefab.sprite.SetUpAnimation("chest_paper_open", 30);
            _PaperChestPrefab.breakAnimName = _PaperChestPrefab.openAnimName;
            tk2dSpriteAnimator animator = _PaperChestPrefab.spriteAnimator;
              animator.SetAudio("chest_paper_open", "paper_crinkle_sound", 4, 15, 18, 24);
              animator.SetAudio("chest_paper_open", "paper_fall_sound", 30);
              animator.defaultClipId = animator.GetClipIdByName(_PaperChestPrefab.sprite.SetUpAnimation("chest_paper_idle", 1));
              _PaperChestPrefab.sprite.SetSprite(animator.library.clips[animator.defaultClipId].frames[0].spriteId);
            _PaperChestPrefab.IsLocked = false; // can't get lock renderer to attach properly after adjusting appearance animation
            _PaperChestPrefab.GetComponent<MajorBreakable>().HitPoints = 1;
            _PaperChestPrefab.gameObject.AutoRigidBody(height: 0.25f);
            _PaperChestPrefab.gameObject.AddComponent<PaperChestInitializer>();

        GunCarryingCase.Init();
        WWIRations.Init();
        Headstone.Init();
    }

    private static T SetupIncredibleItem<T>(this T item) where T : PickupObject
    {
      item.quality                   = CwaffItemQuality.F;
      item.ShouldBeExcludedFromShops = true;
      item.CanBeSold                 = false;
      item.IgnoredByRat              = true;
      item.UsesCustomCost            = true;
      item.CustomCost                = 0;
      _IncredibleItemIDs.Add(item.PickupObjectId);
      return item;
    }

    public static PassiveItem SetupPassive<T>(string ItemName, string ShortDescription, string LongDescription, string Lore) where T : PassiveItem
      => Lazy.SetupPassive<T>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true).SetupIncredibleItem();

    public static PlayerItem SetupActive<T>(string ItemName, string ShortDescription, string LongDescription, string Lore) where T : PlayerItem
      => Lazy.SetupActive<T>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true).SetupIncredibleItem();

    public static Chest SpawnPaperChest()
    {
        return Chest.Spawn(IncredibleItems._PaperChestPrefab,
          GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool _));
    }

    private class PaperChestInitializer : MonoBehaviour
    {
      private void Start()
      {
        Chest chest = base.gameObject.GetComponent<Chest>();
        chest.specRigidbody.enabled = true;
        chest.specRigidbody.Reinitialize();
        chest.m_isMimic = false;
        chest.IsLocked = false;
        chest.m_isGlitchChest = false;
        chest.contents = null;
        chest.forceContentIds = [_IncredibleItemIDs.ChooseRandom()];
        RoomHandler room = chest.transform.position.GetAbsoluteRoom();
        if (room != null)
          chest.RegisterChestOnMinimap(room);
      }
    }

    [HarmonyPatch]
    private static class IncredibleChestPlacerPatches
    {
      #if DEBUG
      private const float _INCREDIBLE_CHEST_CHANCE = 1.00f;
      #else
      private const float _INCREDIBLE_CHEST_CHANCE = 0.01f;
      #endif

      [HarmonyPatch(typeof(FloorChestPlacer), nameof(FloorChestPlacer.ConfigureOnPlacement))]
      [HarmonyPrefix]
      private static void FloorChestPlacerConfigureOnPlacementPatch(FloorChestPlacer __instance, RoomHandler room)
      {
          float magnificence = GameManager.Instance.RewardManager.CurrentRewardData.DetermineCurrentMagnificence(true);
          if (magnificence < 4 || UnityEngine.Random.value > _INCREDIBLE_CHEST_CHANCE)
            return;
          __instance.UseOverrideChest = true;
          __instance.OverrideChestPrefab = _PaperChestPrefab;
          __instance.CenterChestInRegion = true;
          __instance.xPixelOffset = -8;
          __instance.yPixelOffset = -8;
      }
    }
}

public class GunCarryingCase : CwaffPassive
{
    public static string ItemName         = "Gun Carrying Case";
    public static string ShortDescription = "Portable and Convenient";
    public static string LongDescription  = "Increases gun carrying capacity by 1.";
    public static string Lore             = "";

    public static void Init()
    {
        PassiveItem item  = IncredibleItems.SetupPassive<GunCarryingCase>(ItemName, ShortDescription, LongDescription, Lore);
        item.passiveStatModifiers = [StatType.AdditionalGunCapacity.Add(1f)];
    }
}

public class WWIRations : CwaffActive
{
    public static string ItemName         = "WWI Rations";
    public static string ShortDescription = "20 Year Shelf Life";
    public static string LongDescription  = "Restores 3 hearts if consumed before January 1st, 1939.";
    public static string Lore             = "";

    public static void Init()
    {
        PlayerItem item  = IncredibleItems.SetupActive<WWIRations>(ItemName, ShortDescription, LongDescription, Lore);
    }

    public override void DoEffect(PlayerController user)
    {
      user.healthHaver.ApplyHealing((DateTime.Now.Year < 1939) ? 3f : 0f);
      AkSoundEngine.PostEvent("Play_OBJ_med_kit_01", base.gameObject);
      user.PlayEffectOnActor((Items.Ration.AsActive() as RationItem).healVFX, Vector3.zero);
    }
}

public class Headstone : CwaffPassive
{
    public static string ItemName         = "Headstone";
    public static string ShortDescription = "Proper Burial";
    public static string LongDescription  = "Reduces curse to 0 while dead.";
    public static string Lore             = "";

    private HealthHaver lastOwner = null;
    private StatModifier _curseMod = null;

    public static void Init()
    {
        PassiveItem item  = IncredibleItems.SetupPassive<Headstone>(ItemName, ShortDescription, LongDescription, Lore);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        if (player.healthHaver is not HealthHaver hh)
          return;
        this.lastOwner = hh;
        hh.OnDeath -= this.OnDeath;
        hh.OnDeath += this.OnDeath;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (player && player.healthHaver is HealthHaver hh)
          hh.OnDeath -= this.OnDeath;
    }

    private void OnDeath(Vector2 vector)
    {
        if (!this.lastOwner || !this.lastOwner.isPlayerCharacter)
          return;
        this._curseMod = StatType.Curse.Add(-this.lastOwner.m_player.stats.GetStatValue(StatType.Curse));
        this.lastOwner.m_player.ownerlessStatModifiers.Add(this._curseMod);
        this.lastOwner.m_player.stats.RecalculateStats(this.lastOwner.m_player);
    }

    public override void Update()
    {
        if (this._curseMod == null || !this.lastOwner || this.lastOwner.IsDead)
          return;
        this.lastOwner.m_player.ownerlessStatModifiers.Remove(this._curseMod);
        this.lastOwner.m_player.stats.RecalculateStats(this.lastOwner.m_player);
        this._curseMod = null;
        this.lastOwner = null;
    }
}
