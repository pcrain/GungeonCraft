namespace CwaffingTheGungy;

public class Allay : CwaffCompanion
{
    public static string ItemName         = "Allay";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Allay>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;

        AllayCompanion friend = item.InitCompanion<AllayCompanion>(baseFps: 12);

        friend.CanCrossPits = true;
        friend.aiActor.MovementSpeed = 7f;
        friend.aiActor.healthHaver.PreventAllDamage = true;
        friend.aiActor.CollisionDamage = 0f;
        friend.aiActor.HasShadow = false;
        friend.aiActor.specRigidbody.CollideWithOthers = false;
        friend.aiActor.specRigidbody.CollideWithTileMap = false;

        string companionName = ItemName.ToID();
        BehaviorSpeculator bs = friend.gameObject.GetComponent<BehaviorSpeculator>();
        bs.MovementBehaviors.Add(new CompanionFollowPlayerBehavior {
            IdleAnimations = [$"{companionName}_idle"],
            CatchUpRadius = 6,
            CatchUpMaxSpeed = 10,
            CatchUpAccelTime = 1,
            CatchUpSpeed = 7,
            });
    }
}

public class AllayCompanion : CwaffCompanionController
{
}
