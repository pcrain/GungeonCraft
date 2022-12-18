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
using MonoMod.RuntimeDetour;

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

        private bool strikingADeal;
        private bool hasBeenTalkedTo;
        private bool hasAppeared;
        private Vector2 itemCamPosition;

        private FakeShopItem item;

        public static Hook bomboHook;
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

            // Add a hook to spawn near the hero shrine at the beginning of the run
            bomboHook = new Hook(
                // typeof(PlayerController).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(Dungeon).GetMethod("FloorReached", BindingFlags.Public | BindingFlags.Instance),
                typeof(Bombo).GetMethod("SpawnNearHeroShrine"));
        }

        public static void SpawnNearHeroShrine(Action<Dungeon> orig, Dungeon self)
        {
            orig(self);
            ETGModConsole.Log("trying to spawn!");
            GameManager.Instance.PrimaryPlayer.StartCoroutine(SpawnInOnFirstFloor());
        }

        private static IEnumerator SpawnInOnFirstFloor()
        {
            PlayerController p1 = GameManager.Instance.PrimaryPlayer;
            while (!p1.AcceptingAnyInput)
                yield return null;  //wait for level to fully load

            Vector3 v3 = Vector3.zero;
            bool found = false;
            foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
            {
                if (a.IsLegendaryHeroShrine && a.transform.position.GetAbsoluteRoom() == p1.CurrentRoom)
                {
                    ETGModConsole.Log("found it!");
                    found = true;
                    v3 = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2,-8)).ToVector3YUp(0);
                }
            }
            if (!found)
                yield break; //no hero shrine found, not the 1st floor

            Bombo bombyboi = SpawnObjectManager.SpawnObject(Bombo.npcobj,v3).GetComponent<Bombo>();

            // yield return null;
            // bombyboi.GetComponent<Bombo>().AppearInAPuffOfSmoke();
        }

        protected override void Start()
        {
            base.Start();
            base.renderer.enabled = false;
            this.canInteract      = false;
            this.hasBeenTalkedTo  = false;
            this.strikingADeal    = false;
            this.hasAppeared      = false;
        }

        protected override IEnumerator NPCTalkingScript()
        {
            if (this.hasBeenTalkedTo)
            {
                this.ShowText("You like it? Go on, check it out!",1.0f);
                yield break;
            }

            this.hasBeenTalkedTo = true;
            GameObject bombyPickup;
                if (UnityEngine.Random.Range(0,2) == 0)
                    bombyPickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(
                                    PickupObject.ItemQuality.S, GameManager.Instance.RewardManager.GunsLootTable, false).gameObject;
                else
                    bombyPickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(
                                    PickupObject.ItemQuality.S, GameManager.Instance.RewardManager.ItemsLootTable, false).gameObject;
            PickupObject po = bombyPickup.GetComponent<PickupObject>();

            List<string> conversation = new List<string> {
                "Hey buddy, what's good!",
                "Listen up, I've got a *real* nice item for you.",
                };

            yield return StartCoroutine(Converse(conversation,"talker","idler"));

            GameObject bombyPos = new GameObject("ItemPoint3");
                bombyPos.transform.parent = this.transform;
                bombyPos.transform.position = this.transform.position + new Vector3(0f, -3f, 0f);
            GameObject bombyItem = new GameObject("Fake shop item test");
                bombyItem.transform.parent        = bombyPos.transform;
                bombyItem.transform.localPosition = Vector3.zero;
                bombyItem.transform.position      = Vector3.zero;
            item = bombyItem.AddComponent<FakeShopItem>();
                if (!this.transform.position.GetAbsoluteRoom().IsRegistered(item))
                    this.transform.position.GetAbsoluteRoom().RegisterInteractable(item);
                item.purchasingScript = this.StrikeADealScript;
                item.Initialize(po);
            AkSoundEngine.PostEvent("Play_ENM_spawn_appear_01", base.gameObject);

            GameObject smoke = (GameObject)UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
            tk2dBaseSprite smokesprite = smoke.GetComponent<tk2dBaseSprite>();
            smokesprite.PlaceAtPositionByAnchor(po.sprite.WorldCenter.ToVector3ZUp(0f), tk2dBaseSprite.Anchor.MiddleCenter);
            smokesprite.transform.position = bombyItem.transform.parent.position.Quantize(0.0625f);
            smokesprite.HeightOffGround = 5f;
            smokesprite.UpdateZDepth();

            itemCamPosition = (bombyItem.transform.parent.PositionVector2() + new Vector2(0f,3.5f));
            GameManager.Instance.MainCameraController.OverridePosition = itemCamPosition;
            GameManager.Instance.MainCameraController.SetManualControl(true, true);

            yield return new WaitForSeconds(0.8f);

            List<string> conversation2 = new List<string> {
                "Looks good doesn't it?",
                "It won't cost you any shells, keys, blanks, or anything like that!",
                "No no, all I ask for in return...",
                };

            yield return StartCoroutine(Converse(conversation2,"point"));
            base.aiAnimator.PlayUntilCancelled("sad");
            yield return new WaitForSeconds(1.25f);

            List<string> conversation3 = new List<string> {
                "...is a little bit of your " + sacNames[(int)item.sacType] + "!",
                "Of course I mean that figuratively, not literally.",
                };
            yield return StartCoroutine(Converse(conversation3,"point"));

            this.ShowText("...........",1.5f);
            yield return new WaitForSeconds(1.5f);

            List<string> conversation4 = new List<string> {
                "So whaddaya say?",
                };
            yield return StartCoroutine(Converse(conversation4,"point"));

            GameManager.Instance.MainCameraController.SetManualControl(false, true);

            yield break;
        }

        protected override void Update()
        {
            base.Update();
            if (this.hasAppeared)
                return;

            float dist = Vector2.Distance(base.sprite.WorldCenter,GameManager.Instance.PrimaryPlayer.sprite.WorldCenter);
            if (dist < 5f)
                return;

            this.AppearInAPuffOfSmoke();
            this.hasAppeared      = true;
            this.canInteract      = true;
            base.renderer.enabled = true;
            base.aiAnimator.PlayUntilCancelled("talker");
            this.ShowText("Hey buddy! Over here!", 1f);
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
            wimp.StartCoroutine(Bombo.PreventDodgeRolling(wimp,0.65f));
        }

        public static void AppetiteLoss(PlayerController wimp, HealthPickup hp)
        {
            // ETGModConsole.Log(hp.armorAmount+" armor, "+hp.healAmount+" health");
            if (hp.armorAmount > 0)
                wimp.healthHaver.Armor -= hp.armorAmount;
            if (hp.healAmount > 0)
                wimp.healthHaver.currentHealth -= hp.healAmount;
            wimp.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/VFX_Curse") as GameObject, Vector3.zero);
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

        private IEnumerator SacrificeCutsceneScript(PlayerController p)
        {
            // Make some fancy particle effects
            VFXPool v  = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(45) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            VFXPool v2 = (PickupObjectDatabase.GetById(519) as Gun).DefaultModule.projectiles[0].hitEffects.tileMapVertical;
            GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.35f,6f,2.0f,0f), null);
            for (int i = 0; i < 30; ++i)
            {
                Vector2 ppos = p.sprite.WorldCenter + Lazy.AngleToVector(i*UnityEngine.Random.Range(0f,360f),2f);
                v.SpawnAtPosition(ppos.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
                // AkSoundEngine.PostEvent("Play_OBJ_crystal_shatter_01", base.gameObject);
                AkSoundEngine.PostEvent("Play_ENM_cannonball_explode_01", p.gameObject);

                if (i % 3 == 0)
                {
                    if (i % 6 == 0)
                        Pixelator.Instance.CustomFade(0.1875f, 0f, Color.black, Color.black, 1.0f, 0.5f);
                    else
                        Pixelator.Instance.CustomFade(0.1875f, 0f, Color.black, Color.black, 0.5f, 1.0f);
                }
                yield return new WaitForSeconds(0.0625f);
            }
            Vector2 ppos2 = p.sprite.WorldCenter + new Vector2(0f,-0.05f);
            v2.SpawnAtPosition(ppos2.ToVector3ZisY(1f), 0, null, null, null, 5f);
            AkSoundEngine.PostEvent("Play_OBJ_lightning_flash_01", base.gameObject);
            Pixelator.Instance.FadeToColor(0.5f, Color.white, false, 0f);
            yield return new WaitForSeconds(0.5f);
            Pixelator.Instance.FadeToColor(0.5f, Color.white, true, 0f);
            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator StrikeADealScript(FakeShopItem f, PlayerController p)
        {
            if (!(CanBeginConversation()))
                yield break;
            BeginConversation(p);

            ETGModConsole.Log("here1");
            GameManager.Instance.MainCameraController.OverridePosition = itemCamPosition;
            GameManager.Instance.MainCameraController.SetManualControl(true, true);
            ETGModConsole.Log("here2");

            List<string> conversation = new List<string> {
                "You like it, huh?",
                "So we got a deal?",
                };
            ETGModConsole.Log("here3");
            yield return StartCoroutine(Converse(conversation,"point"));
            ETGModConsole.Log("here4");

            yield return StartCoroutine(Prompt(
                "sacrifice your [color #ff8888]"+sacNames[(int)f.sacType]+"[/color] ("+sacDescriptions[(int)f.sacType]+")",
                "actually I rather like having my "+sacNames[(int)f.sacType]
                ));

            if (PromptResult() == 1) //decline
            {
                this.ShowText("Alright, suit yourself!", 1f);
                GameManager.Instance.MainCameraController.SetManualControl(false, true);
                EndConversation();
                yield break;
            }

            f.Purchased(p);
            List<string> conversation2 = new List<string> {
                "Excellent!",
                "I'm just gonna perform a quick little ritual to take your "+sacNames[(int)f.sacType]+" and you'll be on your way.",
                "It shouldn't hurt a bit!",
                "...at least nobody's ever complained about it, anyhow.",
                "Here we go",
                };
            yield return StartCoroutine(Converse(conversation2,"point"));

            yield return StartCoroutine(SacrificeCutsceneScript(p));
            RandomSacrifice(p,f.sacType);

            GameManager.Instance.MainCameraController.SetManualControl(false, true);
            EndConversation();

            this.ShowText("Pleasure working with ya!", 1f);
            AppearInAPuffOfSmoke();
            this.renderer.enabled = false;
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);  //hack to make the sprite immediately disappear
            this.transform.position.GetAbsoluteRoom().DeregisterInteractable(this);
            yield return new WaitForSeconds(1.1f);

            UnityEngine.Object.Destroy(base.gameObject);
            yield break;
        }
    }
}
