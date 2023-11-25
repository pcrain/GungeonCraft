namespace CwaffingTheGungy;

public class VoodooDoll : PassiveItem
{
    public static string ItemName         = "Voodoo Doll";
    public static string SpritePath       = "voodoo_doll_icon";
    public static string ShortDescription = "Pew Pew Unto Others";
    public static string LongDescription  = "Whenever a player-owned projectile hits an enemy, all other enemies of the same type take damage.";
    public static string Lore             = "There are actually two types of voodoo dolls. Traditional voodoo dolls are created in the likeness of a single, specific individual, and doing physical damage to the doll inflicts equivalent physical damage to the individual it depicts. By contrast, the dolls found in the Gungeon depict an unknown figure smiling in the face of life's hardships, motivating the Gundead to do their best. Upon seeing their peers struggle, this motivation is promptly shattered, inflicting (arguably more powerful) emotional damage instead.";

    private static bool _VoodooDollEffectHappening = false;

    internal static GameObject _VoodooGhostVFX;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<VoodooDoll>(ItemName, SpritePath, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.A;
        item.AddToSubShop(ItemBuilder.ShopType.Cursula);

        _VoodooGhostVFX   = VFX.RegisterVFXObject("voodoo_ghost",
            fps: 2, loops: true, anchor: Anchor.MiddleCenter, scale: 0.5f);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.OnDealtDamageContext += this.OnDealtDamage;
    }

    public override DebrisObject Drop(PlayerController player)
    {
        player.OnDealtDamageContext -= this.OnDealtDamage;
        return base.Drop(player);
    }

    private void OnDealtDamage(PlayerController source, float damage, bool fatal, HealthHaver enemy)
    {
        if (_VoodooDollEffectHappening)
            return; // avoid recursive damage
        if (!(enemy?.aiActor?.IsHostile(canBeDead: true) ?? false))
            return; // avoid processing effect for non-hostile enemies

        _VoodooDollEffectHappening = true;
        DoVoodooDollEffect(damage, enemy);
        _VoodooDollEffectHappening = false;
    }

    private void DoVoodooDollEffect(float damage, HealthHaver enemy)
    {
        string myGuid = enemy.aiActor.EnemyGuid;
        List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
        foreach (AIActor other in activeEnemies)
        {
            if (!(other?.IsHostileAndNotABoss() ?? false))
                continue; // don't care about inactive or dead enemies
            if (other.EnemyGuid != myGuid)
                continue; // don't care about non-matching enemies
            if (other == enemy.aiActor)
                continue; // don't care about matching ourself

            other.healthHaver?.ApplyDamage(damage, Vector2.zero, "Voodoo Doll", CoreDamageTypes.Magic, DamageCategory.Unstoppable,
                ignoreInvulnerabilityFrames: true, ignoreDamageCaps: false);

            bool flip = Lazy.CoinFlip();
            Vector2 ppos = flip ? other.sprite.WorldTopRight : other.sprite.WorldTopLeft;
            FancyVFX f = FancyVFX.Spawn(_VoodooGhostVFX, ppos, 0f.EulerZ(),
                velocity: Vector2.zero, lifetime: 0.4f, fadeOutTime: 0.4f);
                f.GetComponent<tk2dSprite>().FlipX = flip;
        }
    }

}
