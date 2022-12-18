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
        _random = -1,
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

        public static List<string> sacNames        = new List<string> ( new string[(int)OhNoMy._last] );
        public static List<string> sacDescriptions = new List<string> ( new string[(int)OhNoMy._last] );

        private bool strikingADeal = false;

        public static void Init()
        {
            sacNames[(int)OhNoMy.EYES]    = "eyes";
            sacNames[(int)OhNoMy.ARMS]    = "arms";
            sacNames[(int)OhNoMy.LEGS]    = "legs";
            sacNames[(int)OhNoMy.FINGERS] = "fingers";
            sacNames[(int)OhNoMy.HEART]   = "heart";
            sacNames[(int)OhNoMy.LUNGS]   = "lungs";
            sacNames[(int)OhNoMy.STOMACH] = "stomach";

            sacDescriptions[(int)OhNoMy.EYES]    = "shot accuracy down";
            sacDescriptions[(int)OhNoMy.ARMS]    = "damage down";
            sacDescriptions[(int)OhNoMy.LEGS]    = "movement speed down";
            sacDescriptions[(int)OhNoMy.FINGERS] = "fire & reload speed down";
            sacDescriptions[(int)OhNoMy.HEART]   = "down to 1 HP";
            sacDescriptions[(int)OhNoMy.LUNGS]   = "dodge rolls require rest";
            sacDescriptions[(int)OhNoMy.STOMACH] = "no healing from health and armor";

            npcobj = FancyNPC.Setup<Bombo>(
                name          : "Bombo",
                prefix        : "cg",
                animationData : new List<SimpleAnimationData>() {
                   new SimpleAnimationData("idler",3, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites3",
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites4",
                       }),
                   new SimpleAnimationData("talker",5, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites1",
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites2",
                       }),
                   new SimpleAnimationData("sad",3, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites5",
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites6",
                       }),
                   new SimpleAnimationData("point",3, new List<string>() {
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites9",
                       "CwaffingTheGungy/Resources/NPCSprites/GrinReaper/grin-allsprites10",
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
                base.aiAnimator.PlayUntilCancelled("point");
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
                base.aiAnimator.PlayUntilCancelled("sad");
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                this.ShowText("...........",1f);
                yield return new WaitForSeconds(1f);
                GameManager.Options.TextSpeed = oldTextSpeed;
                base.aiAnimator.PlayUntilCancelled("idler");
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

        private void RandomSacrifice(PlayerController chump, OhNoMy sacType = OhNoMy._random)
        {
            OhNoMy sacrifice;
            if (sacType == OhNoMy._random)
                sacrifice = (OhNoMy)UnityEngine.Random.Range(0, (int)OhNoMy._last);
            else
                sacrifice = sacType;
            // sacrifice        = OhNoMy.STOMACH;
            switch(sacrifice)
            {
                case OhNoMy.EYES:
                    CutStat(chump,PlayerStats.StatType.Accuracy,2.0f);
                    ETGModConsole.Log("lost your eyes"); break;
                case OhNoMy.ARMS:
                    CutStat(chump,PlayerStats.StatType.Damage,0.6f);
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

            GameUIRoot.Instance.DisplayPlayerConversationOptions(
                this.m_interactor,
                null,
                "sacrifice your [color #ff8888]\""+sacNames[(int)f.sacType]+"\"[/color] ("+sacDescriptions[(int)f.sacType]+")",
                "actually I rather like having my "+sacNames[(int)f.sacType]
                );
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0) //accept
            {
                RandomSacrifice(p,f.sacType);
                f.Purchased(p);

                VFXPool v  = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(45) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
                // VFXPool v2 = (PickupObjectDatabase.GetById(45) as Gun).DefaultModule.projectiles[0].hitEffects.enemy;
                VFXPool v2 = (PickupObjectDatabase.GetById(519) as Gun).DefaultModule.projectiles[0].hitEffects.tileMapVertical;
                for (int i = 0; i < 33; ++i)
                {
                    Vector2 ppos = p.sprite.WorldCenter + Lazy.AngleToVector(i*UnityEngine.Random.Range(0f,360f),2f);
                    v.SpawnAtPosition(ppos.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
                    // AkSoundEngine.PostEvent("Play_OBJ_crystal_shatter_01", base.gameObject);
                    AkSoundEngine.PostEvent("Play_ENM_cannonball_explode_01", p.gameObject);

                    Pixelator.Instance.FadeToColor(0.03f, Color.black, true, 0.03f);
                    yield return new WaitForSeconds(0.0625f);
                }
                Vector2 ppos2 = p.sprite.WorldCenter + new Vector2(0f,-0.05f);
                // v2.effects[0].effects[0].zHeight
                v2.SpawnAtPosition(ppos2.ToVector3ZisY(1f), 0, null, null, null, 5f);
                // AkSoundEngine.PostEvent("Play_OBJ_crystal_shatter_01", base.gameObject);
                // AkSoundEngine.PostEvent("Play_BOSS_dragun_thunder_01", base.gameObject);

                AkSoundEngine.PostEvent("Play_OBJ_lightning_flash_01", base.gameObject);
                // AkSoundEngine.PostEvent("Play_ENV_thunder_flash_01", base.gameObject);
                Pixelator.Instance.FadeToColor(0.1f, Color.white, true, 0.05f);
                yield return new WaitForSeconds(0.15f);
                Pixelator.Instance.FadeToColor(0.1f, Color.white, true, 0.05f);
                yield return new WaitForSeconds(0.1f);
                GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(), null);
                // GameManager.Instance.MainCameraController.DoScreenShake(ThunderShake, null);
                yield return null;
            }

            this.m_interactor.ClearInputOverride("npcConversation");
            this.m_interactor = null;

            // vanish in a puff of smoke
            if (selectedResponse == 0)
            {
                  GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                  tk2dBaseSprite component2 = gameObject2.GetComponent<tk2dBaseSprite>();
                  component2.PlaceAtPositionByAnchor(base.sprite.WorldCenter.ToVector3ZUp(0f), tk2dBaseSprite.Anchor.MiddleCenter);
                  component2.transform.position = component2.transform.position.Quantize(0.0625f);
                  component2.HeightOffGround = 5f;
                  component2.UpdateZDepth();
                  p.CurrentRoom.DeregisterInteractable(this);
                  UnityEngine.Object.Destroy(base.gameObject);
            }
            yield break;
        }
    }
}
