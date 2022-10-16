using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Dungeonator;
using System.Collections.ObjectModel;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using System.Collections;

namespace NevernamedsItems
{
    public class RectangularMirror : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Rectangular Mirror";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/rectangularmirror_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<RectangularMirror>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Table Reflection";
            string longDesc = "Flipped tables reflect bullets. \n\nThis artefact was used by ancient acolytes of the Tabla Sutra to study their own flips from a new perspective. In time it gained an almost mystical reputation.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;

            glint = Shader.Find("Brave/ItemSpecific/LootGlintAdditivePass");
        }
        public override void Pickup(PlayerController player)
        {
            player.OnTableFlipCompleted += TableFlipCompleted;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.OnTableFlipCompleted -= TableFlipCompleted;
            base.DisableEffect(player);
        }
        public static Shader glint;
        private void TableFlipCompleted(FlippableCover obj)
        {
            MeshRenderer component = obj.gameObject.GetComponentInChildren<MeshRenderer>();
            if (!component) return;
            Material[] sharedMaterials = component.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i].shader == glint)
                {
                    return;
                }
            }
            Array.Resize<Material>(ref sharedMaterials, sharedMaterials.Length + 1);
            Material material = new Material(glint);
            material.SetTexture("_MainTex", sharedMaterials[0].GetTexture("_MainTex"));
            sharedMaterials[sharedMaterials.Length - 1] = material;
            component.sharedMaterials = sharedMaterials;
            AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", Owner.gameObject);

            obj.specRigidbody.OnPreRigidbodyCollision += PreCollision;

            if (obj.gameObject.GetComponentInChildren<MajorBreakable>()) StartCoroutine(KillTime(obj.gameObject.GetComponentInChildren<MajorBreakable>()));
        }
        private void PreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (Owner && other && other.GetComponent<Projectile>() != null && other.GetComponent<Projectile>().ProjectilePlayerOwner() == null)
            {
                other.GetComponent<Projectile>().ReflectBullet(true, Owner, 15, true, 1, 5, 5);
                if (Owner.PlayerHasActiveSynergy("Fast Pass")) other.GetComponent<Projectile>().baseData.speed *= 2;
                    other.GetComponent<Projectile>().UpdateSpeed();
                PhysicsEngine.SkipCollision = true;
            }
        }

        private IEnumerator KillTime(MajorBreakable breaka)
        {
            yield return new WaitForSeconds(20f);
            if (breaka && !breaka.m_isBroken) breaka.Break(Vector2.zero);
            yield break;
        }
    }
}