namespace CwaffingTheGungy;

public class StopSign : CwaffActive
{
    public static string ItemName         = "Stop Sign";
    public static string ShortDescription = "Believing";
    public static string LongDescription  = "Briefly stuns any enemies that are currently moving, indefinitely immobilizing them after the stun wears off.";
    public static string Lore             = "There is nothing more effective at stopping the Gundead in their tracks than plastering giant metal street signs in front of their faces as they're darting about. Except for possibly bullets. Or goop. Or pits. Or...okay, maybe a lot of things are more effective. But nothing is quite as octagonally satisfying.";

    internal static GameActorSpeedEffect _SpeedEffect;
    internal static GameObject _StopSignVFX;

    private class StoppedInTheirTracks : MonoBehaviour {}

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<StopSign>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.C;
        item.consumable = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Damage, 160f);

        //WARNING: reusing ammonomicon icon screws up bounding box in ammonomicon
        // _StopSignVFX = VFX.Create("stop_sign_icon", 2, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1f);
        _StopSignVFX = VFX.Create("stop_sign_vfx", 2, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 1f);

        _SpeedEffect = new GameActorSpeedEffect {
            SpeedMultiplier    = 0f,
            CooldownMultiplier = 1f,
            AffectsPlayers     = false,
            AffectsEnemies     = true,
            effectIdentifier   = "stopped",
            resistanceType     = EffectResistanceType.None,
            stackMode          = GameActorEffect.EffectStackingMode.Refresh,
            duration           = 3600f,
            maxStackedDuration = -1f,
            AppliesTint        = false,
            AppliesDeathTint   = false,
            AppliesOutlineTint = false,
            OverheadVFX        = Items.TripleCrossbow.AsGun().DefaultModule.projectiles[0].speedEffect.OverheadVFX,
            PlaysVFXOnActor    = false,
        };
    }

    public override void DoEffect(PlayerController user)
    {
        bool didAnything = false;
        foreach (AIActor enemy in user.CurrentRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.IsHostile(canBeNeutral: true))
                continue;
            if (!enemy.behaviorSpeculator || enemy.behaviorSpeculator.ImmuneToStun)
                continue;
            if (enemy.VoluntaryMovementVelocity.magnitude < 0.1f)
                continue;
            if (enemy.GetComponent<StoppedInTheirTracks>())
                continue;
            FancyVFX.Spawn(_StopSignVFX, (enemy.sprite ? enemy.sprite.WorldTopCenter : enemy.CenterPosition) + new Vector2(0, 1f),
                lifetime: 0.25f, fadeOutTime: 0.5f, endScale: 2f, height: 1f);
            enemy.behaviorSpeculator.Stun(2f);
            enemy.ApplyEffect(_SpeedEffect);
            enemy.gameObject.AddComponent<StoppedInTheirTracks>();
            didAnything = true;
        }
        if (!didAnything)
            return;
        base.gameObject.Play("stop_sign_sound");
    }
}
