using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class ReinforcementRadio : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Reinforcement Radio";
            string resourceName = "NevernamedsItems/Resources/reinforcementradio_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ReinforcementRadio>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "I Need Backup!";
            string longDesc = "Taps into secret Gundead radio frequencies to confuse their reinforcement divisions, and get them to send aid to the wrong side." + "\n\nGundead are not the brightest of creatures.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 600);
            item.consumable = false;
            item.quality = ItemQuality.B;

            //SYNERGIES
            List<string> mandatorySynergyItems = new List<string>() { "nn:reinforcement_radio", "magnum" };
            CustomSynergies.Add("Bullet+", mandatorySynergyItems);
            List<string> mandatorySynergyItems2 = new List<string>() { "nn:reinforcement_radio", "regular_shotgun" };
            CustomSynergies.Add("Red Shotgun+", mandatorySynergyItems2);
            List<string> mandatorySynergyItems3 = new List<string>() { "nn:reinforcement_radio", "winchester" };
            CustomSynergies.Add("Blue Shotgun+", mandatorySynergyItems3);
            List<string> mandatorySynergyItems4 = new List<string>() { "nn:reinforcement_radio", "machine_pistol" };
            CustomSynergies.Add("Bandana+", mandatorySynergyItems4);
            List<string> mandatorySynergyItems5 = new List<string>() { "nn:reinforcement_radio", "ak47" };
            CustomSynergies.Add("Tank+", mandatorySynergyItems5);
            List<string> mandatorySynergyItems6 = new List<string>() { "nn:reinforcement_radio", "pitchfork" };
            CustomSynergies.Add("Devil+", mandatorySynergyItems6);
            List<string> mandatorySynergyItems7 = new List<string>() { "nn:reinforcement_radio", "huntsman" };
            CustomSynergies.Add("Execution+", mandatorySynergyItems7);
            List<string> mandatorySynergyItems8 = new List<string>() { "nn:reinforcement_radio", "sniper_rifle" };
            CustomSynergies.Add("Sniper+", mandatorySynergyItems8);
            List<string> mandatorySynergyItems9 = new List<string>() { "nn:reinforcement_radio", "thompson" };
            CustomSynergies.Add("Spirit+", mandatorySynergyItems9);
            List<string> mandatorySynergyItems10 = new List<string>() { "nn:reinforcement_radio", "bow" };
            CustomSynergies.Add("Arrow+", mandatorySynergyItems10);
            List<string> mandatorySynergyItems11 = new List<string>() { "nn:reinforcement_radio", "the_scrambler" };
            CustomSynergies.Add("Egg+", mandatorySynergyItems11);
            List<string> mandatorySynergyItems12 = new List<string>() { "nn:reinforcement_radio", "bubble_blaster" };
            CustomSynergies.Add("Bubble+", mandatorySynergyItems12);
            List<string> mandatorySynergyItems13 = new List<string>() { "nn:reinforcement_radio", "trank_gun" };
            CustomSynergies.Add("Eye+", mandatorySynergyItems13);
            //SYNERGIES WITH MULTIPLE ITEMS INVOLVED
            List<string> mandatorySynergyItems14 = new List<string>() { "nn:reinforcement_radio" };
            List<string> optionalSynergyItems14 = new List<string>() { "bomb", "ice_bomb", "lil_bomber", "cluster_mine" };
            CustomSynergies.Add("Bomb+", mandatorySynergyItems14, optionalSynergyItems14);
            List<string> mandatorySynergyItemsOilSlick = new List<string>() { "nn:reinforcement_radio" };
            List<string> optionalSynergyItemsOilSlick = new List<string>() { "bundle_of_wands", "hexagun", "magic_bullets" };
            CustomSynergies.Add("Magic+", mandatorySynergyItemsOilSlick, optionalSynergyItemsOilSlick);
        }
        List<List<string>> randomEnemyloadouts = new List<List<string>>()
        {
            new List<string>(){"01972dee89fc4404a5c408d50007dad5","01972dee89fc4404a5c408d50007dad5","01972dee89fc4404a5c408d50007dad5","01972dee89fc4404a5c408d50007dad5"},
            new List<string>(){"128db2f0781141bcb505d8f00f9e4d47", "128db2f0781141bcb505d8f00f9e4d47"},
            new List<string>(){"b54d89f9e802455cbb2b8a96a31e8259", "01972dee89fc4404a5c408d50007dad5", "01972dee89fc4404a5c408d50007dad5"},
            new List<string>(){"88b6b6a93d4b4234a67844ef4728382c", "88b6b6a93d4b4234a67844ef4728382c"},
            new List<string>(){"4d37ce3d666b4ddda8039929225b7ede", "4d37ce3d666b4ddda8039929225b7ede", "c0260c286c8d4538a697c5bf24976ccf"},
        };
        Dictionary<string, string> synergies = new Dictionary<string, string>()
        {
            { "Bullet+", "01972dee89fc4404a5c408d50007dad5"}, //Magnum --> Bullet Kin
            { "Red Shotgun+", "128db2f0781141bcb505d8f00f9e4d47"}, //Regular Shotgun --> Red Shotty
            { "Blue Shotgun+", "b54d89f9e802455cbb2b8a96a31e8259"},//Winchester --> Blue Shotty
            { "Bandana+", "88b6b6a93d4b4234a67844ef4728382c"},//Machine Pistol --> Bandnana Kin
            { "Tank+", "df7fb62405dc4697b7721862c7b6b3cd"},//ak47 --> Tanker
            { "Devil+", "5f3abc2d561b4b9c9e72b879c6f10c7e"},//pitchfork --> Fallen Bullet Kin
            { "Execution+", "b1770e0f1c744d9d887cc16122882b4f"},//Huntsman --> Executioner
            { "Sniper+", "31a3ea0c54a745e182e22ea54844a82d"},//Sniper Rifle --> Sniper Shell
            { "Spirit+", "4db03291a12144d69fe940d5a01de376"},//Thompson --> Hollowpoint
            { "Arrow+", "05891b158cd542b1a5f3df30fb67a7ff"},//Bow --> Arrow Kin
            { "Egg+", "ed37fa13e0fa4fcf8239643957c51293"},//Scrambler --> Gigi
            { "Bubble+", "6e972cd3b11e4b429b888b488e308551"},//Bubble Blaster --> Gunzookie
            { "Eye+", "7b0b1b6d9ce7405b86b75ce648025dd6"},//Trank Gun --> Beadie
            { "Bomb+", "4d37ce3d666b4ddda8039929225b7ede"}, //Bomb, Ice Bomb, Lil Bomber, or Cluster Mine --> Pinhead.
            { "Magic+", "206405acad4d4c33aac6717d184dc8d4"},//Bundle of Wands, Hexagun, Magic Bullets --> Apprentice Gunjurer
        };
        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_supplydrop_activate_01", base.gameObject);
            List<string> reinforcements = new List<string>();
            reinforcements.AddRange(BraveUtility.RandomElement(randomEnemyloadouts));

            foreach (string key in synergies.Keys)
            {
                if (user.PlayerHasActiveSynergy(key)) reinforcements.Add(synergies[key]);
            }

            foreach (string dude in reinforcements)
            {
                IntVector2? bestRewardLocation = LastOwner.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                AIActor spawnedDude = CompanionisedEnemyUtility.SpawnCompanionisedEnemy(user, dude, (IntVector2)bestRewardLocation, false, Color.red, 5, 2, false, true);
                spawnedDude.HandleReinforcementFallIntoRoom(0f);
            }
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return user.IsInCombat;
        }


    }
}
