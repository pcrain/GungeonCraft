using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Dungeonator;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class RainbowGuonStone : AdvancedPlayerOrbitalItem
    {

        public static PlayerOrbital orbitalPrefab;
        public static PlayerOrbital upgradeOrbitalPrefab;
        public static void Init()
        {
            string itemName = "Rainbow Guon Stone"; //The name of the item
            string resourceName = "NevernamedsItems/Resources/GuonStones/rainbowguon_icon3"; //Refers to an embedded png in the project. Make sure to embed your resources!

            GameObject obj = new GameObject();

            var item = obj.AddComponent<RainbowGuonStone>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            string shortDesc = "Insanity Stone";
            string longDesc = "Proof of Alben Smallbore's theory of magical unpredictability." + "\n\nThis guon stone has been stuffed with so much magic that it erratically shifts it's effects like a child unable to sit still.";

            Material material = new Material(ShaderCache.Acquire("Brave/Internal/RainbowChestShader"));
            material.SetFloat("_AllColorsToggle", 1f);
            var def = item.sprite.GetCurrentSpriteDef();
            def.material = item.sprite.renderer.material;
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;


            BuildPrefab();
            item.OrbitalPrefab = orbitalPrefab;
            BuildSynergyPrefab();

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            item.HasAdvancedUpgradeSynergy = true;
            item.AdvancedUpgradeSynergy = "Rainbower Guon Stone";
            item.AdvancedUpgradeOrbitalPrefab = RainbowGuonStone.upgradeOrbitalPrefab.gameObject;

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.SetProjectileSpriteRight("mockorangeguon_proj", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            mockOrangeGuonProj = projectile2;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.RAINBOW_KILLED_LICH, true);
        }
        public static Projectile mockOrangeGuonProj;
        public static void BuildPrefab()
        {
            if (RainbowGuonStone.orbitalPrefab != null) return;
            GameObject prefab = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/rainbowguon_ingame");
            prefab.name = "Rainbow Guon Orbital";
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(5, 9));
            prefab.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, body.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = PlayerOrbital.OrbitalMotionStyle.ORBIT_PLAYER_ALWAYS;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = 2.5f;
            orbitalPrefab.orbitDegreesPerSecond = 120f;
            orbitalPrefab.SetOrbitalTier(0);

            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);
        }
        public static void BuildSynergyPrefab()
        {
            bool flag = RainbowGuonStone.upgradeOrbitalPrefab == null;
            if (flag)
            {
                GameObject gameObject = SpriteBuilder.SpriteFromResource("NevernamedsItems/Resources/GuonStones/rainbowguon_synergy", null);
                gameObject.name = "Rainbow Guon Orbital Synergy Form";
                SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(IntVector2.Zero, new IntVector2(9, 14));
                gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleCenter, gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef().position3);
                RainbowGuonStone.upgradeOrbitalPrefab = gameObject.AddComponent<PlayerOrbital>();
                speculativeRigidbody.CollideWithTileMap = false;
                speculativeRigidbody.CollideWithOthers = true;
                speculativeRigidbody.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;
                RainbowGuonStone.upgradeOrbitalPrefab.shouldRotate = false;
                RainbowGuonStone.upgradeOrbitalPrefab.orbitRadius = 2.5f;
                RainbowGuonStone.upgradeOrbitalPrefab.perfectOrbitalFactor = 10f;
                RainbowGuonStone.upgradeOrbitalPrefab.orbitDegreesPerSecond = 120f;
                RainbowGuonStone.upgradeOrbitalPrefab.SetOrbitalTier(0);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
            }
        }
        bool canFireCyanProjectile = true;
        bool canDoGreyCollisionDMG = true;
        bool canFireOrangeProjectile = true;
        bool canReselectGuonState = true;
        bool canDoBlueSynergyRoomDMG = true;
        private void FireProjectileFromGuon(GameObject projectile, bool scaleStats, bool postProcess, float specialDamageScaling = 1, float angleFromAim = 0, float angleVariance = 0, bool playerStatScalesAccuracy = false)
        {
            
            GameObject gameObject = projectile.GetComponent<Projectile>().InstantiateAndFireTowardsPosition(this.m_extantOrbital.GetComponent<tk2dSprite>().WorldCenter, this.m_extantOrbital.GetComponent<tk2dSprite>().WorldCenter.GetPositionOfNearestEnemy(ActorCenter.SPRITE), angleVariance, angleVariance, (playerStatScalesAccuracy ? Owner : null));
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = Owner;
                component.Shooter = Owner.specRigidbody;
                component.baseData.damage *= specialDamageScaling;
                if (scaleStats)
                {
                    component.TreatedAsNonProjectileForChallenge = true;
                    component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                    component.UpdateSpeed();
                }
                if (postProcess)
                {
                    Owner.DoPostProcessProjectile(component);
                }
            }
        }

        public override void Update()
        {
            if (m_poisonImmunity == null)
            {
                m_poisonImmunity = new DamageTypeModifier();
                m_poisonImmunity.damageMultiplier = 0f;
                m_poisonImmunity.damageType = CoreDamageTypes.Poison;
            }
            if (m_fireImmunity == null)
            {
                m_fireImmunity = new DamageTypeModifier();
                m_fireImmunity.damageMultiplier = 0f;
                m_fireImmunity.damageType = CoreDamageTypes.Fire;
            }
            if (m_electricityImmunity == null)
            {
                m_electricityImmunity = new DamageTypeModifier();
                m_electricityImmunity.damageMultiplier = 0f;
                m_electricityImmunity.damageType = CoreDamageTypes.Electric;
            }
            if (machoDamageMod == null)
            {
                machoDamageMod = new StatModifier();
                machoDamageMod.statToBoost = PlayerStats.StatType.Damage;
                machoDamageMod.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
                machoDamageMod.amount = 1.3f;
            }

            if (this.m_extantOrbital != null && this.Owner != null)
            {
                if (this.m_extantOrbital.GetComponent<PlayerOrbital>().orbitRadius != overrideOrbitalDistance) { this.m_extantOrbital.GetComponent<PlayerOrbital>().orbitRadius = overrideOrbitalDistance; }
                if (this.m_extantOrbital.GetComponent<PlayerOrbital>().orbitDegreesPerSecond != overrideOrbitalSpeed) { this.m_extantOrbital.GetComponent<PlayerOrbital>().orbitDegreesPerSecond = overrideOrbitalSpeed; }
                if (this.m_extantOrbital.GetComponent<PlayerOrbital>().perfectOrbitalFactor != overridePerfectOrbitalFactor) { this.m_extantOrbital.GetComponent<PlayerOrbital>().perfectOrbitalFactor = overridePerfectOrbitalFactor; }

                if (this.m_extantOrbital.GetComponent<tk2dSprite>().renderer.material.shader != ShaderCache.Acquire("Brave/Internal/RainbowChestShader"))
                {
                    this.m_extantOrbital.GetComponent<tk2dSprite>().sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/RainbowChestShader");
                    this.m_extantOrbital.GetComponent<tk2dSprite>().sprite.renderer.material.SetFloat("_AllColorsToggle", 1f);
                }

                if (canReselectGuonState && Owner)
                {
                    canReselectGuonState = false;
                    ReSelectGuonState();
                    Invoke("resetGuonStateCooldown", 5f);
                }


                if (Owner && Owner.IsInCombat && Owner.specRigidbody.Velocity == Vector2.zero && canFireCyanProjectile)
                {
                    if (RandomlySelectedGuonState == GuonState.CYAN)
                    {
                        FireProjectileFromGuon(CyanGuonStone.cyanGuonProj.gameObject, true, true, 1, 0, 5, true);
                        canFireCyanProjectile = false;
                        if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) Invoke("resetCyanFireCooldown", 0.16f);
                        else Invoke("resetCyanFireCooldown", 0.35f);
                    }
                }
                if (Owner && Owner.IsInCombat && canFireOrangeProjectile)
                {
                    if (RandomlySelectedGuonState == GuonState.ORANGE)
                    {
                        canFireOrangeProjectile = false;
                        if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone"))
                        {
                            FireProjectileFromGuon(mockOrangeGuonProj.gameObject, true, true, 1.6f);
                            Invoke("resetOrangeFireCooldown", 0.5f);
                        }
                        else
                        {
                            FireProjectileFromGuon(mockOrangeGuonProj.gameObject, true, true, 1);
                            Invoke("resetOrangeFireCooldown", 1f);
                        }
                    }
                }
            }
            base.Update();
        }
        GuonState RandomlySelectedGuonState;
        private float overrideOrbitalDistance = 2.5f;
        private float overrideOrbitalSpeed = 120f;
        private float overridePerfectOrbitalFactor = 0f;
        DamageTypeModifier m_poisonImmunity;
        DamageTypeModifier m_fireImmunity;
        DamageTypeModifier m_electricityImmunity;
        private void ReSelectGuonState()
        {
            overrideOrbitalDistance = 2.5f;
            overrideOrbitalSpeed = 120f;
            overridePerfectOrbitalFactor = 0f;
            RemoveStat(PlayerStats.StatType.AdditionalBlanksPerFloor);
            RemoveStat(PlayerStats.StatType.DodgeRollSpeedMultiplier);
            Owner.healthHaver.damageTypeModifiers.Remove(m_poisonImmunity);
            Owner.healthHaver.damageTypeModifiers.Remove(m_fireImmunity);
            Owner.healthHaver.damageTypeModifiers.Remove(m_electricityImmunity);
            RandomlySelectedGuonState = RandomEnum<GuonState>.Get();
            Owner.stats.RecalculateStats(Owner, true, false);

            if (GuonTransitionVFX.ContainsKey(RandomlySelectedGuonState))
            {
                UnityEngine.Object.Instantiate<GameObject>(GuonTransitionVFX[RandomlySelectedGuonState], m_extantOrbital.GetComponent<tk2dBaseSprite>().WorldCenter, Quaternion.identity);
            }

            if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) overridePerfectOrbitalFactor = 10f;

            if (RandomlySelectedGuonState == GuonState.RED) { AddStat(PlayerStats.StatType.DodgeRollSpeedMultiplier, 1.3f, StatModifier.ModifyMethod.MULTIPLICATIVE); }
            else if (RandomlySelectedGuonState == GuonState.WHITE) { AddStat(PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE); }
            else if (RandomlySelectedGuonState == GuonState.CLEAR)
            {
                Owner.healthHaver.damageTypeModifiers.Add(m_poisonImmunity);
                if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone"))
                {
                    Owner.healthHaver.damageTypeModifiers.Add(m_fireImmunity);
                    Owner.healthHaver.damageTypeModifiers.Add(m_electricityImmunity);
                }
            }
            else if (RandomlySelectedGuonState == GuonState.BLUE)
            {
                if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone"))
                {
                    overrideOrbitalDistance = 4f;
                    overrideOrbitalSpeed = 360f;
                }
                else { overrideOrbitalSpeed = 240f; }
            }
            else if (RandomlySelectedGuonState == GuonState.BROWN)
            {
                if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) overrideOrbitalDistance = 1.75f;
                else overrideOrbitalDistance = 3f;
                overrideOrbitalSpeed = CalculateSpeedForBrownOrbital();
            }
            else if (RandomlySelectedGuonState == GuonState.INDIGO)
            {
                overridePerfectOrbitalFactor = 10f;
                overrideOrbitalDistance = 1f;
                overrideOrbitalSpeed = 100f;
            }
            Owner.stats.RecalculateStats(Owner, true, false);
            //AssignOrbitalCollisionEffects();
            //DebugPrintType();
        }
        private void DebugPrintType()
        {
            switch (RandomlySelectedGuonState)
            {
                case GuonState.RED:
                    ETGModConsole.Log("Red");
                    break;
                case GuonState.ORANGE:
                    ETGModConsole.Log("Orange");
                    break;
                case GuonState.YELLOW:
                    ETGModConsole.Log("Yellow");
                    break;
                case GuonState.GREEN:
                    ETGModConsole.Log("Green");
                    break;
                case GuonState.BLUE:
                    ETGModConsole.Log("Blue");
                    break;
                case GuonState.WHITE:
                    ETGModConsole.Log("White");
                    break;
                case GuonState.CLEAR:
                    ETGModConsole.Log("Clear");
                    break;
                case GuonState.CYAN:
                    ETGModConsole.Log("Cyan");
                    break;
                case GuonState.GOLD:
                    ETGModConsole.Log("Gold");
                    break;
                case GuonState.GREY:
                    ETGModConsole.Log("Grey");
                    break;
                case GuonState.BROWN:
                    ETGModConsole.Log("Brown");
                    break;
                case GuonState.INDIGO:
                    ETGModConsole.Log("Indigo");
                    break;
            }
        }
        private float CalculateSpeedForBrownOrbital()
        {
            float orbitalSpeed = 40f;
            foreach (PassiveItem item in Owner.passiveItems)
            {
                if (item.quality == PickupObject.ItemQuality.D || item.PickupObjectId == 127)
                {
                    if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) orbitalSpeed += 20;
                    else orbitalSpeed += 10f;
                }
            }
            foreach (Gun gun in Owner.inventory.AllGuns)
            {
                if (gun.quality == PickupObject.ItemQuality.D)
                {
                    if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) orbitalSpeed += 20;
                    else orbitalSpeed += 10f;
                }
            }
            return orbitalSpeed;
        }
        public enum GuonState
        {
            RED, //Done + Synergy
            ORANGE, //Done + Synergy
            YELLOW, //Done + Synergy
            GREEN, //Done + Synergy
            BLUE, //Done  + Synergy
            WHITE, //Done (Synergy not worth it)
            CLEAR, //Done + Synergy
            CYAN, //Done + Synergy
            GOLD, //Done + Synergy
            GREY, //Done + Synergy
            BROWN, //Done + Synergy
            INDIGO,
        }
        public Dictionary<GuonState, GameObject> GuonTransitionVFX = new Dictionary<GuonState, GameObject>()
        {
            {GuonState.RED, RedGuonTransitionVFX},
            {GuonState.ORANGE, OrangeGuonTransitionVFX},
            {GuonState.YELLOW, YellowGuonTransitionVFX},
            {GuonState.GREEN, GreenGuonTransitionVFX},
            {GuonState.BLUE, BlueGuonTransitionVFX},
            {GuonState.WHITE, WhiteGuonTransitionVFX},
            {GuonState.CYAN, CyanGuonTransitionVFX},
            {GuonState.GOLD, GoldGuonTransitionVFX},
            {GuonState.GREY, GreyGuonTransitionVFX},
            {GuonState.INDIGO, IndigoGuonTransitionVFX},
            {GuonState.BROWN, BrownGuonTransitionVFX},
        };
        public static GameObject RedGuonTransitionVFX;
        public static GameObject OrangeGuonTransitionVFX;
        public static GameObject YellowGuonTransitionVFX;
        public static GameObject GreenGuonTransitionVFX;
        public static GameObject BlueGuonTransitionVFX;
        public static GameObject WhiteGuonTransitionVFX;
        public static GameObject CyanGuonTransitionVFX;
        public static GameObject GoldGuonTransitionVFX;
        public static GameObject GreyGuonTransitionVFX;
        public static GameObject BrownGuonTransitionVFX;
        public static GameObject IndigoGuonTransitionVFX;
        private void resetCyanFireCooldown() { canFireCyanProjectile = true; }
        private void resetBlueRoomDMG() { canDoBlueSynergyRoomDMG = true; }
        private void resetOrangeFireCooldown() { canFireOrangeProjectile = true; }
        private void resetGuonStateCooldown() { canReselectGuonState = true; }
        private void resetGreyCollisionDMG() { canDoGreyCollisionDMG = true; }

        private void OnGuonHitByBullet(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody other, PixelCollider otherCollider)
        {
            if (other.projectile && !(other.projectile.Owner is PlayerController))
            {
                //ETGModConsole.Log("Orbital was hit by a bullet");
                if (RandomlySelectedGuonState == GuonState.GOLD)
                {
                    float procChance = 0.1f;
                    if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) procChance = 0.15f;
                    if (UnityEngine.Random.value <= procChance)
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(68).gameObject, other.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    }
                }
                if (RandomlySelectedGuonState == GuonState.BLUE)
                {
                    if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone"))
                    {
                        if (canDoBlueSynergyRoomDMG)
                        {
                            canDoBlueSynergyRoomDMG = false;
                            List<AIActor> activeEnemies = Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                            if (activeEnemies != null)
                            {
                                for (int i = 0; i < activeEnemies.Count; i++)
                                {
                                    AIActor aiactor = activeEnemies[i];
                                    aiactor.healthHaver.ApplyDamage(15, Vector2.zero, "Blue Guon Stone", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                                }
                            }
                            Invoke("resetBlueRoomDMG", 1f);
                        }
                    }
                }
                if (RandomlySelectedGuonState == GuonState.GREY)
                {
                    if (canDoGreyCollisionDMG && other.projectile.Owner is AIActor)
                    {
                        float DMG = 5f;
                        if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) DMG *= 2f;
                        if ((other.projectile.Owner as AIActor).IsBlackPhantom) DMG *= 3f;
                        DMG *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        (other.projectile.Owner as AIActor).healthHaver.ApplyDamage(DMG, Vector2.zero, "Guon Wrath", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);
                        Invoke("resetGreyCollisionDMG", 0.15f);

                    }
                }
                if (RandomlySelectedGuonState == GuonState.RED)
                {
                    if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone"))
                    {
                        Owner.StartCoroutine(this.HandleMachoDamageBoost(Owner));
                    }
                }
                if (RandomlySelectedGuonState == GuonState.INDIGO)
                {
                    if (Owner.IsDodgeRolling) PhysicsEngine.SkipCollision = true;
                    else
                    {
                        float procChance = 0.35f;
                        if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) procChance = 0.6f;
                        if (UnityEngine.Random.value <= procChance)
                        {
                            EasyBlankType blankType = EasyBlankType.MINI;
                            if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone") && UnityEngine.Random.value <= 0.2) blankType = EasyBlankType.FULL;
                            Owner.DoEasyBlank(this.m_extantOrbital.transform.position, blankType);
                        }
                    }
                }
            }
        }
        private void PostProcessBeamTick(BeamController arg1, SpeculativeRigidbody arg2, float arg3)
        {
            if (!this.m_hasUsedShot)
            {
                this.m_beamTickElapsed += BraveTime.DeltaTime;
                if (this.m_beamTickElapsed > 1f)
                {
                    this.m_hasUsedShot = true;
                }
            }
        }
        private void PostProcessProjectile(Projectile targetProjectile, float arg2)
        {
            if (Owner)
            {
                if (RandomlySelectedGuonState == GuonState.CLEAR && Owner.PlayerHasActiveSynergy("Rainbower Guon Stone") && Owner.CurrentGoop)
                {
                    if (Owner.CurrentGoop.CanBeIgnited)
                    {
                        if (!targetProjectile.AppliesFire)
                        {
                            targetProjectile.AppliesFire = true;
                            targetProjectile.FireApplyChance = 1f;
                            targetProjectile.fireEffect = Owner.CurrentGoop.fireEffect;
                        }
                    }
                    else if (Owner.CurrentGoop.CanBeFrozen && !targetProjectile.AppliesFreeze)
                    {
                        targetProjectile.AppliesFreeze = true;
                        targetProjectile.FreezeApplyChance = 1f;
                        targetProjectile.freezeEffect = PickupObjectDatabase.GetById(264).GetComponent<IounStoneOrbitalItem>().DefaultFreezeEffect;
                    }
                }
                if (this.m_destroyVFXSemaphore > 0)
                {
                    targetProjectile.AdjustPlayerProjectileTint(new Color(1f, 0.9f, 0f), 50, 0f);
                    if (!this.m_hasUsedShot)
                    {
                        this.m_hasUsedShot = true;
                        if (base.Owner && EasyVFXDatabase.MachoBraceDustUpVFX)
                        {
                            base.Owner.PlayEffectOnActor(EasyVFXDatabase.MachoBraceDustUpVFX, new Vector3(0f, -0.625f, 0f), false, false, false);
                            AkSoundEngine.PostEvent("Play_ITM_Macho_Brace_Trigger_01", base.gameObject);
                        }
                        if (base.Owner && EasyVFXDatabase.MachoBraceBurstVFX)
                        {
                            base.Owner.PlayEffectOnActor(EasyVFXDatabase.MachoBraceBurstVFX, new Vector3(0f, 0.375f, 0f), false, false, false);
                        }
                    }
                }
            }
        }
        private void OnTookDamage(PlayerController player)
        {
            if (RandomlySelectedGuonState == GuonState.GREEN)
            {
                float critProc = 0.5f;
                if (player.PlayerHasActiveSynergy("Rainbower Guon Stone")) critProc = 0.7f;
                if (player.healthHaver.GetCurrentHealth() < critProc)
                {
                    if (UnityEngine.Random.value <= 0.5f)
                    {
                        player.healthHaver.ApplyHealing(0.5f);
                        if (player.PlayerHasActiveSynergy("Rainbower Guon Stone")) LootEngine.SpawnCurrency(player.CenterPosition, 20, false);
                    }
                }
                else
                {
                    if (UnityEngine.Random.value <= 0.2f)
                    {
                        player.healthHaver.ApplyHealing(0.5f);
                        if (player.PlayerHasActiveSynergy("Rainbower Guon Stone")) LootEngine.SpawnCurrency(player.CenterPosition, 20, false);
                    }

                }
            }
            if (RandomlySelectedGuonState == GuonState.BLUE)
            {
                player.StartCoroutine(this.HandleSlowBullets());
            }
        }
        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemyHealth)
        {
            if (RandomlySelectedGuonState == GuonState.YELLOW && Owner && enemyHealth && fatal)
            {
                if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) StartCoroutine(HandleShield(Owner, 2));
                else StartCoroutine(HandleShield(Owner, 1));
            }
        }
        public override void OnOrbitalCreated(GameObject orbital)
        {
            //ETGModConsole.Log("Orbital Was Created" + "\nOrbital Name: " + orbital.name);
            if (orbital.GetComponent<SpeculativeRigidbody>())
            {
                SpeculativeRigidbody specRigidbody = orbital.GetComponent<SpeculativeRigidbody>();
                specRigidbody.OnPreRigidbodyCollision += this.OnGuonHitByBullet;
            }
            base.OnOrbitalCreated(orbital);
        }
        private void OnTookDamageFromProjectile(Projectile incomingProjectile, PlayerController arg2)
        {
            if (RandomlySelectedGuonState == GuonState.GREY && incomingProjectile.Owner && incomingProjectile.Owner is AIActor)
            {
                float baseDMG = 25f;
                if (Owner.PlayerHasActiveSynergy("Rainbower Guon Stone")) baseDMG = 50f;
                if (incomingProjectile.Owner.aiActor.IsBlackPhantom) incomingProjectile.Owner.healthHaver.ApplyDamage((arg2.stats.GetStatValue(PlayerStats.StatType.Damage) * baseDMG) * 3, Vector2.zero, "Guon Wrath", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);
                else incomingProjectile.Owner.healthHaver.ApplyDamage(arg2.stats.GetStatValue(PlayerStats.StatType.Damage) * baseDMG, Vector2.zero, "Guon Wrath", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeamTick += this.PostProcessBeamTick;
            player.OnReceivedDamage += this.OnTookDamage;
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
            player.OnHitByProjectile += this.OnTookDamageFromProjectile;
            //GameManager.Instance.OnNewLevelFullyLoaded += this.AssignOrbitalCollisionEffects;
            base.Pickup(player);
            //AssignOrbitalCollisionEffects();
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeamTick -= this.PostProcessBeamTick;
            player.OnReceivedDamage -= this.OnTookDamage;
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            //GameManager.Instance.OnNewLevelFullyLoaded -= this.AssignOrbitalCollisionEffects;
            player.OnHitByProjectile -= this.OnTookDamageFromProjectile;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            //GameManager.Instance.OnNewLevelFullyLoaded -= this.AssignOrbitalCollisionEffects;
            if (Owner)
            {
                Owner.PostProcessBeamTick -= this.PostProcessBeamTick;
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
                Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
                Owner.OnReceivedDamage -= this.OnTookDamage;
                Owner.OnHitByProjectile -= this.OnTookDamageFromProjectile;
            }
            base.OnDestroy();
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }

        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }

        float m_activeDuration = 1f;
        bool m_usedOverrideMaterial;
        private IEnumerator HandleShield(PlayerController user, float duration)
        {
            m_activeDuration = duration;
            m_usedOverrideMaterial = user.sprite.usesOverrideMaterial;
            user.sprite.usesOverrideMaterial = true;
            user.SetOverrideShader(ShaderCache.Acquire("Brave/ItemSpecific/MetalSkinShader"));
            SpeculativeRigidbody specRigidbody = user.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
            user.healthHaver.IsVulnerable = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += BraveTime.DeltaTime;
                user.healthHaver.IsVulnerable = false;
                yield return null;
            }
            if (user)
            {
                user.healthHaver.IsVulnerable = true;
                user.ClearOverrideShader();
                user.sprite.usesOverrideMaterial = this.m_usedOverrideMaterial;
                SpeculativeRigidbody specRigidbody2 = user.specRigidbody;
                specRigidbody2.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Remove(specRigidbody2.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.OnPreCollision));
                //IsCurrentlyActive = false;
            }
            if (this)
            {
                AkSoundEngine.PostEvent("Play_OBJ_metalskin_end_01", base.gameObject);
            }
            yield break;
        }
        private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            Projectile component = otherRigidbody.GetComponent<Projectile>();
            if (component != null && !(component.Owner is PlayerController))
            {
                PassiveReflectItem.ReflectBullet(component, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                PhysicsEngine.SkipCollision = true;
            }
        }
        private IEnumerator HandleSlowBullets()
        {
            yield return new WaitForEndOfFrame();
            //this.m_isSlowingBullets = true;
            float slowMultiplier = PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsMultiplier;
            Projectile.BaseEnemyBulletSpeedMultiplier *= slowMultiplier;
            this.m_slowDurationRemaining = PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsDuration;
            while (this.m_slowDurationRemaining > 0f)
            {
                yield return null;
                this.m_slowDurationRemaining -= BraveTime.DeltaTime;
                Projectile.BaseEnemyBulletSpeedMultiplier /= slowMultiplier;
                slowMultiplier = Mathf.Lerp(PickupObjectDatabase.GetById(270).GetComponent<IounStoneOrbitalItem>().SlowBulletsMultiplier, 1f, 1f - this.m_slowDurationRemaining);
                Projectile.BaseEnemyBulletSpeedMultiplier *= slowMultiplier;
            }
            Projectile.BaseEnemyBulletSpeedMultiplier /= slowMultiplier;
            //this.m_isSlowingBullets = false;
            yield break;

        }
        //private bool m_isSlowingBullets;
        private float m_slowDurationRemaining;
        /*private void AssignOrbitalCollisionEffects()
        {
            if (Owner)
            {
                foreach (IPlayerOrbital playerOrbital in Owner.orbitals)
                {
                    PlayerOrbital playerOrbital2 = (PlayerOrbital)playerOrbital;
                    //ETGModConsole.Log(playerOrbital2.name);
                    if (playerOrbital2 == this.m_extantOrbital.GetComponent<PlayerOrbital>())
                    {

                        SpeculativeRigidbody specRigidbody = playerOrbital2.specRigidbody;
                        specRigidbody.OnPreRigidbodyCollision += this.OnGuonHitByBullet;
                    }
                }
            }
        }*/

        //EVERYTHING BELOW THIS POINT IS MACHO BRACE SHIT FOR THE REDDER GUON STONE SYNERGY ----------------------------------------------
        private IEnumerator HandleMachoDamageBoost(PlayerController target)
        {
            this.EnableMachoVFX(target);
            this.m_destroyVFXSemaphore++;
            if (this.m_destroyVFXSemaphore == 1)
            {
                AkSoundEngine.PostEvent("Play_ITM_Macho_Brace_Active_01", base.gameObject);
            }
            this.m_hasUsedShot = false;
            while (target.IsDodgeRolling)
            {
                yield return null;
            }
            this.m_beamTickElapsed = 0f;
            float elapsed = 0f;
            target.ownerlessStatModifiers.Add(this.machoDamageMod);
            target.stats.RecalculateStats(target, false, false);
            while (elapsed < 1.5f && !this.m_hasUsedShot)
            {
                elapsed += BraveTime.DeltaTime;
                yield return null;
            }
            target.ownerlessStatModifiers.Remove(this.machoDamageMod);
            if (this.m_destroyVFXSemaphore == 1)
            {
                AkSoundEngine.PostEvent("Play_ITM_Macho_Brace_Fade_01", base.gameObject);
            }
            target.stats.RecalculateStats(target, false, false);
            this.m_destroyVFXSemaphore--;
            if (this.m_hasUsedShot)
            {
                this.m_destroyVFXSemaphore = 0;
            }
            this.DisableMachoVFX(target);
            yield break;
        }
        public void EnableMachoVFX(PlayerController target)
        {
            if (this.m_destroyVFXSemaphore == 0)
            {
                Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(target.sprite);
                if (outlineMaterial != null)
                {
                    outlineMaterial.SetColor("_OverrideColor", new Color(99f, 99f, 0f));
                }
                if (EasyVFXDatabase.MachoBraceOverheadVFX && !this.m_instanceVFX)
                {
                    this.m_instanceVFX = target.PlayEffectOnActor(EasyVFXDatabase.MachoBraceOverheadVFX, new Vector3(0f, 1.375f, 0f), true, true, false);
                }
            }
        }

        private int m_destroyVFXSemaphore;
        private bool m_hasUsedShot;
        private float m_beamTickElapsed;
        private GameObject m_instanceVFX;
        private StatModifier machoDamageMod;
        public void DisableMachoVFX(PlayerController target)
        {
            if (this.m_destroyVFXSemaphore == 0)
            {
                Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(target.sprite);
                if (outlineMaterial != null)
                {
                    outlineMaterial.SetColor("_OverrideColor", new Color(0f, 0f, 0f));
                }
                if (!this.m_hasUsedShot)
                {
                }
                if (this.m_instanceVFX)
                {
                    SpawnManager.Despawn(this.m_instanceVFX);
                    this.m_instanceVFX = null;
                }
            }
        }
    }
}