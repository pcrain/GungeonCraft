namespace CwaffingTheGungy;

public class GunPowderer : PlayerItem
{
    public static string ItemName         = "Gun Powderer";
    public static string SpritePath       = "gun_powderer_icon";
    public static string ShortDescription = "Ground Up Guns";
    public static string LongDescription  = "Converts the nearest dropped gun to 1-5 spread ammo boxes, depending on its remaining ammo percentage.\n\nThe art of gun powdering is relatively modern, despite the required implements all being rather primitive. This is perhaps because ammunition was in much higher supply in the Gungeon's early days, and powdering was largely unnecessary. Nowadays, resourceful Gungeoneers understand the value of smashing up their old and unused guns for ammo, and one can only hope they will eventually understand they wouldn't need so much ammo in the first place if they didn't miss 90% of their shots.";

    private const float _MAX_DIST = 5f;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<GunPowderer>(ItemName, SpritePath, ShortDescription, LongDescription);
        item.quality      = ItemQuality.A;
        item.consumable   = false;
        item.CanBeDropped = true;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 2f);
    }

    public override void DoEffect(PlayerController user)
    {
        Gun nearestGun    = null;
        float nearestDist = _MAX_DIST;
        foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.IsPickupObject)
                continue;
            if (debris.GetComponentInChildren<PickupObject>()?.GetComponent<Gun>() is not Gun gun)
                continue;

            float gunDist = (gun.sprite.WorldCenter - user.sprite.WorldCenter).magnitude;
            if (gunDist >= nearestDist)
                continue;

            nearestGun  = gun;
            nearestDist = gunDist;
        }
        if (nearestGun)
            ConvertGunToAmmo(nearestGun);
    }

    private void ConvertGunToAmmo(Gun gun)
    {
        float ammoPercent    = (float)gun.CurrentAmmo / (float)gun.AdjustedMaxAmmo;
        int ammoBoxesToSpawn = Mathf.Max(1, Mathf.CeilToInt(ammoPercent * 5f));

        Vector2 spawnCenter = gun.sprite.WorldCenter;
        Lazy.DoSmokeAt(spawnCenter);
        UnityEngine.Object.Destroy(gun.gameObject);

        for (int i = 1; i <= ammoBoxesToSpawn; ++i)
        {
            float angle = (float)i * (360f / ammoBoxesToSpawn);
            StartCoroutine(SpawnSomeAmmo(spawnCenter + angle.ToVector(1.5f), 0.25f * i));
        }
    }

    private IEnumerator SpawnSomeAmmo(Vector2 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        LootEngine.SpawnItem(ItemHelper.Get(Items.PartialAmmo).gameObject, pos, spawnDirection: Vector2.zero, force: 0f, doDefaultItemPoof: true);
        yield break;
    }
}
