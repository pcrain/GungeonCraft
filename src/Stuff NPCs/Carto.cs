using System;
using System.Collections;
using UnityEngine;
using GungeonAPI;
using System.Collections.Generic;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public static class Carto
    {
        public static GameObject CartoPrefab;
        public static void Add()
        {
            OldShrineFactory shrineFactory = new OldShrineFactory
            {
                name = "Carto",
                modID = "omitb",
                spritePath = "NevernamedsItems/Resources/NPCSprites/Carto/carto_idle_001.png",
                //shadowSpritePath = "NevernamedsItems/Resources/NPCSprites/Carto/carto_shadow_001.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/InvestmentShrineRoom.room").room,
                OnAccept = new Action<PlayerController, GameObject>(Carto.Accept),
                OnDecline = null,
                CanUse = new Func<PlayerController, GameObject, bool>(Carto.CanUse),
                talkPointOffset = new Vector3(2.31f, 1.0f, 0f),
                isToggle = false,
                isBreachShrine = false,
                interactableComponent = typeof(CartoInteractible)
            };
            GameObject gameObject = shrineFactory.Build();
            gameObject.AddAnimation("idle", "NevernamedsItems/Resources/NPCSprites/Carto/carto_idle", 12, NPCBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("talk", "NevernamedsItems/Resources/NPCSprites/Carto/carto_talk", 12, NPCBuilder.AnimationType.Talk, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("talk_start", "NevernamedsItems/Resources/NPCSprites/Carto/carto_talk", 12, NPCBuilder.AnimationType.Other, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            gameObject.AddAnimation("do_effect", "NevernamedsItems/Resources/NPCSprites/Carto/carto_talk", 12, NPCBuilder.AnimationType.Other, DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType.None);
            CartoInteractible component = gameObject.GetComponent<CartoInteractible>();
            gameObject.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(gameObject.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            CartoPrefab = gameObject;
        }
        private static bool CanUse(PlayerController player, GameObject npc)
        {
            return true;
        }

        public static void Accept(PlayerController player, GameObject npc)
        {
            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(137).gameObject, player);
        }
    }
    public class CartoInteractible : SimpleInteractable, IPlayerInteractable
    {
        public bool hasBeenUsedThisRun = false;
        public bool hasSpokenToAfterUse = false;
        public bool hasDeclinedAlreadyThisRun = false;
        public List<string> chitChat = new List<string>()
        {
            "Did you ever hear the story of Woban the Cartographer?... thought not.",
            "That 'Lost Adventurer' keeps begging me for my maps, but refuses to pay. I tell him to go make his own!",
            "The only place I feel truly lost... is around others. That's why you'll never see me in the breach.",
            "My name? Oh, I was named after a wagon.",
            "Say, did you ever meet a fellow by the name of Coxswain? No? Such a shame, he was an old friend...",
            "This place is the mapmaker's dream. The shifting walls provide neverending material!",
            "My maps are drawn in only the finest of Glocktopus inks.",
            "Longitude? I hardly even... no, wait? Is that how the joke's supposed to go?... I don't remember.",
            "I wonder who built this place... it's halls form beautiful patterns.",
        };
        private void Start()
        {
            this.talkPoint = base.transform.Find("talkpoint");
            this.m_isToggled = false;
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            this.m_canUse = true;
            base.spriteAnimator.Play("idle");
        }
        public void Interact(PlayerController interactor)
        {
            bool flag = TextBoxManager.HasTextBox(this.talkPoint);
            if (!flag)
            {
                this.m_canUse = ((this.CanUse != null) ? this.CanUse(interactor, base.gameObject) : this.m_canUse);
                if (!this.m_canUse)
                {
                    base.spriteAnimator.PlayForDuration("talk", 2f, "idle", false);
                    TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 2f, "No... not this time.", interactor.characterAudioSpeechTag, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
                }
                else
                {
                    if (hasBeenUsedThisRun)
                    {
                        TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 2f, "Hmm? Oh, not now, sorry. I'm busy.", interactor.characterAudioSpeechTag, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);

                    }
                    else
                    {
                        if (interactor.HasPickupID(281))
                        {
                            base.StartCoroutine(this.HandleGungeonBlueprintConvo(interactor));

                        }
                        else
                        {
                            base.StartCoroutine(this.HandleConversation(interactor));
                        }
                    }
                }
            }
        }
        private IEnumerator HandleGungeonBlueprintConvo(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.spriteAnimator.PlayForDuration("talk_start", 1, "talk");
            interactor.SetInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
            yield return null;

            //Determine Dialogue
            var conversationToUse = new List<string>()
            {
                "Wait... is that?",
                "...no, it can't be...",
                "...but it is!",
                "THE GUNGEON BLUEPRINT!",
                "Please, you simply must give it to me! I need to have it, it is the pinnacle of my craft!",
                "I'll give you 150 in cash for it, mark my words!"
            };
            int conversationIndex = 0;

            while (conversationIndex < conversationToUse.Count - 1)
            {
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
            TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationToUse.Count - 1], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);


            var acceptanceTextToUse = "It's yours! <Give Gungeon Blueprint>";
            var declineTextToUse = "I think I'll hold onto it.";

            GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0)
            {
                TextBoxManager.ClearTextBox(this.talkPoint);
                base.spriteAnimator.PlayForDuration("do_effect", -1, "talk");
                base.spriteAnimator.Play("talk");

                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "Thank you! The money, as promised, is yours!", interactor.characterAudioSpeechTag, instant: false);

                interactor.RemovePassiveItem(281);
                int num = interactor.passiveItems.FindIndex((PassiveItem p) => p.PickupObjectId == 281);
                if (num >= 0)
                {
                    interactor.RemovePassiveItemAtIndex(num);
                }

                hasBeenUsedThisRun = true;
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(74).gameObject, interactor);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(74).gameObject, interactor);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "You break my heart, you ignoble batterlacklamain!", interactor.characterAudioSpeechTag, instant: false);
                hasBeenUsedThisRun = true;
                TextBoxManager.ClearTextBox(this.talkPoint);
            }

            interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.spriteAnimator.Play("idle");
        }
        private IEnumerator HandleConversation(PlayerController interactor)
        {
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.spriteAnimator.PlayForDuration("talk_start", 1, "talk");
            interactor.SetInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
            yield return null;

            //Determine Dialogue
            var conversationToUse = new List<string>() { "Null" };

            //Price Mult
            GameLevelDefinition lastLoadedLevelDefinition = GameManager.Instance.GetLastLoadedLevelDefinition();
            float PriceMult = (lastLoadedLevelDefinition == null) ? 1f : lastLoadedLevelDefinition.priceMultiplier;

            //Check for if ever met before and then else
            if (hasDeclinedAlreadyThisRun)
            {
                conversationToUse = new List<string>()
                {
                    "You're back? Did you change your mind?",
                    "Did you get hopelessly lost?",
                    "Please tell me you got hopelessly lost!",
                };
            }
            else
            {
                conversationToUse = new List<string>()
            {
                "Well hello again, young adventurer!",
                "You look lost... mayhaps you are in need of a finely crafted... map?",
                "Only " + (20 * PriceMult) + " of those little janglies, you know the deal."
            };
            }


            int conversationIndex = 0;

            while (conversationIndex < conversationToUse.Count - 1)
            {
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
            TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationToUse.Count - 1], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);


            var acceptanceTextToUse = "Null";
            var declineTextToUse = "Null";

            if (hasDeclinedAlreadyThisRun)
            {
                acceptanceTextToUse = "...Just give me the map. <Lose " + (20 * PriceMult) + " Money>";
                declineTextToUse = "Not lost, I know EXACTLY where I am!";
            }
            else
            {
                acceptanceTextToUse = "Yeah, a map would be nice <Lose " + (20 * PriceMult) + " Money>";
                declineTextToUse = "No thanks, I can find my way around juuust fine.";
            }

            GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;

            if (selectedResponse == 0)
            {
                TextBoxManager.ClearTextBox(this.talkPoint);
                base.spriteAnimator.PlayForDuration("do_effect", -1, "talk");
                base.spriteAnimator.Play("talk");
                if (interactor.carriedConsumables.Currency >= (20 * PriceMult))
                {
                    TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "Enjoy the fruits of my knowledge!", interactor.characterAudioSpeechTag, instant: false);
                    OnAccept?.Invoke(interactor, this.gameObject);
                    hasBeenUsedThisRun = true;
                }
                else
                {
                    TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "Oh... looks like you don't have the cash. Welp, research ain't free.", interactor.characterAudioSpeechTag, instant: false);
                }
                yield return new WaitForSeconds(1f);
            }
            else
            {
                OnDecline?.Invoke(interactor, this.gameObject);
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "Harumph, enjoy being lost...", interactor.characterAudioSpeechTag, instant: false);
                hasDeclinedAlreadyThisRun = true;
                TextBoxManager.ClearTextBox(this.talkPoint);
            }

            interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.spriteAnimator.Play("idle");
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
        public float GetOverrideMaxDistance()
        {
            return -1f;
        }
    }
}

