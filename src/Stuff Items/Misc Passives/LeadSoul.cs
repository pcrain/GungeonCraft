using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class LeadSoul : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Lead Soul";
            string resourceName = "NevernamedsItems/Resources/leadsoul_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LeadSoul>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "No Voice To Cry Suffering";
            string longDesc = "Grants a regenerating shield."+"\n\nSteel yourself against the tribulations ahead, for the world is dark and cold...";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;
            LeadSoulID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.LICH_BEATEN_SHADE, true);

            #region VFXSetup
            GameObject plagueVFXObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_001", new GameObject("LeadSoulOverhead"));
            plagueVFXObject.SetActive(false);
            tk2dBaseSprite plaguevfxSprite = plagueVFXObject.GetOrAddComponent<tk2dBaseSprite>();
            plaguevfxSprite.GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter, plaguevfxSprite.GetCurrentSpriteDef().position3);
            FakePrefab.MarkAsFakePrefab(plagueVFXObject);
            UnityEngine.Object.DontDestroyOnLoad(plagueVFXObject);
            //Animating the overhead
            tk2dSpriteAnimator plagueanimator = plagueVFXObject.AddComponent<tk2dSpriteAnimator>();
            plagueanimator.Library = plagueVFXObject.AddComponent<tk2dSpriteAnimation>();
            plagueanimator.Library.clips = new tk2dSpriteAnimationClip[0];

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip { name = "LeadSoulOverheadClip", fps = 10, frames = new tk2dSpriteAnimationFrame[0] };
            foreach (string path in OverheadVFXPaths)
            {
                int spriteId = SpriteBuilder.AddSpriteToCollection(path, plagueVFXObject.GetComponent<tk2dBaseSprite>().Collection);

                plagueVFXObject.GetComponent<tk2dBaseSprite>().Collection.spriteDefinitions[spriteId].ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);

                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame { spriteId = spriteId, spriteCollection = plagueVFXObject.GetComponent<tk2dBaseSprite>().Collection };
                clip.frames = clip.frames.Concat(new tk2dSpriteAnimationFrame[] { frame }).ToArray();
            }

            plagueanimator.Library.clips = plagueanimator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            plagueanimator.playAutomatically = true;
            plagueanimator.DefaultClipId = plagueanimator.GetClipIdByName("LeadSoulOverheadClip");
            VFXPrefab = plagueVFXObject;
            #endregion
        }
        public static List<string> OverheadVFXPaths = new List<string>()
        {
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_001",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_002",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_003",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_004",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_005",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_006",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_007",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_008",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_009",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_010",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_011",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_012",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_013",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_014",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_015",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_016",
            "NevernamedsItems/Resources/MiscVFX/LeadSoulVFX/leadsouloverhead_017",
        };
        public static GameObject VFXPrefab;
        private GameObject extantOverhead;

        public static int LeadSoulID;
        private bool shieldCharged;
        private int enemiesKilledSinceShieldReset;
        public override void Pickup(PlayerController player)
        {
            player.healthHaver.ModifyDamage += ModifyDamage;
            player.OnAnyEnemyReceivedDamage += EnemyHurt;
            if (!m_pickedUpThisRun) shieldCharged = true;
            base.Pickup(player);
            if (shieldCharged) AddOverhead();
        }
        public override DebrisObject Drop(PlayerController player)
        {
            ClearOverhead();
            player.healthHaver.ModifyDamage -= ModifyDamage;
            player.OnAnyEnemyReceivedDamage -= EnemyHurt;

            return base.Drop(player);
        }
        private void EnemyHurt(float dmg, bool fatal, HealthHaver enemy)
        {
            if (fatal) enemiesKilledSinceShieldReset++;
        }
        private void ModifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
        {
            if (args.InitialDamage > 0)
            {
                if (shieldCharged)
                {
                    shieldCharged = false;
                    enemiesKilledSinceShieldReset = 0;
                    args.ModifiedDamage = 0;
                    ClearOverhead();
                AkSoundEngine.PostEvent("Play_OBJ_crystal_shatter_01", Owner.gameObject);
                    if (Owner.PlayerHasActiveSynergy("No Will To Break")) Owner.DoEasyBlank(Owner.specRigidbody.UnitCenter, EasyBlankType.MINI);
                    (player.gameActor as PlayerController).TriggerInvulnerableFrames(2f);
                    ScreenShakeSettings shakesettings = new ScreenShakeSettings(0.25f, 7f, 0.1f, 0.3f);
                    GameManager.Instance.MainCameraController.DoScreenShake(shakesettings, new Vector2?(Owner.specRigidbody.UnitCenter), false);
                }
            }
        }
        public override void Update()
        {
            if ((enemiesKilledSinceShieldReset >= 15) && !shieldCharged) shieldCharged = true;

            if(shieldCharged && extantOverhead == null)
            {
                AddOverhead();
            }
            else if (!shieldCharged && extantOverhead != null)
            {
                ClearOverhead();
            }
            base.Update();
        }
        private void AddOverhead()
        {
            if (VFXPrefab && Owner)
            {
                AkSoundEngine.PostEvent("Play_OBJ_metalskin_deflect_01", Owner.gameObject);
                GameObject newSprite = Instantiate(VFXPrefab);
                newSprite.transform.parent = Owner.transform;
                newSprite.transform.position = (Owner.transform.position + new Vector3(0.7f, 2f));
                extantOverhead = newSprite;
                UnityEngine.Object.Instantiate<GameObject>(RainbowGuonStone.WhiteGuonTransitionVFX, extantOverhead.GetComponent<tk2dBaseSprite>().WorldCenter, Quaternion.identity);
            }
        }
        private void ClearOverhead()
        {
            if (extantOverhead)
            {
                UnityEngine.Object.Instantiate<GameObject>(RainbowGuonStone.WhiteGuonTransitionVFX, extantOverhead.GetComponent<tk2dBaseSprite>().WorldCenter, Quaternion.identity);
                Destroy(extantOverhead);
                extantOverhead = null;
            }
        }
        public override void OnDestroy()
        {
            if (extantOverhead)
            {
                ClearOverhead();
            }
            if (Owner)
            {
                Owner.OnAnyEnemyReceivedDamage += EnemyHurt;
                Owner.healthHaver.ModifyDamage -= ModifyDamage;
            }
            base.OnDestroy();
        }

    }
}
