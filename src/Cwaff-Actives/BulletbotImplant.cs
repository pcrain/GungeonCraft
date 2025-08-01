namespace CwaffingTheGungy;

public class BulletbotImplant : CwaffActive
{
    public static string ItemName         = "Bulletbot Implant";
    public static string ShortDescription = "Loyal Gunpanions"; //How to Train your Dragun
    public static string LongDescription  = "Grants the nearest unarmed companion a random gun and the ability to fire it at nearby enemies. Quadruples the chance of finding companions while held.";
    public static string Lore             = "A microchip designed to rewire the neurons and DNA of its host to instantaneously train them in the art of armed combat. Conventional wisdom posits that if you give a dog a gun, he'll bark at it all day, but if you teach a dog to gun, he'll fight by your side for the rest of his life.";

    private const float _COMPANION_CHANCE_MULT = 4f;

    private AIActor _nearestCompanion = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<BulletbotImplant>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.A;
        item.consumable = false;
        item.canStack   = false;
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.PerRoom, 10);
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToShop(ModdedShopType.Handy);

        FakeItem.Create<UsedBulletbotImplant>();
    }

    public override void OnFirstPickup(PlayerController player)
    {
        base.OnFirstPickup(player);
        foreach (var companion in Cammy._CompanionTable.defaultItemDrops.elements)
            this.IncreaseLootChance(companion.pickupId, _COMPANION_CHANCE_MULT);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        this._nearestCompanion = null;
        if (!base.CanBeUsed(user))
            return false;
        if (user.IsGunLocked)
            return false;
        if (user.CurrentGun is not Gun gun)
            return false;
        if (!gun.CanBeDropped)
            return false;
        if (user.companions == null)
            return false;

        Vector2 ppos  = user.CenterPosition;
        float nearest = 9999f;
        foreach (AIActor companion in user.companions)
        {
            if (!companion.GetComponent<CompanionController>())
                continue;
            if (companion.aiShooter)
                continue; // can't arm an enemy that already has a gun

            float dist = (ppos - companion.CenterPosition).sqrMagnitude;
            if (dist > nearest)
                continue;
           nearest = dist;
           this._nearestCompanion = companion;
        }

        return this._nearestCompanion;
    }

    public override void DoEffect(PlayerController user)
    {
        if (!this._nearestCompanion)
            return;

        Items replacementGunId = (Items)HeckedMode.HeckedModeGunWhiteList.ChooseRandom();
        user.AcquireFakeItem<UsedBulletbotImplant>().Setup(this._nearestCompanion, replacementGunId);
    }

    /// <summary>Dummy item for storing data between floors and saves</summary>
    private class UsedBulletbotImplant : FakeItem
    {
        private static List<UsedBulletbotImplant> _ActiveImplants = new();

        private AIActor _companion;
        private string _companionGuid;
        private Items _gunId; //WARNING: this has to be a vanilla gun ID, or it could change between mod loads
        private bool _armed = false; // whether the appropriate companion is currently armed (reset between floors and item drops)
        private bool _deserialized = false; // whether we were just deserialized
        //WARNING: deserialization doesn't seem to work right with multiple copies of an item -> n copies of an item results in n*n deserializations, and all
        //         copies of an item are given the attributes of the final item deserialized. so, we get around this vanilla bug by using an index

        public void Setup(AIActor companion, Items gunId)
        {
            this._companionGuid = companion.EnemyGuid;
            this._gunId         = gunId;

            ArmCompanion(companion);
            this.Owner.gameObject.Play("gun_synthesizer_activate_sound");  //TODO: give this a different sound
        }

        public override void OnDestroy()
        {
            _ActiveImplants.Remove(this);
            GameManager.Instance.OnNewLevelFullyLoaded -= OnNewLevelFullyLoaded;
            base.OnDestroy();
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            GameManager.Instance.OnNewLevelFullyLoaded -= OnNewLevelFullyLoaded;
            GameManager.Instance.OnNewLevelFullyLoaded += OnNewLevelFullyLoaded;
            _ActiveImplants.Add(this);
        }

        public override void MidGameSerialize(List<object> data)
        {
            base.MidGameSerialize(data);
            data.Add(_ActiveImplants.IndexOf(this)); // index of the item in _ActiveImplants, used to work around a vanilla bug where all copies of a passive item get the same data
            data.Add(this._companionGuid);
            data.Add((int)this._gunId);
        }

        public override void MidGameDeserialize(List<object> data)
        {
            base.MidGameDeserialize(data);
            int index = (int)data[0];
            if (index != _ActiveImplants.IndexOf(this))
                return; // work around vanilla deserialization of multiple copies of the same passive item
            this._companionGuid = (string)data[1];
            this._gunId = (Items)((int)data[2]); //TODO: is this double cast necessary? idk how unboxing works D:
            this._deserialized = true;
        }

        private void ArmCompanion(AIActor companion)
        {
            if (this._armed)
                return;

            Gun gunToArm           = this._gunId.AsGun();
            AIShooter shooter      = companion.EnableGunShooting(gunToArm);
            shooter.ArmToTheTeeth(gunToArm);
            shooter.Initialize();
            this._companion = companion;
            this._armed = true;  // prevent the item from arming multiple companions with the same guid
            this._deserialized = false; // we're no longer in the deserialized state
        }

        private static void OnNewLevelFullyLoaded()
        {
            foreach (UsedBulletbotImplant implant in _ActiveImplants)
            {
                implant._companion = null;
                implant._armed     = false;
                if (!implant._deserialized)
                    continue;

                //NOTE: if we were just deserialized, we need to do setup here since companion creation happens BEFORE deserialization when loading a midgame save
                foreach (AIActor c in implant.Owner.companions)
                {
                    if (c.EnemyGuid != implant._companionGuid)
                        continue;
                    if (c.GetComponent<AIShooter>())
                        continue;
                    // ETGModConsole.Log($"    arming {c.EnemyGuid} with {implant._gunId}");
                    implant.ArmCompanion(c);
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(CompanionItem), nameof(CompanionItem.CreateCompanion))]
        private class CreateCompanionPatch
        {
            static void Postfix(CompanionItem __instance, PlayerController owner)
            {
                if (!__instance.m_extantCompanion)
                    return;
                if (__instance.m_extantCompanion.GetComponent<AIActor>() is not AIActor companion)
                    return;
                if (companion.GetComponent<AIShooter>())
                    return;

                foreach (UsedBulletbotImplant implant in _ActiveImplants)
                {
                    if (!implant || implant._armed)
                        continue;
                    if (implant._companionGuid != companion.EnemyGuid)
                        continue;

                    implant.ArmCompanion(companion);
                    break; // don't arm more than one companion
                }
            }
        }

        [HarmonyPatch(typeof(CompanionItem), nameof(CompanionItem.DestroyCompanion))]
        private class DestroyCompanionPatch
        {
            static void Prefix(CompanionItem __instance)
            {
                if (__instance.m_extantCompanion is not GameObject g)
                    return;
                if (!g || g.GetComponent<AIActor>() is not AIActor companion)
                    return;
                foreach (UsedBulletbotImplant implant in _ActiveImplants)
                {
                    if (!implant || !implant._armed || (implant._companion && implant._companion != companion))
                        continue; // if the implant doesn't exist, if it's not armed, or if the reference to the companion is still valid, we're fine
                    implant._companion = null;
                    implant._armed = false;
                }
            }
        }
    }
}
