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
                talkPointOffset : new Vector3(0.5f, 0, 0)
                // talkPointOffset : new Vector3(0.5f, 4, 0)
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
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, "idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, "talk", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

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
                        Prefix = "idle",
                        AnimNames = new string[] {""},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    };

                    aIAnimator.TalkAnimation = new DirectionalAnimation
                    {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = "talk",
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
        protected PlayerController m_interactor;

        private Vector3 talkPointOffset;

        // minimum amount of time to show textboxes during interactive dialogue
        private const float MIN_TEXTBOX_TIME = 0.5f;

        private void Start()
        {
            this.talkPoint = base.transform;
            // base.sprite.SetSprite(base.sprite.GetSpriteIdByName("talk"));
            Vector3 size = base.sprite.GetCurrentSpriteDef().position3;
            // base.sprite.SetSprite(base.sprite.GetSpriteIdByName("idle"));
            this.talkPointOffset = new Vector3(size.x / 2, size.y, 0);
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
        }

        public void Interact(PlayerController interactor)
        {
            if (TextBoxManager.HasTextBox(this.talkPoint))
                return;
            if (this.m_interactor != null)
                return;
            base.StartCoroutine(this.HandleConversation(interactor));
        }

        protected void ShowText(string convoLine, float autoContinueTimer = -1f)
        {
            if (this.m_interactor == null)
            {
                ETGModConsole.Log("trying to talk with null interactor!");
                return;
            }
            TextBoxManager.ShowTextBox(
                this.talkPoint.position + this.talkPointOffset,
                this.talkPoint,
                autoContinueTimer,
                convoLine,
                this.m_interactor.characterAudioSpeechTag,
                instant: false,
                showContinueText: true
                );
        }

        private IEnumerator HandleConversation(PlayerController interactor)
        {
            // Verify we can actually interact with this interactible
            this.m_interactor = interactor;
            if (!this.m_canUse)
            {
                base.aiAnimator.PlayForDuration("talk", 2f);
                this.ShowText("I have nothing to say right now.", 2f);
                this.m_interactor = null;
                yield break;
            }

            // Set up input overrides and letterboxing
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            this.m_interactor.SetInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
            yield return null;

            // Run the actual script
            IEnumerator script = NPCTalkingScript();
            while(script.MoveNext())
                yield return script.Current;

            // Tear down input overrides and letterboxing
            TextBoxManager.ClearTextBox(this.talkPoint);
            this.m_interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.aiAnimator.PlayUntilCancelled("idle");
            this.m_interactor = null;  //if this method is overridden, needs to be set to null after conversation is done
        }

        protected virtual IEnumerator NPCTalkingScript()
        {
            List<string> conversation = new List<string> {
                "Hey guys!",
                "Got custom NPCs working o:",
                "Neat huh?",
                };

            for (int ci = 0; ci < conversation.Count - 1; ci++)
            {
                TextBoxManager.ClearTextBox(this.talkPoint);
                base.aiAnimator.PlayUntilCancelled("talk");
                this.ShowText(conversation[ci]);
                float timer = 0;
                while (!BraveInput.GetInstanceForPlayer(this.m_interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < MIN_TEXTBOX_TIME)
                {
                    timer += BraveTime.DeltaTime;
                    bool npcIsTalking = TextBoxManager.TextBoxCanBeAdvanced(this.talkPoint);
                    if (timer >= MIN_TEXTBOX_TIME && !npcIsTalking)
                        base.aiAnimator.PlayUntilCancelled("idle");
                    yield return null;
                }
                base.aiAnimator.PlayUntilCancelled("idle");
            }
            this.ShowText(conversation[conversation.Count-1]);

            // var acceptanceTextToUse = "i accept" + " (" + 5 + "[sprite \"ui_coin\"])";
            // var declineTextToUse = "i decline" + " (" + 5 + "[sprite \"hbux_text_icon\"])";
            var acceptanceTextToUse = "Very neat! :D";
            var declineTextToUse = "Not impressed. :/";
            GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, acceptanceTextToUse, declineTextToUse);
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
        }

        public void OnEnteredRange(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
            base.sprite.UpdateZDepth();
        }

        public void OnExitRange(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
        }

        public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
        {
            shouldBeFlipped = false;
            return string.Empty;
        }

        public float GetDistanceToPoint(Vector2 point)
        {
            if (base.sprite == null)
                return 100f;
            Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
            return Vector2.Distance(point, v) / 1.5f;
        }

        public float GetOverrideMaxDistance()
        {
            return -1f;
        }
    }
}

