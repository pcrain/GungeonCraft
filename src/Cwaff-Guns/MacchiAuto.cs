namespace CwaffingTheGungy;

public class MacchiAuto : CwaffGun
{
    public static string ItemName         = "Macchi Auto";
    public static string ShortDescription = "Espresso Shots";
    public static string LongDescription  = "Fires a stream of coffee that inflicts a stacking caffeinated status on enemies, increasing their speed and inflicting damage over time.";
    public static string Lore             = "This gun was designed to answer one simple question: 'what would happen if you filled a water gun with espresso?' While the gun's designer may have been the first and only person to ever ask this question, it turns out many of the Gundead are actually very sensitive to caffeine and enter a berserker-like state when exposed to it. Assuming you can stay out of their warpath, the caffeine quickly takes its toll on even the most resilient among the Gundead.";

    internal static OverdoseEffect _OverdoseEffect = null;
    internal static Color _OverdoseTint = new Color(0.25f, 0.125f, 0.0f, 1.0f);

    public static void Init()
    {
        Lazy.SetupGun<MacchiAuto>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.BEAM, reloadTime: 1.0f, ammo: 100, audioFrom: Items.MegaDouser, defaultAudio: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(baseProjectile: Items.MegaDouser.Projectile(), clipSize: -1, shootStyle: ShootStyle.Beam,
            customClip: true, damage: 1f, speed: 50.0f, force: 0.0f, beamSprite: "overdose", beamFps: 20,
            beamEmission: 3f, beamStatusDelay: 0f, beamStartIsMuzzle: true, beamInterpolate: false,
            beamGoop: EasyGoopDefinitions.CoffeeGoop))
          .Attach<OverdoseJuice>();

        _OverdoseEffect = new OverdoseEffect() {
            TintColor        = _OverdoseTint,
            DeathTintColor   = _OverdoseTint,
            AppliesTint      = true,
            AppliesDeathTint = true,
            AffectsEnemies   = true,
            duration         = 10000000,
            effectIdentifier = ItemName,
            stackMode        = GameActorEffect.EffectStackingMode.DarkSoulsAccumulate,
            };
    }

    /// <summary>Allow beams to apply arbitrary status effects</summary>
    [HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.FrameUpdate))]
    private class BeamApplyArbitraryStatusEffectPatch
    {
        [HarmonyILManipulator]
        private static void BeamApplyArbitraryStatusEffectIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before,
              instr => instr.MatchCall<BraveBehaviour>("get_projectile"),
              instr => instr.MatchLdfld<Projectile>("AppliesSpeedModifier")))
                return;

            // BeamController is already on the stack here
            cursor.Emit(OpCodes.Ldloc_S, (byte)30); // V_30 == the enemy gameActor
            cursor.Emit(OpCodes.Call, typeof(BeamApplyArbitraryStatusEffectPatch).GetMethod("ApplyExtraBeamStatusEffects", BindingFlags.Static | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Ldarg_0); // put the BeamController back on the call stack
        }

        private static void ApplyExtraBeamStatusEffects(BeamController beam, GameActor gameActor)
        {
            // ETGModConsole.Log($"applying extra status effects");
            foreach (GameActorEffect e in beam.projectile.statusEffectsToApply)
                if (UnityEngine.Random.value < BraveMathCollege.SliceProbability(beam.statusEffectChance, BraveTime.DeltaTime))
                    gameActor.ApplyEffect(e);
        }
    }
}

public class OverdoseJuice : MonoBehaviour
{
    private void Start()
    {
        base.GetComponent<Projectile>().statusEffectsToApply.Add(MacchiAuto._OverdoseEffect);
    }
}

public class OverdoseEffect : GameActorEffect
{
    private const float _ACCUM_RATE = 0.5f;
    private const float _DAMAGE_RATE = 10.0f;

    public override void OnDarkSoulsAccumulate(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f, Projectile sourceProjectile = null)
    {
        if (actor is not AIActor enemy)
            return;
        effectData.accumulator += partialAmount * _ACCUM_RATE * BraveTime.DeltaTime;
        enemy.LocalTimeScale = 1f + effectData.accumulator;
    }

    public override void EffectTick(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        if (actor is not AIActor enemy)
            return;
        float dosage = _DAMAGE_RATE * effectData.accumulator;
        enemy.healthHaver.ApplyDamage(
            damage         : dosage * dosage * BraveTime.DeltaTime,
            direction      : Vector2.zero,
            sourceName     : this.effectIdentifier,
            damageTypes    : CoreDamageTypes.None,
            damageCategory : DamageCategory.DamageOverTime);
    }
}

public class GameActorCaffeineGoopEffect : GameActorSpeedEffect
{
    private static StatModifier[] _CaffeineGoopBuffs = null;

    public override void OnEffectApplied(GameActor actor, RuntimeGameActorEffectData effectData, float partialAmount = 1f)
    {
        base.OnEffectApplied(actor, effectData, partialAmount);
        if (actor is not PlayerController player)
            return;
        if (SpeedMultiplier == 1f)
            return;
        _CaffeineGoopBuffs ??= new[] {  //NOTE: speed handled by base GameActorSpeedEffect
            new StatModifier(){
                amount      = 1.2f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.RateOfFire,
            },
            new StatModifier(){
                amount      = 1.2f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.DodgeRollSpeedMultiplier,
            },
            new StatModifier(){
                amount      = 0.8f,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                statToBoost = PlayerStats.StatType.ReloadSpeed,
            },
        };
        foreach (StatModifier stat in _CaffeineGoopBuffs)
            player.ownerlessStatModifiers.Add(stat);
    }

    public override void OnEffectRemoved(GameActor actor, RuntimeGameActorEffectData effectData)
    {
        base.OnEffectRemoved(actor, effectData);
        if (actor is not PlayerController player)
            return;
        if (SpeedMultiplier == 1f)
            return;
        foreach (StatModifier stat in _CaffeineGoopBuffs)
            player.ownerlessStatModifiers.Remove(stat);
    }
}
