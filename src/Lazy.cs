using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices; // audio loading
using System.Text.RegularExpressions;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public static class Lazy // all-purpose helper methods for being a lazy dumdum
    {
        /// <summary>
        /// Perform basic initialization for a new passive, active, or gun item definition.
        /// </summary>
        public static TItemClass SetupItem<TItemClass, TItemSpecific>(string itemName, string spritePath, string projectileName, string shortDescription, string longDescription)
            where TItemClass : PickupObject   // must be PickupObject for passive items, PlayerItem for active items, or Gun for guns
            where TItemSpecific : TItemClass  // must be a subclass of TItemClass
        {
            string newItemName  = itemName.Replace("-", "").Replace(".", "");  //get sane gun for item rename
            string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner gun name for commands
            IDs.InternalNames[itemName] = C.MOD_PREFIX+":"+baseItemName;

            TItemClass item;

            if (typeof(TItemClass) == typeof(Gun))
            {
                string spriteName = spritePath; // TODO: guns use names, regular items use full paths -- should be made uniform eventually
                Gun gun = ETGMod.Databases.Items.NewGun(itemName, spriteName);  //create a new gun using specified sprite name
                Game.Items.Rename("outdated_gun_mods:"+baseItemName, IDs.InternalNames[itemName]);  //rename the gun for commands
                gun.SetupSprite(null, spriteName+"_idle_001"); //set the gun's ammonomicon sprite

                int projectileId = 0;
                if (int.TryParse(projectileName, out projectileId))
                    gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(projectileId) as Gun, true, true); //set the gun's default projectile to inherit
                else
                    gun.AddProjectileModuleFrom(projectileName, true, false); //set the gun's default projectile to inherit
                item = gun as TItemClass;
            }
            else
            {
                GameObject obj = new GameObject(itemName);
                item = obj.AddComponent<TItemSpecific>();
                ItemBuilder.AddSpriteToObject(itemName, spritePath, obj);

                ETGMod.Databases.Items.SetupItem(item, item.name);

                Gungeon.Game.Items.Add(IDs.InternalNames[itemName], item);
                SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
                item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;
            }

            item.encounterTrackable.EncounterGuid = C.MOD_PREFIX+"-"+baseItemName; //create a unique guid for the item
            item.SetShortDescription(shortDescription);
            item.SetLongDescription(longDescription);
            ETGMod.Databases.Items.Add(item);

            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database
            if (item is Gun)
            {
                IDs.Guns[baseItemName] = item.PickupObjectId; //register item in gun ID database
                ETGModConsole.Log("Lazy Initialized Gun: "+baseItemName);
            }
            else if (item is PlayerItem)
            {
                IDs.Actives[baseItemName] = item.PickupObjectId; //register item in active ID database
                ETGModConsole.Log("Lazy Initialized Active: "+baseItemName);
            }
            else
            {
                IDs.Passives[baseItemName] = item.PickupObjectId; //register item in passive ID database
                ETGModConsole.Log("Lazy Initialized Passive: "+baseItemName);
            }
            return item;
        }

        /// <summary>
        /// Perform basic initialization for a new passive item definition.
        /// </summary>
        public static PickupObject SetupPassive<T>(string itemName, string spritePath, string shortDescription, string longDescription)
            where T : PickupObject
        {
            return SetupItem<PickupObject, T>(itemName, spritePath, "", shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization for a new active item definition.
        /// </summary>
        public static PlayerItem SetupActive<T>(string itemName, string spritePath, string shortDescription, string longDescription)
            where T : PlayerItem
        {
            return SetupItem<PlayerItem, T>(itemName, spritePath, "", shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization for a new gun definition.
        /// </summary>
        public static Gun SetupGun(string gunName, string spritePath, string projectileName, string shortDescription, string longDescription)
        {
            return SetupItem<Gun, Gun>(gunName, spritePath, projectileName, shortDescription, longDescription);
        }

        /// <summary>
        /// Perform basic initialization of beam sprites for a projectile, override the beam controller's existing sprites if they exist
        /// </summary>
        public static BasicBeamController SetupBeamSprites(this Projectile projectile, string spriteName, int fps, Vector2 dims, Vector2? impactDims = null, int impactFps = -1)
        {
            // Fix breakage with GenerateBeamPrefab() expecting a non-null specrigidbody
            projectile.specRigidbody = projectile.gameObject.GetOrAddComponent<SpeculativeRigidbody>();

            // Unnecessary to delete these
            // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimation>());
            // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dTiledSprite>());
            // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<tk2dSpriteAnimator>());
            // UnityEngine.Object.Destroy(projectile.GetComponentInChildren<BasicBeamController>());

            // Compute beam offsets from middle-left of sprite
            Vector2 offsets = new Vector2(0, Mathf.Ceil(dims.y / 2f));
            // Compute impact offsets from true center of sprite
            Vector2? impactOffsets = impactDims.HasValue ? new Vector2(Mathf.Ceil(impactDims.Value.x / 2f), Mathf.Ceil(impactDims.Value.y / 2f)) : null;

            // Create the beam itself using our resource map lookup
            BasicBeamController beamComp = projectile.FixedGenerateBeamPrefab(
                spritePath                  : ResMap.Get($"{spriteName}_mid")[0],
                colliderDimensions          : dims,
                colliderOffsets             : offsets,
                beamAnimationPaths          : ResMap.Get($"{spriteName}_mid"),
                beamFPS                     : fps,
                //Impact
                impactVFXAnimationPaths     : ResMap.Get($"{spriteName}_impact"),
                beamImpactFPS               : (impactFps > 0) ? impactFps : fps,
                impactVFXColliderDimensions : impactDims,
                impactVFXColliderOffsets    : impactOffsets,
                //End
                endVFXAnimationPaths        : ResMap.Get($"{spriteName}_end"),
                beamEndFPS                  : fps,
                endVFXColliderDimensions    : dims,
                endVFXColliderOffsets       : offsets,
                //Beginning
                muzzleVFXAnimationPaths     : ResMap.Get($"{spriteName}_start"),
                beamMuzzleFPS               : fps,
                muzzleVFXColliderDimensions : dims,
                muzzleVFXColliderOffsets    : offsets //,
                //Other Variables
                // glowAmount                  : 0f,
                // emissivecolouramt           : 0f
                );


            // fix some more animation glitches (don't consistently work, check and enable on a case by case basis)
            // beamComp.usesChargeDelay = false;
            // beamComp.muzzleAnimation = "beam_start;
            // beamComp.beamStartAnimation = null;
            // beamComp.chargeAnimation = null;
            // beamComp.rotateChargeAnimation = true;
            // projectile.shouldRotate = true;
            // projectile.shouldFlipVertically = true;

            return beamComp;
        }

        /// <summary>
        /// Perform basic initialization for a new projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromGun(Gun gun, bool setGunDefaultProjectile = true)
        {
            //actually instantiate the projectile
            Projectile projectile = gun.DefaultModule.projectiles[0].ClonePrefab<Projectile>();
            if (setGunDefaultProjectile)
                gun.DefaultModule.projectiles[0] = projectile; //reset the gun's default projectile
            return projectile;
        }

        /// <summary>
        /// Perform basic initialization for a copied projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromExistingProjectile(Projectile baseProjectile)
        {
            return baseProjectile.ClonePrefab<Projectile>();
        }

        /// <summary>
        /// Post a custom item pickup notification to the bottom of the screen
        /// </summary>
        public static void CustomNotification(string header, string text, tk2dBaseSprite sprite = null)
        {
            sprite ??= GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }

        /// <summary>
        /// Create a basic list of named directional animations given a list of animation names previously setup with SpriteBuilder.AddSpriteToCollection
        /// </summary>
        public static List<AIAnimator.NamedDirectionalAnimation> EasyNamedDirectionalAnimations(string[] animNameList)
        {
            var theList = new List<AIAnimator.NamedDirectionalAnimation>();
            for(int i = 0; i < animNameList.Count(); ++i)
            {
                string anim = animNameList[i];
                theList.Add(new AIAnimator.NamedDirectionalAnimation() {
                    name = anim,
                    anim = new DirectionalAnimation() {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = anim,
                        AnimNames = new string[] {anim},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    }
                });
            }
            return theList;
        }

        // Stolen from NN
        public static bool PlayerHasActiveSynergy(this PlayerController player, string synergyNameToCheck)
        {
            foreach (int index in player.ActiveExtraSynergies)
            {
                if (GameManager.Instance.SynergyManager.synergies[index].NameKey == synergyNameToCheck)
                    return true;
            }
            return false;
        }

        public static void MovePlayerTowardsPositionUntilHittingWall(PlayerController player, Vector2 position)
        {
            int num_steps = 100;

            Vector2 playerPos   = player.transform.position;
            Vector2 targetPos   = position;
            Vector2 deltaPos    = (targetPos - playerPos)/((float)(num_steps));
            Vector2 adjustedPos = Vector2.zero;

            // magic code that slowly moves the player out of walls
            for (int i = 0; i < num_steps; ++i)
            {
                player.transform.position = (playerPos + i * deltaPos).ToVector3ZisY();
                player.specRigidbody.Reinitialize();
                if (PhysicsEngine.Instance.OverlapCast(player.specRigidbody, null, true, false, null, null, false, null, null))
                {
                    player.transform.position = adjustedPos;
                    break;
                }
                adjustedPos = player.transform.position;
            }
        }

        public static string GetBaseIdleAnimationName(PlayerController p, float gunAngle)
        {
            string anim = string.Empty;
            bool hasgun = p.CurrentGun != null;
            bool invertThresholds = false;
            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
            {
                hasgun = false;
            }
            float num = 155f;
            float num2 = 25f;
            if (invertThresholds)
            {
                num = -155f;
                num2 = -25f;
            }
            float num3 = 120f;
            float num4 = 60f;
            float num5 = -60f;
            float num6 = -120f;
            bool flag2 = gunAngle <= num && gunAngle >= num2;
            if (invertThresholds)
                flag2 = gunAngle <= num || gunAngle >= num2;
            if (flag2)
            {
                if (gunAngle < num3 && gunAngle >= num4)
                    anim = (((!hasgun) && !p.ForceHandless) ? "_backward_twohands" : ((!p.RenderBodyHand) ? "_backward" : "_backward_hand"));
                else
                    anim = ((hasgun || p.ForceHandless) ? "_bw" : "_bw_twohands");
            }
            else if (gunAngle <= num5 && gunAngle >= num6)
                anim = (((!hasgun) && !p.ForceHandless) ? "_forward_twohands" : ((!p.RenderBodyHand) ? "_forward" : "_forward_hand"));
            else
                anim = (((!hasgun) && !p.ForceHandless) ? "_twohands" : ((!p.RenderBodyHand) ? "" : "_hand"));
            if (p.UseArmorlessAnim)
                anim += "_armorless";
            return "idle"+anim;
        }

        public static string GetBaseDodgeAnimationName(PlayerController p, Vector2 vector)
        {
            return ((!(Mathf.Abs(vector.x) < 0.1f)) ? (((!(vector.y > 0.1f)) ? "dodge_left" : "dodge_left_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")) : (((!(vector.y > 0.1f)) ? "dodge" : "dodge_bw") + ((!p.UseArmorlessAnim) ? string.Empty : "_armorless")));
        }

        // Get a random angle in range [-180,180]
        public static float RandomAngle()
        {
          return UnityEngine.Random.Range(-180f,180f);
        }

        // Get a random vector
        public static Vector2 RandomVector(float magnitude = 1f)
        {
          return magnitude * RandomAngle().ToVector();
        }

        // Get a random Quaternion rotated on the Z axis
        public static Quaternion RandomEulerZ()
        {
          return RandomAngle().EulerZ();
        }

        // Get a random boolean
        public static bool CoinFlip()
        {
          return UnityEngine.Random.Range(0,2) == 1;
        }

        // Get the defautl projectile for a gun by id
        public static Projectile GunDefaultProjectile(int gunid)
        {
            return (PickupObjectDatabase.GetById(gunid) as Gun).DefaultModule.projectiles[0];
        }

        // Blend two colors
        public static Color Blend(Color a, Color b, float t = 0.5f, bool blendAlpha = true)
        {
            return new Color(
                (1f - t) * a.r + t * b.r,
                (1f - t) * a.g + t * b.g,
                (1f - t) * a.b + t * b.b,
                blendAlpha ? ((1f - t) * a.a + t * b.a) : a.a
                );
        }

        // Given a floating point amount (e.g., 45.71), use the fractional component (e.g., .71) as the odds to return
        //  the ceiling of the amount (e.g., 48), returning the floor of the amount (e.g., 47) otherwise
        public static int RoundWeighted(this float amount)
        {
            return (UnityEngine.Random.value <= (amount - Math.Truncate(amount))
                ? Mathf.CeilToInt(amount)
                : Mathf.FloorToInt(amount));
        }

        // Get an enemy's idle animation blended with a color of choice, with optional sheen
        public static Texture2D GetTexturedEnemyIdleAnimation(AIActor enemy, Color blendColor, float blendAmount, Color? sheenColor = null, float sheenWidth = 20.0f)
        {
            // Get the best idle sprite for the enemy
            tk2dSpriteDefinition bestIdleSprite = enemy.sprite.collection.spriteDefinitions[CwaffToolbox.GetIdForBestIdleAnimation(enemy)];
            // If the x coordinate of the first two UVs match, we're using a rotated sprite
            bool isRotated = (bestIdleSprite.uvs[0].x == bestIdleSprite.uvs[1].x);
            // Remove the texture from the sprite sheet
            Texture2D spriteTexture = bestIdleSprite.DesheetTexture();
            // Create a new gold-blended texture for the sprite
            Texture2D goldTexture = new Texture2D(
                isRotated ? spriteTexture.height : spriteTexture.width,
                isRotated ? spriteTexture.width : spriteTexture.height);
            // Blend the pixels piece by piece
            for (int x = 0; x < goldTexture.width; x++)
            {
                for (int y = 0; y < goldTexture.height; y++)
                {
                    Color pixelColor = spriteTexture.GetPixel(isRotated ? y : x, isRotated ? x : y);
                    if (pixelColor.a > 0)
                    {
                        // Blend opaque pixels with blendColor
                        pixelColor = Color.Lerp(pixelColor, blendColor, blendAmount);
                        // Add a diagonal white sheen
                        if (sheenColor.HasValue)
                            pixelColor = Color.Lerp(pixelColor, sheenColor.Value, Mathf.Sin( 6.28f * ( ( (x+y) % sheenWidth) / sheenWidth )));
                    }
                    goldTexture.SetPixel(x, y, pixelColor);
                }
            }
            return goldTexture;
        }

        internal static Dictionary<string, float> _SoundTimers = new();
        public static void PlaySoundUntilDeathOrTimeout(string soundName, GameObject source, float timer)
        {
            if (_SoundTimers.ContainsKey(soundName))
                _SoundTimers[soundName] = timer; // reset the timer
            else
                GameManager.Instance.StartCoroutine(PlaySoundUntilDeathOrTimeout_CR(soundName, source, timer)); // play the sound
        }

        public static IEnumerator PlaySoundUntilDeathOrTimeout_CR(string soundName, GameObject source, float timer)
        {
            _SoundTimers[soundName] = timer;
            AkSoundEngine.PostEvent(soundName, source);
            while (source != null && _SoundTimers[soundName] > 0)
            {
                _SoundTimers[soundName] -= BraveTime.DeltaTime;
                yield return null;
            }
            AkSoundEngine.PostEvent($"{soundName}_stop_all", source);
            _SoundTimers.Remove(soundName);
        }

        public static void DoSmokeAt(Vector3 pos)
        {
            UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof") as GameObject)
                .GetComponent<tk2dBaseSprite>()
                .PlaceAtPositionByAnchor(pos, tk2dBaseSprite.Anchor.MiddleCenter);
        }

        public static void DoPickupAt(Vector3 pos)
        {
            GameObject original = (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Pickup");
              GameObject gameObject = UnityEngine.Object.Instantiate(original);
                tk2dSprite sprite = gameObject.GetComponent<tk2dSprite>();
                    sprite.PlaceAtPositionByAnchor(pos, tk2dBaseSprite.Anchor.MiddleCenter);
                    sprite.HeightOffGround = 6f;
                    sprite.UpdateZDepth();
            AkSoundEngine.PostEvent("Play_OBJ_item_pickup_01", gameObject);
        }

        public static DebrisObject MakeDebrisFromSprite(tk2dBaseSprite sprite, Vector3 position, Vector2? initialVelocity = null, float? angularVelocity = null)
        {
            GameObject debrisObject = new GameObject("debrisboi");
                debrisObject.transform.position = position;
            debrisObject.AddComponent<tk2dSprite>().SetSprite(sprite.collection, sprite.spriteId);
            // SpeculativeRigidbody rigidbody = fakeDebris.AddComponent<SpeculativeRigidbody>();
            DebrisObject debris = debrisObject.AddComponent<DebrisObject>();
            if (initialVelocity.HasValue)
                debris.Trigger(initialVelocity.Value/*new Vector2(4f, 4f)*/, 1f, angularVelocity ?? 0f);
            return debris;
        }

        public static RoomHandler CurrentRoom()
        {
            return GameManager.Instance.PrimaryPlayer.CurrentRoom;
        }

        private static Projectile _NullProjectilePrefab = null;
        public static Projectile NoProjectile()
        {
            if (_NullProjectilePrefab == null)
            {
                _NullProjectilePrefab                     = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.Ak47) as Gun, setGunDefaultProjectile: false);
                _NullProjectilePrefab.damageTypes         = CoreDamageTypes.None;
                _NullProjectilePrefab.collidesWithEnemies = false;
                _NullProjectilePrefab.collidesWithPlayer  = false;
                _NullProjectilePrefab.baseData.damage     = 0;
                _NullProjectilePrefab.baseData.speed      = 0.0001f;
                _NullProjectilePrefab.baseData.range      = 1f;
                _NullProjectilePrefab.gameObject.AddComponent<Expiration>().expirationTimer = 0f;
            }
            return _NullProjectilePrefab;
        }

        public static Vector2? NearestEnemyWithinConeOfVision(Vector2 start, float coneAngle, float maxDeviation, bool useNearestAngleInsteadOfDistance, bool ignoreWalls = false)
        {
            List<AIActor> activeEnemies = start.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            bool foundTarget            = false;
            float bestAngle             = maxDeviation;
            float bestDist              = 9999f;
            Vector2 bestTarget          = Vector2.zero;
            foreach (AIActor enemy in activeEnemies)
            {
                if (!enemy.IsHostileAndNotABoss())
                    continue;
                Vector2 tentativeTarget = enemy.sprite.WorldCenter;
                if (!ignoreWalls && !start.HasLineOfSight(tentativeTarget))
                    continue;
                Vector2 delta        = (tentativeTarget - start);
                float dist           = delta.magnitude;
                float angle          = delta.ToAngle().Clamp360();
                float angleDeviation = Mathf.Abs(coneAngle - angle);
                bool bestSoFar       = useNearestAngleInsteadOfDistance
                    ? (angleDeviation < bestAngle)
                    : (dist < bestDist);
                if (bestSoFar)
                {
                    foundTarget = true;
                    bestTarget  = tentativeTarget;
                    bestAngle   = angleDeviation;
                    bestDist    = dist;
                }
            }
            return foundTarget ? bestTarget : null;
        }

        public static Vector2? NearestEnemy(Vector2 start, float coneAngle, bool useNearestAngleInsteadOfDistance = false, bool ignoreWalls = false)
        {
            return NearestEnemyWithinConeOfVision(start, coneAngle, 360f, useNearestAngleInsteadOfDistance, ignoreWalls);
        }
    }
}