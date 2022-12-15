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
    public class Bombo : FancyNPC
    {
        public static GameObject npcobj;

        public static void Init()
        {
            npcobj = FancyNPC.Setup<Bombo>(
                name          : "Bombo",
                prefix        : "cg",
                animationData : new List<SimpleAnimationData>() {
                   new SimpleAnimationData("idler",2, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle1",
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle2",
                       }),
                   new SimpleAnimationData("talker",8, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle1",
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-idle2",
                       }),
                   new SimpleAnimationData("annoyed",1, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-annoyed",
                       }),
                   new SimpleAnimationData("peeved",1, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/Bombo/bombo-peeved",
                       })
                }
                // talkPointAdjust : new Vector3(2.5f, 2.5f, 0)
                );
        }

        protected override IEnumerator NPCTalkingScript()
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
                base.aiAnimator.PlayUntilCancelled("annoyed");
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                GameManager.Options.TextSpeed = oldTextSpeed;
                base.aiAnimator.PlayUntilCancelled("peeved");
                this.ShowText("WELL WHO ASKED YOU?!",2f);
                Exploder.Explode(this.talkPoint.position, DerailGun.bigTrainExplosion, Vector2.zero);
            }
        }
    }
}
