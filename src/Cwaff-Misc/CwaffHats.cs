namespace CwaffingTheGungy;

public static class CwaffHats
{
    public static void Init()
    {
      Alexandria.cAPI.HatUtility.NecessarySetup();

      GameObject toadHatObj = UnityEngine.Object.Instantiate(new GameObject())/*.RegisterPrefab()*/;
      Hat toadHatController = toadHatObj.AddComponent<Hat>();
      toadHatController.hatName = "toad_hat";
      toadHatController.hatDepthType = Hat.HatDepthType.BehindWhenFacingBack;

      toadHatController.hatOffset = new Vector2(0f, -3f/16f);
      HatUtility.SetupHatSprites(ResMap.Get("toad_hat_south"), toadHatObj, 1, new Vector2(14f, 11f));

      // toadHatController.hatOffset = Vector2.zero;
      // HatUtility.SetupHatSprites(ResMap.Get("cool_hat_south"), toadHatObj, 1, new Vector2(14f, 11f));

      HatUtility.AddHatToDatabase(toadHatObj);

      CwaffEvents.OnRunStart += (p1, p2, gamemode) => {
          ETGModConsole.Log($"Hatting it up!");
          p1.AddComponent<HatController>();
      };
    }
}
