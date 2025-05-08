namespace CwaffingTheGungy;

/* NOTE: pierces armor for the following vanilla enemies (maybe some others? who knows):
    - Lead Maiden     - pierces armor and prevents reflection (invulnerability frames)
    - Minelet         - pierces armor and prevents reflection (TransformBehavior invulnerability)
    - Gat             - pierces armor and prevents reflection (TransformBehavior invulnerability)
    - Bloodbulon      - pierces minimum health (HealthHaver minimumHealth)
    - Shambling Round - pierces minimum health (HealthHaver minimumHealth)
    - Lead Cube       - pierces room clear invulnerability (HealthHaver PreventAllDamage)
    - Flesh Cube      - pierces room clear invulnerability (HealthHaver PreventAllDamage)
    - Gunreaper       - pierces room clear invulnerability (HealthHaver PreventAllDamage)
*/

public class ArmorPiercingRounds : CwaffPassive
{
    public static string ItemName         = "Armor Piercing Rounds";
    public static string ShortDescription = "Bored to Death";
    public static string LongDescription  = "Projectiles ignore the invulnerable phases of most enemies. Does not break boss DPS caps.";
    public static string Lore             = "A handful of the Gungeon's denizens have been gifted with various means of protecting themselves from the thousands of bullets, lasers, foam darts, and T-shirts fired their way on a daily basis. Bullet researchers have known for years that most of these defenses are thwarted by a heavy yet crude application of torque to projectiles, but lobbying from Big Arma has largely suppressed this knowledge from the general public in order to sell beefier and more impressive guns.";

    internal static GameObject _PierceVFX = null;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ArmorPiercingRounds>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        item.AddToShop(ModdedShopType.Doug);

        _PierceVFX = VFX.Create("armor_pierce_effect", fps: 40, loops: false);
    }

    // HACK: a lot of this permanently changes attributes of the enemy and will affect enemies even after ArmorPiercingRounds are disabled.
    //       undecided whether this is a bug or a feature...
    // NOTE: called by patch in CwaffPatches, but used by K.A.L.I. as well, possibly relocate
    internal static bool PossiblyDisableArmor(Projectile p, SpeculativeRigidbody body)
    {
        if (!p || p.Owner is not PlayerController player)
            return false;
        if (player.HasPassive<ArmorPiercingRounds>())
            {} // pierce
        else if (p.GetComponent<KaliProjectile>() is KaliProjectile kp && kp.Mastered)
            {} // pierce
        else
            return false; // don't pierce

        bool playPierceSound = false;
        if (body.ReflectProjectiles || body.ReflectBeams)  // Lead Maiden
        {
            body.ReflectProjectiles = false;
            body.ReflectBeams       = false;
            playPierceSound         = true;
        }
        if (body.GetComponent<HealthHaver>() is HealthHaver hh)
        {
            playPierceSound |= hh.PreventAllDamage; // Lead Cube, Flesh Cube, and Gunreaper
            hh.PreventAllDamage = false;
            if (!hh.IsBoss && !hh.IsSubboss) // prevent issues with modded bosses
            {
                playPierceSound |= hh.minimumHealth > 0; // Bloodbulon and Shambling Round
                hh.minimumHealth = 0;
            }
        }
        if (body.GetComponent<BehaviorSpeculator>() is BehaviorSpeculator bs)
        {
            foreach (AttackBehaviorBase ab in bs.AttackBehaviors.EmptyIfNull())
            {
                if (ab is TransformBehavior tb)
                {
                    playPierceSound |= tb.Invulnerable;
                    tb.Invulnerable = false;
                    continue;
                }
                if (ab is not AttackBehaviorGroup abg)
                    continue;
                foreach (AttackBehaviorGroup.AttackGroupItem agi in abg.AttackBehaviors.EmptyIfNull())
                {
                    if (agi.Behavior is not TransformBehavior tb2)
                        continue;
                    playPierceSound |= tb2.Invulnerable;  // Minelet and Gat
                    tb2.Invulnerable = false;
                }
            }
        }
        if (playPierceSound) // if we actually did some piercing, play a nice sound effect
        {
            body.gameObject.Play("armor_pierced_sound");
            SpawnManager.SpawnVFX(_PierceVFX, p.SafeCenter, Quaternion.identity);
        }
        return true;
    }
}
