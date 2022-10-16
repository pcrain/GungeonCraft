using GungeonAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static GungeonAPI.OldShrineFactory;
using Gungeon;
using ItemAPI;
using Dungeonator;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Collections;

namespace NevernamedsItems
{
    public static class TurtleShrine
    {

        public static void Add()
        {
            OldShrineFactory aa = new OldShrineFactory
            {

                name = "TurtleShrine",
                modID = "omitb",
                text = "A shrine to a bizarre Gungeoneer, whose psychopathy was rewarded by a bloodthirsty reptilian horde.",
                spritePath = "NevernamedsItems/Resources/Shrines/turtle_shrine.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/TurtleShrineRoom.room").room,
                RoomWeight = 1f,
                acceptText = "Beat your head against the statue <Lose Half a Heart>",
                declineText = "Leave",
                OnAccept = Accept,
                OnDecline = null,
                CanUse = CanUse,
                offset = new Vector3(-1, -1, 0),
                talkPointOffset = new Vector3(0, 3, 0),
                isToggle = false,
                isBreachShrine = false,


            };
            aa.Build();
            spriteId = SpriteBuilder.AddSpriteToCollection(spriteDefinition, ShrineFactory.ShrineIconCollection);
        }
        public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/turtle_icon";
        public static bool CanUse(PlayerController player, GameObject shrine)
        {
            if (!player.ForceZeroHealthState && player.healthHaver.GetCurrentHealth() > 0.5f)
            {
                return true;
            }
            else if (player.ForceZeroHealthState && player.healthHaver.Armor > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static IEnumerator DoShrineEffect(PlayerController player)
        {
            float SST = UnityEngine.Random.value;
            TurtleShrineEffectHandler eff = player.gameObject.GetOrAddComponent<TurtleShrineEffectHandler>();
            yield return null;
            if (player.characterIdentity == PlayableCharacters.Robot)
            {
                player.healthHaver.Armor -= 1;
                if (SST <= 0.01f)
                {
                    PickupObject byId = PickupObjectDatabase.GetById(301);
                    player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else
                {
                    eff.SpawnNewTurtle();
                    eff.SpawnNewTurtle();
                    eff.SpawnNewTurtle();
                    eff.SpawnNewTurtle();
                }
            }
            else
            {
                player.healthHaver.ApplyHealing(-0.5f);
                if (SST <= 0.005f)
                {
                    PickupObject byId = PickupObjectDatabase.GetById(301);
                    player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                }
                else
                {
                    eff.SpawnNewTurtle();
                    eff.SpawnNewTurtle();
                }
            }
            yield break;
        }
        public static void Accept(PlayerController player, GameObject shrine)
        {
            shrine.GetComponent<CustomShrineController>().numUses++;

            GameUIRoot.Instance.notificationController.DoCustomNotification(
                   "Turtle Power",
                    "A new friend?",
                    ShrineFactory.ShrineIconCollection,
                spriteId,
                    UINotificationController.NotificationColor.SILVER,
                    true,
                    false
                    );

            player.StartCoroutine(DoShrineEffect(player));
            
            AkSoundEngine.PostEvent("Play_OBJ_shrine_accept_01", shrine);
        }
        public static int spriteId;
    }
    public class TurtleShrineEffectHandler : MonoBehaviour
    {
        public TurtleShrineEffectHandler()
        {

        }
        private void Start()
        {
            player = base.GetComponent<PlayerController>();
            FloorAndGenerationToolbox.OnFloorEntered += LevelLoadStart;
            FloorAndGenerationToolbox.OnFloorEntered += LevelLoadFinish;
            activeTurtles = new List<GameObject>();
        }
        private void OnDestroy()
        {
            FloorAndGenerationToolbox.OnFloorEntered -= LevelLoadStart;
            FloorAndGenerationToolbox.OnFloorEntered -= LevelLoadFinish;
        }
        private void LevelLoadStart()
        {
            CountForNextFloor = activeTurtles.Count;
            for (int i = (activeTurtles.Count - 1); i >= 0; i--)
            {
                UnityEngine.Object.Destroy(activeTurtles[i]);
            }
            activeTurtles.Clear();
        }
        private void LevelLoadFinish()
        {
            if (CountForNextFloor > 0)
            {
                for (int i = 0; i < CountForNextFloor; i++)
                {
                    SpawnNewTurtle();
                }
            }
        }
        public void SpawnNewTurtle()
        {
            string guid = PickupObjectDatabase.GetById(645).GetComponent<MulticompanionItem>().CompanionGuid;
            AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(guid);
            Vector3 vector = player.transform.position;
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject, vector, Quaternion.identity);
            CompanionController orAddComponent = gameObject.GetOrAddComponent<CompanionController>();
            this.activeTurtles.Add(gameObject);
            orAddComponent.Initialize(player);
            if (orAddComponent.specRigidbody)
            {
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(orAddComponent.specRigidbody, null, false);
            }
            HealthHaver helf = gameObject.GetComponent<HealthHaver>();
            if  (helf != null)
            {
                float helfNew = helf.GetMaxHealth() * 3f;
                helf.SetHealthMaximum(helfNew);
                helf.ForceSetCurrentHealth(helfNew);
            }
        }
        private int CountForNextFloor;
        private List<GameObject> activeTurtles;
        private PlayerController player;
    }
}