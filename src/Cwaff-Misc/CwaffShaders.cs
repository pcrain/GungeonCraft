namespace CwaffingTheGungy;

public static class CwaffShaders
{
    public static Shader DigitizeShader = null;
    public static Shader UnlitDigitizeShader = null;
    public static Shader GoldShader = null;
    public static Shader CosmicShader = null;
    public static Shader ElectricShader = null;
    public static Shader EmissiveAlphaShader = null;
    public static Shader CorruptShader = null;
    public static Shader ChromaShader = null;
    public static Shader DesatShader = null;
    public static Shader WiggleShader = null;
    public static Shader ScreenWiggleShader = null;
    public static Shader ScreenBasicShader = null;
    public static Shader ShatterShader = null;
    public static Shader GlassShader = null;
    public static Texture2D DigitizeTexture = null;
    public static Texture2D StarsTexture = null;
    public static Texture2D NoiseTexture = null;
    public static Texture2D PowerdownTexture = null;
    public static Texture2D PowerupTexture = null;

    private static string GetShaderBundleNameForPlatform()
    {
        string platform =
            Application.platform == RuntimePlatform.LinuxPlayer ? "linux" :
            Application.platform == RuntimePlatform.OSXPlayer ? "macos" : "windows";
        return $"{C.MOD_INT_NAME}.Resources.cwaffshaders-{platform}";
    }

    public static void Init()
    {
        string shaderBundleName = GetShaderBundleNameForPlatform();
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(shaderBundleName))
        {
            if (stream == null)
            {
                Lazy.DebugWarn($" null shader stream D:");
                return;
            }

            AssetBundle shaderBundle = AssetBundle.LoadFromStream(stream);
            // foreach (string s in shaderBundle.GetAllAssetNames())
            //     ETGModConsole.Log($"  found asset {s}");
            // TestShader = ShaderBundle.LoadAsset<Shader>("assets/sillyshader.shader");
            // TestShader = ShaderBundle.LoadAsset<Shader>("assets/mirageshader.shader");
            NoiseTexture = shaderBundle.LoadAsset<Texture2D>("assets/sf_noise_clouds_01.png");
            ElectricShader = shaderBundle.LoadAsset<Shader>("assets/electroshader.shader");
            DigitizeShader = shaderBundle.LoadAsset<Shader>("assets/digitizeshader.shader");
            DigitizeTexture = shaderBundle.LoadAsset<Texture2D>("assets/bits.png");
            UnlitDigitizeShader = shaderBundle.LoadAsset<Shader>("assets/digitizeshaderunlit.shader");
            GoldShader = shaderBundle.LoadAsset<Shader>("assets/goldshader.shader");
            CosmicShader = shaderBundle.LoadAsset<Shader>("assets/cosmicshader.shader");
            StarsTexture = shaderBundle.LoadAsset<Texture2D>("assets/startexture_cropped.png");
            EmissiveAlphaShader = shaderBundle.LoadAsset<Shader>("assets/emissivealphashader.shader");
            CorruptShader = shaderBundle.LoadAsset<Shader>("assets/corruptshader.shader");
            ChromaShader = shaderBundle.LoadAsset<Shader>("assets/chromashiftshader.shader");
            DesatShader = shaderBundle.LoadAsset<Shader>("assets/desaturateshader.shader");
            PowerdownTexture = shaderBundle.LoadAsset<Texture2D>("assets/powerdown.png");
            PowerupTexture = shaderBundle.LoadAsset<Texture2D>("assets/powerup.png");
            WiggleShader = shaderBundle.LoadAsset<Shader>("assets/wiggleshader.shader");
            ScreenWiggleShader = shaderBundle.LoadAsset<Shader>("assets/screenwiggleshader.shader");
            ScreenBasicShader = shaderBundle.LoadAsset<Shader>("assets/screenbasicshader.shader");
            ShatterShader = shaderBundle.LoadAsset<Shader>("assets/scattershader.shader");
            // ShatterShader = shaderBundle.LoadAsset<Shader>("assets/scattershaderunlit.shader");
            GlassShader = shaderBundle.LoadAsset<Shader>("assets/glassshader.shader");
        }

