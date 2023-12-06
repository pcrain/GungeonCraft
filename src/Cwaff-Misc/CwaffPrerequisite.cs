namespace CwaffingTheGungy;

public enum CwaffPrerequisites
{
  NONE,
  INSURANCE_PREREQUISITE,
  WHITE_MAGE_PREREQUISITE,
  BARTER_SHOP_PREREQUISITE,
  TEST_PREREQUISITE,
}

public class SpawnConditions
{
  public FancyRoomBuilder.SpawnCondition validator     = null;
  public int                             spawnsThisRun = 0;
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
      if (spawn != null)
        spawn.spawnsThisRun = 0;
    }
  }

  public static void AddPrequisiteValidator(CwaffPrerequisites prereq, FancyRoomBuilder.SpawnCondition validator)
  {
    if (SpawnConditions[(int)prereq] != null)
    {
      ETGModConsole.Log($"  Tried to re-initialize a prerequisite!");
      return;
    }
    SpawnConditions[(int)prereq] = new SpawnConditions(){
      validator  = validator,
    };
  }

  public override bool CheckConditionsFulfilled()
  {
    // Debug.Log("CHECKING PREREQS NOW");
    SpawnConditions conditions = SpawnConditions[(int)prerequisite];
    Lazy.DebugLog($"checking prereqs for {Enum.GetName(typeof(CwaffPrerequisites), prerequisite)}");
    if (prerequisite == CwaffPrerequisites.NONE || conditions == null)
    {
      // ETGModConsole.Log($"  auto-pass");
      return true;
    }
    if (conditions.validator == null)
    {
      // ETGModConsole.Log($"  auto-pass");
      return true;
    }
    bool passed = conditions.validator();
    ETGModConsole.Log($"  passed? {passed}");
    return passed;
  }

  // Predicate checker functions
  public static bool OnFirstFloor()
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded == "tt_castle";
  }

  // Predicate checker functions
  public static bool NotOnFirstFloor()
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded != "tt_castle";
  }

  public static bool OnSecondFloor()
  {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      return levelBeingLoaded == "tt5";
  }

  public static bool OnThirdFloor()
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
