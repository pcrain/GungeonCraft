using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using Alexandria.ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class EargesplittenLoudenboomerRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Eargesplitten Loudenboomers";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/loudenboomer_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<EargesplittenLoudenboomerRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Big Cat";
            string longDesc = "These whimsical rounds were designed to pack a big punch." + "\n\nTheir sonic boom is capable of pushing foes away and stunning them.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.15f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.25f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            EargesplittenLoudenboomerRoundsID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_LOUDENBOOMER, true);
            item.AddItemToTrorcMetaShop(50);
        }
        public static int EargesplittenLoudenboomerRoundsID;
        private void PostProcessProj(Projectile proj, float flot)
        {
            StartCoroutine(KnockbackDoer(proj, flot));
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.projectile)
            {
                Exploder.DoRadialKnockback(beam.Origin, 40, 7);
                if (beam.projectile && beam.projectile.transform.position.GetAbsoluteRoom() != null)
                {
                    List<AIActor> activeEnemies = beam.projectile.transform.position.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    if (activeEnemies != null && activeEnemies.Count > 0)
                    {
                        for (int i = 0; i < activeEnemies.Count; i++)
                        {
                            AIActor aiactor = activeEnemies[i];
                            if (aiactor.IsNormalEnemy && aiactor.behaviorSpeculator)
                            {
                                if (Vector2.Distance(beam.Origin, aiactor.sprite.WorldCenter) < 3)
                                {
                                    if (UnityEngine.Random.value <= (0.25f * 2))
                                    {
                                        aiactor.behaviorSpeculator.Stun(1);
                                    }
                                }
                                else if (Vector2.Distance(beam.Origin, aiactor.sprite.WorldCenter) < 6)
                                {
                                    if (UnityEngine.Random.value <= (0.07f * 2))
                                    {
                                        aiactor.behaviorSpeculator.Stun(1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private IEnumerator KnockbackDoer(Projectile proj, float flot)
        {
            yield return null;
            Exploder.DoRadialKnockback(proj.specRigidbody.UnitCenter, (20 * flot), 7);
            if (proj.LastPosition.GetAbsoluteRoom() != null)
            {
                List<AIActor> activeEnemies = proj.LastPosition.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null && activeEnemies.Count >0)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy && aiactor.behaviorSpeculator)
                        {
                            if (Vector2.Distance(proj.specRigidbody.UnitCenter, aiactor.sprite.WorldCenter) < 3)
                            {
                                if (UnityEngine.Random.value <= (0.25f * flot))
                                {
                                    aiactor.behaviorSpeculator.Stun(1);
                                }
                            }
                            else if (Vector2.Distance(proj.specRigidbody.UnitCenter, aiactor.sprite.WorldCenter) < 6)
                            {
                                if (UnityEngine.Random.value <= (0.07f * flot))
                                {
                                    aiactor.behaviorSpeculator.Stun(1);
                                }
                            }
                        }
                    }
                }
            }
            yield break;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += PostProcessProj;
            player.PostProcessBeamChanceTick += PostProcessBeam;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessBeamChanceTick -= PostProcessBeam;
            player.PostProcessProjectile -= PostProcessProj;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner != null)
            {
                Owner.PostProcessProjectile -= PostProcessProj;
                Owner.PostProcessBeamChanceTick += PostProcessBeam;
            }
            base.OnDestroy();
        }
    }
}