        CwaffEvents.OnCleanStart += ResetShaders;
        CwaffEvents.OnStatsRecalculated += CheckShaders;
    }

    internal static void CheckShaders(PlayerController player)
    {
        CheckSpiceShaders();
    }

    private static void ResetShaders()
    {
        CheckSpiceShaders(enabled: false);
    }

    private static bool SpiceShadersEnabled(bool? enabled = null)
    {
        if (!(enabled ?? CwaffConfig._Gunfig.Enabled(CwaffConfig._SPICE_SHADERS)))
            return false;
        if (!GameManager.Instance || !GameManager.Instance.PrimaryPlayer)
            return false;
        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
            return false;
        return GameManager.Instance.PrimaryPlayer.spiceCount > 0;
    }

    internal static Material _WiggleMat = null;
    internal static void CheckSpiceShaders(bool? enabled = null)
    {
        if (!SpiceShadersEnabled(enabled))
        {
            if (Pixelator.HasInstance)
                Pixelator.Instance.DeregisterAdditionalRenderPass(_WiggleMat);
            return;
        }
        _WiggleMat ??= new Material(CwaffShaders.ScreenWiggleShader);
        _WiggleMat.SetFloat("_Amplitude", 0.0008f * GameManager.Instance.PrimaryPlayer.spiceCount);
        if (Pixelator.HasInstance)
            Pixelator.Instance.RegisterAdditionalRenderPass(_WiggleMat);
        // GameManager.Instance.PrimaryPlayer.ToggleShadowVisiblity(false);
        // SpriteOutlineManager.ToggleOutlineRenderers(GameManager.Instance.PrimaryPlayer.sprite, false);
    }

    [HarmonyPatch(typeof(SpiceItem), nameof(SpiceItem.DoEffect))]
    private static class SpiceItemDoEffectPatch
    {
        static void Postfix(SpiceItem __instance, PlayerController user)
        {
            CheckShaders(user);
        }
    }

    public static void Digitize<T>(T sprite, float delay = 0.0f) where T : tk2dBaseSprite
    {
        GameObject g = new GameObject();
        tk2dBaseSprite newSprite = null;
        if (typeof(T) == typeof(tk2dSlicedSprite))
        {
            newSprite = g.AddComponent<tk2dSlicedSprite>();
            newSprite.SetSprite(sprite.collection, sprite.spriteId);
            tk2dSlicedSprite slicedSprite = sprite as tk2dSlicedSprite;
            tk2dSlicedSprite newSlicedSprite = newSprite as tk2dSlicedSprite;
            newSlicedSprite.dimensions = slicedSprite.dimensions;
            newSlicedSprite._tileStretchedSprites = slicedSprite._tileStretchedSprites;
        }
        else // assume tk2dSprite
        {
            newSprite = g.AddComponent<tk2dSprite>();
            newSprite.SetSprite(sprite.collection, sprite.spriteId);
        }
        newSprite.FlipX = sprite.FlipX;
        newSprite.PlaceAtRotatedPositionByAnchor(sprite.WorldCenter, Anchor.MiddleCenter);
        newSprite.StartCoroutine(Digitize_CR(newSprite, delay));
        g.Play("femtobyte_digitize_sound");
    }

    public static void MaterializePartial(tk2dBaseSprite sprite)
    {
        sprite.StartCoroutine(Materialize_CR(sprite, 0.0f, partial: true));
        sprite.gameObject.PlayUnique("femtobyte_materialize_sound");
    }

    public static void Materialize(tk2dBaseSprite sprite)
    {
        sprite.StartCoroutine(Materialize_CR(sprite, 0.0f));
        sprite.gameObject.PlayUnique("femtobyte_materialize_sound");
    }

    public static void Materialize(tk2dBaseSprite sprite, float delay)
    {
        sprite.StartCoroutine(Materialize_CR(sprite, delay));
        sprite.gameObject.PlayUnique("femtobyte_materialize_sound");
    }

    public static IEnumerator Digitize_CR(tk2dBaseSprite s, float delay = 0.0f)
    {
        s.renderer.material.shader = CwaffShaders.DigitizeShader;
        s.renderer.material.SetTexture(CwaffVFX._BinaryTexId, CwaffShaders.DigitizeTexture);
        s.renderer.material.SetFloat(CwaffVFX._BinarizeProgressId, 0.0f);
        s.renderer.material.SetFloat(CwaffVFX._ColorizeProgressId, 0.0f);
        s.renderer.material.SetFloat(CwaffVFX._FadeProgressId, 0.0f);
        s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -3.0f);
        SpriteOutlineManager.RemoveOutlineFromSprite(s);

        if (delay > 0)
            yield return new WaitForSeconds(delay);

        float phaseTime = 0.05f;
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._BinarizeProgressId, percentDone);
            yield return null;
        }
        yield return new WaitForSeconds(0.35f);

        phaseTime = 0.2f;
        s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -3.75f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._ColorizeProgressId, percentDone);
            // s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, 2.5f + 2.5f * percentDone);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);

        phaseTime = 0.2f;
        // s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -4.5f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._FadeProgressId, percentDone);
            // s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, 5f + 5f * percentDone);
            yield return null;
        }

        UnityEngine.Object.Destroy(s.gameObject);
    }

    public static IEnumerator Materialize_CR(tk2dBaseSprite s, float delay = 0.0f, bool partial = false)
    {
        Shader oldShader = s.renderer.material.shader;

        s.usesOverrideMaterial = true;
        s.renderer.material.shader = CwaffShaders.DigitizeShader;
        s.renderer.material.SetTexture(CwaffVFX._BinaryTexId, CwaffShaders.DigitizeTexture);
        s.renderer.material.SetFloat(CwaffVFX._BinarizeProgressId, 1.0f);
        s.renderer.material.SetFloat(CwaffVFX._ColorizeProgressId, 1.0f);
        s.renderer.material.SetFloat(CwaffVFX._FadeProgressId, 1.0f);

        float phaseTime = 0.1f;
        s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -4.5f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._FadeProgressId, 1f - percentDone);
            // s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, 5f + 5f * percentDone);
            yield return null;
        }
        if (partial)
            yield break;
        yield return new WaitForSeconds(0.4f);

        phaseTime = 0.15f;
        s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -3.75f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._ColorizeProgressId, 1f - percentDone);
            // s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, 2.5f + 2.5f * percentDone);
            yield return null;
        }

        phaseTime = 0.1f;
        s.renderer.material.SetFloat(CwaffVFX._ScrollSpeedId, -3.0f);
        for (float elapsed = 0f; elapsed < phaseTime; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / phaseTime;
            s.renderer.material.SetFloat(CwaffVFX._BinarizeProgressId, 1f - percentDone);
            yield return null;
        }

        s.renderer.material.shader = oldShader;
    }

    public static void Cosmify(tk2dBaseSprite s)
    {
        s.usesOverrideMaterial = true;
        s.renderer.material.shader = CwaffShaders.CosmicShader;
        s.renderer.material.SetTexture("_StarTex", CwaffShaders.StarsTexture);
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
    private class RandomCosmicKinPatch
    {
        private const float _COSMIFY_CHANCE = 0.0011f;

        static void Postfix(AIActor __instance)
        {
            if (UnityEngine.Random.value > _COSMIFY_CHANCE)
                return;
            if (__instance.EnemyGuid != Enemies.BulletKin)
                return;
            if (__instance.sprite is not tk2dSprite sprite)
                return;
            Cosmify(sprite);
            for (int i = 0; i < sprite.gameObject.transform.childCount; ++i)
            {
                Transform child = sprite.gameObject.transform.GetChild(i);
                if (child.GetComponent<tk2dSprite>() is tk2dSprite csprite)
                    Cosmify(csprite);
            }
        }
    }
}
