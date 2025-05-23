namespace CwaffingTheGungy;

using static NeedStatus;

public class KalibersJustice : CwaffActive
{
    public static string ItemName         = "Kaliber's Justice";
    public static string ShortDescription = "Knows What You Need";
    public static string LongDescription  = "Gives one or more items based on what the player currently needs most, but may take away items the player has in excess. Reward quality scales with the quality and quantity of items lost.";
    public static string Lore             = "Praying to Kaliber for assistance is something of a mixed bag, even among her followers. On the one hand, she tends to be highly responsive to prayers and considerate of one's present situation when choosing her mode of assistance. On the flipside, she is less than thrilled by the modern trend of praying to her only when in need something, and has been known to make the equally modern decision of treating her blessings as business transactions and taking as much as she gives. Still, she has been known to freely offer her aid to those who remain humble.";

    private float _lastUse = 0f;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<KalibersJustice>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;
        item.consumable   = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Damage, 1000f);
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (user.IsGunLocked)
            return false; // prevents me from having to do some very messy logic
        if ((BraveTime.ScaledTimeSinceStartup - _lastUse) < 1f)
            return false;
        return base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
        this._lastUse = BraveTime.ScaledTimeSinceStartup;

        // Get the user's needs
        List<Need> needs = GetItemNeeds(user);

        // Categorize the user's needs
        List<Need> minimal   = new();
        List<Need> lacking   = new();
        List<Need> enough    = new();
        List<Need> plenty    = new();
        List<Need> excessive = new();
        // ETGModConsole.Log($"checking needs");
        foreach (Need need in needs)
        {
            // ETGModConsole.Log($"  user's need for {need.type} is {need.status}");
            switch(need.status)
            {
                case Minimal:   minimal.Add(need);   break;
                case Lacking:   lacking.Add(need);   break;
                case Enough:    enough.Add(need);    break;
                case Plenty:    plenty.Add(need);    break;
                case Excessive: excessive.Add(need); break;
                default:                             break;
            }
        }

        // First try to trade off excessive items
        if (minimal.Count > 0)
        {
            if (minimal.Count > 2)
                DoBigTrade(user, null, minimal); // big freebie
            else if (excessive.Count > 0)
                DoBigTrade(user, excessive, minimal); // big trade can only happen with minimal and excess
            else if (minimal.Count > 1)
                DoBigTrade(user, null, minimal); // big freebie
            else if (plenty.Count > 0)
                DoModerateTrade(user, plenty, minimal);
            else
                DoModerateTrade(user, null, minimal); // moderate freebie
        }
        else if (lacking.Count > 0)
        {
            if (lacking.Count > 1)
                DoModerateTrade(user, null, lacking); // moderate freebie
            else if (excessive.Count > 0)
                DoModerateTrade(user, excessive, lacking);
            else if (plenty.Count > 0)
                DoModerateTrade(user, plenty, lacking);
            else
                DoModerateTrade(user, null, lacking); // moderate freebie
        }
        else if (enough.Count > 0)
            DoModerateTrade(user, null, enough); // moderate freebie
        else
            ETGModConsole.Log($"player has way too much stuff o.o"); // maybe just give them money or something
    }

    private void DoTrade(PlayerController user, List<Need> offerList, List<Need> receiveList, bool big)
    {
        if (receiveList == null || receiveList.Count == 0)
            return; // this should never happen, but bail out just in case

        // Figure out what item we're giving and give it
        Need? whatToOffer = null;
        if (offerList != null && offerList.Count > 0 && !user.HasSynergy(Synergy.KALIBERS_FAVOR))
        {
            whatToOffer = offerList.ChooseRandom<Need>();
            switch(whatToOffer.Value.type)
            {
                case NeedType.Health:
                    user.healthHaver.ApplyDamage(big ? 3.5f : 2.0f, Vector2.zero, "Balance", CoreDamageTypes.None, DamageCategory.Unstoppable, true);
                    break;
                case NeedType.Armor:
                    user.healthHaver.Armor -= (big ? 4 : 2);
                    break;
                case NeedType.Money:
                    GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency -= (big ? 125 : 50);
                    break;
                case NeedType.Keys:
                    GameManager.Instance.PrimaryPlayer.carriedConsumables.KeyBullets -= (big ? 3 : 1);
                    break;
                case NeedType.Blanks:
                    user.Blanks -= (big ? 3 : 1);
                    break;
                case NeedType.Ammo:
                    foreach (Gun g in user.inventory.AllGuns)
                    {
                        if (g.InfiniteAmmo)
                            continue;
                        g.CurrentAmmo = (int)(g.CurrentAmmo * (big ? 0.5f : 0.75f));
                    }
                    break;
                case NeedType.Guns:
                    for (int i = 0; i < (big ? 2 : 1); ++i)
                    {
                        Gun gunToDrop = null;
                        for (int n = 0; n < 100; ++n)
                        {
                            gunToDrop = user.inventory.AllGuns.ChooseRandom();
                            if (!gunToDrop.CanActuallyBeDropped(user))
                                continue;
                            if (n < 50 && !big && (gunToDrop.quality == ItemQuality.S || gunToDrop.quality == ItemQuality.A))
                                continue; // try not to drop rare items if we're not doing a big trade
                        }
                        if (gunToDrop && gunToDrop.CanActuallyBeDropped(user))
                            UnityEngine.Object.Destroy(user.ForceDropGun(gunToDrop).gameObject);
                        else
                            ETGModConsole.Log($"dropping a gun went horrifically wrong o.o");
                    }
                    break;
                case NeedType.Passives:
                    for (int i = 0; i < (big ? 2 : 1); ++i)
                    {
                        PassiveItem passiveToDrop = null;
                        for (int n = 0; n < 100; ++n)
                        {
                            passiveToDrop = user.passiveItems.ChooseRandom();
                            if (!passiveToDrop.CanActuallyBeDropped(user))
                                continue;
                            if (n < 50 && !big && (passiveToDrop.quality == ItemQuality.S || passiveToDrop.quality == ItemQuality.A))
                                continue; // try not to drop rare items if we're not doing a big trade
                        }
                        if (passiveToDrop && passiveToDrop.CanActuallyBeDropped(user))
                            UnityEngine.Object.Destroy(user.DropPassiveItem(passiveToDrop).gameObject);
                        else
                            ETGModConsole.Log($"dropping a passive went horrifically wrong o.o");
                    }
                    break;
                // case NeedType.Actives:
                //     for (int i = 0; i < (big ? 2 : 1); ++i)
                //     {
                //         PlayerItem activeToDrop = null;
                //         for (int n = 0; n < 100; ++n)
                //         {
                //             activeToDrop = user.activeItems.ChooseRandom();
                //             if (activeToDrop == this)
                //                 continue;
                //             if (!activeToDrop.CanActuallyBeDropped(user))
                //                 continue;
                //             if (n < 50 && !big && (activeToDrop.quality == ItemQuality.S || activeToDrop.quality == ItemQuality.A))
                //                 continue; // try not to drop rare items if we're not doing a big trade
                //         }
                //         if (activeToDrop && activeToDrop.CanActuallyBeDropped(user))
                //             UnityEngine.Object.Destroy(user.DropActiveItem(activeToDrop).gameObject);
                //         else
                //             ETGModConsole.Log($"dropping an active went horrifically wrong o.o");
                //     }
                //     break;
            }
        }

        // Figure out what blessing we're receiving
        Need whatToReceive = receiveList.ChooseRandom<Need>();
        Vector2 where = user.CenterPosition;
        switch(whatToReceive.type)
        {
            case NeedType.Health:
                LootEngine.SpawnItem(ItemHelper.Get(big ? Items.HeartSynthesizer : Items.HeartHolster).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                for (int i = 0; i < 3; ++i)
                    LootEngine.SpawnItem(ItemHelper.Get(Items.Heart).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Armor:
                LootEngine.SpawnItem(ItemHelper.Get(big ? Items.ArmorSynthesizer : Items.Nanomachines).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                for (int i = 0; i < 2; ++i)
                    LootEngine.SpawnItem(ItemHelper.Get(Items.Armor).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Money:
                if (big)
                    LootEngine.SpawnItem(ItemHelper.Get(Items.BriefcaseOfCash).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                else
                    LootEngine.SpawnCurrency(SpotNear(where), 70, false, null, null);
                break;
            case NeedType.Keys:
                LootEngine.SpawnItem(ItemHelper.Get(big ? Items.Akey47 : Items.ShelletonKey).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Blanks:
                Items blankItem = (big && !user.HasPassive((int)Items.BlankBullets)) ? Items.BlankBullets : Items.ElderBlank;
                if (!user.HasPassive((int)blankItem))
                    LootEngine.SpawnItem(ItemHelper.Get(blankItem).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                for (int i = 0; i < (big ? 4 : 2); ++i)
                    LootEngine.SpawnItem(ItemHelper.Get(Items.Blank).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Ammo:
                LootEngine.SpawnItem(ItemHelper.Get(big ? Items.AmmoSynthesizer : Items.MagazineRack).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                for (int i = 0; i < (big ? 2 : 1); ++i)
                    LootEngine.SpawnItem(ItemHelper.Get(Items.PartialAmmo).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Guns:
                ItemQuality qg = Lazy.CoinFlip()
                    ? (big ? ItemQuality.S : ItemQuality.B)
                    : (big ? ItemQuality.A : ItemQuality.C);
                LootEngine.SpawnItem(GameManager.Instance.RewardManager.GetItemForPlayer(
                    user, GameManager.Instance.RewardManager.GunsLootTable, qg, null), SpotNear(where), Vector2.zero, 0f, true, true, false);
                break;
            case NeedType.Passives:
                ItemQuality qp = Lazy.CoinFlip()
                    ? (big ? ItemQuality.S : ItemQuality.B)
                    : (big ? ItemQuality.A : ItemQuality.C);
                while (true) // retry until we get a passive
                {
                    GameObject item = GameManager.Instance.RewardManager.GetItemForPlayer(user, GameManager.Instance.RewardManager.ItemsLootTable, qp, null);
                    if (item == null)
                        break;
                    if (item.GetComponent<PlayerItem>())
                        continue; // don't accept actives
                    LootEngine.SpawnItem(item, SpotNear(where), Vector2.zero, 0f, true, true, false);
                    break;
                }
                break;
            // case NeedType.Actives:
            //     LootEngine.SpawnItem(ItemHelper.Get(Items.Backpack).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
            //     LootEngine.SpawnItem(ItemHelper.Get(big ? Items.Relodestone : Items.PortableTurret).gameObject, SpotNear(where), Vector2.zero, 0f, true, true, false);
            //     break;
        }

        GameObject vfx = Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.tileMapVertical.effects[0].effects[0].effect;
        for (int i = 0; i < 8; ++i)
        {
            float angle = 45f * i;
            SpawnManager.SpawnVFX(vfx, where + angle.ToVector(1.5f), Quaternion.identity);
        }

        user.gameObject.Play("Play_OBJ_dice_bless_01");
        Lazy.CustomNotification(
            "Kaliber's Justice",
            $"Offered {(whatToOffer.HasValue ? whatToOffer.Value.type : NeedType.Nothing)}, Received {whatToReceive.type}",
            this.sprite);
    }

    private static Vector2 SpotNear(Vector2 where)
    {
        return where + Lazy.RandomVector(0.75f);
    }

    private void DoBigTrade(PlayerController user, List<Need> offerList, List<Need> receiveList)
    {
        DoTrade(user, offerList, receiveList, true);
    }

    private void DoModerateTrade(PlayerController user, List<Need> offerList, List<Need> receiveList)
    {
        DoTrade(user, offerList, receiveList, false);
    }

    private List<Need> GetItemNeeds(PlayerController user)
    {
        List<Need> needs = new();
        #region Health
            Need healthNeed = new Need(NeedType.Health);
            healthNeed.status = user.ForceZeroHealthState ? Enough : user.healthHaver.currentHealth switch {
                <= 1f => Minimal,
                <= 2f => Lacking,
                <= 4f => Enough,
                <= 6f => Plenty,
                _     => Excessive,
            };
            needs.Add(healthNeed);
        #endregion

        #region Armor
            Need armorNeed = new Need(NeedType.Armor);
            int adjustedArmor = (int)user.healthHaver.currentArmor + (user.ForceZeroHealthState ? -1 : 1);
            armorNeed.status = adjustedArmor switch {
                <= 2 => Minimal,
                <= 3 => Lacking,
                <= 5 => Enough,
                <= 8 => Plenty,
                _    => Excessive,
            };
            needs.Add(armorNeed);
        #endregion

        #region Money
            Need moneyNeed = new Need(NeedType.Money);
            int money = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
            moneyNeed.status = money switch {
                < 10  => Minimal,
                < 25  => Lacking,
                < 100 => Enough,
                < 200 => Plenty,
                _     => Excessive,
            };
            needs.Add(moneyNeed);
        #endregion

        #region Keys
            Need keysNeed = new Need(NeedType.Keys);
            int keys = GameManager.Instance.PrimaryPlayer.carriedConsumables.KeyBullets;
            bool infKeys = GameManager.Instance.PrimaryPlayer.carriedConsumables.InfiniteKeys
              || Lazy.AnyoneHasGun((int)Items.Akey47) || Lazy.AnyoneHas((int)Items.ShelletonKey);
            if (infKeys && keys < 4)
                keys = 4;
            keysNeed.status = keys switch {
                > 6 => Excessive,
                > 4 => Plenty,
                > 2 => Enough,
                > 0 => Lacking,
                _   => Minimal,
            };
            needs.Add(keysNeed);
        #endregion

        #region Blanks
            Need blanksNeed = new Need(NeedType.Blanks);
            keysNeed.status = user.Blanks switch {
                > 5 => Excessive,
                > 3 => Plenty,
                > 1 => Enough,
                > 0 => Lacking,
                _   => Minimal,
            };
            needs.Add(blanksNeed);
        #endregion

        #region Ammo
            Need ammoNeed = new Need(NeedType.Ammo);
            int gunsWithAmmo = user.inventory.AllGuns.Count;
            float averageGunAmmoPercent = 0.0f;
            foreach (Gun g in user.inventory.AllGuns)
            {
                if (g.InfiniteAmmo)
                    gunsWithAmmo -= 1;
                else
                    averageGunAmmoPercent += ((float)g.CurrentAmmo / (float)g.AdjustedMaxAmmo);
            }
            if (gunsWithAmmo == 0)
                ammoNeed.status = Enough;
            else
            {
                averageGunAmmoPercent /= (float)gunsWithAmmo;
                if      (averageGunAmmoPercent < 0.34f) ammoNeed.status = Minimal;
                else if (averageGunAmmoPercent < 0.50f) ammoNeed.status = Lacking;
                else if (averageGunAmmoPercent < 0.67f) ammoNeed.status = Enough;
                else if (averageGunAmmoPercent < 0.75f) ammoNeed.status = Plenty;
                else                                    ammoNeed.status = Excessive;
            }
            needs.Add(ammoNeed);
        #endregion

        #region Guns
            Need gunsNeed = new Need(NeedType.Guns);
            int numGuns = user.inventory.AllGuns.Count;
            ItemQuality bestGunQuality = (user.inventory.AllGuns.HighestQualityItem() is PickupObject pg)
                ? pg.quality : ItemQuality.D;
            if      (numGuns <= 2 || bestGunQuality == ItemQuality.D) gunsNeed.status = Minimal;
            else if (numGuns <= 4 || bestGunQuality == ItemQuality.C) gunsNeed.status = Lacking;
            else if (numGuns <= 6 || bestGunQuality == ItemQuality.B) gunsNeed.status = Enough;
            else if (numGuns <= 8 || bestGunQuality == ItemQuality.A) gunsNeed.status = Plenty;
            else                                                      gunsNeed.status = Excessive;
            needs.Add(gunsNeed);
        #endregion

        #region Passives
            Need passivesNeed = new Need(NeedType.Passives);
            int numPassives = user.passiveItems.Count;
            ItemQuality bestPassiveQuality = (user.passiveItems.HighestQualityItem() is PickupObject pp)
                ? pp.quality : ItemQuality.D;
            if      (numPassives < 2 || bestPassiveQuality == ItemQuality.D) passivesNeed.status = Minimal;
            else if (numPassives < 4 || bestPassiveQuality == ItemQuality.C) passivesNeed.status = Lacking;
            else if (numPassives < 6 || bestPassiveQuality == ItemQuality.B) passivesNeed.status = Enough;
            else if (numPassives < 8 || bestPassiveQuality == ItemQuality.A) passivesNeed.status = Plenty;
            else                                                             passivesNeed.status = Excessive;
            needs.Add(passivesNeed);
        #endregion

        //NOTE: this literally cannot ever trigger since Kaliber's Justice itself will always count as having "Enough"
        // #region Actives
        //     Need activesNeed = new Need(NeedType.Actives);
        //     int numActives = user.activeItems.Count;
        //     ItemQuality bestActiveQuality = (user.activeItems.HighestQualityItem() is PickupObject pa)
        //         ? pa.quality : ItemQuality.D;
        //     if (user.activeItems.Count == 1 && user.activeItems[0] == this)
        //         activesNeed.status = Enough;
        //     else if (numActives  < 2 || bestActiveQuality == ItemQuality.D) activesNeed.status = Lacking;
        //     else if (numActives  < 3 || bestActiveQuality == ItemQuality.C) activesNeed.status = Enough;
        //     else if (numActives  < 4 || bestActiveQuality == ItemQuality.A) activesNeed.status = Plenty;
        //     else                                                            activesNeed.status = Excessive;
        //     needs.Add(activesNeed);
        // #endregion

        return needs;
    }
}

internal enum NeedType {
    Health,
    Armor,
    Money,
    Keys,
    Blanks,
    Ammo,
    Guns,
    Passives,
    // Actives, //NOTE: can't activate
    // Curse / Cleansing,
    Nothing,
}

internal enum NeedStatus {
    Minimal   = -2,
    Lacking   = -1,
    Enough    =  0,
    Plenty    =  1,
    Excessive =  2,
}

internal struct Need
{
    public NeedType type;
    public NeedStatus status;
    public Need(NeedType type)
    {
        this.type = type;
        this.status = Enough;
    }
}
