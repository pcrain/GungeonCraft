using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Albedo : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Albedo";
            string resourceName = "NevernamedsItems/Resources/albedo_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Albedo>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Clarity";
            string longDesc = "Speeds up Glass Guon Stones." + "\n\nThe second phase of the prime materia's transition into the Philosopher's Stone, where the murky darkness of the Nigredo is purified into a lunarily charged clarity.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
            AlbedoID = item.PickupObjectId;
        }
        public static int AlbedoID;
        private float lastOrbitals;
        private bool hadSynergyLastChecked;
        public override void Update()
        {
            base.Update();
            if (Owner && Owner.orbitals.Count >= 0)
            {
                float currentOrbitals = Owner.orbitals.Count();
                if (currentOrbitals != lastOrbitals)
                {
                    if (currentOrbitals > 0) UpdateOrbitals();
                    lastOrbitals = currentOrbitals;
                }
                if (hadSynergyLastChecked != Owner.PlayerHasActiveSynergy("White Ethesia"))
                {
                    if (currentOrbitals > 0) UpdateOrbitals();
                    hadSynergyLastChecked = Owner.PlayerHasActiveSynergy("White Ethesia");
                }
            }
        }
        private void UpdateOrbitals()
        {
            foreach (var o in Owner.orbitals)
            {
                var orbital = (PlayerOrbital)o;
                if (orbital.name == "IounStone_Glass(Clone)")
                {
                    if (orbital.gameObject.GetComponent<BoostedByAlbedo>() == null)
                    {
                        int mult = 3;
                        if (Owner.PlayerHasActiveSynergy("White Ethesia")) mult = 4;
                        BoostedByAlbedo boost = orbital.gameObject.AddComponent<BoostedByAlbedo>();
                        boost.currentMultiplier = mult;
                        boost.storedOrbitalTier = orbital.GetOrbitalTier();
                        orbital.orbitDegreesPerSecond *= mult;
                        orbital.SetOrbitalTier(1010);
                        orbital.SetOrbitalTierIndex(PlayerOrbital.GetNumberOfOrbitalsInTier(Owner, 1010));
                    }
                    else if (orbital.gameObject.GetComponent<BoostedByAlbedo>().currentMultiplier == 3 && Owner.PlayerHasActiveSynergy("White Ethesia"))
                    {
                        orbital.orbitDegreesPerSecond /= 3;
                        orbital.orbitDegreesPerSecond *= 4;
                        orbital.gameObject.GetComponent<BoostedByAlbedo>().currentMultiplier = 4;
                        orbital.SetOrbitalTier(1010);
                        orbital.SetOrbitalTierIndex(PlayerOrbital.GetNumberOfOrbitalsInTier(Owner, 1010));
                    }
                    else if (orbital.gameObject.GetComponent<BoostedByAlbedo>().currentMultiplier == 4 && !Owner.PlayerHasActiveSynergy("White Ethesia"))
                    {
                        orbital.orbitDegreesPerSecond /= 4;
                        orbital.orbitDegreesPerSecond *= 3;
                        orbital.gameObject.GetComponent<BoostedByAlbedo>().currentMultiplier = 3;
                        orbital.SetOrbitalTier(1010);
                        orbital.SetOrbitalTierIndex(PlayerOrbital.GetNumberOfOrbitalsInTier(Owner, 1010));
                    }
                }
            }
            RecalcOrbIndex();
        }
        private void ResetOrbitals(PlayerController player)
        {
            if (player.orbitals != null && player.orbitals.Count > 0)
            {
                foreach (var o in Owner.orbitals)
                {
                    var orbital = (PlayerOrbital)o;
                    if (orbital.gameObject.GetComponent<BoostedByAlbedo>() != null)
                    {
                        orbital.orbitDegreesPerSecond /= orbital.gameObject.GetComponent<BoostedByAlbedo>().currentMultiplier;
                        orbital.SetOrbitalTier(PlayerOrbital.CalculateTargetTier(player, o));
                        orbital.SetOrbitalTierIndex(PlayerOrbital.GetNumberOfOrbitalsInTier(player, orbital.gameObject.GetComponent<BoostedByAlbedo>().storedOrbitalTier));
                        UnityEngine.Object.Destroy(orbital.gameObject.GetComponent<BoostedByAlbedo>());
                    }
                }
                RecalcOrbIndex();
            }
        }
        private void RecalcOrbIndex()
        {
            Owner.RecalculateOrbitals();
        }
        public override void Pickup(PlayerController player)
        {
            if (!m_pickedUpThisRun)
            {
                for (int i = 0; i < 3; i++)
                {
                    player.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);
                }
            }

            base.Pickup(player);
            UpdateOrbitals();
            GameManager.Instance.OnNewLevelFullyLoaded += this.NewFloor;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewFloor;
            if (!(player.GetNumberOfItemInInventory(Albedo.AlbedoID) > 1)) ResetOrbitals(player);
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.NewFloor;
            if (Owner)
            {
                ResetOrbitals(Owner);
            }
            base.OnDestroy();
        }
        private void NewFloor()
        {
            if (Owner)
            {
                Owner.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);
                ResetOrbitals(Owner);
            }
        }
        class BoostedByAlbedo : MonoBehaviour { public int currentMultiplier; public int storedOrbitalTier; }
    }
}
