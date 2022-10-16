using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using System.Collections;
using SaveAPI;
using Gungeon;

namespace NevernamedsItems
{
    class KalibersEye : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Kaliber's Eye";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/kaliberseye_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<KalibersEye>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "They are mine.";
            string longDesc = "Makes the Gundead your own." + "\n\nDestroy them, but do not waste them.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;

            //NPC Pools
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            KalibersEyeID = item.PickupObjectId;

            item.SetupUnlockOnCustomStat(CustomTrackedStats.CHARMED_ENEMIES_KILLED, 99, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);
            Game.Items.Rename("nn:kaliber's_eye", "nn:kalibers_eye");

        }
        public static int KalibersEyeID;
        private bool EnemyValidForKalibersEye(bool fatal, HealthHaver enemy)
        {
            if (enemy.aiActor && fatal && !enemy.IsBoss && Owner.IsInCombat && enemy.aiActor.EnemyGuid != "249db525a9464e5282d02162c88e0357")
            {
                if (!enemy.aiActor.IgnoreForRoomClear && (enemy.GetComponent<MirrorImageController>() == null))
                {
                    return true;
                }
                return false;
            }
            else return false;
        }
        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemy)
        {
            if (fatal)
            {
                if (EnemyValidForKalibersEye(fatal, enemy))
                {
                    //ETGModConsole.Log("Enemy valid");
                    try
                    {
                        float procChance;
                        if (Owner.PlayerHasActiveSynergy("All Seeing")) procChance = 1f;
                        else procChance = 0.5f;
                        if (UnityEngine.Random.value < procChance)
                        {
                            bool isJammed = enemy.aiActor.IsBlackPhantom;
                            string enemyGuid = enemy.aiActor.EnemyGuid;
                            Vector2 worldBottomLeft = enemy.sprite.WorldBottomLeft;
                            IntVector2 PosToSpawn = worldBottomLeft.ToIntVector2();
                            GameManager.Instance.StartCoroutine(this.DoEnemySpawn(enemyGuid, PosToSpawn, isJammed, (enemy.GetComponent<DisplacedImageController>() != null)));
                        }
                    }
                    catch (Exception e)
                    {
                        ETGModConsole.Log(e.Message);
                        ETGModConsole.Log(e.StackTrace);
                    }
                }
                //else ETGModConsole.Log("Enemy invalid");
            }
        }
        private IEnumerator DoEnemySpawn(string enemyGuid, IntVector2 position, bool isJammed, bool isDisplaced)
        {
            //ETGModConsole.Log("DoEnemySpawn triggered");
            yield return new WaitForSeconds(1f);
            try
            {
                if (Owner.IsInCombat)
                {

                    var enemyToSpawn = EnemyDatabase.GetOrLoadByGuid(enemyGuid);
                    Instantiate<GameObject>(EasyVFXDatabase.BloodiedScarfPoofVFX, position.ToVector3(), Quaternion.identity);
                    AIActor TargetActor = AIActor.Spawn(enemyToSpawn, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(position), true, AIActor.AwakenAnimationType.Default, true);
                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
                    CustomEnemyTagsSystem tags = TargetActor.gameObject.GetOrAddComponent<CustomEnemyTagsSystem>();
                    tags.isKalibersEyeMinion = true;
                    tags.ignoreForGoodMimic = true;

                    CompanionController orAddComponent = TargetActor.gameObject.GetOrAddComponent<CompanionController>();
                    orAddComponent.companionID = CompanionController.CompanionIdentifier.NONE;
                    orAddComponent.Initialize(Owner);

                    TargetActor.OverrideHitEnemies = true;
                    TargetActor.CollisionDamage = 0.5f;
                    TargetActor.CollisionDamageTypes |= CoreDamageTypes.Electric;

                    if (isJammed == true)
                    {
                        TargetActor.BecomeBlackPhantom();
                    }
                    CompanionisedEnemyBulletModifiers companionisedBullets = TargetActor.gameObject.GetOrAddComponent<CompanionisedEnemyBulletModifiers>();
                    companionisedBullets.jammedDamageMultiplier = 2f;
                    companionisedBullets.TintBullets = true;
                    companionisedBullets.TintColor = ExtendedColours.honeyYellow;
                    companionisedBullets.baseBulletDamage = 10f;
                    companionisedBullets.scaleSpeed = true;
                    companionisedBullets.scaleDamage = true;
                    companionisedBullets.scaleSize = false;
                    companionisedBullets.doPostProcess = false;
                    companionisedBullets.enemyOwner = Owner;

                    TargetActor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
                    AdvancedKillOnRoomClear advKill = TargetActor.gameObject.AddComponent<AdvancedKillOnRoomClear>();
                    advKill.triggersOnRoomUnseal = true;

                    if (isDisplaced)
                    {
                        DisplacedImageController displacedness = TargetActor.gameObject.AddComponent<DisplacedImageController>();
                        displacedness.Init();
                    }

                    
                    TargetActor.IsHarmlessEnemy = true;
                    TargetActor.RegisterOverrideColor(Color.grey, "Ressurection");
                    TargetActor.IgnoreForRoomClear = true;
                    if (TargetActor.gameObject.GetComponent<SpawnEnemyOnDeath>())
                    {
                        Destroy(TargetActor.gameObject.GetComponent<SpawnEnemyOnDeath>());
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
            yield break;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            }
            base.OnDestroy();

        }
    }
}