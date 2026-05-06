namespace CwaffingTheGungy;

public static class IncredibleItems
{
    internal static Chest _PaperChestPrefab = null;

    internal static readonly List<int> _IncredibleItemIDs = new();

    public static void Init()
    {
        _PaperChestPrefab = GameManager.Instance.RewardManager.GetTargetChestPrefab(ItemQuality.A).gameObject.ClonePrefab().GetComponent<Chest>();
            _PaperChestPrefab.spawnAnimName = _PaperChestPrefab.sprite.SetUpAnimation("chest_paper_appear", 2);
            _PaperChestPrefab.openAnimName  = _PaperChestPrefab.sprite.SetUpAnimation("chest_paper_open", 30);
            _PaperChestPrefab.breakAnimName = _PaperChestPrefab.openAnimName;
            tk2dSpriteAnimator animator = _PaperChestPrefab.spriteAnimator;
              animator.SetAudio("chest_paper_open", "paper_crinkle_sound", 4, 15, 18, 24);
              animator.SetAudio("chest_paper_open", "paper_fall_sound", 30);
            _PaperChestPrefab.sprite.SetUpAnimation("chest_paper_idle", 11);
            _PaperChestPrefab.IsLocked = false; // can't get lock renderer to attach properly after adjusting appearance animation
            _PaperChestPrefab.GetComponent<MajorBreakable>().HitPoints = 1;

        GunCarryingCase.Init();
        WWIRations.Init();
    }

    private static T SetupIncredibleItem<T>(this T item) where T : PickupObject
    {
      item.quality = CwaffItemQuality.F;
      item.ShouldBeExcludedFromShops = true;
      item.CanBeSold = false;
      item.IgnoredByRat = true;
      return item;
    }

    public static PassiveItem SetupPassive<T>(string ItemName, string ShortDescription, string LongDescription, string Lore) where T : PassiveItem
      => Lazy.SetupPassive<T>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true).SetupIncredibleItem();

    public static PlayerItem SetupActive<T>(string ItemName, string ShortDescription, string LongDescription, string Lore) where T : PlayerItem
      => Lazy.SetupActive<T>(ItemName, ShortDescription, LongDescription, Lore, hideFromAmmonomicon: true).SetupIncredibleItem();

    public static int RandomIncredibleItem()
    {
        if (_IncredibleItemIDs.Count == 0)
          foreach (PickupObject item in PickupObjectDatabase.Instance.Objects)
            if (item && item.quality == CwaffItemQuality.F)
              _IncredibleItemIDs.Add(item.PickupObjectId);
        return _IncredibleItemIDs.ChooseRandom();
    }

    public static Chest SpawnPaperChest()
    {
        Chest chest = Chest.Spawn(IncredibleItems._PaperChestPrefab, GameManager.Instance.PrimaryPlayer.CurrentRoom.GetCenteredVisibleClearSpot(2, 2, out bool _));
        chest.m_isMimic = false;
        chest.IsLocked = false;
        chest.m_isGlitchChest = false;
        chest.contents = null;
        chest.forceContentIds = [RandomIncredibleItem()];
        return chest;
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
