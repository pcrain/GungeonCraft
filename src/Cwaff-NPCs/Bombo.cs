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

using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public enum OhNoMy
    {
        EYES,
        ARMS,
        LEGS,
        FINGERS,
        HEART,
        LUNGS,
        STOMACH,
        _last
    }
    public class Bombo : FancyNPC
    {
        public static GameObject npcobj;

        private bool strikingADeal = false;

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

        private void CutStat(PlayerController chump, PlayerStats.StatType stat, float amount)
        {
            StatModifier statModifier = new StatModifier();
                statModifier.statToBoost = stat;
                statModifier.amount = amount;
                statModifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                chump.ownerlessStatModifiers.Add(statModifier);
                chump.stats.RecalculateStats(chump, false, false);
        }

        private void RandomSacrifice(PlayerController chump)
        {
            OhNoMy sacrifice = (OhNoMy)UnityEngine.Random.Range(0, (int)OhNoMy._last);
            sacrifice        = OhNoMy.STOMACH;
            switch(sacrifice)
            {
                case OhNoMy.EYES:
                    CutStat(chump,PlayerStats.StatType.Accuracy,2.0f);
                    ETGModConsole.Log("lost your eyes"); break;
                case OhNoMy.ARMS:
                    CutStat(chump,PlayerStats.StatType.Damage,0.5f);
                    ETGModConsole.Log("lost your arms"); break;
                case OhNoMy.FINGERS:
                    CutStat(chump,PlayerStats.StatType.ReloadSpeed,1.5f);
                    CutStat(chump,PlayerStats.StatType.RateOfFire,0.75f);
                    ETGModConsole.Log("lost your fingers"); break;
                case OhNoMy.LEGS:
                    CutStat(chump,PlayerStats.StatType.MovementSpeed,0.6f);
                    ETGModConsole.Log("lost your legs"); break;
                case OhNoMy.HEART:
                    if (chump.characterIdentity == PlayableCharacters.Robot)
                        chump.healthHaver.Armor = 1;
                    else
                    {
                        chump.healthHaver.Armor = 0;
                        chump.healthHaver.currentHealth = 0.5f;
                    }
                    ETGModConsole.Log("lost your heart"); break;
                case OhNoMy.LUNGS:
                    chump.OnPreDodgeRoll -= Bombo.DodgeRollsAreExhausting;
                    chump.OnPreDodgeRoll += Bombo.DodgeRollsAreExhausting;
                    ETGModConsole.Log("lost your lungs"); break;
                case OhNoMy.STOMACH:
                    chump.GetExtComp().OnPickedUpHP -= Bombo.AppetiteLoss;
                    chump.GetExtComp().OnPickedUpHP += Bombo.AppetiteLoss;
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(73).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(85).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    ETGModConsole.Log("lost your stomach"); break;
            }
        }

        public static void DodgeRollsAreExhausting(PlayerController wimp)
        {
            wimp.StartCoroutine(Bombo.PreventDodgeRolling(wimp,0.5f));
        }

        public static void AppetiteLoss(PlayerController wimp, HealthPickup hp)
        {
            // ETGModConsole.Log(hp.armorAmount+" armor, "+hp.healAmount+" health");
            if (hp.armorAmount > 0)
                wimp.healthHaver.Armor -= hp.armorAmount;
            if (hp.healAmount > 0)
                wimp.healthHaver.currentHealth -= hp.healAmount;
            wimp.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/VFX_Curse") as GameObject, Vector3.zero);
            // wimp.StartCoroutine(Bombo.PreventDodgeRolling(wimp,0.5f));
        }

        public static IEnumerator PreventDodgeRolling(PlayerController wimp, float timer)
        {
            wimp.SetInputOverride("exhausted");
            yield return null;
            while (wimp.IsDodgeRolling)
                yield return null;
            yield return new WaitForSeconds(timer);
            wimp.ClearInputOverride("exhausted");
        }

        public IEnumerator StrikeADealScript(FakeShopItem f, PlayerController p)
        {
            if (this.m_interactor != null)
                yield break;
            this.m_interactor = p;
            this.m_interactor.SetInputOverride("npcConversation");

            GameUIRoot.Instance.DisplayPlayerConversationOptions(this.m_interactor, null, "do it", "don't do it");
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0) //accept
            {
                RandomSacrifice(p);
                // f.Purchased(p);
            }

            this.m_interactor.ClearInputOverride("npcConversation");
            this.m_interactor = null;
            yield break;
        }
    }
}
