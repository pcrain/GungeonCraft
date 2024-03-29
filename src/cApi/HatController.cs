using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using System.Reflection;
using Alexandria.ItemAPI;
using System.Collections;
using System.Globalization;

namespace Alexandria.cAPI
{
    public class HatController : MonoBehaviour
    {
        /// <summary>Ensure any created PlayerControllers also have a HatController</summary>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Start))]
        private class EnsureHatControllerPatch
        {
            static void Postfix(PlayerController __instance)
            {
              __instance.gameObject.GetOrAddComponent<HatController>();
            }
        }

        public Hat CurrentHat { get; private set; }
        private GameObject m_extantHatObject;
        private PlayerController m_WearingPlayer;

        private void Start()
        {
            m_WearingPlayer = base.GetComponent<PlayerController>();
            if (!m_WearingPlayer)
                return; // no player means nothing to add a hat to
            if (!Hatabase.StoredHats.TryGetValue(m_WearingPlayer.name, out string storedHatName) || storedHatName == null)
            {
                if (CurrentHat != null)
                    RemoveCurrentHat(); //Removes the current hat if we don't have a stored one
                return;
            }
            if (CurrentHat != null && storedHatName == CurrentHat.hatName)
                return; // hat hasn't changed, nothing to do
            if (CurrentHat != null)
                RemoveCurrentHat(); //Removes the current hat if it's not equal to the stored one
            if (Hatabase.Hats.TryGetValue(storedHatName, out Hat hat))
                SetHat(hat);
        }

        public void SetHat(Hat hat)
        {
            if (m_extantHatObject)
                UnityEngine.Object.Destroy(m_extantHatObject); //Makes sure we're not trying to add a hat where one already exists.
            m_extantHatObject = UnityEngine.Object.Instantiate(hat.gameObject);
            m_extantHatObject.SetActive(true);
            CurrentHat = m_extantHatObject.GetComponent<Hat>();
            CurrentHat.StickHatToPlayer(m_WearingPlayer);
            Hatabase.StoredHats[m_WearingPlayer.name] = CurrentHat.hatName;
        }

        public void RemoveCurrentHat()
        {
            UnityEngine.Object.Destroy(m_extantHatObject);
            Hatabase.StoredHats[m_WearingPlayer.name] = null;
            m_extantHatObject = null;
            CurrentHat = null;
        }
    }
}
