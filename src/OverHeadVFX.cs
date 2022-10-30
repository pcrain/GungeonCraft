using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Gungeon;
using ItemAPI;

namespace CwaffingTheGungy
{
    public static class OverheadVFX
    {
        private const int PIXELS_ABOVE_HEAD = 2;

        private static GameObject VFXScapegoat;
        private static tk2dSpriteCollectionData OverheadVFXCollection;
        private static Dictionary<GameActor,List<GameObject>> extantSprites;

        public static Dictionary<string,int> sprites;
        public static Dictionary<string,GameObject> animations;

        private static void RegisterSprite(string path)
        {
            sprites[path.Substring(path.LastIndexOf("/")+1)] = SpriteBuilder.AddSpriteToCollection(path, OverheadVFXCollection);
        }

        public static void RegisterAnimatedSprite(List<string> filepaths, string name, int fps)
        {
            GameObject overheadderVFX = SpriteBuilder.SpriteFromResource(filepaths[0], new GameObject(name));
            overheadderVFX.SetActive(false);
            tk2dBaseSprite baseSprite = overheadderVFX.GetComponent<tk2dBaseSprite>();
            baseSprite.GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter, baseSprite.GetCurrentSpriteDef().position3);
            FakePrefab.MarkAsFakePrefab(overheadderVFX);
            UnityEngine.Object.DontDestroyOnLoad(overheadderVFX);

            tk2dSpriteAnimator animator = overheadderVFX.AddComponent<tk2dSpriteAnimator>();
            animator.Library            = overheadderVFX.AddComponent<tk2dSpriteAnimation>();
            animator.Library.clips      = new tk2dSpriteAnimationClip[0];

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip { name = name, fps = fps, frames = new tk2dSpriteAnimationFrame[0] };
            foreach (string path in filepaths)
            {
                int spriteId = SpriteBuilder.AddSpriteToCollection(path, overheadderVFX.GetComponent<tk2dBaseSprite>().Collection);

                overheadderVFX.GetComponent<tk2dBaseSprite>().Collection.spriteDefinitions[spriteId].ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);

                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame { spriteId = spriteId, spriteCollection = overheadderVFX.GetComponent<tk2dBaseSprite>().Collection };
                clip.frames = clip.frames.Concat(new tk2dSpriteAnimationFrame[] { frame }).ToArray();
            }
            animator.Library.clips     = animator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            animator.playAutomatically = true;
            animator.DefaultClipId     = animator.GetClipIdByName(name);
            animations[name]           = overheadderVFX;
        }

        public static void Init()
        {
            sprites      = new Dictionary<string,int>();
            VFXScapegoat  = new GameObject();
            extantSprites = new Dictionary<GameActor,List<GameObject>>();
            animations    = new Dictionary<string,GameObject>();
            OverheadVFXCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "OverheadVFX_Collection");
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            UnityEngine.Object.DontDestroyOnLoad(OverheadVFXCollection);

            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter1");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter2");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter3");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter4");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter5");

            RegisterAnimatedSprite(new List<string>() {
                    "CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter1",
                    "CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter2",
                    "CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter3",
                    "CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter4",
                },"PumpChargeAnimated",4);

            RegisterAnimatedSprite(new List<string>()
            {
                "CwaffingTheGungy/Resources/MiscVFX/friendlyoverhead_vfx_001",
                "CwaffingTheGungy/Resources/MiscVFX/friendlyoverhead_vfx_002",
                "CwaffingTheGungy/Resources/MiscVFX/friendlyoverhead_vfx_003",
                "CwaffingTheGungy/Resources/MiscVFX/friendlyoverhead_vfx_004",
                "CwaffingTheGungy/Resources/MiscVFX/friendlyoverhead_vfx_005"
            },"FriendlyOverhead", 10);
        }

        public static void ShowOverheadVFX(this GameActor gunOwner, string name, int timeout)
        {
            gunOwner.StartCoroutine(ShowVFXCoroutine(gunOwner, name, 2));
        }

        public static void ShowOverheadAnimatedVFX(this GameActor gunOwner, string name, int timeout)
        {
            gunOwner.StartCoroutine(ShowAnimatedVFXCoroutine(gunOwner, name, 2));
        }

        public static IEnumerator ShowVFXCoroutine(this GameActor gunOwner, string name, int timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();
            GameObject newSprite = new GameObject(name, new Type[] { typeof(tk2dSprite) }) { layer = 0 };
            // newSprite.transform.position = (gunOwner.transform.position + new Vector3(0.5f, 2));
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/16.0f);
            tk2dSprite overheadSprite = newSprite.AddComponent<tk2dSprite>();
            extantSprites[gunOwner].Add(newSprite);
            overheadSprite.SetSprite(OverheadVFX.OverheadVFXCollection, OverheadVFX.sprites[name]);
            overheadSprite.PlaceAtPositionByAnchor(newSprite.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            overheadSprite.transform.localPosition = overheadSprite.transform.localPosition.Quantize(0.0625f);
            newSprite.transform.parent = gunOwner.transform;
            if (overheadSprite)
            {
                gunOwner.sprite.AttachRenderer(overheadSprite);
                overheadSprite.depthUsesTrimmedBounds = true;
                overheadSprite.UpdateZDepth();
            }
            gunOwner.sprite.UpdateZDepth();
            if (timeout > 0)
            {
                yield return new WaitForSeconds(timeout);
                if (newSprite)
                {
                    extantSprites[gunOwner].Remove(newSprite);
                    UnityEngine.Object.Destroy(newSprite.gameObject);
                }
            }
            else {
                yield break;
            }
        }

        public static IEnumerator ShowAnimatedVFXCoroutine(this GameActor gunOwner, string name, int timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();

            GameObject newSprite = UnityEngine.Object.Instantiate<GameObject>(animations[name]);

            tk2dBaseSprite baseSprite = newSprite.GetComponent<tk2dBaseSprite>();
            newSprite.transform.parent = gunOwner.transform;
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/16.0f);

            extantSprites[gunOwner].Add(baseSprite.gameObject);

            Bounds bounds = gunOwner.sprite.GetBounds();
            Vector3 vector = gunOwner.transform.position + new Vector3((bounds.max.x + bounds.min.x) / 2f, bounds.max.y, 0f).Quantize(0.0625f);
            newSprite.transform.position = gunOwner.sprite.WorldCenter.ToVector3ZUp(0f).WithY(vector.y);
            baseSprite.HeightOffGround = 0.5f;

            gunOwner.sprite.AttachRenderer(baseSprite);

            if (timeout > 0)
            {
                yield return new WaitForSeconds(timeout);
                if (baseSprite)
                {
                    extantSprites[gunOwner].Remove(baseSprite.gameObject);
                    UnityEngine.Object.Destroy(baseSprite.gameObject);
                }
            }
            else {
                yield break;
            }
        }

        public static void StopAllOverheadVFX(this GameActor gunOwner)
        {
            if (extantSprites[gunOwner].Count > 0)
            {
                for (int i = extantSprites[gunOwner].Count - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(extantSprites[gunOwner][i].gameObject);
                }
                extantSprites[gunOwner].Clear();
            }
        }
    }
}

