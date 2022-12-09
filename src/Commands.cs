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

namespace CwaffingTheGungy
{
    public class Commands
    {
        public static void Init()
        {
            // Base command for doing whatever I'm testing at the moment
            ETGModConsole.Commands.AddGroup("gg", delegate (string[] args)
            {
                LootEngine.SpawnItem(
                    // PickupObjectDatabase.GetById(IDs.Actives["borrowed_time"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["shine"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["ki_blast"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Pickups["superstitious"]).gameObject,
                    PickupObjectDatabase.GetById(IDs.Pickups["deadline"]).gameObject,
                    GameManager.Instance.PrimaryPlayer.CenterPosition,
                    Vector2.zero,
                    0);
                // ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
            });
            // Another base command for loading my latest debug flow
            ETGModConsole.Commands.AddGroup("ff", delegate (string[] args)
            {
                FlowCommands.LoadFlowFunction(new string[]{"simplest"});
            });
            // Another base command for testing npc shenanigans
            ETGModConsole.Commands.AddGroup("tt", delegate (string[] args)
            {
                var bundle = ResourceManager.LoadAssetBundle("shared_auto_001");
                // foreach (string s in bundle.GetAllAssetNames())
                //     ETGModConsole.Log(s);
                GameObject gk = bundle.LoadAsset<GameObject>("npc_gunslingking");
                bundle = null;
                // return;
                PlayerController p1 = GameManager.Instance.PrimaryPlayer;
                // TalkDoer td = new TalkDoer();
                // td.BeginConversation(p1);
                // GameObject go = ItsDaFuckinShopApi.SetUpGenericNpc(
                //     "Boomhildr",
                //      "cg",
                //      new List<string>() {
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_001",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_002",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_003",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_004",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_005",
                //      },
                //      new List<string>()
                //      {
                //     "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_001",
                //     "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_002",
                //      },
                //      7,
                //      new Vector3(0.5f, 4, 0)
                // );
                // UnityEngine.Object.Instantiate(go,p1.sprite.WorldCenter+(new Vector2(2,2)),Quaternion.identity);
                // GameObject npc = UnityEngine.Object.Instantiate(go,p1.sprite.WorldCenter,Quaternion.identity);

                // GameObject npc = UnityEngine.Object.Instantiate(Boomhildr.boomhildrNPCObj,p1.sprite.WorldCenter,Quaternion.identity);
                // TalkDoerLite td = npc.GetComponent<TalkDoerLite>();
                // td.ForceTimedSpeech(":D");
                // Dissect.DumpFieldsAndProperties<TalkDoerLite>(td);
                // ScriptableObject.CreateInstance<DungeonPlaceable>()

                // GameObject npc = UnityEngine.Object.Instantiate(gk,p1.sprite.WorldCenter,Quaternion.identity);
                // // Dissect.DumpFieldsAndProperties<GameObject>(npc);
                // // Dissect.DumpComponents(npc);
                // // Dissect.DumpFieldsAndProperties<TalkDoerLite>(npc.GetComponent<TalkDoerLite>());
                // TalkDoerLite td  = npc.GetComponent<TalkDoerLite>();
                // td.ShowText(p1.sprite.WorldCenter,td.sprite.transform,1f,"testaroo");

                // SpawnObjectManager.SpawnObject(gk,p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3());
                // IPlayerInteractable ia;
                // ia.Interact()
                SpawnObjectManager.SpawnObject(Boomhildr.boomhildrNPCObj,p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3());

                // gk.GetComponent<TalkDoerLite>().InstantiateObject(p1.CurrentRoom,p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
            });
        }
    }

    public class CustomInteractible : BraveBehaviour, IPlayerInteractable
    {

        // // Token: 0x0400002F RID: 47
        // public Action<PlayerController, GameObject> OnAccept;

        // // Token: 0x04000030 RID: 48
        // public Action<PlayerController, GameObject> OnDecline;

        // // Token: 0x04000031 RID: 49
        // public List<string> conversation;

        // // Token: 0x04000032 RID: 50
        // public List<string> conversation2;

        // public List<string> conversation3;

        // public List<string> conversation4;

        // // Token: 0x04000033 RID: 51
        // public Func<PlayerController, GameObject, bool> CanUse;

        // Token: 0x04000034 RID: 52
        public Transform talkPoint;

        // // Token: 0x04000035 RID: 53
        // public string text;

        // // Token: 0x04000036 RID: 54
        // public string acceptText;

        // // Token: 0x04000037 RID: 55
        // public string acceptText2;

        // // Token: 0x04000038 RID: 56
        // public string declineText;

        // // Token: 0x04000039 RID: 57
        // public string declineText2;

        // Token: 0x0400003A RID: 58
        public bool isToggle;

        // Token: 0x0400003B RID: 59
        protected bool m_isToggled;

        // Token: 0x0400003C RID: 60
        protected bool m_canUse = true;

        private void Start()
        {
            // this.talkPoint = base.transform.Find("talkpoint");
            this.talkPoint = base.transform;
            this.m_isToggled = false;
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            this.m_canUse = true;
            base.spriteAnimator.Play("idle");
        }

        public void Interact(PlayerController interactor)
        {
            ETGModConsole.Log("we're interacting :D");
            bool flag = TextBoxManager.HasTextBox(this.talkPoint);
            if (!flag)
            {
                ETGModConsole.Log("we're in o:");
                // this.m_canUse = ((this.CanUse != null) ? this.CanUse(interactor, base.gameObject) : this.m_canUse);
                // bool flag2 = !this.m_canUse;
                bool flag2 = false;
                if (flag2)
                {
                    base.spriteAnimator.PlayForDuration("talk", 2f, "idle", false);
                    TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 2f, "No... not this time.", interactor.characterAudioSpeechTag, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
                }
                else
                {
                    base.StartCoroutine(this.HandleConversation(interactor));
                }
            }
        }

        // private IEnumerator HandleConversation(PlayerController interactor)
        // {
        //     TextBoxManager.ClearTextBox(this.talkPoint);
        //     TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, "hello thre", interactor.characterAudioSpeechTag, instant: false, showContinueText: true);
        //     yield return null;
        // }

        private IEnumerator HandleConversation(PlayerController interactor)
        {
            //ETGModConsole.Log("HandleConversation Started");
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.spriteAnimator.PlayForDuration("talk_start", 1, "talk");
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
            //ETGModConsole.Log("We made it to the while loop");
            while (conversationIndex < conversationToUse.Count - 1)
            {
                // Tools.Print($"Index: {conversationIndex}");
                TextBoxManager.ClearTextBox(this.talkPoint);
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationIndex], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);
                float timer = 0;
                while (!BraveInput.GetInstanceForPlayer(interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < 0.4f)
                {
                    timer += BraveTime.DeltaTime;
                    yield return null;
                }
                conversationIndex++;
            }
            //ETGModConsole.Log("We made it through the while loop");
            m_allowMeToIntroduceMyself = false;
            TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationToUse.Count - 1], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);

            var acceptanceTextToUse = "i accept";
            var declineTextToUse = "i decline";
            GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;
            //ETGModConsole.Log("We made it to the if statement");
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
            //ETGModConsole.Log("We made it through the if statement");

            // // Free player and run OnAccept/OnDecline actions
            interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.spriteAnimator.Play("idle");
            // //ETGModConsole.Log("We made it");
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

