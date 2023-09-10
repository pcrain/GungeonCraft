using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using ItemAPI;

namespace CwaffingTheGungy
{
    public static class VFX
    {
        private const int PIXELS_ABOVE_HEAD = 2;

        private static GameObject VFXScapegoat = new();
        private static tk2dSpriteCollectionData OverheadVFXCollection;
        private static Dictionary<GameActor,List<GameObject>> extantSprites = new();

        public static Dictionary<string,int> sprites = new();
        public static Dictionary<string,GameObject> animations = new();
        public static Dictionary<string,VFXPool> vfxpool = new();
        public static Dictionary<string,VFXComplex> vfxcomplex = new();
        private static Dictionary<GameObject,VFXPool> vfxObjectToPoolMap = new();

        public static GameObject laserSightPrefab;

        public static tk2dSpriteCollectionData SpriteCollection
        {
            get { return OverheadVFXCollection; }
        }

        public static void Init()
        {
            OverheadVFXCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "OverheadVFX_Collection");
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            UnityEngine.Object.DontDestroyOnLoad(OverheadVFXCollection);

            for (int i = 0; i < 10; ++i)
            {
                string istring = i.ToString();
                // RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/Numbers/"+i.ToString());
                RegisterVFX(istring, new List<string>() {
                        "CwaffingTheGungy/Resources/MiscVFX/Numbers/"+i.ToString(),
                    }, 1, loops: false, anchor: tk2dBaseSprite.Anchor.LowerCenter, persist: true);
            }

            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter1");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter2");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter3");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter4");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/PumpChargeMeter5");

            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle-white");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle-orange");
            RegisterSprite("CwaffingTheGungy/Resources/MiscVFX/reticle-blue");

            RegisterVFX("PumpChargeAnimated", ResMap.Get("PumpChargeMeter"), 4, loops: true,
                anchor: tk2dBaseSprite.Anchor.LowerCenter);

            RegisterVFX("FriendlyOverhead", ResMap.Get("friendlyoverhead_vfx"), 10, loops: true,
                anchor: tk2dBaseSprite.Anchor.LowerCenter);

            RegisterVFX("PlagueOverhead", ResMap.Get("plaguevfxframe"), 4, loops: true,
                anchor: tk2dBaseSprite.Anchor.LowerCenter);

            RegisterVFX("CampfireA", ResMap.Get("campfire-a"), 6, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.5f, emissivePower: 5f);
            RegisterVFX("CampfireB", ResMap.Get("campfire-b"), 6, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.5f, emissivePower: 10f);
            RegisterVFX("CampfireC", ResMap.Get("campfire-c"), 6, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.5f, emissivePower: 20f);
            RegisterVFX("CampfireD", ResMap.Get("campfire-d"), 6, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.5f, emissivePower: 30f);

            RegisterVFX("SoulLinkParticle", ResMap.Get("soul-link-particle"), 16, loops: true,
                anchor: tk2dBaseSprite.Anchor.LowerCenter, scale: 0.3f, emissivePower: 100);

            RegisterVFX("MidasParticle", ResMap.Get("midas-sparkle"), 8, loops: true,
                anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 5);

            RegisterVFX("ExorcismParticle", ResMap.Get("exorcism-particles"), 12, loops: false,
                anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 2);

            RegisterVFX("SoulLinkSoul", ResMap.Get("soul-link-soul"), 5, loops: true,
                anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.3f, emissivePower: 200);

            RegisterVFX("VoodooGhost", ResMap.Get("voodoo-ghost"), 2, loops: true,
                anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);

            RegisterVFX("CanOfPepsi", ResMap.Get("can-of-pepsi"), 2, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);
            RegisterVFX("CanOfCoke", ResMap.Get("can-of-coke"), 2, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);
            RegisterVFX("CanOfSprite", ResMap.Get("can-of-sprite"), 2, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);

            RegisterVFX("Shine", new List<string>() {
                    "CwaffingTheGungy/Resources/MiscVFX/shine2",
                }, 1, loops: true, anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 100);

            RegisterVFX("Splode", ResMap.Get("splode"), 18, loops: true,
                    anchor: tk2dBaseSprite.Anchor.MiddleCenter, emissivePower: 100, emissiveColour: Color.cyan);

            RegisterVFX("Rebar", ResMap.Get("RebarGunImpactVFX2"), 6, loops: false);

            RegisterVFX("Tornado", ResMap.Get("tornado"), 20, loops: true, anchor: tk2dBaseSprite.Anchor.LowerCenter);

            // TODO: setting alpha on the first frame doesn't work, so we create an empty dummy sprite to start with
            RegisterVFX("VacuumParticle", ResMap.Get("vacuum_wind_sprite_a"), 30, loops: true, loopStart: 6,
                anchor: tk2dBaseSprite.Anchor.MiddleCenter, scale: 0.5f);

            laserSightPrefab = LoadHelper.LoadAssetFromAnywhere("assets/resourcesbundle/global vfx/vfx_lasersight.prefab") as GameObject;
        }

        /// <summary>
        /// Register a single-frame static sprite
        /// </summary>
        private static void RegisterSprite(string path)
        {
            sprites[path.Substring(path.LastIndexOf("/")+1)] = SpriteBuilder.AddSpriteToCollection(path, OverheadVFXCollection);
        }

        /// <summary>
        /// Generically register a VFX as a GameObject (animated sprite), VFXComplex, or VFXPool
        /// </summary>
        public static void RegisterVFX(string name, List<string> spritePaths, int fps, bool loops = true, int loopStart = -1, float scale = 1.0f, tk2dBaseSprite.Anchor anchor = tk2dBaseSprite.Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null)
        {
            // GameObject Obj     = new GameObject(name);
            GameObject Obj     = SpriteBuilder.SpriteFromResource(spritePaths[0], new GameObject(name));
            VFXComplex complex = new VFXComplex();
            VFXObject vfObj    = new VFXObject();
            VFXPool pool       = new VFXPool();
            pool.type          = VFXPoolType.All;
            Obj.RegisterPrefab();

            tk2dBaseSprite baseSprite = Obj.GetComponent<tk2dBaseSprite>();
            tk2dSpriteDefinition baseDef = baseSprite.GetCurrentSpriteDef();
            baseDef.ConstructOffsetsFromAnchor(
                tk2dBaseSprite.Anchor.LowerCenter,
                baseDef.position3);

            tk2dSpriteCollectionData VFXSpriteCollection = SpriteBuilder.ConstructCollection(Obj, (name + "_Pool"));
            int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], VFXSpriteCollection);

            tk2dSprite sprite = Obj.GetOrAddComponent<tk2dSprite>();
            sprite.SetSprite(VFXSpriteCollection, spriteID);
            tk2dSpriteDefinition defaultDef = sprite.GetCurrentSpriteDef();

            if (dimensions is IntVector2 dims)
            {
                defaultDef.colliderVertices = new Vector3[]{
                          new Vector3(0f, 0f, 0f),
                          new Vector3((dims.x / C.PIXELS_PER_TILE), (dims.y / C.PIXELS_PER_TILE), 0f)
                      };
            }
            else
            {
                defaultDef.colliderVertices = new Vector3[]{
                          new Vector3(0f, 0f, 0f),
                          new Vector3(
                            baseSprite.GetCurrentSpriteDef().position3.x / C.PIXELS_PER_TILE,
                            baseSprite.GetCurrentSpriteDef().position3.y / C.PIXELS_PER_TILE,
                            0f)
                      };
            }

            tk2dSpriteAnimator animator           = Obj.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation         = Obj.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips                       = new tk2dSpriteAnimationClip[0];
            animator.Library                      = animation;
            tk2dSpriteAnimationClip clip          = new tk2dSpriteAnimationClip() { name = "start", frames = new tk2dSpriteAnimationFrame[0], fps = fps };
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < spritePaths.Count; i++)
            {
                tk2dSpriteCollectionData collection = VFXSpriteCollection;
                int frameSpriteId                   = SpriteBuilder.AddSpriteToCollection(spritePaths[i], collection);
                tk2dSpriteDefinition frameDef       = collection.spriteDefinitions[frameSpriteId];
                frameDef.ConstructOffsetsFromAnchor(anchor);
                frameDef.colliderVertices = defaultDef.colliderVertices;
                frameDef.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                frameDef.materialInst.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                if (emissivePower > 0) {
                    frameDef.material.SetFloat("_EmissivePower", emissivePower);
                    frameDef.material.SetFloat("_EmissiveColorPower", 1.55f);
                    frameDef.materialInst.SetFloat("_EmissivePower", emissivePower);
                    frameDef.materialInst.SetFloat("_EmissiveColorPower", 1.55f);
                }
                if (emissiveColour != null)
                {
                    frameDef.material.SetColor("_EmissiveColor", (Color)emissiveColour);
                    frameDef.materialInst.SetColor("_EmissiveColor", (Color)emissiveColour);
                }
                frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
            }
            if (emissivePower > 0) {
                sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                sprite.renderer.material.SetFloat("_EmissivePower", emissivePower);
                sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
            }
            if (emissiveColour != null)
                sprite.renderer.material.SetColor("_EmissiveColor", (Color)emissiveColour);
            clip.frames     = frames.ToArray();
            if (loopStart > 0)
            {
                clip.wrapMode  = tk2dSpriteAnimationClip.WrapMode.LoopSection;
                clip.loopStart = loopStart;
            }
            else
                clip.wrapMode   = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
            animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            if (!persist)
            {
                SpriteAnimatorKiller kill = animator.gameObject.AddComponent<SpriteAnimatorKiller>();
                kill.fadeTime = -1f;
                kill.animator = animator;
                kill.delayDestructionTime = -1f;
            }
            animator.playAutomatically = true;
            animator.DefaultClipId     = animator.GetClipIdByName("start");
            vfObj.attached             = true;
            vfObj.persistsOnDeath      = persist;
            vfObj.usesZHeight          = usesZHeight;
            vfObj.zHeight              = zHeightOffset;
            vfObj.alignment            = alignment;
            vfObj.destructible         = false;

            if (scale != 1.0f)
                sprite.scale = new Vector3(scale, scale, scale);

            vfObj.effect               = Obj;
            complex.effects            = new VFXObject[] { vfObj };
            pool.effects               = new VFXComplex[] { complex };

            vfxpool[name]    = pool;
            vfxcomplex[name] = complex;
            animations[name] = Obj;
        }

        /// <summary>
        /// Register and return a VFXPool
        /// </summary>
        public static VFXPool RegisterVFXPool(string name, List<string> spritePaths, int fps, bool loops = true, float scale = 1.0f, tk2dBaseSprite.Anchor anchor = tk2dBaseSprite.Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null)
        {
            RegisterVFX(
                name           : name,
                spritePaths    : spritePaths,
                fps            : fps,
                loops          : loops,
                scale          : scale,
                anchor         : anchor,
                dimensions     : dimensions,
                usesZHeight    : usesZHeight,
                zHeightOffset  : zHeightOffset,
                persist        : persist,
                alignment      : alignment,
                emissivePower  : emissivePower,
                emissiveColour : emissiveColour
                );
            return vfxpool[name];
        }


        public static void ShowOverheadVFX(this GameActor gunOwner, string name, float timeout)
        {
            gunOwner.StartCoroutine(ShowVFXCoroutine(gunOwner, name, timeout));
        }

        public static void ShowOverheadAnimatedVFX(this GameActor gunOwner, string name, float timeout)
        {
            gunOwner.StartCoroutine(ShowAnimatedVFXCoroutine(gunOwner, name, timeout));
        }

        /// <summary>
        /// Spawn prefabricated vfx, optionally locked relative to a gameobject's position
        /// </summary>
        public static void SpawnVFXPool(string name, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {
            SpawnVFXPool(VFX.vfxpool[name], position, above, degAngle, relativeTo);
        }

        public static void SpawnVFXPool(VFXPool vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {
            Transform t = (relativeTo != null) ? relativeTo.transform : null;
            vfx.SpawnAtPosition(
                position.ToVector3ZisY(above ? -1f : 1f), /* -1 = above player sprite */
                degAngle, t, null, null, -0.05f);
        }

        public static void SpawnVFXPool(GameObject vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {

            SpawnVFXPool(CreatePoolFromVFXGameObject(vfx), position, above, degAngle, relativeTo);
        }

        public static VFXPool CreatePoolFromVFXGameObject(GameObject vfx)
        {
            if (!(vfxObjectToPoolMap.ContainsKey(vfx)))
            {
                VFXObject vfObj         = new VFXObject();
                vfObj.attached          = false;
                vfObj.persistsOnDeath   = false;
                vfObj.usesZHeight       = false;
                vfObj.zHeight           = 0;
                vfObj.alignment         = VFXAlignment.NormalAligned;
                vfObj.destructible      = false;
                vfObj.effect            = vfx;

                VFXComplex complex      = new VFXComplex();
                complex.effects         = new VFXObject[] { vfObj };

                VFXPool pool            = new VFXPool();
                pool.type               = VFXPoolType.All;
                pool.effects            = new VFXComplex[] { complex };

                vfxObjectToPoolMap[vfx] = pool;
            }
            return vfxObjectToPoolMap[vfx];
        }

        private static IEnumerator ShowVFXCoroutine(this GameActor gunOwner, string name, float timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();
            GameObject newSprite = new GameObject(name, new Type[] { typeof(tk2dSprite) }) { layer = 0 };
            // newSprite.transform.position = (gunOwner.transform.position + new Vector3(0.5f, 2));
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);
            tk2dSprite overheadSprite = newSprite.AddComponent<tk2dSprite>();
            extantSprites[gunOwner].Add(newSprite);
            overheadSprite.SetSprite(OverheadVFXCollection, sprites[name]);
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

        private static IEnumerator ShowAnimatedVFXCoroutine(this GameActor gunOwner, string name, float timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();

            GameObject newSprite = UnityEngine.Object.Instantiate<GameObject>(animations[name]);

            tk2dBaseSprite baseSprite = newSprite.GetComponent<tk2dBaseSprite>();
            newSprite.transform.parent = gunOwner.transform;
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);

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

        // Blatantly stolen from Noonum
        public static GameObject CreateLaserSight(Vector2 position, float length, float width, float angle, Color? colour = null, float power = 0)
        {
            GameObject gameObject = SpawnManager.SpawnVFX(laserSightPrefab, position, Quaternion.Euler(0, 0, angle));

            tk2dTiledSprite component2 = gameObject.GetComponent<tk2dTiledSprite>();
            float newWidth = 1f;
            if (width != -1) newWidth = width;
            component2.dimensions = new Vector2(length, newWidth);
            if (colour != null)
            {
                component2.usesOverrideMaterial = true;
                component2.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                component2.sprite.renderer.material.SetColor("_OverrideColor", (Color)colour);
                component2.sprite.renderer.material.SetColor("_EmissiveColor", (Color)colour);
                component2.sprite.renderer.material.SetFloat("_EmissivePower", power);
                component2.sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
            }
            return gameObject;
        }

        // Opacity management
        public static void SetAlpha(this Renderer renderer, float newAlpha = 1.0f)
        {
            // NOTE: might need to also make sure sprite has override material
            if (renderer.material.shader.name != "Brave/Internal/SimpleAlphaFadeUnlit")
                renderer.material.shader = Shader.Find("Brave/Internal/SimpleAlphaFadeUnlit");
            renderer.material.SetFloat("_Fade", newAlpha);
        }
    }

    // Helper class for making movable / fadeable  VFX
    public class FancyVFX : MonoBehaviour
    {
        private GameObject _vfx;
        private float _lifeTime;
        private tk2dSprite _sprite;

        private Vector3 _velocity;
        private bool _fadeOut;
        private float _fadeStartTime;
        private float _fadeTotalTime;
        private float _maxLifeTime;

        private void Start()
        {
            this._vfx = base.gameObject;
            this._lifeTime = 0.0f;
            this._sprite = this._vfx.GetComponent<tk2dSprite>();
        }

        private void LateUpdate()
        {
            if (!this._vfx)
            {
                UnityEngine.Object.Destroy(this);
                return;
            }
            this._lifeTime += BraveTime.DeltaTime;
            if (this._lifeTime > this._maxLifeTime)
            {
                if (this._vfx)
                    UnityEngine.Object.Destroy(this._vfx);
                UnityEngine.Object.Destroy(this);
                return;
            }

            this._sprite.transform.position += this._velocity;

            if (this._lifeTime > this._fadeStartTime)
                this._sprite.renderer.SetAlpha(1.0f - (this._lifeTime - this._fadeStartTime) / this._fadeTotalTime);
        }

        public void Setup(Vector2 velocity, float lifetime = 0, float? fadeOutTime = null, Transform parent = null, float emissivePower = 0, Color? emissiveColor = null)
        {
            this._velocity = (1.0f / C.PIXELS_PER_CELL) * velocity.ToVector3ZisY(0);
            this._maxLifeTime = lifetime;
            this._fadeOut = fadeOutTime.HasValue;
            if (this._fadeOut)
            {
                this._fadeTotalTime = fadeOutTime.Value;
                this._fadeStartTime = lifetime - this._fadeTotalTime;
            }
            this.transform.parent = parent;

            // if (emissivePower > 0)
            // {
            //     this._sprite.usesOverrideMaterial = true;
            //     Material m = this._sprite.renderer.material;
            //         m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            //         m.SetFloat("_EmissivePower", emissivePower);

            //     if (emissiveColor.HasValue)
            //     {
            //         m.SetFloat("_EmissiveColorPower", 1.55f);
            //         m.SetColor("_EmissiveColor", emissiveColor.Value);
            //         m.SetColor("_OverrideColor", emissiveColor.Value);
            //     }
            // }
        }
    }
}

