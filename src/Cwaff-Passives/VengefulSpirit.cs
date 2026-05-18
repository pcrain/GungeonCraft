namespace CwaffingTheGungy;

public class VengefulSpirit : CwaffPassive
{
    public static string ItemName         = "Vengeful Spirit";
    public static string ShortDescription = "D:<";
    public static string LongDescription  = "Deal double damage to any enemy that has injured you.";
    public static string Lore             = "TBD";

    public List<string> vengeantNames = new();

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<VengefulSpirit>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.A;
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        player.PostProcessProjectile += PostProcessProjectile;
        player.healthHaver.Ext().OnDamagedContext += this.OnDamagedContext;
    }

    private void OnDamagedContext(HealthHaver hh, float damage, string source, float resultValue, float maxValue, CoreDamageTypes damageTypes,
      DamageCategory damageCategory, Vector2 damageDirection, bool ignoreInvulnerabilityFrames, bool ignoreDamageCaps)
    {
        this.vengeantNames.AddUnique(source);
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        if (!player)
          return;
        player.PostProcessProjectile -= PostProcessProjectile;
        player.healthHaver.Ext().OnDamagedContext -= this.OnDamagedContext;
    }

    private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
    {
        if (!proj.gameObject.GetComponent<VengefulDamage>())
          proj.gameObject.AddComponent<VengefulDamage>().spirit = this;
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(this.vengeantNames.Count);
        foreach (string enemy in this.vengeantNames)
          data.Add(enemy);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        int i = 0;
        int count = (int)data[i++];
        for (int n = 0; n < count; ++n)
          this.vengeantNames.Add((string)data[i++]);
    }

    private class VengefulDamage : DamageAdjuster
    {
        public VengefulSpirit spirit;

        protected override float AdjustDamage(float currentDamage, Projectile proj, AIActor enemy)
        {
          if (!enemy || !spirit || string.IsNullOrEmpty(enemy.EnemyGuid) || !spirit.vengeantNames.Contains(enemy.GetActorName()))
            return currentDamage;
          return currentDamage * 2;
        }
    }
}
