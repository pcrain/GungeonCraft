using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.ItemAPI;
using UnityEngine;
using SaveAPI;
using System.Collections;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    public class WitchsBrew : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Witches Brew";
            string resourceName = "NevernamedsItems/Resources/witchsbrew_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<WitchsBrew>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Fire Burn and Cauldron Bubble";
            string longDesc = "Doubles all enemies, but makes them much weaker." + "\n\nNot suitable for vegans or vegetarians.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.CHALLENGE_TOILANDTROUBLE_BEATEN, true);
        }
        public void AIActorMods(AIActor target)
        {
            if (target && target.healthHaver && !target.healthHaver.IsBoss && !target.healthHaver.IsSubboss && !target.IsSecretlyTheMineFlayer())
            {
                if (target.GetComponent<CompanionController>() == null && target.GetComponent<HasBeenDoubledByWitchsBrew>() == null && target.GetComponent<DisplacedImageController>() == null && target.GetComponent<MirrorImageController>() == null)
                {
                         if (target.EnemyGuid != "22fc2c2c45fb47cf9fb5f7b043a70122")
                    {
                        StartCoroutine(ToilEnemyDupe(target));
                    }         
                }
            }
        }
        private IEnumerator ToilEnemyDupe(AIActor AIActor)
        {
            yield return null;

            string guid = AIActor.EnemyGuid;
            var enemyPrefab = EnemyDatabase.GetOrLoadByGuid(guid);
            AIActor aiactor = AIActor.Spawn(enemyPrefab, AIActor.gameActor.CenterPosition.ToIntVector2(VectorConversions.Floor), AIActor.GetAbsoluteParentRoom(), true, AIActor.AwakenAnimationType.Default, true);

            HasBeenDoubledByWitchsBrew challengitude = aiactor.gameObject.AddComponent<HasBeenDoubledByWitchsBrew>();
            challengitude.linkedOther = AIActor;
            HasBeenDoubledByWitchsBrew challengitude2 = AIActor.gameObject.AddComponent<HasBeenDoubledByWitchsBrew>();
            challengitude2.linkedOther = aiactor;
            aiactor.procedurallyOutlined = true;
            AIActor.procedurallyOutlined = true;
            aiactor.IsWorthShootingAt = AIActor.IsWorthShootingAt;
            aiactor.IgnoreForRoomClear = AIActor.IgnoreForRoomClear;
            aiactor.AssignedCurrencyToDrop = AIActor.AssignedCurrencyToDrop;
            aiactor.AdditionalSafeItemDrops = AIActor.AdditionalSafeItemDrops;
            aiactor.AdditionalSimpleItemDrops = AIActor.AdditionalSimpleItemDrops;
            aiactor.CanTargetEnemies = AIActor.CanTargetEnemies;
            aiactor.CanTargetPlayers = AIActor.CanTargetPlayers;
            if (AIActor.IsInReinforcementLayer) aiactor.HandleReinforcementFallIntoRoom(0f);
            if (AIActor.GetComponent<KillOnRoomClear>() != null)
            {
                KillOnRoomClear kill = aiactor.gameObject.GetOrAddComponent<KillOnRoomClear>();
                kill.overrideDeathAnim = AIActor.GetComponent<KillOnRoomClear>().overrideDeathAnim;
                kill.preventExplodeOnDeath = AIActor.GetComponent<KillOnRoomClear>().preventExplodeOnDeath;
            }
            if (aiactor.EnemyGuid == "249db525a9464e5282d02162c88e0357")
            {
                if (aiactor.gameObject.GetComponent<SpawnEnemyOnDeath>())
                {
                    UnityEngine.Object.Destroy(aiactor.gameObject.GetComponent<SpawnEnemyOnDeath>());
                }
            }
            else if (aiactor.HasTag("mimic"))
            {
                if (AIActor.AdditionalSafeItemDrops != null && aiactor.AdditionalSafeItemDrops != null)
                {
                    List<PickupObject> newDrops = new List<PickupObject>();
                    PickupObject.ItemQuality qual = PickupObject.ItemQuality.D;
                    int itemsToReAdd = 0;
                    for (int i = (aiactor.AdditionalSafeItemDrops.Count - 1); i >= 0; i--)
                    {
                        if (!BabyGoodChanceKin.lootIDlist.Contains(aiactor.AdditionalSafeItemDrops[i].PickupObjectId))
                        {
                            qual = aiactor.AdditionalSafeItemDrops[i].quality;
                            itemsToReAdd++;
                        }
                        else
                        {
                            newDrops.Add(PickupObjectDatabase.GetById(aiactor.AdditionalSafeItemDrops[i].PickupObjectId));
                        }
                    }
                    if (itemsToReAdd > 0)
                    {
                        for (int i = 0; i < itemsToReAdd; i++)
                        {
                            PickupObject item = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(qual, null, false);
                            if (UnityEngine.Random.value <= 0.5f) item = LootEngine.GetItemOfTypeAndQuality<Gun>(qual, null, false);
                            newDrops.Add(item);
                        }
                        aiactor.AdditionalSafeItemDrops = newDrops;
                    }
                }
            }


            GameManager.Instance.StartCoroutine(Shrimk(aiactor));
            GameManager.Instance.StartCoroutine(Shrimk(AIActor));

            aiactor.specRigidbody.Reinitialize();
            yield break;
        }
        private IEnumerator Shrimk(AIActor actor)
        {
            while (!actor.HasBeenEngaged || !actor.HasBeenAwoken) { yield return null; }
            int cachedLayer = actor.gameObject.layer;
            actor.gameObject.layer = LayerMask.NameToLayer("Unpixelated");
            int cachedOutlineLayer = SpriteOutlineManager.ChangeOutlineLayer(actor.sprite, LayerMask.NameToLayer("Unpixelated"));
            actor.EnemyScale = TargetScale;
            actor.gameObject.layer = cachedLayer;
            SpriteOutlineManager.ChangeOutlineLayer(actor.sprite, cachedOutlineLayer);
            ModifyHP(actor.healthHaver);
            yield break;
        }
        private void ModifyHP(HealthHaver hp)
        {
            float startMax = hp.GetMaxHealth();
            hp.SetHealthMaximum(startMax * 0.25f);
            hp.ForceSetCurrentHealth(startMax * 0.25f);
        }
        public static Vector2 TargetScale = new Vector2(0.75f, 0.75f);
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            ETGMod.AIActor.OnPreStart += AIActorMods;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            ETGMod.AIActor.OnPreStart -= AIActorMods;
            base.OnDestroy();
        }
        public class HasBeenDoubledByWitchsBrew : MonoBehaviour
        {
            public HasBeenDoubledByWitchsBrew()
            {
                linkedOther = null;
            }
            private void Start()
            {
                self = base.GetComponent<AIActor>();
                if (self && self.specRigidbody)
                {
                    self.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollide;
                }
            }
            private void OnPreCollide(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
            {
                if (linkedOther != null & linkedOther.specRigidbody != null)
                {
                    if (otherRigidbody == linkedOther.specRigidbody)
                    {
                        PhysicsEngine.SkipCollision = true;
                    }
                }
            }
            private AIActor self;
            public AIActor linkedOther;
        }
    }
}