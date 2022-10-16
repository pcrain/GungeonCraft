using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class Prescription : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Prescription";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/ringofoddlyspecificbenefits_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<Prescription>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Ultimate";
            string longDesc = "Apparently being rich = being cool these days." + "\n\nMaybe you should write a song about how rich you are.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.EXCLUDED; //c
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected && !PillPoolsAssigned)
            {
                AssignPillPools();
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        public static List<string> pillEffectPool = new List<string>()
        {
            "badgas",
            "badtrip",
            "ballsofsteel",
            "blanksarekey",
            "explosivediarrhea",
        };
        public static bool PillPoolsAssigned = false;
        public static string BlueBluePillEffect;
        public static string BlueWhitePillEffect;
        public static string OrangeOrangePillEffect;
        public static string WhiteWhitePillEffect;
        public static string RedWhiteSpeckledPillEffect;
        public static string RedWhitePillEffect;
        public static string GreenBluePillEffect;
        public static string OrangeYellowPillEffect;
        public static string WhiteOrangeSpeckledPillEffect;
        public static string LightBlueWhitePillEffect;
        public static string YellowBlackPillEffect;
        public static string BlackWhitePillEffect;
        public static string YellowWhitePillEffect;
        private void AssignPillPools()
        {
            BlueBluePillEffect = BraveUtility.RandomElement(pillEffectPool);
            pillEffectPool.Remove(BlueBluePillEffect);

            PillPoolsAssigned = true;
        }
        private void OnPillUse(PlayerController user, String pillEffect)
        {
            switch (pillEffect)
            {
                case "badgas":
                    BadGas(user);
                    break;
                case "badtrip":
                    BadTrip(user);
                    break;
                case "ballsofsteel":
                    BallsOfSteel(user);
                    break;
                case "blanksarekey":
                    BlanksAreKey(user);
                    break;
            }
        }
        private void Notify(string header, string text)
        {
            tk2dBaseSprite notificationObjectSprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(header, text, notificationObjectSprite.Collection, notificationObjectSprite.spriteId, UINotificationController.NotificationColor.PURPLE, false, true);
        }
        public void BadGas(PlayerController user)
        {
            PlayerController owner = base.Owner;
            GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
            GameObject gameObject = new GameObject("silencer");
            SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
            float additionalTimeAtMaxRadius = 0.25f;
            silencerInstance.TriggerSilencer(user.specRigidbody.UnitCenter, 25f, 5f, silencerVFX, 0f, 3f, 3f, 3f, 250f, 5f, additionalTimeAtMaxRadius, owner, false, false);
            user.CurrentRoom.ApplyActionToNearbyEnemies(user.CenterPosition, 30f, delegate (AIActor enemy, float dist)
            {
                enemy.gameActor.ApplyEffect(this.poisonEffect, 1f, null);
            });
            Notify(null, "Bad Gas");
        }
        GameActorHealthEffect poisonEffect = Gungeon.Game.Items["irradiated_lead"].GetComponent<BulletStatusEffectItem>().HealthModifierEffect;
        public void BadTrip(PlayerController user)
        {
            if (user.characterIdentity == PlayableCharacters.Robot)
            {
                if (user.healthHaver.GetCurrentHealth() > 1) Owner.healthHaver.ApplyDamage(1f, Vector2.zero, "Pills", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                else if (user.healthHaver.GetCurrentHealth() == 1) Owner.healthHaver.ApplyDamage(0.5f, Vector2.zero, "Pills", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                //else fullhealth
            }
            else
            {
                if (user.healthHaver.Armor > 1) Owner.healthHaver.ApplyDamage(0.5f, Vector2.zero, "Pills", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                //else fullhealth
            }
            Notify(null, "Bad Trip");
        }
        public void BallsOfSteel(PlayerController user)
        {
            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
            Notify(null, "Balls of Steel");
        }
        public void BlanksAreKey(PlayerController user)
        {
            int numberOfBlanks = user.Blanks;
            int numberOfKeys = user.carriedConsumables.KeyBullets;
            user.Blanks = numberOfKeys;
            user.carriedConsumables.KeyBullets = numberOfBlanks;
            Notify(null, "Blanks are Key");
        }
        public IEnumerator ExplosiveDiarrhea(PlayerController user)
        {
            Notify(null, "Explosive Diarrhea");
            for (int i = 0; i < 5; i++)
            {
                SpawnObjectPlayerItem component3 = PickupObjectDatabase.GetById(108).GetComponent<SpawnObjectPlayerItem>();
                GameObject gameObject3 = component3.objectToSpawn.gameObject;
                GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(gameObject3, user.specRigidbody.UnitCenter, Quaternion.identity);
                tk2dBaseSprite component4 = gameObject4.GetComponent<tk2dBaseSprite>();
                bool flag6 = component4;
                if (flag6)
                {
                    component4.PlaceAtPositionByAnchor(user.specRigidbody.UnitCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                }
                yield return new WaitForSeconds(1f);
            }
            yield return null;
        }
    }
}
