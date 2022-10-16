using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;
using UnityEngine;
using Gungeon;
using System.Collections;
using UnityEngine.Events;

namespace NevernamedsItems
{
    class PassiveTestingItem : PassiveItem
    {
        public static void Init()
        {
            string itemName = "PassiveTestingItem";
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";
            GameObject obj = new GameObject();
            var item = obj.AddComponent<PassiveTestingItem>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);


            string shortDesc = "wip";
            string longDesc = "Did you seriously give yourself a testing item just to read the flavour text?";


            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.EXCLUDED;
            item.sprite.IsPerpendicular = true;
            item.CanBeDropped = true;
            item.CanBeSold = true;
            DebugPassiveID = item.PickupObjectId;

           
        }
        public static int DebugPassiveID;
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            /* if (!bullet.GetComponent<HasBeenDoubleProcessed>())
             {
                 StartCoroutine(doLateFrameProcessing(bullet));
                 bullet.gameObject.AddComponent<HasBeenDoubleProcessed>();
             }*/
            if (bullet.sprite) bullet.sprite.renderer.enabled = false;
            bullet.baseData.range = 5;

        }
        private IEnumerator doLateFrameProcessing(Projectile projectile)
        {
            yield return null;
            //if (projectile.ProjectilePlayerOwner()) projectile.ProjectilePlayerOwner().DoPostProcessProjectile(projectile);
            yield break;
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam.GetComponent<BeamSplittingModifier>())
            {
                ETGModConsole.Log("Split found");
                beam.GetComponent<BeamSplittingModifier>().amtToSplitTo++;
            }
        }
        private void OnRoll(PlayerController player, Vector2 vec)
        {
            ETGModConsole.Log(player.specRigidbody.UnitCenter.ToString());
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            //player.PostProcessThrownGun += PostProcessGun;
            // player.OnRollStarted += OnRoll;
            // player.PostProcessBeam += this.PostProcessBeam;
            base.Pickup(player);
        }
        private void PostProcessGun(Projectile fucker)
        {
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessThrownGun -= PostProcessGun;
            player.PostProcessProjectile -= this.onFired;
            // player.PostProcessBeam -= this.PostProcessBeam;

            DebrisObject result = base.Drop(player);
            return result;
        }
        public class HasBeenDoubleProcessed : MonoBehaviour { }

    }
    public class JamPlayerBulletModifier : MonoBehaviour
    {
        private void Awake()
        {
            this.m_projectile = base.GetComponent<Projectile>();
        }
        private void Update()
        {
            if (!m_projectile.IsBlackBullet)
            {
                m_projectile.BecomeBlackBullet();
            }
        }
        private Projectile m_projectile;
    }
}