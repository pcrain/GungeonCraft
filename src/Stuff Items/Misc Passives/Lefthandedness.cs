using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class Lefthandedness : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Lefthandedness";
            string resourceName = "NevernamedsItems/Resources/lefthandedness_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Lefthandedness>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Shell'tan's Sign";
            string longDesc = "Empowers bullets when firing to the left." + "\n\nFor some unknown reason, left-handed people are statistically better at all known branches of Ammomancy.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;
        }
        private void PostProcessProj(Projectile bullet, float num)
        {
            if (Owner && bullet && Owner.SpriteFlipped)
            {
                bullet.baseData.damage *= 1.3f;
                bullet.RuntimeUpdateScale(1.2f);
                if (Owner.PlayerHasActiveSynergy("Sinister Handed"))
                {
                    AdvancedTransmogrifyBehaviour transmog = bullet.gameObject.GetOrAddComponent<AdvancedTransmogrifyBehaviour>();
                    AdvancedTransmogrifyBehaviour.TransmogData newData = new AdvancedTransmogrifyBehaviour.TransmogData()
                    {
                        TargetGuids =  new List<string>() { "76bc43539fc24648bff4568c75c686d1" },
                        TransmogChance = 0.06f,
                        identifier = "SinisterHanded",
                    };
                    transmog.TransmogDataList.Add(newData);
                }
            }
        }
        private void ProcessBeam(BeamController bem)
        {
            if (Owner && bem)
            {
                GameObject bemObj = bem.gameObject;
                Projectile bemPrj = bemObj.GetComponent<Projectile>();
                BeamIsBenefittingFromLeftHand beamBuffed = bemObj.GetComponent<BeamIsBenefittingFromLeftHand>();
                if (beamBuffed && !Owner.SpriteFlipped)
                {
                    bemPrj.baseData.damage /= 1.3f;
                    UnityEngine.Object.Destroy(beamBuffed);
                }
                else if (!beamBuffed && Owner.SpriteFlipped)
                {
                    bemPrj.baseData.damage *= 1.3f;
                    bemObj.AddComponent<BeamIsBenefittingFromLeftHand>();
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += PostProcessProj;
            player.PostProcessBeamChanceTick += ProcessBeam;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= PostProcessProj;
            player.PostProcessBeamChanceTick -= ProcessBeam;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= PostProcessProj;
                Owner.PostProcessBeamChanceTick -= ProcessBeam;
            }
            base.OnDestroy();
        }
        class BeamIsBenefittingFromLeftHand : MonoBehaviour { }
    }

}

