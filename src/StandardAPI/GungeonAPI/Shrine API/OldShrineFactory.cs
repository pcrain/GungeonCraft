using System;
using System.Collections.Generic;
using Dungeonator;
using ItemAPI;
using UnityEngine;
namespace GungeonAPI
{
	// Token: 0x02000006 RID: 6
	public class OldShrineFactory
	{
		// Token: 0x06000016 RID: 22 RVA: 0x00003624 File Offset: 0x00001824
		public static void Init()
		{
			bool initialized = OldShrineFactory.m_initialized;
			bool flag = !initialized;
			if (flag)
			{
				DungeonHooks.OnFoyerAwake += OldShrineFactory.PlaceBreachShrines;
				DungeonHooks.OnPreDungeonGeneration += delegate (LoopDungeonGenerator generator, Dungeon dungeon, DungeonFlow flow, int dungeonSeed)
				{
					bool flag2 = flow.name != "Foyer Flow" && !GameManager.IsReturningToFoyerWithPlayer;
					bool flag3 = flag2;
					if (flag3)
					{
						foreach (OldShrineFactory.CustomShrineController customShrineController in UnityEngine.Object.FindObjectsOfType<OldShrineFactory.CustomShrineController>())
						{
							bool flag4 = !ShrineFakePrefab.IsFakePrefab(customShrineController);
							bool flag5 = flag4;
							if (flag5)
							{
								UnityEngine.Object.Destroy(customShrineController.gameObject);
							}
						}
						OldShrineFactory.m_builtShrines = false;
					}
				};
				OldShrineFactory.m_initialized = true;
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00003680 File Offset: 0x00001880
		public GameObject Build()
		{
			GameObject result;
			try
			{
				Texture2D textureFromResource = ResourceExtractor.GetTextureFromResource(this.spritePath);
				GameObject gameObject = SpriteBuilder.SpriteFromResource(this.spritePath, null);
				string text = (this.modID + ":" + this.name).ToLower().Replace(" ", "_");
				//string roomPath = this.roomPath;
				gameObject.name = text;
				tk2dSprite component = gameObject.GetComponent<tk2dSprite>();
				component.IsPerpendicular = true;
				component.PlaceAtPositionByAnchor(this.offset, tk2dBaseSprite.Anchor.LowerCenter);
				Transform transform = new GameObject("talkpoint").transform;
				transform.position = gameObject.transform.position + this.talkPointOffset;
				transform.SetParent(gameObject.transform);
				bool flag = !this.usesCustomColliderOffsetAndSize;
				bool flag2 = flag;
				if (flag2)
				{
					IntVector2 intVector = new IntVector2(textureFromResource.width, textureFromResource.height);
					this.colliderOffset = new IntVector2(0, 0);
					this.colliderSize = new IntVector2(intVector.x, intVector.y / 2);
				}
				SpeculativeRigidbody speculativeRigidbody = component.SetUpSpeculativeRigidbody(this.colliderOffset, this.colliderSize);

				speculativeRigidbody.PixelColliders.Add(new PixelCollider
				{
					ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
					CollisionLayer = CollisionLayer.PlayerBlocker,
					IsTrigger = false,
					BagleUseFirstFrameOnly = false,
					SpecifyBagelFrame = string.Empty,
					BagelColliderNumber = 0,
					ManualOffsetX = this.colliderOffset.x,
					ManualOffsetY = this.colliderOffset.y,
					ManualWidth = this.colliderSize.x,
					ManualHeight = this.colliderSize.y,
					ManualDiameter = 0,
					ManualLeftX = 0,
					ManualLeftY = 0,
					ManualRightX = 0,
					ManualRightY = 0,
				});
				speculativeRigidbody.PixelColliders.Add(new PixelCollider
				{

					ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
					CollisionLayer = CollisionLayer.EnemyBlocker,
					IsTrigger = false,
					BagleUseFirstFrameOnly = false,
					SpecifyBagelFrame = string.Empty,
					BagelColliderNumber = 0,
					ManualOffsetX = this.colliderOffset.x,
					ManualOffsetY = this.colliderOffset.y,
					ManualWidth = this.colliderSize.x,
					ManualHeight = this.colliderSize.y,
					ManualDiameter = 0,
					ManualLeftX = 0,
					ManualLeftY = 0,
					ManualRightX = 0,
					ManualRightY = 0,
				});
				speculativeRigidbody.PixelColliders.Add(new PixelCollider
				{
					ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
					CollisionLayer = CollisionLayer.BulletBlocker,
					IsTrigger = false,
					BagleUseFirstFrameOnly = false,
					SpecifyBagelFrame = string.Empty,
					BagelColliderNumber = 0,
					ManualOffsetX = this.colliderOffset.x,
					ManualOffsetY = this.colliderOffset.y,
					ManualWidth = this.colliderSize.x,
					ManualHeight = this.colliderSize.y,
					ManualDiameter = 0,
					ManualLeftX = 0,
					ManualLeftY = 0,
					ManualRightX = 0,
					ManualRightY = 0,
				});
				speculativeRigidbody.PixelColliders.Add(new PixelCollider
				{
					ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
					CollisionLayer = CollisionLayer.EnemyBulletBlocker,
					IsTrigger = false,
					BagleUseFirstFrameOnly = false,
					SpecifyBagelFrame = string.Empty,
					BagelColliderNumber = 0,
					ManualOffsetX = this.colliderOffset.x,
					ManualOffsetY = this.colliderOffset.y,
					ManualWidth = this.colliderSize.x,
					ManualHeight = this.colliderSize.y,
					ManualDiameter = 0,
					ManualLeftX = 0,
					ManualLeftY = 0,
					ManualRightX = 0,
					ManualRightY = 0,
				});
				speculativeRigidbody.PixelColliders.Add(new PixelCollider
				{
					ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
					CollisionLayer = CollisionLayer.BeamBlocker,
					IsTrigger = false,
					BagleUseFirstFrameOnly = false,
					SpecifyBagelFrame = string.Empty,
					BagelColliderNumber = 0,
					ManualOffsetX = this.colliderOffset.x,
					ManualOffsetY = this.colliderOffset.y,
					ManualWidth = this.colliderSize.x,
					ManualHeight = this.colliderSize.y,
					ManualDiameter = 0,
					ManualLeftX = 0,
					ManualLeftY = 0,
					ManualRightX = 0,
					ManualRightY = 0,
				});

				OldShrineFactory.CustomShrineController customShrineController = gameObject.AddComponent<OldShrineFactory.CustomShrineController>();
				customShrineController.ID = text;
				customShrineController.roomStyles = this.roomStyles;
				customShrineController.isBreachShrine = true;
				customShrineController.offset = this.offset;
				customShrineController.pixelColliders = speculativeRigidbody.specRigidbody.PixelColliders;
				customShrineController.factory = this;
				customShrineController.OnAccept = this.OnAccept;
				customShrineController.OnDecline = this.OnDecline;
				customShrineController.CanUse = this.CanUse;
				bool flag3 = this.interactableComponent != null;
				bool flag4 = flag3;
				IPlayerInteractable item;
				if (flag4)
				{
					item = (gameObject.AddComponent(this.interactableComponent) as IPlayerInteractable);
				}
				else
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
					item = simpleShrine;
				}
				GameObject gameObject2 = ShrineFakePrefab.Clone(gameObject);
				gameObject2.GetComponent<OldShrineFactory.CustomShrineController>().Copy(customShrineController);
				gameObject2.name = text;
				bool flag5 = this.isBreachShrine;
				bool flag6 = flag5;
				if (flag6)
				{
					bool flag7 = !RoomHandler.unassignedInteractableObjects.Contains(item);
					bool flag8 = flag7;
					if (flag8)
					{
						RoomHandler.unassignedInteractableObjects.Add(item);
					}
				}
				else
				{
					bool flag9 = !this.room;
					bool flag10 = flag9;
					if (flag10)
					{
						this.room = RoomFactory.CreateEmptyRoom(12, 12);
					}
					OldShrineFactory.RegisterShrineRoom(gameObject2, this.room, text, this.offset, this.RoomWeight);
				}
				OldShrineFactory.builtShrines.Add(text, gameObject2);
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
		// Token: 0x06000018 RID: 24 RVA: 0x0000399C File Offset: 0x00001B9C
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
			//RoomFactory.RoomData roomData = RoomFactory.ExtractRoomDataFromResource(roomPath);
			RoomFactory.rooms.Add(ID, roomData);
			DungeonHandler.RegisterForShrine(roomData);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00003ADC File Offset: 0x00001CDC
		private static void PlaceBreachShrines()
		{
			bool flag = OldShrineFactory.m_builtShrines;
			bool flag2 = !flag;
			if (flag2)
			{
				foreach (GameObject gameObject in OldShrineFactory.builtShrines.Values)
				{
					try
					{
						OldShrineFactory.CustomShrineController component = gameObject.GetComponent<OldShrineFactory.CustomShrineController>();
						bool flag3 = !component.isBreachShrine;
						bool flag4 = !flag3;
						if (flag4)
						{
							OldShrineFactory.CustomShrineController component2 = UnityEngine.Object.Instantiate<GameObject>(gameObject).GetComponent<OldShrineFactory.CustomShrineController>();
							component2.Copy(component);
							component2.gameObject.SetActive(true);
							component2.sprite.PlaceAtPositionByAnchor(component2.offset, tk2dBaseSprite.Anchor.LowerCenter);
							IPlayerInteractable component3 = component2.GetComponent<IPlayerInteractable>();
							bool flag5 = component3 is SimpleInteractable;
							bool flag6 = flag5;
							if (flag6)
							{
								((SimpleInteractable)component3).OnAccept = component2.OnAccept;
								((SimpleInteractable)component3).OnDecline = component2.OnDecline;
								((SimpleInteractable)component3).CanUse = component2.CanUse;
							}
							bool flag7 = !RoomHandler.unassignedInteractableObjects.Contains(component3);
							bool flag8 = flag7;
							if (flag8)
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
				OldShrineFactory.m_builtShrines = true;
			}
		}

		// Token: 0x0400001C RID: 28
		public string name;

		// Token: 0x0400001D RID: 29
		public string modID;

		// Token: 0x0400001E RID: 30
		public string spritePath;

		public string roomPath;

		// Token: 0x0400001F RID: 31
		public string text;

		// Token: 0x04000020 RID: 32
		public string acceptText;

		// Token: 0x04000021 RID: 33
		public string declineText;

		// Token: 0x04000022 RID: 34
		public Action<PlayerController, GameObject> OnAccept;

		// Token: 0x04000023 RID: 35
		public Action<PlayerController, GameObject> OnDecline;

		// Token: 0x04000024 RID: 36
		public Func<PlayerController, GameObject, bool> CanUse;

		// Token: 0x04000025 RID: 37
		public Vector3 talkPointOffset;

		// Token: 0x04000026 RID: 38
		public Vector3 offset = new Vector3(43.8f, 42.4f, 42.9f);

		// Token: 0x04000027 RID: 39
		public IntVector2 colliderOffset;

		// Token: 0x04000028 RID: 40
		public IntVector2 colliderSize;

		// Token: 0x04000029 RID: 41
		public bool isToggle;

		// Token: 0x0400002A RID: 42
		public bool usesCustomColliderOffsetAndSize;

		// Token: 0x0400002B RID: 43
		public Type interactableComponent = null;

		// Token: 0x0400002C RID: 44
		public bool isBreachShrine = false;

		// Token: 0x0400002D RID: 45
		public PrototypeDungeonRoom room;

		// Token: 0x0400002E RID: 46
		public Dictionary<string, int> roomStyles;

		// Token: 0x0400002F RID: 47
		public static Dictionary<string, GameObject> builtShrines = new Dictionary<string, GameObject>();

		// Token: 0x04000030 RID: 48
		private static bool m_initialized;

		// Token: 0x04000031 RID: 49
		private static bool m_builtShrines;

		// Token: 0x02000074 RID: 116
		public class CustomShrineController : DungeonPlaceableBehaviour
		{
			// Token: 0x06000303 RID: 771 RVA: 0x00021FE0 File Offset: 0x000201E0
			private void Start()
			{
				string text = base.name.Replace("(Clone)", "");
				bool flag = OldShrineFactory.builtShrines.ContainsKey(text);
				bool flag2 = flag;
				if (flag2)
				{
					this.Copy(OldShrineFactory.builtShrines[text].GetComponent<OldShrineFactory.CustomShrineController>());
				}
				else
				{
					Tools.PrintError<string>("Was this shrine registered correctly?: " + text, "FF0000");
				}
				base.GetComponent<SimpleInteractable>().OnAccept = this.OnAccept;
				base.GetComponent<SimpleInteractable>().OnDecline = this.OnDecline;
				base.GetComponent<SimpleInteractable>().CanUse = this.CanUse;
			}

			// Token: 0x06000304 RID: 772 RVA: 0x0002207C File Offset: 0x0002027C
			public void Copy(OldShrineFactory.CustomShrineController other)
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
			}

			// Token: 0x06000305 RID: 773 RVA: 0x000220F6 File Offset: 0x000202F6
			public void ConfigureOnPlacement(RoomHandler room)
			{
				this.m_parentRoom = room;
				this.RegisterMinimapIcon(room);
			}

			// Token: 0x06000306 RID: 774 RVA: 0x00022107 File Offset: 0x00020307
			public void RegisterMinimapIcon(RoomHandler room)
			{
				this.m_instanceMinimapIcon = Minimap.Instance.RegisterRoomIcon(room, (GameObject)BraveResources.Load("Global Prefabs/Minimap_Shrine_Icon", ".prefab"));
			}

			// Token: 0x06000307 RID: 775 RVA: 0x00022138 File Offset: 0x00020338
			public void GetRidOfMinimapIcon()
			{
				bool flag = this.m_instanceMinimapIcon != null;
				bool flag2 = flag;
				if (flag2)
				{
					Minimap.Instance.DeregisterRoomIcon(this.m_parentRoom, this.m_instanceMinimapIcon);
					this.m_instanceMinimapIcon = null;
				}
			}

			// Token: 0x04000155 RID: 341
			public string ID;

			// Token: 0x04000156 RID: 342
			public bool isBreachShrine;

			// Token: 0x04000157 RID: 343
			public Vector3 offset;

			// Token: 0x04000158 RID: 344
			public List<PixelCollider> pixelColliders;

			// Token: 0x04000159 RID: 345
			public Dictionary<string, int> roomStyles;

			// Token: 0x0400015A RID: 346
			public OldShrineFactory factory;

			// Token: 0x0400015B RID: 347
			public Action<PlayerController, GameObject> OnAccept;

			// Token: 0x0400015C RID: 348
			public Action<PlayerController, GameObject> OnDecline;

			// Token: 0x0400015D RID: 349
			public Func<PlayerController, GameObject, bool> CanUse;

			// Token: 0x0400015E RID: 350
			private RoomHandler m_parentRoom;

			// Token: 0x0400015F RID: 351
			private GameObject m_instanceMinimapIcon;

			// Token: 0x04000160 RID: 352
			public int numUses = 0;
			public int specialUses = 0;
		}
	}
}