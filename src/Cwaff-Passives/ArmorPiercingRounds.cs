namespace CwaffingTheGungy;

/* NOTE: pierces armor for the following vanilla enemies (maybe some bosses? who knows):
    - Lead Maiden     - pierces armor and prevents reflection (invulnerability frames)
    - Minelet         - pierces armor and prevents reflection (TransformBehavior invulnerability)
    - Gat             - pierces armor and prevents reflection (TransformBehavior invulnerability)
    - Bloodbulon      - pierces minimum health (HealthHaver minimumHealth)
    - Shambling Round - pierces minimum health (HealthHaver minimumHealth)
    - Lead Cube       - pierces room clear invulnerability (HealthHaver PreventAllDamage)
    - Flesh Cube      - pierces room clear invulnerability (HealthHaver PreventAllDamage)
    - Gunreaper       - pierces room clear invulnerability (HealthHaver PreventAllDamage)
*/

public class ArmorPiercingRounds : PassiveItem
{
    public static string ItemName         = "Armor Piercing Rounds";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static int    ID;

    internal static GameObject _PierceVFX = null;

    public static void Init()
    {
        PickupObject item = Lazy.SetupPassive<ArmorPiercingRounds>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        _PierceVFX = VFX.Create("armor_pierce_effect", fps: 40, loops: false);

        ID = item.PickupObjectId;
    }

    // NOTE: called by patch in CwaffPatches
    private static bool PossiblyDisableArmor(Projectile p, SpeculativeRigidbody body)
    {
        if (!(p && p.Owner is PlayerController player && player.HasPassiveItem(ArmorPiercingRounds.ID)))
            return false;

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
            playPierceSound |= hh.minimumHealth > 0; // Bloodbulon and Shambling Round
            hh.minimumHealth = 0;
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
