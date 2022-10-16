using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using FullInspector;
using System.Collections;

using Random = UnityEngine.Random;
namespace ItemAPI
{

    public class BabyGoodBlob : CompanionItem
    {
        public static GameObject blobPrefab;
        private static readonly string guid = "baby_good_blob";

        public static void Init()
        {
            string itemName = "Baby Good Blob";
            string resourceName = "ItemAPI/Resources/P2/baby_blob";

            GameObject obj = new GameObject();
            var item = obj.AddComponent<BabyGoodBlob>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Scourge of the Empire";
            string longDesc = "This young blobulin was injected with a serum that gave him ultimate power.\n\n" +
                "Afraid that he would grow too powerful to control, the chief blobulonians made an attempt on his life.\n\n" +
                "Wrong move.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            item.quality = PickupObject.ItemQuality.A;
            item.CompanionGuid = guid;
            item.Synergies = new CompanionTransformSynergy[0];
            BuildPrefab();
        }


        private static string[] spritePaths = new string[]
        {
            "ItemAPI/Resources/P2/blob/blob_001",
            "ItemAPI/Resources/P2/blob/blob_002",
            "ItemAPI/Resources/P2/blob/blob_003",
            "ItemAPI/Resources/P2/blob/blob_004",
            "ItemAPI/Resources/P2/blob/blob_005",
            "ItemAPI/Resources/P2/blob/blob_006",
        };

        private static tk2dSpriteCollectionData blobCollection;
        public static void BuildPrefab()
        {
            if (blobPrefab != null || CompanionBuilder.companionDictionary.ContainsKey(guid))
            {
                ETGModConsole.Log("Tried to make the same Blob prefab twice!");
                return;
            }

            blobPrefab = CompanionBuilder.BuildPrefab("Baby Good Blob", guid, spritePaths[0], new IntVector2(1, 0), new IntVector2(9, 9));
            var blob = blobPrefab.AddComponent<RandomGoopTrailBehaviour>();

            var aiAnimator = blobPrefab.GetComponent<AIAnimator>();
            aiAnimator.MoveAnimation = new DirectionalAnimation() { AnimNames = new string[] { "idle" }, Type = DirectionalAnimation.DirectionType.None };
            aiAnimator.IdleAnimation = aiAnimator.MoveAnimation;

            if (blobCollection == null)
            {
                blobCollection = SpriteBuilder.ConstructCollection(blobPrefab, "Baby_Good_Blob_Collection");
                GameObject.DontDestroyOnLoad(blobCollection);
                for (int i = 0; i < spritePaths.Length; i++)
                {
                    SpriteBuilder.AddSpriteToCollection(spritePaths[i], blobCollection);
                }
                SpriteBuilder.AddAnimation(blob.spriteAnimator, blobCollection, new List<int>() { 0, 1, 2, 3, 4, 5 }, "idle", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 5;
            }

            var bs = blobPrefab.GetComponent<BehaviorSpeculator>();
            bs.MovementBehaviors.Add(new CompanionFollowPlayerBehavior() { IdleAnimations = new string[] { "idle" } });
            bs.MovementBehaviors.Add(new SeekTargetBehavior() { LineOfSight = false, StopWhenInRange = true, CustomRange = 1f });

            blob.aiActor.MovementSpeed = 7;

            GameObject.DontDestroyOnLoad(blobPrefab);
            FakePrefab.MarkAsFakePrefab(blobPrefab);
            blobPrefab.SetActive(false);
        }

        //--------------------------------------Companion Controller--------------------------------------------

        private static string[] goops =
        {
            "assets/data/goops/blobulongoop.asset",
            "assets/data/goops/napalmgoopthatworks.asset",
            "assets/data/goops/poison goop.asset",
            //"assets/data/goops/water goop.asset",
        };

        private static Color[] tints =
        {
            new Color(.9f, .34f, .45f), //blob
            new Color(1f, .5f, .35f), //napalm
            new Color(.7f, .9f, .7f), //poison
            //new Color(.4f, .7f, .9f), //electricity
            new Color(.9f, .4f, .8f), //charm
        };

        private static List<GoopDefinition> goopDefs;
        public class RandomGoopTrailBehaviour : CompanionController
        {
            int goopIndex;
            float lastSwitch = 0;
            private const float switchTime = 10f;
            GoopDefinition currentGoop;
            Color tint = Color.white;

            void Start()
            {
                var bundle = ResourceManager.LoadAssetBundle("shared_auto_001");
                goopDefs = new List<GoopDefinition>();
                foreach (string goopName in goops)
                {
                    GoopDefinition goop;
                    try
                    {
                        var asset = bundle.LoadAsset(goopName) as GameObject;
                        goop = asset.GetComponent<GoopDefinition>();
                    }
                    catch
                    {
                        goop = bundle.LoadAsset(goopName) as GoopDefinition;
                    }
                    goop.name = goopName.Replace("assets/data/goops/", "").Replace(".asset", "");
                    goopDefs.Add(goop);
                }

                goopDefs.Add(PickupObjectDatabase.GetById(310)?.GetComponent<WingsItem>()?.RollGoop);
                SetGoopIndex(0);
                sprite.color = tints[0];
                spriteAnimator.Play("idle");
            }

            void FixedUpdate()
            {
                if (Time.time - lastSwitch > switchTime)
                {
                    SetGoopIndex(Random.Range(1, goopDefs.Count));

                    lastSwitch = Time.time;
                    this.aiActor.OverrideTarget = this.m_owner?.CurrentRoom?.GetRandomActiveEnemy(false)?.specRigidbody;
                }

                if (!this.m_owner.IsInCombat)
                    SetGoopIndex(0);

                sprite.color = Color.Lerp(sprite.color, tint, .1f);


                float circleSize = .5f;
                if (PassiveItem.IsFlagSetForCharacter(m_owner, typeof(BattleStandardItem)))
                    circleSize *= 2;

                var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(currentGoop);
                ddgm.AddGoopCircle(sprite.WorldCenter, circleSize);

                //if (currentGoop.name.Contains("water"))
                    //ddgm.ElectrifyGoopCircle(sprite.WorldBottomCenter, 1);
            }

            void SetGoopIndex(int index)
            {
                this.goopIndex = index;
                currentGoop = goopDefs[index];
                tint = tints[index];
            }

        }
    }
}
