using System;
using System.Collections.Generic;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace GungeonAPI
{
	
	public class ShrineFactory
	{
		private static GameObject VFXScapegoat;
		public static tk2dSpriteCollectionData ShrineIconCollection;

		public static void Init()
		{

			VFXScapegoat = new GameObject("ShrineVFXScapegoat");
			VFXScapegoat.gameObject.SetActive(false);
			UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
			ShrineIconCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "ShrineIcon_Collection");
			UnityEngine.Object.DontDestroyOnLoad(ShrineIconCollection);

			bool initialized = ShrineFactory.m_initialized;
			bool flag = !initialized;
			bool flag2 = flag;
			if (flag2)
			{
				DungeonHooks.OnFoyerAwake += ShrineFactory.PlaceBreachShrines;
				DungeonHooks.OnPreDungeonGeneration += delegate (LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
				{
					bool flag3 = flow.name != "Foyer Flow" && !GameManager.IsReturningToFoyerWithPlayer;
					bool flag4 = flag3;
					bool flag5 = flag4;
					if (flag5)
					{
						ShrineFactory.CleanupBreachShrines();
					}
				};
				ShrineFactory.m_initialized = true;
			}
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00008084 File Offset: 0x00006284
		public GameObject Build()
		{
			GameObject result;
			try
			{
				Texture2D textureFromResource = ResourceExtractor.GetTextureFromResource(this.spritePath);
				GameObject gameObject = SpriteBuilder.SpriteFromResource(this.spritePath, null);
				string text = (this.modID + ":" + this.name).ToLower().Replace(" ", "_");
				gameObject.name = text;
				tk2dSprite component = gameObject.GetComponent<tk2dSprite>();
				component.IsPerpendicular = true;
				component.PlaceAtPositionByAnchor(this.offset, tk2dBaseSprite.Anchor.LowerCenter);
				Transform transform = new GameObject("talkpoint").transform;
				transform.position = gameObject.transform.position + this.talkPointOffset;
				transform.SetParent(gameObject.transform);
				bool flag = !this.usesCustomColliderOffsetAndSize;
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					IntVector2 intVector = new IntVector2(textureFromResource.width, textureFromResource.height);
					this.colliderOffset = new IntVector2(0, 0);
					this.colliderSize = new IntVector2(intVector.x, intVector.y / 2);
				}
				SpeculativeRigidbody speculativeRigidbody = component.SetUpSpeculativeRigidbody(this.colliderOffset, this.colliderSize);
				ShrineFactory.CustomShrineController customShrineController = gameObject.AddComponent<ShrineFactory.CustomShrineController>();
				customShrineController.ID = text;
				customShrineController.roomStyles = this.roomStyles;
				customShrineController.isBreachShrine = true;
				customShrineController.offset = this.offset;
				customShrineController.pixelColliders = speculativeRigidbody.specRigidbody.PixelColliders;
				customShrineController.factory = this;
				customShrineController.OnAccept = this.OnAccept;
				customShrineController.OnDecline = this.OnDecline;
				customShrineController.CanUse = this.CanUse;
				customShrineController.text = this.text;
				customShrineController.acceptText = this.acceptText;
				customShrineController.declineText = this.declineText;
				bool flag4 = this.interactableComponent == null;
				bool flag5 = flag4;
				bool flag6 = flag5;
				if (flag6)
				{
					SimpleShrine simpleShrine = gameObject.AddComponent<SimpleShrine>();
					simpleShrine.isToggle = this.isToggle;
					simpleShrine.OnAccept = this.OnAccept;
					simpleShrine.OnDecline = this.OnDecline;
					simpleShrine.CanUse = this.CanUse;
					simpleShrine.text = this.text;
					simpleShrine.acceptText = this.acceptText;
					simpleShrine.declineText = this.declineText;
					simpleShrine.talkPoint = transform;
				}
				else
				{
					gameObject.AddComponent(this.interactableComponent);
				}
				gameObject.name = text;
				bool flag7 = !this.isBreachShrine;
				bool flag8 = flag7;
				bool flag9 = flag8;
				if (flag9)
				{
					bool flag10 = !this.room;
					bool flag11 = flag10;
					bool flag12 = flag11;
					if (flag12)
					{
						this.room = RoomFactory.CreateEmptyRoom(12, 12);
					}
					ShrineFactory.RegisterShrineRoom(gameObject, this.room, text, this.offset, this.RoomWeight);
				}
				ShrineFactory.registeredShrines.Add(text, gameObject);
				FakePrefab.MarkAsFakePrefab(gameObject);
				result = gameObject;
			}
			catch (Exception e)
			{
				Tools.PrintException(e, "FF0000");
				result = null;
			}
			return result;
		}

		public float RoomWeight;

		// Token: 0x0600009B RID: 155 RVA: 0x00008390 File Offset: 0x00006590
		public static void RegisterShrineRoom(GameObject shrine, PrototypeDungeonRoom protoroom, string ID, Vector2 offset, float roomweight)
		{

			DungeonPrerequisite[] array = new DungeonPrerequisite[0];
			Vector2 vector = new Vector2((float)(protoroom.Width / 2) + offset.x, (float)(protoroom.Height / 2) + offset.y);
			protoroom.placedObjectPositions.Add(vector);
			protoroom.placedObjects.Add(new PrototypePlacedObjectData
			{
				contentsBasePosition = vector,
				fieldData = new List<PrototypePlacedObjectFieldData>(),
				instancePrerequisites = array,
				linkedTriggerAreaIDs = new List<int>(),
				placeableContents = new DungeonPlaceable
				{
					width = 2,
					height = 2,
					respectsEncounterableDifferentiator = true,
					variantTiers = new List<DungeonPlaceableVariant>
					{
						new DungeonPlaceableVariant
						{
							percentChance = 1f,
							nonDatabasePlaceable = shrine,
							prerequisites = array,
							materialRequirements = new DungeonPlaceableRoomMaterialRequirement[0]
						}
					}
				}
			});
			RoomFactory.RoomData roomData = new RoomFactory.RoomData
			{
				room = protoroom,
				category = protoroom.category.ToString(),
				weight = roomweight,
			};
			RoomFactory.rooms.Add(ID, roomData);
			DungeonHandler.Register(roomData);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x000084D0 File Offset: 0x000066D0
		public static void PlaceBreachShrines()
		{
			ShrineFactory.CleanupBreachShrines();
			foreach (GameObject gameObject in ShrineFactory.registeredShrines.Values)
			{
				try
				{
					ShrineFactory.CustomShrineController component = gameObject.GetComponent<ShrineFactory.CustomShrineController>();
					bool flag = !component.isBreachShrine;
					bool flag2 = !flag;
					bool flag3 = flag2;
					if (flag3)
					{
						ShrineFactory.CustomShrineController component2 = UnityEngine.Object.Instantiate<GameObject>(gameObject).GetComponent<ShrineFactory.CustomShrineController>();
						component2.Copy(component);
						component2.gameObject.SetActive(true);
						component2.sprite.PlaceAtPositionByAnchor(component2.offset, tk2dBaseSprite.Anchor.LowerCenter);
						SpriteOutlineManager.AddOutlineToSprite(component2.sprite, Color.black);
						IPlayerInteractable component3 = component2.GetComponent<IPlayerInteractable>();
						bool flag4 = component3 is SimpleInteractable;
						bool flag5 = flag4;
						bool flag6 = flag5;
						if (flag6)
						{
							((SimpleInteractable)component3).OnAccept = component2.OnAccept;
							((SimpleInteractable)component3).OnDecline = component2.OnDecline;
							((SimpleInteractable)component3).CanUse = component2.CanUse;
						}
						bool flag7 = !RoomHandler.unassignedInteractableObjects.Contains(component3);
						bool flag8 = flag7;
						bool flag9 = flag8;
						if (flag9)
						{
							RoomHandler.unassignedInteractableObjects.Add(component3);
						}
					}
				}
				catch (Exception e)
				{
					Tools.PrintException(e, "FF0000");
				}
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00008664 File Offset: 0x00006864
		private static void CleanupBreachShrines()
		{
			foreach (ShrineFactory.CustomShrineController customShrineController in UnityEngine.Object.FindObjectsOfType<ShrineFactory.CustomShrineController>())
			{
				bool flag = !FakePrefab.IsFakePrefab(customShrineController);
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					UnityEngine.Object.Destroy(customShrineController.gameObject);
				}
				else
				{
					customShrineController.gameObject.SetActive(false);
				}
			}
		}

		// Token: 0x04000055 RID: 85
		public string name;

		// Token: 0x04000056 RID: 86
		public string modID;

		// Token: 0x04000057 RID: 87
		public string spritePath;

		// Token: 0x04000058 RID: 88
		public string shadowSpritePath;

		// Token: 0x04000059 RID: 89
		public string text;

		// Token: 0x0400005A RID: 90
		public string acceptText;

		// Token: 0x0400005B RID: 91
		public string declineText;

		// Token: 0x0400005C RID: 92
		public Action<PlayerController, GameObject> OnAccept;

		// Token: 0x0400005D RID: 93
		public Action<PlayerController, GameObject> OnDecline;

		// Token: 0x0400005E RID: 94
		public Func<PlayerController, GameObject, bool> CanUse;

		// Token: 0x0400005F RID: 95
		public Vector3 talkPointOffset;

		// Token: 0x04000060 RID: 96
		public Vector3 offset = new Vector3(43.8f, 42.4f, 42.9f);

		// Token: 0x04000061 RID: 97
		public IntVector2 colliderOffset;

		// Token: 0x04000062 RID: 98
		public IntVector2 colliderSize;

		// Token: 0x04000063 RID: 99
		public bool isToggle;

		// Token: 0x04000064 RID: 100
		public bool usesCustomColliderOffsetAndSize;

		// Token: 0x04000065 RID: 101
		public Type interactableComponent = null;

		// Token: 0x04000066 RID: 102
		public bool isBreachShrine = false;



		// Token: 0x04000067 RID: 103
		public PrototypeDungeonRoom room;

		// Token: 0x04000068 RID: 104
		public Dictionary<string, int> roomStyles;

		// Token: 0x04000069 RID: 105
		public static Dictionary<string, GameObject> registeredShrines = new Dictionary<string, GameObject>();

		// Token: 0x0400006A RID: 106
		private static bool m_initialized;

		// Token: 0x0200007C RID: 124
		public class CustomShrineController : DungeonPlaceableBehaviour
		{
			// Token: 0x0600031F RID: 799 RVA: 0x000227B0 File Offset: 0x000209B0
			private void Start()
			{
				string text = base.name.Replace("(Clone)", "");
				bool flag = ShrineFactory.registeredShrines.ContainsKey(text);
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					this.Copy(ShrineFactory.registeredShrines[text].GetComponent<ShrineFactory.CustomShrineController>());
				}
				else
				{
					Tools.PrintError<string>("Was this shrine registered correctly?: " + text, "FF0000");
				}
				SimpleInteractable component = base.GetComponent<SimpleInteractable>();
				bool flag4 = !component;
				bool flag5 = !flag4;
				bool flag6 = flag5;
				if (flag6)
				{
					component.OnAccept = this.OnAccept;
					component.OnDecline = this.OnDecline;
					component.CanUse = this.CanUse;
					component.text = this.text;
					component.acceptText = this.acceptText;
					component.declineText = this.declineText;
				}
			}

			// Token: 0x06000320 RID: 800 RVA: 0x00022888 File Offset: 0x00020A88
			public void Copy(ShrineFactory.CustomShrineController other)
			{
				this.ID = other.ID;
				this.roomStyles = other.roomStyles;
				this.isBreachShrine = other.isBreachShrine;
				this.offset = other.offset;
				this.pixelColliders = other.pixelColliders;
				this.factory = other.factory;
				this.OnAccept = other.OnAccept;
				this.OnDecline = other.OnDecline;
				this.CanUse = other.CanUse;
				this.text = other.text;
				this.acceptText = other.acceptText;
				this.declineText = other.declineText;
			}

			// Token: 0x06000321 RID: 801 RVA: 0x00022926 File Offset: 0x00020B26
			public void ConfigureOnPlacement(RoomHandler room)
			{
				this.m_parentRoom = room;
				this.RegisterMinimapIcon();
			}

			// Token: 0x06000322 RID: 802 RVA: 0x00022937 File Offset: 0x00020B37
			public void RegisterMinimapIcon()
			{
				this.m_instanceMinimapIcon = Minimap.Instance.RegisterRoomIcon(this.m_parentRoom, (GameObject)BraveResources.Load("Global Prefabs/Minimap_Shrine_Icon", ".prefab"), false);
			}

			// Token: 0x06000323 RID: 803 RVA: 0x00022968 File Offset: 0x00020B68
			public void GetRidOfMinimapIcon()
			{
				bool flag = this.m_instanceMinimapIcon != null;
				bool flag2 = flag;
				bool flag3 = flag2;
				if (flag3)
				{
					Minimap.Instance.DeregisterRoomIcon(this.m_parentRoom, this.m_instanceMinimapIcon);
					this.m_instanceMinimapIcon = null;
				}
			}

			// Token: 0x04000192 RID: 402
			public string ID;

			// Token: 0x04000193 RID: 403
			public bool isBreachShrine;

			// Token: 0x04000194 RID: 404
			public Vector3 offset;

			// Token: 0x04000195 RID: 405
			public List<PixelCollider> pixelColliders;

			// Token: 0x04000196 RID: 406
			public Dictionary<string, int> roomStyles;

			// Token: 0x04000197 RID: 407
			public ShrineFactory factory;

			// Token: 0x04000198 RID: 408
			public Action<PlayerController, GameObject> OnAccept;

			// Token: 0x04000199 RID: 409
			public Action<PlayerController, GameObject> OnDecline;

			// Token: 0x0400019A RID: 410
			public Func<PlayerController, GameObject, bool> CanUse;

			// Token: 0x0400019B RID: 411
			private RoomHandler m_parentRoom;

			// Token: 0x0400019C RID: 412
			private GameObject m_instanceMinimapIcon;

			// Token: 0x0400019D RID: 413
			public int numUses = 0;

			// Token: 0x0400019E RID: 414
			public string text;

			// Token: 0x0400019F RID: 415
			public string acceptText;

			// Token: 0x040001A0 RID: 416
			public string declineText;
		}
	}
}
