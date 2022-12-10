using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;
using NpcApi;

using GungeonAPI;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemAPI;
using System.Reflection;
using static NpcApi.CustomShopController;

namespace CwaffingTheGungy
{
    public static class CwaffNPC
    {
        public static GameObject testNPCObj;

        public static void InitAllNPCs()
        {
            testNPCObj = CwaffNPC.SetUpGenericNPCObject(
                name            : "Boomhildr",
                prefix          : "cg",
                idleSpritePaths : new List<string>() {
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_001",
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_002",
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_003",
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_004",
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_005",
                   },
                idleFps         : 7,
                talkSpritePaths : new List<string>() {
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_001",
                   "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_002",
                   },
                talkFps         : 3,
                talkPointOffset : new Vector3(0.5f, 4, 0)
                );
        }

        public static GameObject SetUpGenericNPCObject(string name, string prefix, List<string> idleSpritePaths, int idleFps, List<string> talkSpritePaths, int talkFps, Vector3 talkPointOffset)
        {
            try
            {
                AssetBundle shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");

                GameObject npcObj = SpriteBuilder.SpriteFromResource(idleSpritePaths[0], new GameObject(prefix + ":" + name));
                    FakePrefab.MarkAsFakePrefab(npcObj);
                    UnityEngine.Object.DontDestroyOnLoad(npcObj);
                    npcObj.SetActive(false);
                    npcObj.layer = 22;
                    npcObj.name = prefix + ":" + name;

                GameObject SpeechPoint = new GameObject("SpeechPoint");
                    SpeechPoint.transform.position = talkPointOffset;
                    SpeechPoint.transform.parent = npcObj.transform;
                    FakePrefab.MarkAsFakePrefab(SpeechPoint);
                    UnityEngine.Object.DontDestroyOnLoad(SpeechPoint);
                    SpeechPoint.SetActive(true);

                tk2dSpriteCollectionData collection = npcObj.GetComponent<tk2dSprite>().Collection;
                    var idleIdsList = new List<int>();
                    foreach (string sprite in idleSpritePaths)
                        idleIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));
                    var talkIdsList = new List<int>();
                    foreach (string sprite in talkSpritePaths)
                        talkIdsList.Add(SpriteBuilder.AddSpriteToCollection(sprite, collection));

                tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, name + "_idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, name + "_talk", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

                SpeculativeRigidbody rigidbody = ItsDaFuckinShopApi.GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

                CustomInteractible ci = npcObj.AddComponent<CustomInteractible>();

                UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
                    dreamLuck.goopRadius = 2;
                    dreamLuck.beamRadius = 2;
                    dreamLuck.bulletRadius = 2;
                    dreamLuck.bulletSpeedModifier = 0.8f;
                    dreamLuck.vfxOffset = 0.625f;
                    dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

                AIAnimator aIAnimator = ItsDaFuckinShopApi.GenerateBlankAIAnimator(npcObj);
                    aIAnimator.spriteAnimator = spriteAnimator;
                    aIAnimator.IdleAnimation = new DirectionalAnimation
                    {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = name + "_idle",
                        AnimNames = new string[] {""},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    };

                    aIAnimator.TalkAnimation = new DirectionalAnimation
                    {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = name + "_talk",
                        AnimNames = new string[] {""},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    };

