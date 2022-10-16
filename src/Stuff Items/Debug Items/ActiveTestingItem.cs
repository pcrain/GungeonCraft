using System;
using System.Collections.Generic;
using System.Linq;
using Dungeonator;
using System.Text;

using ItemAPI;
using UnityEngine;
using System.Collections;
using SaveAPI;

namespace NevernamedsItems
{
    class ActiveTestingItem : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "<WIP> Active Testing Item <WIP>";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<ActiveTestingItem>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Work In Progress";
            string longDesc = "This item was created by an amateur gunsmith so that he may test different concepts instead of going the whole nine yards and making a whole new item.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 0);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.EXCLUDED;
        }

        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
        //float duration = 20f;
        public override void DoEffect(PlayerController user)
        {
            //IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
            //ChestToolbox.ChestTier tier = RandomEnum<ChestToolbox.ChestTier>.Get();
            //ChestToolbox.SpawnChestEasy(bestRewardLocation, tier, true, Chest.GeneralChestType.UNSPECIFIED);
            //SpawnObjectManager.SpawnObject(ExoticPlaceables.SteelTableHorizontal, user.specRigidbody.UnitCenter, null);

            /*   IPlayerInteractable nearestInteractable = user.CurrentRoom.GetNearestInteractable(user.CenterPosition, 1f, user);
               if (!(nearestInteractable is Chest)) return;

               Chest rerollChest = nearestInteractable as Chest;
               if (rerollChest.IsMimic)
               {
                   rerollChest.ForceOpen(user);
                   return;
               }
               rerollChest.contents = new List<PickupObject>()
               {
                   PickupObjectDatabase.GetById(51)
               };*/

            //ProjectileImpactVFXPool hit = (PickupObjectDatabase.GetById(178) as Gun).GetComponent<FireOnReloadSynergyProcessor>().DirectedBurstSettings.ProjectileInterface.SpecifiedProjectile.hitEffects;

            //hit.DeconstructHitEffects();
            /*  string enemyCode = ActorTemplateGenerator.GenerateActorTemplate(
                  "Testy Boss Boy",
                  "TESTENEMYGUID832974628653498",
                  "nn",
                  ActorType.BOSS,
                  true,
                  new AllAnimations()
                  {
                      idleAnimation = new WholeAnimationData()
                      {
                          animName = "IdleAnimation",
                          animShortname = "idle",
                          Directionality = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                          flipType = DirectionalAnimation.FlipType.None,
                          DirectionalAnimations = new List<DirectionalAnimationData>()
                          {
                              new DirectionalAnimationData()
                              {
                                  suffix = "right",
                                  fps = 15,
                                  wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                  Frames = new List<AnimationFrameData>()
                                  {
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_right_001",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_right_002",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_right_003",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_right_004",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                  },
                              },
                              new DirectionalAnimationData()
                              {
                                  suffix = "left",
                                  fps = 15,
                                  wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                  Frames = new List<AnimationFrameData>()
                                  {
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_left_001",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_left_002",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_left_003",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Idle_left_004",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                  },
                              },
                          }
                      },
                      walkAnimation = new WholeAnimationData()
                      {
                          animName = "MoveAnimation",
                          animShortname = "move",
                          Directionality = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                          flipType = DirectionalAnimation.FlipType.None,
                          DirectionalAnimations = new List<DirectionalAnimationData>()
                          {
                              new DirectionalAnimationData()
                              {
                                  suffix = "right",
                                  fps = 15,
                                  wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                  Frames = new List<AnimationFrameData>()
                                  {
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_right_001",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_right_002",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_right_003",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_right_004",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                  },
                              },
                              new DirectionalAnimationData()
                              {
                                  suffix = "left",
                                  fps = 15,
                                  wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                  Frames = new List<AnimationFrameData>()
                                  {
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_left_001",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_left_002",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_left_003",
                                          frameAudioEvent = null,
                                          frameXOffset = 5,
                                          frameYOffset = 6,
                                      },
                                      new AnimationFrameData()
                                      {
                                          filePath = "Sex/Penis/Cock/Walk_left_004",
                                          frameAudioEvent = null,
                                          frameXOffset = 2,
                                          frameYOffset = 3,
                                      },
                                  },
                              },
                          }
                      },
                      otherAnimations = new List<WholeAnimationData>()
                      {
                          new WholeAnimationData()
                          {
                              animName = "sex",
                              animShortname = "sex",
                              Directionality = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                              flipType = DirectionalAnimation.FlipType.None,
                              DirectionalAnimations = new List<DirectionalAnimationData>()
                              {
                                  new DirectionalAnimationData()
                                  {
                                      suffix = "right",
                                      fps = 20,
                                      wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                      Frames = new List<AnimationFrameData>()
                                      {
                                          new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_right_001",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 1,
                                          },
                                           new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_right_002",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 2,
                                          },
                                            new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_right_003",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 1,
                                          }
                                      },
                                  },
                                  new DirectionalAnimationData()
                                  {
                                      suffix = "left",
                                      fps = 20,
                                      wrap = tk2dSpriteAnimationClip.WrapMode.Once,
                                      Frames = new List<AnimationFrameData>()
                                      {
                                          new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_left_001",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 1,
                                          },
                                           new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_left_002",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 2,
                                          },
                                            new AnimationFrameData()
                                          {
                                              filePath = "Sex/Penis/Cock/Fuck_left_003",
                                              frameAudioEvent = "",
                                              frameXOffset = 0,
                                              frameYOffset = 1,
                                          }
                                      },
                                  },
                              },
                          },
                      },
                  },
                  7,
                  1,
                  true,
                  false,
                  false,
                  true,
                  false,
                  10,
                  true,
                  true,
                  10,
                  15,
                  false,
                  true,
                  true,
                  14,
                  5,
                  5,
                  5,
                  false,
                  true,
                  1,
                  false,
                  false,
                  "Thingy",
                  "Thingy2",
                  "Thingy3",
                  "Thingy4",
                  "Thingy5",
                  false,
                  "red",
                  true,
                  true,
                  true,
                  5,
                  false,
                  true,
                  -1,
                  false,
                  0,
                  0,
                  false,
                  "Bingly Bungly Boo",
                  "Bungle Bish bash bosh",
                  true,
                  "Quote", //Ammonomicon Quote
                  "Description", //Ammonomicon Desc
                  15 //Pos in ammonomicon
                  );

              FileLogger.Log(enemyCode,"OMITBOutput");*/
            //GameManager.Instance.MainCameraController.Camera.transform.rotation = Quaternion.Euler(0, 0, 180);
            //CurseManager.AddCurse("Curse of Butterfingers", true);
            /* AkSoundEngine.PostEvent("Play_ClownHonk", user.gameObject);
             foreach(Projectile proj in StaticReferenceManager.AllProjectiles)
             {

                 ETGModConsole.Log($"<color=#ff0000ff>{proj.gameObject.name}</color>");
                 bool isBem = proj.GetComponent<BasicBeamController>() != null;

                 ETGModConsole.Log($"<color=#ff0000ff>Is Beam: </color>{isBem}");
                 if (isBem)
                 {
                     ETGModConsole.Log("Bone Count: " + proj.GetComponent<BasicBeamController>().GetBoneCount());
                     ETGModConsole.Log("UsesBones: " + proj.GetComponent<BasicBeamController>().UsesBones);
                     ETGModConsole.Log("Interpolate: " + proj.GetComponent<BasicBeamController>().interpolateStretchedBones);
                 }
                 ETGModConsole.Log("<color=#ff0000ff>Components</color>");
                 ETGModConsole.Log("<color=#ff0000ff>Children</color>");
                 foreach (Component component in proj.GetComponentsInChildren<Component>())
                 {
                     ETGModConsole.Log(component.GetType().ToString());
                 }
                 ETGModConsole.Log("<color=#ff0000ff>Parents</color>");
                 foreach (Component component in proj.GetComponentsInParent<Component>())
                 {
                     ETGModConsole.Log(component.GetType().ToString());
                 }
                 ETGModConsole.Log("<color=#ff0000ff>---------------------------------</color>");
             }*/



            //Vector3 place = user.GetCursorPosition(4);
            //GameObject carto = UnityEngine.Object.Instantiate<GameObject>(Carto.CartoPrefab, place, Quaternion.identity);
            //DungeonPlaceableUtility.InstantiateDungeonPlaceable(carto, user.CurrentRoom, ((Vector2)carto.transform.position).ToIntVector2(), false);
            //SaveAPIManager.SetFlag(CustomDungeonFlags.CHEATED_DEATH_SHADE, true);
            //CurseManager.AddCurse("Curse of Infestation");
            /*  ChamberGunProcessor processor = user.CurrentGun.GetComponentInChildren<ChamberGunProcessor>();
              if (processor)
              {
                  ETGModConsole.Log("Keep: " + processor.CastleGunID);
                  ETGModConsole.Log("Oub: " + processor.OublietteGunID);
                  ETGModConsole.Log("GP: " + processor.GungeonGunID);
                  ETGModConsole.Log("Abbey: " + processor.AbbeyGunID);
                  ETGModConsole.Log("Mines: " + processor.MinesGunID);
                  ETGModConsole.Log("Rat: " + processor.RatgeonGunID);
                  ETGModConsole.Log("Hollow: " + processor.HollowGunID);
                  ETGModConsole.Log("R&G: " + processor.OfficeGunID);
                  ETGModConsole.Log("Forge: " + processor.ForgeGunID);
                  ETGModConsole.Log("BulletHell: " + processor.HellGunID);
              }*/


            //BeamToolbox.FreeFireBeamFromAnywhere(LaserBullets.SimpleRedBeam, user, user.gameObject, Vector2.zero, false, user.CurrentGun.CurrentAngle, 10, true);

            /*    Vector2 yourPosition = user.sprite.WorldCenter;
                List<AIActor> activeEnemies = user.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];

                       GameActorSizeEffect shrinky = StatusEffectHelper.GenerateSizeEffect(10, new Vector2(0.4f, 0.4f));
                        aiactor.ApplyEffect(shrinky);
                    }
                }*/
            /* TalkDoerLite[] allChests = FindObjectsOfType<TalkDoerLite>();
             foreach (TalkDoerLite chest in allChests)
             {
                 ETGModConsole.Log(chest.name);
             }*/
            PlayerItem itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<PlayerItem>( ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
            LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.left, 1f, false, true, false);
        }
        public override void Update()
        {
            base.Update();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);

        }


    }
}
