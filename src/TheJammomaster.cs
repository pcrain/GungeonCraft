using System;
using System.Collections.Generic;
using Gungeon;
using GungeonAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    // Token: 0x02000009 RID: 9
    public static class TheJammomaster
    {
        // Token: 0x0600004D RID: 77 RVA: 0x00004700 File Offset: 0x00002900
        public static void Add()
        {
            ShrineFactory shrineFactory = new ShrineFactory
            {

                name = "The Jammomaster",
                modID = "nevernamedsMod",
                //text = "Might not be smart to stick around.",
                spritePath = "NevernamedsItems/Resources/JammomasterSprites/jammomaster_idle_001.png",
                shadowSpritePath = "NevernamedsItems/Resources/JammomasterSprites/jammomaster_shadow_001.png",
                acceptText = "I Accept",
                declineText = "...no thanks",
                OnAccept = new Action<PlayerController, GameObject>(TheJammomaster.Accept),
                OnDecline = null,
                CanUse = new Func<PlayerController, GameObject, bool>(TheJammomaster.CanUse),
                //offset = new Vector3(43.8f, 42.4f, 42.9f),
                offset = new Vector3(51.2f, 50.8f, 51.3f),
                talkPointOffset = new Vector3(0.75f, 1.5f, 0f),
                isToggle = false,
                isBreachShrine = true,
                interactableComponent = typeof(JammomasterInteractible)
            };
            GameObject gameObject = shrineFactory.Build();
            gameObject.AddAnimation("idle", "NevernamedsItems/Resources/JammomasterSprites/jammomaster_idle", 12, NPCBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("talk", "NevernamedsItems/Resources/JammomasterSprites/jammomaster_talk", 12, NPCBuilder.AnimationType.Talk, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("talk_start", "NevernamedsItems/Resources/JammomasterSprites/jammomaster_talk", 12, NPCBuilder.AnimationType.Other, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("do_effect", "NevernamedsItems/Resources/JammomasterSprites/jammomaster_talk", 12, NPCBuilder.AnimationType.Other, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            JammomasterInteractible component = gameObject.GetComponent<JammomasterInteractible>();
            component.conversation = new List<string>
            {
                "All things wise and wonderful...",
                "All creatures great and small...",
                "All things bright and beautiful...",
                "I've cursed them, one and all..."
            };
            component.conversation2 = new List<string>
            {
                "What?....",
                "What do you desire now, young one?"
            };
            component.declineText2 = "I am content";
            component.acceptText2 = "Please undo what is done...";
            //gameObject.SetActive(false);
        }

        private static bool CanUse(PlayerController player, GameObject npc)
        {
            return true;
        }

        public static void Accept(PlayerController player, GameObject npc)
        {
            //ETGModConsole.Log("Acceptance began");
            if (AllJammedState.AllJammedActive)
            {
                npc.GetComponent<tk2dSpriteAnimator>().PlayForDuration("doEffect", -2f, "idle", false);
                string header = "All-Jammed Mode Disabled";
                string text = "";
                Notify(header, text);
                SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_CONSOLE, false);
                SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_GENUINE, false);
            }
            else
            {
                npc.GetComponent<tk2dSpriteAnimator>().PlayForDuration("doEffect", -2f, "idle", false);
                string header = "All-Jammed Mode Enabled";
                string text = "";
                Notify(header, text);
                SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_CONSOLE, false);
                SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_GENUINE, true);
            }
            //ETGModConsole.Log("Acceptance didn't break");
        }
        private static void Notify(string header, string text)
        {
            tk2dSpriteCollectionData encounterIconCollection = AmmonomiconController.Instance.EncounterIconCollection;
            int spriteIdByName = encounterIconCollection.GetSpriteIdByName("NevernamedsItems/Resources/JammomasterSprites/alljammedmode_icon");
            GameUIRoot.Instance.notificationController.DoCustomNotification(header, text, null, spriteIdByName, UINotificationController.NotificationColor.PURPLE, false, true);
        }
        // Token: 0x0600004F RID: 79 RVA: 0x00004810 File Offset: 0x00002A10

    }
}
