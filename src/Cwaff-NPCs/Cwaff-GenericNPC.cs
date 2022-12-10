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
                name            : "Bombo",
                prefix          : "cg",
                idleSpritePaths : new List<string>() {
                   "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle1",
                   "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle2",
                   },
                idleFps         : 2,
                talkSpritePaths : new List<string>() {
                   "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle1",
                   "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle2",
                   },
                talkFps         : 8
                // talkPointAdjust : new Vector3(2.5f, 2.5f, 0)
                );
        }

        public static GameObject SetUpGenericNPCObject(string name, string prefix, List<string> idleSpritePaths, int idleFps, List<string> talkSpritePaths, int talkFps, Vector3? talkPointAdjust = null)
        {
            AssetBundle shared_auto_001 = null;
            try
            {
                shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");

                GameObject npcObj = SpriteBuilder.SpriteFromResource(idleSpritePaths[0], new GameObject(prefix + ":" + name));
                    FakePrefab.MarkAsFakePrefab(npcObj);
                    UnityEngine.Object.DontDestroyOnLoad(npcObj);
                    npcObj.SetActive(false);
                    npcObj.layer = 22;
                    npcObj.name = prefix + ":" + name;

                tk2dSpriteCollectionData collection = npcObj.GetComponent<tk2dSprite>().Collection;
                    var idleIdsList = new List<int>();
                    foreach (string sprite in idleSpritePaths)
                    {
                        int fid = SpriteBuilder.AddSpriteToCollection(sprite, collection);
                        idleIdsList.Add(fid);
                        collection.spriteDefinitions[fid].ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    }
                    var talkIdsList = new List<int>();
                    foreach (string sprite in talkSpritePaths)
                    {
                        int fid = SpriteBuilder.AddSpriteToCollection(sprite, collection);
                        talkIdsList.Add(fid);
                        collection.spriteDefinitions[fid].ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);
                    }

                tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, "idler", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
                    SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, "talker", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

                AIAnimator aIAnimator = ItsDaFuckinShopApi.GenerateBlankAIAnimator(npcObj);
                    aIAnimator.spriteAnimator  = spriteAnimator;
                    aIAnimator.OtherAnimations = Lazy.EasyNamedDirectionalAnimations(new string[]{"idler","talker"});

                SpeculativeRigidbody rigidbody = ItsDaFuckinShopApi.GenerateOrAddToRigidBody(npcObj, CollisionLayer.BulletBlocker, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, new IntVector2(20, 18), new IntVector2(5, 0));

                CustomInteractible ci = npcObj.AddComponent<CustomInteractible>();
                    ci.talkPointAdjustment = talkPointAdjust.HasValue ? talkPointAdjust.Value : Vector3.zero;

                UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
                    dreamLuck.goopRadius = 2;
                    dreamLuck.beamRadius = 2;
                    dreamLuck.bulletRadius = 2;
                    dreamLuck.bulletSpeedModifier = 0.8f;
                    dreamLuck.vfxOffset = 0.625f;
                    dreamLuck.sparkOctantVFX = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

                shared_auto_001 = null; //this fixes crashes apparently
                return npcObj;
            }
            catch (Exception message)
            {
                ETGModConsole.Log(message.ToString());
                shared_auto_001 = null; //this fixes crashes apparently
                return null;
            }
        }
    }
    public class CustomInteractible : BraveBehaviour, IPlayerInteractable
    {
        public Transform talkPoint;
        public Vector3 talkPointAdjustment;

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
            this.talkPointOffset = new Vector3(size.x / 2, size.y, 0) + this.talkPointAdjustment;
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.aiAnimator.PlayUntilCancelled("idler");
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
                base.aiAnimator.PlayForDuration("talker", 2f);
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
            // TextBoxManager.ClearTextBox(this.talkPoint);
            this.m_interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.aiAnimator.PlayUntilCancelled("idler");
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
                base.aiAnimator.PlayUntilCancelled("talker");
                base.sprite.FlipX = true;
                this.ShowText(conversation[ci]);
                float timer = 0;
                bool playingTalkingAnimation = true;
                while (!BraveInput.GetInstanceForPlayer(this.m_interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < MIN_TEXTBOX_TIME)
                {
                    timer += BraveTime.DeltaTime;
                    bool npcIsTalking = TextBoxManager.TextBoxCanBeAdvanced(this.talkPoint);
                    if (playingTalkingAnimation && timer >= MIN_TEXTBOX_TIME && !npcIsTalking)
                    {
                        playingTalkingAnimation = false;
                        base.aiAnimator.PlayUntilCancelled("idler");
                        base.sprite.FlipX = false;
                    }
                    yield return null;
                }
                base.aiAnimator.PlayUntilCancelled("idler");
            }
            this.ShowText(conversation[conversation.Count-1]);

            // var acceptanceTextToUse = "i accept" + " (" + 5 + "[sprite \"ui_coin\"])";
            // var declineTextToUse = "i decline" + " (" + 5 + "[sprite \"hbux_text_icon\"])";
            var acceptanceTextToUse = "Very neat! :D";
            var declineTextToUse = "Not impressed. :/" + " (pay " + 99 + "[sprite \"hbux_text_icon\"] to disagree)";
            GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0)
            {
                this.ShowText("Yay! :D Have some money!",2f);
                for(int i = 0; i < 30; ++i)
                {
                    LootEngine.SpawnCurrency(this.talkPoint.position, 1, false, Lazy.AngleToVector(360f*UnityEngine.Random.value), 0, 4);
                    yield return null;
                    yield return null;
                }
            }
            else
            {
                var oldTextSpeed = GameManager.Options.TextSpeed;
                GameManager.Options.TextSpeed = GameOptions.GenericHighMedLowOption.LOW;
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                GameManager.Options.TextSpeed = oldTextSpeed;
                this.ShowText("WELL WHO ASKED YOU?!",2f);
                Exploder.Explode(this.talkPoint.position, DerailGun.bigTrainExplosion, Vector2.zero);
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

