namespace CwaffingTheGungy;

public static class CwaffShaders
{
    public static Shader BinarizeShader = null;
    public static Texture2D BinaryTexture = null;
    public static Texture2D TestGradientTexture = null;
    public static AssetBundle ShaderBundle = null;

    private static string GetShadersForPlatform()
    {
        string platform =
            Application.platform == RuntimePlatform.LinuxPlayer ? "linux" :
            Application.platform == RuntimePlatform.OSXPlayer ? "macos" : "windows";
        return $"{C.MOD_INT_NAME}.Resources.cwaffshaders-{platform}";
    }

    public static void Init()
    {
        string shaderBundle = GetShadersForPlatform();
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(shaderBundle))
        {
            if (stream == null)
            {
                Lazy.DebugWarn($" null shader stream D:");
                return;
            }

            ShaderBundle = AssetBundle.LoadFromStream(stream);
            // foreach (string s in ShaderBundle.GetAllAssetNames())
            //     ETGModConsole.Log($"  found asset {s}");
            // TestShader = ShaderBundle.LoadAsset<Shader>("assets/sillyshader.shader");
            // TestShader = ShaderBundle.LoadAsset<Shader>("assets/electroshader.shader");
            // TestShader = ShaderBundle.LoadAsset<Shader>("assets/mirageshader.shader");
            // TestShaderTexture = ShaderBundle.LoadAsset<Texture2D>("assets/sf_noise_clouds_01.png");
            BinarizeShader = ShaderBundle.LoadAsset<Shader>("assets/digitizeshader.shader");
            BinaryTexture = ShaderBundle.LoadAsset<Texture2D>("assets/bits.png");
        }
    }

    public static void Digitize(tk2dBaseSprite sprite, float time = 3.0f)
    {
        tk2dSprite newSprite = new GameObject().AddComponent<tk2dSprite>();
        newSprite.SetSprite(sprite.collection, sprite.spriteId);
        newSprite.FlipX = sprite.FlipX;
        newSprite.PlaceAtPositionByAnchor(sprite.transform.position, sprite.FlipX ? Anchor.LowerRight : Anchor.LowerLeft);
        newSprite.StartCoroutine(Digitize_CR(newSprite, time));
    }

    public static IEnumerator Digitize_CR(tk2dSprite s, float time)
    {
        s.renderer.material.shader = CwaffShaders.BinarizeShader;
        s.renderer.material.SetTexture("_BinaryTex", CwaffShaders.BinaryTexture);
        s.renderer.material.SetFloat("_BinarizeProgress", 0.0f);
        s.renderer.material.SetFloat("_ColorizeProgress", 0.0f);
        s.renderer.material.SetFloat("_FadeProgress", 0.0f);
        s.renderer.material.SetFloat("_ScrollSpeed", -3.0f);
        SpriteOutlineManager.RemoveOutlineFromSprite(s);

        yield return new WaitForSeconds(1.0f);

        float phaseTime = 0.25f;
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat("_BinarizeProgress", percentDone);
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);

        phaseTime = 0.25f;
        s.renderer.material.SetFloat("_ScrollSpeed", -3.75f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat("_ColorizeProgress", percentDone);
            // s.renderer.material.SetFloat("_ScrollSpeed", 2.5f + 2.5f * percentDone);
            yield return null;
        }
        yield return new WaitForSeconds(0.75f);

        phaseTime = 0.25f;
        s.renderer.material.SetFloat("_ScrollSpeed", -4.5f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat("_FadeProgress", percentDone);
            // s.renderer.material.SetFloat("_ScrollSpeed", 5f + 5f * percentDone);
            yield return null;
        }

        UnityEngine.Object.Destroy(s.gameObject);
    }
}
