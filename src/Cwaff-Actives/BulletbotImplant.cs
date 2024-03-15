namespace CwaffingTheGungy;


/* TODO:
    - #BUG: companions guns disappear and cause null dereferences when changing floors
*/

public class BulletbotImplant : PlayerItem
{
    public static string ItemName         = "Bulletbot Implant";
    public static string ShortDescription = "Loyal Gunpanions"; //How to Train your Dragun
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private AIActor _nearestCompanion = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<BulletbotImplant>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.A;
        item.consumable = true;
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (!base.CanBeUsed(user))
            return false;
        if (user.IsGunLocked)
            return false;
        if (user.CurrentGun is not Gun gun)
            return false;
        if (!gun.CanBeDropped)
            return false;

        this._nearestCompanion = null;
        Vector2 ppos           = user.CenterPosition;
        float nearest          = 9999f;
        foreach (AIActor companion in user.companions)
        {
            if (!companion.GetComponent<CompanionController>())
                continue;

            float dist = (ppos - companion.CenterPosition).sqrMagnitude;
            if (dist > nearest)
                continue;
           nearest = dist;
           this._nearestCompanion = companion;
        }

        if (!this._nearestCompanion)
            return false;

        if (this._nearestCompanion.aiShooter is AIShooter shooter)
            return false; // can't arm an enemy that already has a gun

        return true;
    }

    public override void DoEffect(PlayerController user)
    {
        if (!this._nearestCompanion)
            return;


        Items replacementGunId = (Items)HeckedMode.HeckedModeGunWhiteList.ChooseRandom();
        Gun gunToArm = ItemHelper.Get(replacementGunId) as Gun;
        AIShooter shooter = this._nearestCompanion.EnableGunShooting(gunToArm);
        shooter.ArmToTheTeeth(gunToArm);
        shooter.Initialize();
        UnityEngine.Object.DontDestroyOnLoad(shooter.CurrentGun.gameObject);

        // this._nearestCompanion.ArmToTheTeeth(user.CurrentGun);
        // user.inventory.RemoveGunFromInventory(user.CurrentGun);
        user.gameObject.Play("gun_synthesizer_activate_sound");
    }
}
