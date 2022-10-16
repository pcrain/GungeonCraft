using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using Alexandria.Misc;
using UnityEngine;

namespace NevernamedsItems
{
    public class SculptorsChisel : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Sculptors Chisel";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/sculptorschisel_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<SculptorsChisel>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "A Real Boy";
            string longDesc = "Chance to sculpt flipped tables into immaculate decoys." + "\n\nThe chisel of an ancient sculptor who came to the gungeon in search of a way to cure his petrified beloved.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.C;
            ID = item.PickupObjectId;
        }
        public static int ID;
        public override void Pickup(PlayerController player)
        {
            player.OnTableFlipCompleted += OnFlip;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.OnTableFlipCompleted -= OnFlip;
            base.DisableEffect(player);
        }
        public void OnFlip(FlippableCover table)
        {
            if (table && table.gameObject)
            {
                if (UnityEngine.Random.value <= 0.25f) StartCoroutine(BecomeDecoy(table));
            }
        }
        private IEnumerator BecomeDecoy(FlippableCover table)
        {
            yield return new WaitForSeconds(0.1f);
            if (table && Owner)
            {
                SpawnManager.SpawnVFX((PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX, table.specRigidbody.UnitCenter, Quaternion.identity);
                GameObject decoy = UnityEngine.Object.Instantiate<GameObject>(PickupObjectDatabase.GetById(71).GetComponent<SpawnObjectPlayerItem>().objectToSpawn.gameObject, table.sprite.WorldBottomCenter, Quaternion.identity);
                tk2dBaseSprite decoySprite = decoy.GetComponent<tk2dBaseSprite>();
                if (decoySprite) { decoySprite.PlaceAtPositionByAnchor(table.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter); }
                AkSoundEngine.PostEvent("Play_ITM_Folding_Table_Use_01", Owner.gameObject);
                UnityEngine.Object.Destroy(table.gameObject);

                MeshRenderer component = decoy.gameObject.GetComponentInChildren<MeshRenderer>();
                if (Owner.PlayerHasActiveSynergy("Sterling") && component)
                {
                    Material[] sharedMaterials = component.sharedMaterials;
                    bool skip = false;
                    for (int i = 0; i < sharedMaterials.Length; i++)
                    {
                        if (sharedMaterials[i].shader == RectangularMirror.glint) skip = true;
                    }
                    if (!skip)
                    {
                        Array.Resize<Material>(ref sharedMaterials, sharedMaterials.Length + 1);
                        Material material = new Material(RectangularMirror.glint);
                        material.SetTexture("_MainTex", sharedMaterials[0].GetTexture("_MainTex"));
                        sharedMaterials[sharedMaterials.Length - 1] = material;
                        component.sharedMaterials = sharedMaterials;
                    }
                    if (decoy.GetComponent<SpeculativeRigidbody>()) decoy.GetComponent<SpeculativeRigidbody>().OnPreRigidbodyCollision += PreCollision;
                    if (decoy.gameObject.GetComponentInChildren<MajorBreakable>()) StartCoroutine(KillTime(decoy.gameObject.GetComponentInChildren<MajorBreakable>()));

                }
            }
            yield break;
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
            yield return new WaitForSeconds(8f);
            if (breaka && !breaka.m_isBroken) breaka.Break(Vector2.zero);
            yield break;
        }
    }
}