                return npcObj;
            }
            catch (Exception message)
            {
                ETGModConsole.Log(message.ToString());
                return null;
            }
        }
    }
    public class CustomInteractible : BraveBehaviour, IPlayerInteractable
    {
        // Token: 0x04000034 RID: 52
        public Transform talkPoint;

        // Token: 0x0400003C RID: 60
        protected bool m_canUse = true;

        private void Start()
        {
            // this.talkPoint = base.transform.Find("talkpoint");
            this.talkPoint = base.transform;
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            this.m_canUse = true;
            // base.spriteAnimator.Play("idle");
            // base.spriteAnimator.Play("Boomhildr_talk");
        }

        public void Interact(PlayerController interactor)
        {
            if (TextBoxManager.HasTextBox(this.talkPoint))
                return;
            if (!this.m_canUse)
            {
                base.spriteAnimator.PlayForDuration("talk", 2f, "idle", false);
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 2f, "No... not this time.", interactor.characterAudioSpeechTag, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
                return;
            }
            base.StartCoroutine(this.HandleConversation(interactor));
        }

        private IEnumerator HandleConversation(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.spriteAnimator.PlayForDuration("Boomhildr_talk", -1f, "Boomhildr_talk");
            interactor.SetInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
            yield return null;

            List<string> conversation = new List<string>
            {
                "All things wise and wonderful...",
                "All creatures great and small...",
                "All things bright and beautiful...",
                "I've cursed them, one and all..."
            };

            var conversationToUse = conversation;
            int conversationIndex = 0;
            while (conversationIndex < conversationToUse.Count - 1)
            {
                TextBoxManager.ClearTextBox(this.talkPoint);
                // base.spriteAnimator.PlayForDuration("talk", talkDuration, "talk");
                // base.spriteAnimator.PlayForDuration("Boomhildr_talk",-1f,"Boomhildr_talk");
                base.aiAnimator.PlayUntilCancelled("Boomhildr_talk");
                string convoLine = conversationToUse[conversationIndex];
                TextBoxManager.ShowTextBox(
                    this.talkPoint.position,
                    this.talkPoint,
                    -1f,
                    convoLine,
                    interactor.characterAudioSpeechTag,
                    instant: false,
                    showContinueText: true
                    );
                float minDuration = 2.0f;
                float timer = 0;
                while (!BraveInput.GetInstanceForPlayer(interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < minDuration)
                {
                    timer += BraveTime.DeltaTime;
                    bool npcIsTalking = TextBoxManager.TextBoxCanBeAdvanced(this.talkPoint);
                    if(timer >= minDuration && !npcIsTalking)
                        base.aiAnimator.PlayUntilCancelled("Boomhildr_idle");
                    yield return null;
                }
                base.aiAnimator.PlayUntilCancelled("Boomhildr_idle");
                conversationIndex++;
            }
            m_allowMeToIntroduceMyself = false;
            TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationToUse.Count - 1], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);

            var acceptanceTextToUse = "i accept" + " (" + 5 + "[sprite \"ui_coin\"])";
            var declineTextToUse = "i decline" + " (" + 5 + "[sprite \"hbux_text_icon\"])";
            GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0)
            {
                // TextBoxManager.ClearTextBox(this.talkPoint);
                // base.spriteAnimator.PlayForDuration("do_effect", -1, "talk");
                // OnAccept?.Invoke(interactor, this.gameObject);
                // base.spriteAnimator.Play("talk");
                // TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "It is done...", interactor.characterAudioSpeechTag, instant: false);
                // yield return new WaitForSeconds(1f);
            }
            else
            {
                // OnDecline?.Invoke(interactor, this.gameObject);
                // TextBoxManager.ClearTextBox(this.talkPoint);
            }

            TextBoxManager.ClearTextBox(this.talkPoint);

            // // Free player and run OnAccept/OnDecline actions
            interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            // base.spriteAnimator.Play("idle");
            base.spriteAnimator.Play("Boomhildr_talk");
            ETGModConsole.Log("donezo");
        }

        // Token: 0x06000062 RID: 98 RVA: 0x0000556A File Offset: 0x0000376A
        public void OnEnteredRange(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
            base.sprite.UpdateZDepth();
        }

        // Token: 0x06000063 RID: 99 RVA: 0x00005595 File Offset: 0x00003795
        public void OnExitRange(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        }

        // Token: 0x06000064 RID: 100 RVA: 0x000055B4 File Offset: 0x000037B4
        public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
        {
            shouldBeFlipped = false;
            return string.Empty;
        }

        // Token: 0x06000065 RID: 101 RVA: 0x000055D0 File Offset: 0x000037D0
        public float GetDistanceToPoint(Vector2 point)
        {
            bool flag = base.sprite == null;
            float result;
            if (flag)
            {
                result = 100f;
            }
            else
            {
                Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
                result = Vector2.Distance(point, v) / 1.5f;
            }
            return result;
        }

        // Token: 0x06000066 RID: 102 RVA: 0x00005630 File Offset: 0x00003830
        public float GetOverrideMaxDistance()
        {
            return -1f;
        }

        // Token: 0x0400002D RID: 45
        private bool m_allowMeToIntroduceMyself = true;
    }
}

