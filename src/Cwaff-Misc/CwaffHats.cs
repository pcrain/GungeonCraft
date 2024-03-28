namespace CwaffingTheGungy;

public static class CwaffHats
{
    public static void Init()
    {
      Alexandria.cAPI.HatUtility.NecessarySetup();

      EasyHat(name: "debug_hat",      offset: null);
      EasyHat(name: "toad_hat",       offset: new Vector2(0f/16f, -3f/16f));
      EasyHat(name: "bunny_hat",      offset: new Vector2(1f/16f, -3f/16f), flipXOffset: -2f/16f);
      EasyHat(name: "jester_hat",     offset: new Vector2(0f/16f, -3f/16f));
      // EasyHat(name: "bunny_ears_hat", offset: new Vector2(0f/16f, -5f/16f));
      // EasyHat(name: "saiyan_hat",     offset: new Vector2(0f/16f, -3f/16f));
      // EasyHat(name: "shades_glasses", offset: new Vector2(0f/16f, -3f/16f));
      // EasyHat(name: "witch_hat",      offset: new Vector2(0f/16f, -3f/16f));

      CwaffEvents.OnRunStart += (p1, p2, gamemode) => {
          ETGModConsole.Log($"Hatting it up!");
          p1.AddComponent<HatController>();
      };
    }

    private static void EasyHat(string name, Vector2? offset = null, float flipXOffset = 0f)
    {
      GameObject hatObj = UnityEngine.Object.Instantiate(new GameObject());
      Hat hat = hatObj.AddComponent<Hat>();
      hat.hatName = name;
      hat.hatOffset = offset ?? Vector2.zero;
      hat.flipXOffset = flipXOffset;
      HatUtility.SetupHatSprites(spritePaths: ResMap.Get($"{name}_south"), hatObj: hatObj, fps: 1);
      HatUtility.AddHatToDatabase(hatObj);
    }
}
