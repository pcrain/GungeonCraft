namespace CwaffingTheGungy;

public class Telefragger : CwaffGun
{
    public static string ItemName          = "Telefragger";
    public static string ShortDescription  = "Voip";
    public static string LongDescription   = "TBD";
    public static string Lore              = "TBD";

    private const float _HOVER_TIME        = 1.25f;
    private const float _FLICKER_PORTION   = 0.5f;
    private const float _FLICKER_FREQ      = 0.1f;

    private static VFXPool _TeleportVFX    = null;
    private static GameObject _FloorVFX    = null;

    // lots of logic borrowed from RatBootsItem.cs
    private tk2dSprite m_extantFloor       = null;
    private bool m_frameWasPartialPit      = false;
    private bool m_wasAboutToFallLastFrame = false;
    private float m_elapsedAboutToFall     = 0.0f;
    private int m_lastFrameAboutToFall     = 0;
    private float _invulnTime              = 0.0f;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Telefragger>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 500, shootFps: 14, reloadFps: 4,
                muzzleFrom: Items.Mailbox /*fireAudio: "paintball_shoot_sound", reloadAudio: "paintball_reload_sound"*/);
            gun.gunHandedness = GunHandedness.HiddenOneHanded;

        Projectile projectile = gun.InitProjectile(GunData.New(baseProjectile: Items.DemonHead.Projectile(), clipSize: -1, cooldown: 0.18f, shootStyle: ShootStyle.Beam,
            damage: 12.0f, speed: 40f, range: 18f, force: 12f, scale: 0.5625f, ammoCost: 5)).Attach<TelefragJuice>();

        //HACK: this is necessary when copying beams to avoid weird beam offsets from walls...why???
        projectile.gameObject.transform.localScale = Vector3.one;
        projectile.gameObject.transform.localPosition = Vector3.zero;

        BasicBeamController beamComp = projectile.SetupBeamSprites(spriteName: "telefragger_beam", fps: 20, dims: new Vector2(15, 15), impactDims: new Vector2(7, 7));
            // fix some animation glitches (don't blindly copy paste; need to be set on a case by case basis depending on your beam's needs)
            beamComp.muzzleAnimation = beamComp.beamStartAnimation;  //use start animation for muzzle animation, make start animation null
            beamComp.beamStartAnimation = null; //TODO: fix start animation / add proper charge animation

        _TeleportVFX = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        _FloorVFX    = ItemHelper.Get(Items.RatBoots).gameObject.GetComponent<RatBootsItem>().FloorVFX;
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner && m_extantFloor)
            m_extantFloor.renderer.sharedMaterial.SetVector("_PlayerPos", this.PlayerOwner.CenterPosition.ToVector4());
        if (Time.timeScale <= 0f)
        {
            m_lastFrameAboutToFall = Time.frameCount;
            return;
        }
        if (!m_wasAboutToFallLastFrame && m_extantFloor)
        {
            SpawnManager.Despawn(m_extantFloor.gameObject);
            m_extantFloor = null;
        }
        m_wasAboutToFallLastFrame = false;
        if (this._invulnTime > 0)
        {
            this._invulnTime -= BraveTime.DeltaTime;
            if (this._invulnTime <= 0)
                DisablePitSafety(this.PlayerOwner);
        }
    }

    private void LateUpdate()
    {
        if (m_extantFloor)
            m_extantFloor.UpdateZDepth();
    }

    internal void TeleportPlayerToPosition(PlayerController player, Vector2 target)
    {
        Vector2 startPosition = player.transform.position;
        player.StartCoroutine(DoPlayerTeleport(player, startPosition, target, 0.125f));
    }

    private IEnumerator DoPlayerTeleport(PlayerController player, Vector2 start, Vector2 end, float duration)
    {
        GameManager.Instance.MainCameraController.SetManualControl(true, true);
        Vector2 startCamPos = GameManager.Instance.MainCameraController.GetIdealCameraPosition();
        Vector2 endCamPos   = end;
        Vector2 deltaPos    = (endCamPos - startCamPos);

        player.transform.position = start;
        player.specRigidbody.Reinitialize();
        player.sprite.renderer.enabled = false;
        player.specRigidbody.enabled = false;

        player.gameObject.Play("Play_OBJ_chestwarp_use_01");
        _TeleportVFX.SpawnAtPosition(start.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
        player.DoEasyBlank(end, EasyBlankType.MINI);

        for (float timer = 0f; timer < duration; timer += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - timer / duration;
            GameManager.Instance.MainCameraController.OverridePosition = (startCamPos + (1f - percentLeft * percentLeft) * deltaPos);
            yield return null;
        }

        player.gameObject.Play("Play_OBJ_chestwarp_use_01");
        yield return null;

        _TeleportVFX.SpawnAtPosition(end.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
        EnablePitSafety(player);

        GameManager.Instance.MainCameraController.SetManualControl(false, true);
        player.transform.position = end;
        player.sprite.renderer.enabled = true;
        player.specRigidbody.enabled = true;
        player.specRigidbody.Reinitialize();
        player.specRigidbody.CorrectForWalls(andRigidBodies: true);

        yield break;
    }

    private void EnablePitSafety(PlayerController player)
    {
        if (!player)
            return;
        player.OnAboutToFall += this.HandleAboutToFall;
        this._invulnTime = _HOVER_TIME;

        // enabled shaders
        Material[] array = player.SetOverrideShader(ShaderCache.Acquire("Brave/Internal/RainbowChestShader"));
        for (int i = 0; i < array.Length; i++)
            if (array[i] != null)
                array[i].SetFloat("_AllColorsToggle", 1f);

        player.healthHaver.IsVulnerable = false;
        this.gun.CanBeDropped = false;
        this.gun.CanBeSold = false;
        player.inventory.GunLocked.SetOverride(ItemName, true);
    }

    private void DisablePitSafety(PlayerController player)
    {
        if (!player)
            return;
        player.OnAboutToFall -= this.HandleAboutToFall;
        this._invulnTime = 0.0f;
        player.ClearOverrideShader();
        player.healthHaver.IsVulnerable = true;
        if (m_extantFloor)
        {
            SpawnManager.Despawn(m_extantFloor.gameObject);
            m_extantFloor = null;
        }
        this.gun.CanBeDropped = true;
        this.gun.CanBeSold = true;
        player.inventory.GunLocked.RemoveOverride(ItemName);
    }

    private bool HandleAboutToFall(bool partialPit)
    {
        if (!this.PlayerOwner || this.PlayerOwner.IsFlying)
            return false;
        m_frameWasPartialPit = partialPit;
        m_wasAboutToFallLastFrame = true;
        if (Time.frameCount <= m_lastFrameAboutToFall)
            m_lastFrameAboutToFall = Time.frameCount - 1;
        if (Time.frameCount != m_lastFrameAboutToFall + 1)
            m_elapsedAboutToFall = 0f;
        if (partialPit)
            m_elapsedAboutToFall = 0f;
        m_lastFrameAboutToFall = Time.frameCount;
        m_elapsedAboutToFall += BraveTime.DeltaTime;
        if (this._invulnTime <= 0)
        {
            DisablePitSafety(this.PlayerOwner);
            return true;
        }
        if (!m_extantFloor)
        {
            GameObject gameObject = SpawnManager.SpawnVFX(_FloorVFX);
            gameObject.transform.parent = this.PlayerOwner.transform;
            tk2dSprite sprite = gameObject.GetComponent<tk2dSprite>();
            sprite.PlaceAtPositionByAnchor(this.PlayerOwner.SpriteBottomCenter, tk2dBaseSprite.Anchor.MiddleCenter);
            sprite.IsPerpendicular = false;
            sprite.HeightOffGround = -2.25f;
            sprite.UpdateZDepth();
            m_extantFloor = sprite;
        }
        if (m_elapsedAboutToFall > _HOVER_TIME - _FLICKER_PORTION)
            m_extantFloor.renderer.enabled = (Mathf.PingPong(m_elapsedAboutToFall - (_HOVER_TIME - _FLICKER_PORTION), _FLICKER_FREQ * 2f) < _FLICKER_FREQ);
        else
            m_extantFloor.renderer.enabled = true;
        return false;
    }
}

public class TelefragJuice : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Telefragger _telefragger;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;
        if (this._owner.CurrentGun is not Gun gun)
            return;
        if (gun.GetComponent<Telefragger>() is not Telefragger telefragger)
            return;
        this._telefragger = telefragger;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile proj, SpeculativeRigidbody other, bool willKill)
    {
        if (willKill && this._owner && this._telefragger && other.gameObject.GetComponent<AIActor>() is AIActor enemy)
            this._telefragger.TeleportPlayerToPosition(this._owner, enemy.CenterPosition);
    }
}
