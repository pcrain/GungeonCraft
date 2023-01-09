using Alexandria.Misc;
using Dungeonator;
using MonoMod.RuntimeDetour;
using SaveAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CwaffingTheGungy
{
    public static class PlayerToolsSetup  // hooks and stuff for PlayerControllers on game start
    {
        public static Hook playerStartHook;

        public static void Init()
        {
            playerStartHook = new Hook(
                typeof(PlayerController).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(PlayerToolsSetup).GetMethod("DoSetup"));
        }
        public static void DoSetup(Action<PlayerController> action, PlayerController player)
        {
            action(player);
            if (player.GetComponent<HatController>() == null) player.gameObject.AddComponent<HatController>();
            if (player.GetComponent<PlayerToolbox>() == null) player.gameObject.AddComponent<PlayerToolbox>();
        }
    }

    class PlayerToolbox : MonoBehaviour
    {
        private PlayerController m_attachedPlayer;
        private bool isSecondaryPlayer;

        public static string enemyWithoutAFuture = "";

        private void Start()
        {
            m_attachedPlayer = base.GetComponent<PlayerController>();
            if (m_attachedPlayer)
                isSecondaryPlayer = (GameManager.Instance.SecondaryPlayer == m_attachedPlayer);
        }

        private void Update()
        {
            if (enemyWithoutAFuture != "")
            {
                ETGModConsole.Log("futureless: " + enemyWithoutAFuture);
                enemyWithoutAFuture = "";
            }
        }
    }
}
