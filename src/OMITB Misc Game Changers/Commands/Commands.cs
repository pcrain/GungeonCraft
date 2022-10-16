using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;

namespace NevernamedsItems
{
    public class Commands
    {
        public static bool allJammedState;
        public static bool itemsHaveBeenRarityBoosted;
        public static void Init()
        {
            ETGModConsole.Commands.AddGroup("nn", delegate (string[] args)
            {
                ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
            });
            ETGModConsole.Commands.GetGroup("nn").AddUnit("help", delegate (string[] args)
            {
                ETGModConsole.Log("<size=100><color=#ff0000ff>List of Commands</color></size>", false);
                ETGModConsole.Log("<size=100><color=#ff0000ff>-------------------</color></size>", false);
                ETGModConsole.Log("<color=#ff0000ff>checkunlocks</color> - Lists what OMITB unlocks you have yet to achieve, with explanations for each one.", false);
                ETGModConsole.Log("<color=#ff0000ff>togglealljammed</color> - Turns on and off All-Jammed mode.", false);
                // ETGModConsole.Log("<color=#ff0000ff>togglelooping</color> - Turns on and off the experimental looping mode.", false);
                ETGModConsole.Log("<color=#ff0000ff>togglerarityboost</color> - Greatly increases the loot weight of modded items, making them show up much more.", false);
                ETGModConsole.Log("<color=#ff0000ff>roomdata</color> - Displays data about the current room (currently only it's name)", false);
                
            });
            ETGModConsole.Commands.GetGroup("nn").AddUnit("togglealljammed", delegate (string[] args)
            {
                if (AllJammedState.AllJammedActive == true)
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_CONSOLE, false);
                    SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_GENUINE, false);
                    ETGModConsole.Log("All-Jammed Mode has been disabed.");
                }
                else
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_CONSOLE, true);
                    SaveAPIManager.SetFlag(CustomDungeonFlags.ALLJAMMEDMODE_ENABLED_GENUINE, false);
                    ETGModConsole.Log("All-Jammed Mode has been enabled.");
                }
            });
            ETGModConsole.Commands.GetGroup("nn").AddUnit("togglecurses", delegate (string[] args)
            {
                if (AllJammedState.AllJammedActive == true)
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.CURSES_DISABLED, true);
                    ETGModConsole.Log("Y'know what sucks? Putting time and effort into something only for everyone to just disable it and tell you how much they hate it.");
                    ETGModConsole.Log("Yeah this is a petty message, but consider it developer venting or something.");
                    ETGModConsole.Log("Anyways... curses have been disabed.");
                }
                else
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.CURSES_DISABLED, false);
                    ETGModConsole.Log("Curses have been enabled.   ...thanks.");

                }
            });
           
            ETGModConsole.Commands.GetGroup("nn").AddUnit("roomdata", delegate (string[] args)
            {
                PlayerController player = GameManager.Instance.PrimaryPlayer;
                var roomName = player.CurrentRoom.GetRoomName();
                ETGModConsole.Log("<color=#ff0000ff>Room Name: </color>" + roomName);
            });
            ETGModConsole.Commands.GetGroup("nn").AddUnit("togglerarityboost", delegate (string[] args)
            {
                if (itemsHaveBeenRarityBoosted)
                {
                    ETGModConsole.Log("The loot weight of modded items and guns has been reset to normal.", false);
                    foreach (WeightedGameObject obj in GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.elements)
                    {
                        if (obj.pickupId > 823 || obj.pickupId < 0)
                        {
                            obj.weight /= 100;
                        }
                    }
                    foreach (WeightedGameObject obj in GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.elements)
                    {
                        if (obj.pickupId > 823 || obj.pickupId < 0)
                        {
                            obj.weight /= 100;
                        }
                    }
                    itemsHaveBeenRarityBoosted = false;
                }
                else
                {
                    ETGModConsole.Log("The loot weight of modded items and guns has been GREATLY increased.", false);
                    foreach (WeightedGameObject obj in GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.elements)
                    {
                        if (obj.pickupId > 823 || obj.pickupId < 0)
                        {
                            float initialWeight = obj.weight;
                            obj.weight *= 100;

                            string displayName = "ERROR DISPLAY NAME NULL";
                            if (PickupObjectDatabase.GetById(obj.pickupId) != null) displayName = (PickupObjectDatabase.GetById(obj.pickupId) as Gun).DisplayName;
                            ETGModConsole.Log(displayName + " (" + obj.pickupId + "): " + initialWeight + " ---> " + obj.weight);

                        }
                    }
                    foreach (WeightedGameObject obj in GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.elements)
                    {
                        if (obj.pickupId > 823 || obj.pickupId < 0)
                        {
                            obj.weight *= 100;
                        }
                    }
                    itemsHaveBeenRarityBoosted = true;
                }
            });
            CheckUnlocks.Init();
            CheatUnlocks.Init();
            ETGModConsole.Commands.GetGroup("nn").AddUnit("givebyid", delegate (string[] args)
            {
                if (args == null) ETGModConsole.Log("No args");
                if (args.Length > 1) ETGModConsole.Log("Too many args");
                string id = args[0];
                int id2 = -1;
                try
                {
                    id2 = int.Parse(id);
                }
                catch
                {
                    ETGModConsole.Log("That's not a numerical id");
                }
                if (id2 >= 0)
                {
                    if (PickupObjectDatabase.GetById(id2) != null)
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(id2).gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.zero, 0);
                    }
                }

            });

            DeconstructGun.Init();

            #region LoadRoomCommands
            /* ETGModConsole.Commands.GetGroup("nn").AddUnit("removegun", delegate (string[] args)
             {
                 PlayerController player = GameManager.Instance.PrimaryPlayer;
                 player.inventory.RemoveGunFromInventory(player.CurrentGun);
             });
                 ETGModConsole.Commands.GetGroup("nn").AddUnit("goto", delegate (string[] args)
             {
                 if (args == null) ETGModConsole.Log("No args");
                 if (args.Length > 2) ETGModConsole.Log("Too many args");

                 string targetdungeon = args[0];
                 string targetname = args[1];

                 bool hasFoundtarget = false;
                 PrototypeDungeonRoom targetPrototype = null;
                 if (DungeonDatabase.GetOrLoadByName(targetdungeon) != null)
                 {
                     Dungeon levelInsance = DungeonDatabase.GetOrLoadByName(targetdungeon);
                     foreach (WeightedRoom wRoom in levelInsance.PatternSettings.flows[0].fallbackRoomTable.includedRooms.elements)
                     {
                         if (wRoom.room != null && !string.IsNullOrEmpty(wRoom.room.name))
                         {
                             if (wRoom.room.name == targetname)
                             {
                                 hasFoundtarget = true;
                                 targetPrototype = wRoom.room;
                             }
                         }
                     }
                     levelInsance = null;
                     if (hasFoundtarget)
                     {
                         GameManager.Instance.StartCoroutine(GenerateRoomAndGoTo(GameManager.Instance.PrimaryPlayer, targetPrototype));
                     }
                     else
                     {
                         ETGModConsole.Log("Could not find target room in floor table");
                     }
                 }
                 else ETGModConsole.Log("Not a valid dungeon");
             });
         }
         public static bool m_IsTeleporting;
         public static IEnumerator GenerateRoomAndGoTo(PlayerController user, PrototypeDungeonRoom target)
         {
             RoomHandler currentRoom = user.CurrentRoom;
             user.ForceStopDodgeRoll();
             user.healthHaver.IsVulnerable = false;
             yield return new WaitForSeconds(0.1f);
             Dungeon dungeon = GameManager.Instance.Dungeon;

             RoomHandler GlitchRoom = otherapacheshit.AddCustomRuntimeRoom(target);
             if (GlitchRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET && GlitchRoom.IsSecretRoom)
             {
                 GlitchRoom.secretRoomManager.OpenDoor();
             }
             TeleportToRoom(user, GlitchRoom);
             yield return null;
             while (m_IsTeleporting) { yield return null; }
             yield break;
         }

         public static void TeleportToRoom(PlayerController targetPlayer, RoomHandler targetRoom, bool isSecondaryPlayer = false)
         {
             m_IsTeleporting = true;
             IntVector2? randomAvailableCell = targetRoom.GetRandomAvailableCell(new IntVector2?(new IntVector2(2, 2)), new CellTypes?(CellTypes.FLOOR), false, null);
             if (targetRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.EXIT)
             {
                 randomAvailableCell = (new IntVector2(5, 2) + targetRoom.area.basePosition);
             }
             if (!randomAvailableCell.HasValue)
             {
                 m_IsTeleporting = false;
                 return;
             }
             if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && !isSecondaryPlayer)
             {
                 PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(targetPlayer);
                 if (otherPlayer) { TeleportToRoom(otherPlayer, targetRoom, true); }
             }
             // targetPlayer.m_isStartingTeleport = false;
             targetPlayer.DoVibration(Vibration.Time.Normal, Vibration.Strength.Medium);
             GameManager.Instance.StartCoroutine(HandleTeleportToRoom(targetPlayer, randomAvailableCell.Value.ToCenterVector2()));
             targetPlayer.specRigidbody.Velocity = Vector2.zero;
             targetPlayer.knockbackDoer.TriggerTemporaryKnockbackInvulnerability(1f);
             targetRoom.EnsureUpstreamLocksUnlocked();
             // GameManager.Instance.StartCoroutine(DelayedRoomReset(targetPlayer, targetPlayer.CurrentRoom));
         }
         private static IEnumerator HandleTeleportToRoom(PlayerController targetPlayer, Vector2 targetPoint)
         {
             targetPlayer.healthHaver.IsVulnerable = false;
             CameraController cameraController = GameManager.Instance.MainCameraController;
             Vector2 offsetVector = (cameraController.transform.position - targetPlayer.transform.position);
             offsetVector -= cameraController.GetAimContribution();
             Minimap.Instance.ToggleMinimap(false, false);
             cameraController.SetManualControl(true, false);
             cameraController.OverridePosition = cameraController.transform.position;
             // targetPlayer.CurrentInputState = PlayerInputState.NoInput;
             yield return new WaitForSeconds(0.1f);
             // yield return new WaitForSeconds(0.4f);
             yield return new WaitForSeconds(1);
             targetPlayer.ToggleRenderer(false, "arbitrary teleporter");
             targetPlayer.ToggleGunRenderers(false, "arbitrary teleporter");
             targetPlayer.ToggleHandRenderers(false, "arbitrary teleporter");
             yield return new WaitForSeconds(1);
             Pixelator.Instance.FadeToBlack(0.15f, false, 0f);
             yield return new WaitForSeconds(0.15f);
             // targetPlayer.specRigidbody.Position = new Position(targetPoint);
             targetPlayer.transform.position = targetPoint;
             GameUIRoot.Instance.HideCoreUI(string.Empty);            
             targetPlayer.specRigidbody.Reinitialize();
             targetPlayer.SetIsStealthed(true, "meh");
             GameObject stealthVFX = OMITBReflectionHelpers.ReflectGetField<GameObject>(typeof(GameActor), "m_stealthVfx", targetPlayer.gameActor);
             UnityEngine.Object.Destroy(stealthVFX);
             targetPlayer.specRigidbody.RecheckTriggers = true;
             if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
             {
                 cameraController.OverridePosition = cameraController.GetIdealCameraPosition();
             }
             else
             {
                 cameraController.OverridePosition = (targetPoint + offsetVector).ToVector3ZUp(0f);
             }
             targetPlayer.WarpFollowersToPlayer();
             targetPlayer.WarpCompanionsToPlayer(false);
             Pixelator.Instance.MarkOcclusionDirty();
             Pixelator.Instance.FadeToBlack(0.15f, true, 0f);
             yield return null;
             cameraController.SetManualControl(false, true);
             // yield return new WaitForSeconds(0.75f);
             yield return new WaitForSeconds(0.15f);
             targetPlayer.DoVibration(Vibration.Time.Normal, Vibration.Strength.Medium);
             // yield return new WaitForSeconds(0.25f);
             yield return new WaitForSeconds(1.7f);
             //  targetPlayer.ToggleRenderer(true, "arbitrary teleporter");
             //  targetPlayer.ToggleGunRenderers(true, "arbitrary teleporter");
             // targetPlayer.ToggleHandRenderers(true, "arbitrary teleporter");
             PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(targetPlayer.specRigidbody, null, false);

             //7  targetPlayer.CurrentInputState = PlayerInputState.AllInput;
             targetPlayer.healthHaver.IsVulnerable = true;
             m_IsTeleporting = false;
             yield break;
         }*/
            #endregion

        }
       
    }
}

