namespace CwaffingTheGungy;

public enum CwaffPrerequisites
{
  NONE,
  INSURANCE_PREREQUISITE,
  COMPANION_SHOP_PREREQUISITE,
  BARTER_SHOP_PREREQUISITE,
  NOT_COOP_MODE_PREREQUISITE,
  TEST_PREREQUISITE,
}

public class SpawnConditions
{
  public FancyShopBuilder.SpawnCondition validator              = null;
  public int                             spawnsThisRun          = 0;
  public float                           randomNumberForThisRun = 0.0f;
}

public class CwaffPrerequisite : CustomDungeonPrerequisite
{
  internal static List<SpawnConditions> SpawnConditions =
    new(Enumerable.Repeat<SpawnConditions>(null, Enum.GetNames(typeof(CwaffPrerequisites)).Length).ToList());

  public CwaffPrerequisites prerequisite = CwaffPrerequisites.NONE;

  public static void Init()
  {
    CwaffEvents.OnCleanStart += ResetPerRunPrerequisites;
  }

  public static void ResetPerRunPrerequisites()
  {
    // Lazy.DebugLog($"  clearing spawn conditions");
    foreach (SpawnConditions spawn in SpawnConditions)
    {
      if (spawn == null)
        continue;
      spawn.spawnsThisRun          = 0;
      spawn.randomNumberForThisRun = UnityEngine.Random.value;
    }
  }

  public static void AddPrequisiteValidator(CwaffPrerequisites prereq, FancyShopBuilder.SpawnCondition validator)
  {
    if (SpawnConditions[(int)prereq] != null)
    {
      ETGModConsole.Log($"  Tried to re-initialize a prerequisite!");
      return;
    }
    SpawnConditions[(int)prereq] = new SpawnConditions(){
      validator              = validator,
      spawnsThisRun          = 0,
      randomNumberForThisRun = UnityEngine.Random.value,
    };
  }

  public override bool CheckConditionsFulfilled()
  {
    if (prerequisite == CwaffPrerequisites.NOT_COOP_MODE_PREREQUISITE)
      if (GameManager.HasInstance && GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
        return false;
    SpawnConditions conditions = SpawnConditions[(int)prerequisite];
    // Lazy.DebugLog($"checking prereqs for {Enum.GetName(typeof(CwaffPrerequisites), prerequisite)}");
    if (prerequisite == CwaffPrerequisites.NONE || conditions == null)
    {
      // Lazy.DebugLog($"  auto-pass");
      return true;
    }
    if (conditions.validator == null)
    {
      // Lazy.DebugLog($"  auto-pass");
      return true;
    }
    bool passed = conditions.validator(conditions);
    // Lazy.DebugLog($"  passed? {passed}");
    return passed;
  }

  // Predicate checker functions
  public static bool OnFirstFloor(SpawnConditions conds)
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded == "tt_castle";
  }

  // Predicate checker functions
  public static bool NotOnFirstFloor(SpawnConditions conds)
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded != "tt_castle";
  }

  public static bool OnSecondFloor(SpawnConditions conds)
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded == "tt5";
  }

  public static bool OnThirdFloor(SpawnConditions conds)
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded == "tt_mines";
  }

  // Prerequisite tracker to be attached to game objects to count how many times they've spawned in a run, etc.
  public class Tracker : MonoBehaviour
  {
    public CwaffPrerequisites prereq = CwaffPrerequisites.NONE; // must be public for serializatioin

    public void Setup(CwaffPrerequisites prereq)
    {
      this.prereq = prereq;
    }

    private void Start()
    {
      // ETGModConsole.Log($"shop created with prereq {Enum.GetName(typeof(CwaffPrerequisites), this.prereq)}!");
      CwaffPrerequisite.SpawnConditions[(int)this.prereq].spawnsThisRun += 1;
    }
  }
}
