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
        BRAIN,
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

        private static ParticleSystem sweat;
        private static bool brainless;

        public static Hook bomboHook;
        public static void Init()
        {
            sacNames[(int)OhNoMy.BRAIN]   = "brain";
            sacNames[(int)OhNoMy.EYES]    = "eyes";
            sacNames[(int)OhNoMy.ARMS]    = "arms";
            sacNames[(int)OhNoMy.LEGS]    = "legs";
            sacNames[(int)OhNoMy.FINGERS] = "fingers";
            sacNames[(int)OhNoMy.HEART]   = "heart";
            sacNames[(int)OhNoMy.LUNGS]   = "lungs";
            sacNames[(int)OhNoMy.STOMACH] = "stomach";

            sacDescriptions[(int)OhNoMy.BRAIN]   = "remove most UI information";
            sacDescriptions[(int)OhNoMy.EYES]    = "lower shot accuracy & enemy visibility";
            sacDescriptions[(int)OhNoMy.ARMS]    = "damage, reload speed, & fire rate down";
            sacDescriptions[(int)OhNoMy.LEGS]    = "movement speed down";
            sacDescriptions[(int)OhNoMy.FINGERS] = "chance to drop gun upon firing";
            sacDescriptions[(int)OhNoMy.HEART]   = "lose most of your health & armor";
            sacDescriptions[(int)OhNoMy.LUNGS]   = "dodge rolls require rest";
            sacDescriptions[(int)OhNoMy.STOMACH] = "no healing from health & armor";

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

            // Set up sweat particles
            sweat = FakePrefab.Clone(PickupObjectDatabase.GetById(449).GetComponent<TeleporterPrototypeItem>().TelefragVFXPrefab.gameObject).GetComponent<ParticleSystem>();
            #pragma warning disable 0618 //disable deprecation warnings for a bit
                sweat.startLifetime = 0.3f;
                sweat.startColor = Color.cyan;
            #pragma warning restore 0618
            sweat.emission.SetBurst(0, new ParticleSystem.Burst { count = 5, time = 0, cycleCount = 1, repeatInterval = 0.010f, maxCount = 5, minCount = 5 });
            sweat.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(sweat.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(sweat.gameObject);

            // Add a hook to spawn near the hero shrine at the beginning of the run
            bomboHook = new Hook(
                typeof(Dungeon).GetMethod("FloorReached", BindingFlags.Public | BindingFlags.Instance),
                typeof(Bombo).GetMethod("HandleNewFloor"));
        }

        public static void HandleNewFloor(Action<Dungeon> orig, Dungeon self)
        {
            orig(self);
            if (GameManager.Instance?.PrimaryPlayer == null)
                return;
            GameManager.Instance.PrimaryPlayer.StartCoroutine(HandleFirstFloor());

        }

        private static void BecomeBrainless()
        {
            OhTheresMyBrain();
            brainless = true;
            GameUIRoot.Instance.HideCoreUI("brainless");
            GameUIRoot.Instance.ForceHideGunPanel = true;
            GameUIRoot.Instance.ForceHideItemPanel = true;
            foreach (PlayerController p in GameManager.Instance.AllPlayers)
            {
                GameUIRoot.Instance.UpdatePlayerBlankUI(p);
                GameUIRoot.Instance.UpdatePlayerHealthUI(p, p.healthHaver);
            }

            // removing the map completely makes the game too hard imo
            // Minimap.Instance.ToggleMinimap(false);
            // Minimap.Instance.TemporarilyPreventMinimap = true;
            // Minimap.Instance.UIMinimap.RatTaunty.IsVisible = true;
        }

        private static void OhTheresMyBrain()
        {
            if (!brainless)
                return;
            brainless = false;
            GameUIRoot.Instance.ShowCoreUI("brainless");
            GameUIRoot.Instance.ForceHideGunPanel = false;
            GameUIRoot.Instance.ForceHideItemPanel = false;
            foreach (PlayerController p in GameManager.Instance.AllPlayers)
            {
                GameUIRoot.Instance.UpdatePlayerBlankUI(p);
                GameUIRoot.Instance.UpdatePlayerHealthUI(p, p.healthHaver);
            }
        }

        private static IEnumerator HandleFirstFloor()
        {
            PlayerController p1 = GameManager.Instance.PrimaryPlayer;
            while (GameManager.Instance.IsLoadingLevel)
                yield return null;  //wait for level to fully load

            Vector3 v3 = Vector3.zero;
            bool found = false;
            foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
            {
                if (a.IsLegendaryHeroShrine && a.transform.position.GetAbsoluteRoom() == p1.CurrentRoom)
                {
                    found = true;
                    v3 = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2,-3f)).ToVector3ZisY(0);
                }
            }
            if (found) // hero shrine found, so we're on the 1st floor
            {
                // Reset brainless status on first floor on first floor
                OhTheresMyBrain();
                // Spawn Bombo on first floor
                while (!p1.AcceptingAnyInput)
                    yield return null;  //wait for player to touch down
                Bombo bombyboi = SpawnObjectManager.SpawnObject(Bombo.npcobj,v3).GetComponent<Bombo>();
            }
            else
            {
                if (brainless) // become brainless if necessary
                    BecomeBrainless();
            }
            yield break;
        }

        protected override void Start()
        {
            base.Start();
            base.renderer.enabled      = false;
            this.canInteract           = false;
            this.hasBeenTalkedTo       = false;
            this.strikingADeal         = false;
            this.hasAppeared           = false;

            // reset state from last run if necessary
            ETGMod.AIActor.OnPreStart -= ICantSee;
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
                "Listen, I've got a *real* nice item for you.",
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

            yield return new WaitForSeconds(0.75f);

            List<string> conversation2 = new List<string> {
                "Looks good, eh? It won't cost you any shells, keys, blanks, or anything like that!",
                "No no, all I ask in return for this beautiful treasure...",
                };

            yield return StartCoroutine(Converse(conversation2,"point"));
            base.aiAnimator.PlayUntilCancelled("sad");
            yield return new WaitForSeconds(0.4f);

            List<string> conversation3 = new List<string> {
                "...is a little bit of self-sacrifice! Specifically your *" + sacNames[(int)item.sacType] + "*!",
                "Of course I mean that figuratively, not literally.",
                };
            yield return StartCoroutine(Converse(conversation3,"point"));

            List<string> conversation3b = new List<string> {
                "(...kinda..............)",
                };
            yield return StartCoroutine(Converse(conversation3b,"sad"));

            List<string> conversation3c = new List<string> {
                "So whaddaya say?",
                };
            yield return StartCoroutine(Converse(conversation3c,"point"));

            GameManager.Instance.MainCameraController.SetManualControl(false, true);
            yield break;
        }

        protected override void Update()
        {
            base.Update();
            if (this.hasAppeared)
                return;

            Vector2 mepos = base.sprite.WorldCenter;
            Vector2 ppos  = GameManager.Instance.PrimaryPlayer.sprite.WorldCenter;
            float dist = Vector2.Distance(mepos,ppos);
            if (dist < 3f)
                return;

            this.AppearInAPuffOfSmoke();
            this.hasAppeared      = true;
            this.canInteract      = true;
            base.renderer.enabled = true;
            base.aiAnimator.PlayUntilCancelled("talker");
            this.ShowText("Pssst! Over here!", 1f);
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

        private static void ICantSee(AIActor enemy)
        {
            enemy.RegisterOverrideColor(Color.black, "blindness");
            enemy.StartCoroutine(ImInvisible(enemy));
        }

        private static IEnumerator ImInvisible(AIActor self)
        {
            Material newmat = BraveResources.Load("Global VFX/WhiteMaterial", ".mat") as Material;
            while (true)
            {
                yield return null;
                if (self.healthHaver != null && !self.healthHaver.IsAlive)
                    continue;

                self.sprite.usesOverrideMaterial = true;
                self.renderer.material.shader = ShaderCache.Acquire("Brave/LitBlendUber");
                self.renderer.material.SetFloat("_VertexColor", 1f);
                self.sprite.color = self.sprite.color.WithAlpha(0.0f);
            }
        }

        private void RandomSacrifice(PlayerController chump, OhNoMy sacType = OhNoMy._random)
        {
            OhNoMy sacrifice;
            if (sacType == OhNoMy._random)
                sacrifice = (OhNoMy)UnityEngine.Random.Range(0, (int)OhNoMy._last);
            else
                sacrifice = sacType;
            switch(sacrifice)
            {
                case OhNoMy.BRAIN:
                    BecomeBrainless();
                    // ETGModConsole.Log("lost your brain");
                    break;
                case OhNoMy.EYES:
                    CutStat(chump,PlayerStats.StatType.Accuracy,2.0f);
                    ETGMod.AIActor.OnPreStart += Bombo.ICantSee;
                    // ETGModConsole.Log("lost your eyes");
                    break;
                case OhNoMy.ARMS:
                    CutStat(chump,PlayerStats.StatType.Damage,0.6f);
                    CutStat(chump,PlayerStats.StatType.ReloadSpeed,1.5f);
                    CutStat(chump,PlayerStats.StatType.RateOfFire,0.75f);
                    // ETGModConsole.Log("lost your arms");
                    break;
                case OhNoMy.FINGERS:
                    chump.PostProcessProjectile += Bombo.MightDropTheGun;
                    // ETGModConsole.Log("lost your fingers");
                    break;
                case OhNoMy.LEGS:
                    CutStat(chump,PlayerStats.StatType.MovementSpeed,0.6f);
                    // ETGModConsole.Log("lost your legs");
                    break;
                case OhNoMy.HEART:
                    if (chump.characterIdentity == PlayableCharacters.Robot)
                        chump.healthHaver.Armor = 1;
                    else
                    {
                        chump.healthHaver.Armor = 0;
                        chump.healthHaver.currentHealth = 0.5f;
                    }
                    // ETGModConsole.Log("lost your heart");
                    break;
                case OhNoMy.LUNGS:
                    chump.OnPreDodgeRoll -= Bombo.DodgeRollsAreExhausting;
                    chump.OnPreDodgeRoll += Bombo.DodgeRollsAreExhausting;
                    // ETGModConsole.Log("lost your lungs");
                    break;
                case OhNoMy.STOMACH:
                    chump.GetExtComp().OnPickedUpHP -= Bombo.AppetiteLoss;
                    chump.GetExtComp().OnPickedUpHP += Bombo.AppetiteLoss;
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(73).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(85).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(120).gameObject, chump.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3(), Vector2.zero, 1f, false, true, false);
                    // ETGModConsole.Log("lost your stomach");
                    break;
            }
        }

        public static void MightDropTheGun(Projectile p, float f)
        {
            const int AVG_NUM_TIMES_TO_DROP = 10;
            PlayerController klutz = p.ProjectilePlayerOwner();
            if (klutz.CurrentGun == null || !klutz.CurrentGun.CanBeDropped)
                return;

            Gun gunToSlip = klutz.CurrentGun;
            int maxammo = gunToSlip.AdjustedMaxAmmo;

            if (UnityEngine.Random.Range(0,maxammo) < AVG_NUM_TIMES_TO_DROP)
            {
                // stealing from NN again oh boy
                klutz.inventory.RemoveGunFromInventory(gunToSlip);
                gunToSlip.ForceThrowGun();
                // yield return new WaitForSeconds(0.1f);
                // gunToSlip.ToggleRenderers(true);
                // gunToSlip.RegisterMinimapIcon();
            }
        }

        public static void DodgeRollsAreExhausting(PlayerController wimp)
        {
            wimp.StartCoroutine(Bombo.PreventDodgeRolling(wimp,0.65f));
        }

        public static void AppetiteLoss(PlayerController wimp, HealthPickup hp)
        {
            if (hp.armorAmount > 0)
                wimp.healthHaver.Armor -= hp.armorAmount;
            if (hp.healAmount > 0)
                wimp.healthHaver.currentHealth -= hp.healAmount;
            wimp.StartCoroutine(PlayNotHungryEffect(wimp));
        }

        public static IEnumerator PlayNotHungryEffect(PlayerController wimp)
        {
            var o = wimp.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/VFX_Fear") as GameObject, new Vector3(0,1f,0));
            yield return new WaitForSeconds(1.25f);
            UnityEngine.Object.Destroy(o);
        }

        public static IEnumerator PreventDodgeRolling(PlayerController wimp, float timer)
        {
            wimp.SetInputOverride("exhausted");
            yield return null;
            while (wimp.IsDodgeRolling)
                yield return null;

            var burst = UnityEngine.Object.Instantiate(sweat.gameObject,wimp.sprite.WorldTopCenter,Quaternion.identity);
            burst.SetActive(true);
            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            yield return new WaitForSeconds(0.5f);
            ps.Clear(true);  //hack to prevent old blood splotches from appearing on the ground

            if (timer > 0.5f)
                yield return new WaitForSeconds(timer-0.5f);
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
                Vector2 ppos = p.sprite.WorldCenter + BraveMathCollege.DegreesToVector(i*UnityEngine.Random.Range(0f,360f),1.75f);
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

            GameManager.Instance.MainCameraController.OverridePosition = itemCamPosition;
            GameManager.Instance.MainCameraController.SetManualControl(true, true);

            List<string> conversation = new List<string> {
                "You like it, huh? So we got a deal?",
                };
            yield return StartCoroutine(Converse(conversation,"point"));

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
                "Excellent! I'm just gonna perform a quick little ritual to take your *"+sacNames[(int)f.sacType]+"* and you'll be on your way.",
                "It shouldn't hurt a bit! (...at least nobody's ever complained about it, anyhow.)",
                "Here we go!!!",
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
