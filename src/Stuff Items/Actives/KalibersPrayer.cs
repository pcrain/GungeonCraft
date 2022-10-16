using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class KalibersPrayer : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Kalibers Prayer";
            string resourceName = "NevernamedsItems/Resources/kalibersprayer_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<KalibersPrayer>();


            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Book of Secrets";
            string longDesc = "Though the most commonly known prayer to Kaliber is the famous Dodge Roll, this tome is filled with lesser rites to briefly grant Kaliber's protection in battle." + "\n\nIt is mandated that every adult Gun Cultist carry a copy of this holy tome, though not all can read it's strange spiralling text.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 8);

            item.consumable = false;
            item.quality = ItemQuality.D;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.GUNCULTIST_QUEST_REWARDED, true);
        }

        public override void DoEffect(PlayerController user)
        {
            float incorporealitySeconds = 2;
            if (user.PlayerHasActiveSynergy("Gunsignor") && UnityEngine.Random.value <= 0.1) incorporealitySeconds = 8f;
            if (user.PlayerHasActiveSynergy("Liturgist")) incorporealitySeconds *= 2f;
            if (user.PlayerHasActiveSynergy("Rod of Iron")) user.StartCoroutine(HandleShield(user, incorporealitySeconds));
            else user.StartCoroutine(this.IncorporealityOnHit(user, incorporealitySeconds));
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
        private IEnumerator IncorporealityOnHit(PlayerController player, float incorporealityTime)
        {
            int enemyMask = CollisionMask.LayerToMask(CollisionLayer.EnemyCollider, CollisionLayer.EnemyHitBox, CollisionLayer.Projectile);
            player.specRigidbody.AddCollisionLayerIgnoreOverride(enemyMask);
            player.healthHaver.IsVulnerable = false;
            yield return null;
            float timer = 0f;
            float subtimer = 0f;
            while (timer < incorporealityTime)
            {
                while (timer < incorporealityTime)
                {
                    timer += BraveTime.DeltaTime;
                    subtimer += BraveTime.DeltaTime;
                    if (subtimer > 0.12f)
                    {
                        player.IsVisible = false;
                        subtimer -= 0.12f;
                        break;
                    }
                    yield return null;
                }
                while (timer < incorporealityTime)
                {
                    timer += BraveTime.DeltaTime;
                    subtimer += BraveTime.DeltaTime;
                    if (subtimer > 0.12f)
                    {
                        player.IsVisible = true;
                        subtimer -= 0.12f;
                        break;
                    }
                    yield return null;
                }
            }
            this.EndIncorporealityOnHit(player);
            yield break;
        }
        private void EndIncorporealityOnHit(PlayerController player)
        {
            int mask = CollisionMask.LayerToMask(CollisionLayer.EnemyCollider, CollisionLayer.EnemyHitBox, CollisionLayer.Projectile);
            player.IsVisible = true;
            player.healthHaver.IsVulnerable = true;
            player.specRigidbody.RemoveCollisionLayerIgnoreOverride(mask);
        }
        bool m_usedOverrideMaterial;
        private IEnumerator HandleShield(PlayerController user, float duration)
        {
            m_usedOverrideMaterial = user.sprite.usesOverrideMaterial;
            user.sprite.usesOverrideMaterial = true;
            user.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = user.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            user.healthHaver.IsVulnerable = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                user.healthHaver.IsVulnerable = false;
                yield return null;
            }
            if (user)
            {
                user.healthHaver.IsVulnerable = true;
                user.ClearOverrideShader();
                user.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
                SpeculativeRigidbody specRigidbody2 = user.specRigidbody;
                specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
                //IsCurrentlyActive = false;
            }
            if (this)
            {
                AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
            }
            yield break;
        }
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, LastOwner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }
    }
}