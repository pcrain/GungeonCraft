using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class AppleActive : PlayerItem
    {
        private static int[] spriteIDs;
        private static readonly string[] spritePaths = new string[]
        {
            "NevernamedsItems/Resources/apple_icon",
            "NevernamedsItems/Resources/goldenapple_icon",
        };
        public static void Init()
        {
            string itemName = "Apple";
            string resourceName = AppleActive.spritePaths[0];
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AppleActive>();

            AppleActive.spriteIDs = new int[AppleActive.spritePaths.Length];

            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Doesn't Fall Far";
            string longDesc = "Heals a small amount. Can only be eaten once." + "\n\nAn apple from Kaliber's garden.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            AppleActive.spriteIDs[0] = item.sprite.spriteId; //Norm
            AppleActive.spriteIDs[1] = SpriteBuilder.AddSpriteToCollection(AppleActive.spritePaths[1], item.sprite.Collection); //Gold

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 0);
            item.CustomCost = 10;
            item.UsesCustomCost = true;
            item.consumable = true;
            item.quality = ItemQuality.D;
            AppleID = item.PickupObjectId;
        }
        public static int AppleID;
        private bool WasGoldenLastChecked = false;
        public override void Update()
        {
            if (LastOwner)
            {
                if (LastOwner.PlayerHasActiveSynergy("Golden Apple") && !WasGoldenLastChecked)
                {
                    base.sprite.SetSprite(AppleActive.spriteIDs[1]);
                    WasGoldenLastChecked = true;
                }
                else if (!LastOwner.PlayerHasActiveSynergy("Golden Apple") && WasGoldenLastChecked)
                {
                    base.sprite.SetSprite(AppleActive.spriteIDs[0]);
                    WasGoldenLastChecked = false;
                }
            }
            base.Update();
        }
        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_heart_heal_01", user.gameObject);
            user.PlayEffectOnActor((PickupObjectDatabase.GetById(73).GetComponent<HealthPickup>().healVFX), Vector3.zero, true, false, false);
            if (user.ForceZeroHealthState)
            {
                if (user.PlayerHasActiveSynergy("Apple A Day")) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                if (user.PlayerHasActiveSynergy("Golden Apple"))
                {
                    LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                    LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                    LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                }
            }
            else
            {
                if (user.PlayerHasActiveSynergy("Golden Apple")) user.healthHaver.ApplyHealing(100000000000000f);
                else if (user.PlayerHasActiveSynergy("Apple A Day")) user.healthHaver.ApplyHealing(3f);
                else user.healthHaver.ApplyHealing(1.5f);
                
            }
            if (user.PlayerHasActiveSynergy("Golden Apple"))
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(GoldenAppleCore.GoldenAppleCoreID).gameObject, user);
                GoldenAppleEffectHandler handler = ETGModMainBehaviour.Instance.gameObject.AddComponent<GoldenAppleEffectHandler>();
                handler.timer = 60;
                handler.target = user;
            }
            else LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(AppleCore.AppleCoreID).gameObject, user);
            foreach (PassiveItem item in user.passiveItems)
            {
                if (item.PickupObjectId == AppleCore.AppleCoreID || item.PickupObjectId == GoldenAppleCore.GoldenAppleCoreID)
                {
                    AppleCore core = item.GetComponent<AppleCore>();
                    GoldenAppleCore goldcore = item.GetComponent<GoldenAppleCore>();
                    if (user.PlayerHasActiveSynergy("Newton"))
                    {
                        if (core != null) core.givesFlight = true;
                        if (goldcore != null) goldcore.givesFlight = true;
                    }

                }
            }
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
    }
    public class GoldenAppleEffectHandler : MonoBehaviour
    {
        public PlayerController target;
        public float timer;
        private DamageTypeModifier fireImmunity;
        private StatModifier SpeedBuff;
        private StatModifier DamageBuff;
        public GoldenAppleEffectHandler()
        {
        }
        public void Start()
        {
            if (fireImmunity == null)
            {
                fireImmunity = new DamageTypeModifier();
                fireImmunity.damageMultiplier = 0;
                fireImmunity.damageType = CoreDamageTypes.Fire;
            }
            if (SpeedBuff == null)
            {
                SpeedBuff = new StatModifier();
                SpeedBuff.statToBoost = PlayerStats.StatType.MovementSpeed;
                SpeedBuff.amount = 2f;
                SpeedBuff.modifyType = StatModifier.ModifyMethod.ADDITIVE;
            }
            if (DamageBuff == null)
            {
                DamageBuff = new StatModifier();
                DamageBuff.statToBoost = PlayerStats.StatType.Damage;
                DamageBuff.amount = 1.25f;
                DamageBuff.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
            }
            if (target) GoldenEffectStart(target);
            timer = 60;
        }
        private float ParticleTimer;
        public void Update()
        {
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                if (target) GoldenEffectEnd(target);
            }
            if (ParticleTimer > 0)
            {
                ParticleTimer -= BraveTime.DeltaTime;
            }
            if (ParticleTimer <= 0)
            {
                if (GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.LOW && GameManager.Options.ShaderQuality != GameOptions.GenericHighMedLowOption.VERY_LOW && target && target.IsVisible && !target.IsFalling)
                {
                    GlobalSparksDoer.DoRandomParticleBurst(3, target.sprite.WorldBottomLeft.ToVector3ZisY(0f), target.sprite.WorldTopRight.ToVector3ZisY(0f), Vector3.up, 90f, 0.5f, null, null, null, GlobalSparksDoer.SparksType.RED_MATTER);
                }  ParticleTimer = 0.1f;
            }
        }
        public void GoldenEffectStart(PlayerController player)
        {
            player.healthHaver.damageTypeModifiers.Add(this.fireImmunity);
            player.ownerlessStatModifiers.Add(DamageBuff);
            player.ownerlessStatModifiers.Add(SpeedBuff);
            player.stats.RecalculateStats(player, false, false);
        }
        public void GoldenEffectEnd(PlayerController player)
        {
            player.healthHaver.damageTypeModifiers.Remove(this.fireImmunity);
            player.ownerlessStatModifiers.Remove(DamageBuff);
            player.ownerlessStatModifiers.Remove(SpeedBuff);
            player.stats.RecalculateStats(player, false, false);
            UnityEngine.Object.Destroy(this);
        }
    }
    public class AppleCore : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Apple Core";
            string resourceName = "NevernamedsItems/Resources/applecore_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AppleCore>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Trash";
            string longDesc = "A worthless apple core.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.PlayerBulletScale, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.EXCLUDED;
            AppleCoreID = item.PickupObjectId;
        }
        public static int AppleCoreID;
        public bool givesFlight = false;
        public override void Update()
        {
            if (Owner)
            {
                if (!Owner.IsFlying && this.givesFlight)
                {
                    Owner.SetIsFlying(true, "AppleCoreNewton", true, false);
                    Owner.AdditionalCanDodgeRollWhileFlying.AddOverride("AppleCoreNewton", null);
                }
            }
            base.Update();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }
        public override void OnDestroy()
        {
            if (Owner && givesFlight)
            {
                Owner.SetIsFlying(false, "AppleCoreNewton", true, false);
                Owner.AdditionalCanDodgeRollWhileFlying.RemoveOverride("AppleCoreNewton");
            }
            base.OnDestroy();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            if (givesFlight)
            {
                player.SetIsFlying(false, "AppleCoreNewton", true, false);
                player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("AppleCoreNewton");
            }
            return base.Drop(player);
        }
    }
    public class GoldenAppleCore : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Golden Apple Core";
            string resourceName = "NevernamedsItems/Resources/goldenapplecore_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GoldenAppleCore>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Trash?";
            string longDesc = "An apple core." + "\n\nMaybe this one isn't so worthless.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.PlayerBulletScale, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.CustomCost = 100;
            item.UsesCustomCost = true;
            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.EXCLUDED;
            GoldenAppleCoreID = item.PickupObjectId;
        }
        public static int GoldenAppleCoreID;
        public bool givesFlight = false;
        public override void Update()
        {
            if (Owner)
            {
                if (!Owner.IsFlying && this.givesFlight)
                {
                    Owner.SetIsFlying(true, "GoldenAppleCoreNewton", true, false);
                    Owner.AdditionalCanDodgeRollWhileFlying.AddOverride("GoldenAppleCoreNewton", null);
                }
            }
            base.Update();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }
        public override void OnDestroy()
        {
            if (Owner && givesFlight)
            {
                Owner.SetIsFlying(false, "GoldenAppleCoreNewton", true, false);
                Owner.AdditionalCanDodgeRollWhileFlying.RemoveOverride("GoldenAppleCoreNewton");
            }
            base.OnDestroy();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            if (givesFlight)
            {
                player.SetIsFlying(false, "GoldenAppleCoreNewton", true, false);
                player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("GoldenAppleCoreNewton");
            }
            return base.Drop(player);
        }
    }
}
