namespace CwaffingTheGungy;

public class Telefragger : CwaffGun
{
    public static string ItemName          = "Telefragger";
    public static string ShortDescription  = "Voip";
    public static string LongDescription   = "Fires a beam that teleports the player into an enemy on kill. Teleporting creates a mini blank effect at the destination and briefly grants invulernability and pit immunity. Will not teleport the player off screen in co-op.";
    public static string Lore              = "Many years ago, a Gungeoneer named Jackson Doomquake gained notoriety for dropping firearms on the Gungeon's teleporters as bait for unsuspecting Bullet Kin. While teleporting into Bullet Kin all day proved to be an effective and ammo-efficient method of execution, it also provoked the ire of Bello, who allegedly lost several customers due to the clamor caused by his shop's teleporter constantly activating. The teleporter system was subsequently patched to prevent usage while any Gundead are around, but while Doomquake's exploits are no longer replicable, the Telefragger ensures the spirit of his antics live on.";

    private const float _MERCY_TIME        = 1.5f;
    private const float _FLICKER_PORTION   = 0.5f;
    private const float _FLICKER_FREQ      = 0.1f;
    private const float _GLOW              = 50f;

    private static VFXPool _TeleportVFX    = null;
    private static GameObject _FloorVFX    = null;

    // lots of logic borrowed from RatBootsItem.cs
    private tk2dSprite m_extantFloor       = null;
    private bool m_frameWasPartialPit      = false;
    private bool m_wasAboutToFallLastFrame = false;
    private int m_lastFrameAboutToFall     = 0;
    private float _invulnTime              = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<Telefragger>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.BEAM, reloadTime: 0.9f, ammo: 600, shootFps: 30,
            reloadFps: 4, handedness: GunHandedness.HiddenOneHanded, loopFireAt: 12)
          .SetFireAudio("telefragger_amp_up_sound", 0, 3, 6, 9)
          .InitProjectile(GunData.New(baseProjectile: Items.DemonHead.Projectile(), clipSize: -1, cooldown: 0.18f, shootStyle: ShootStyle.Beam,
            ammoType: GameUIAmmoType.AmmoType.BEAM, ammoCost: 5, angleVariance: 0f, beamSprite: "telefragger_beam", beamFps: 17,
            beamImpactFps: 14, beamLoopCharge: false, beamChargeDelay: 0.4f, beamEmission: 2f))
          .Attach<TelefragJuice>();

        _TeleportVFX = VFX.CreatePoolFromVFXGameObject(Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
        _FloorVFX    = ItemHelper.Get(Items.RatBoots).gameObject.GetComponent<RatBootsItem>().FloorVFX;
    }

    private void Start()
    {
        this.gun.sprite.SetGlowiness(10f, glowColor: new Color(0.0f, 0.625f, 0.664f, 1f));
        gun.sprite.renderer.material.SetFloat("_EmissiveColorPower", 10f); // extra spicy colors
    }

    private bool SynchronizeSpriteWithBeam()
    {
        if (this.gun.m_moduleData == null || !this.gun.m_moduleData.TryGetValue(this.gun.DefaultModule, out ModuleShootData beamMod))
            return false;
        if (this.gun.m_activeBeams == null || !this.gun.m_activeBeams.Contains(beamMod) || (beamMod.beam is not BasicBeamController beam))
            return false;

        float chargeFraction = 0f;
        if (beam.State == BeamState.Firing)
        {
            chargeFraction = 1f;
            Lazy.PlaySoundUntilDeathOrTimeout("telefragger_fire_loop_sound", base.gameObject, 0.1f);
        }
        else if (beam.State == BeamState.Charging)
            chargeFraction = beam.m_chargeTimer / beam.chargeDelay;
        Material m = gun.sprite.renderer.material;
        m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
        m.SetFloat("_EmissivePower", _GLOW * chargeFraction);
        return true;
    }

    public override void Update()
    {
        base.Update();
        if (!SynchronizeSpriteWithBeam())
            gun.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");

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

    // TODO: something in this method causes a _StencilVal warning in the debug log
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

        player.gameObject.Play("Play_OBJ_teleport_depart_01");
        _TeleportVFX.SpawnAtPosition(start.ToVector3ZisY(-1f), 0, null, null, null, -0.05f);
        player.DoEasyBlank(end, EasyBlankType.MINI);

        for (float timer = 0f; timer < duration; timer += BraveTime.DeltaTime)
        {
            float percentDone = timer / duration;
            GameManager.Instance.MainCameraController.OverridePosition = (startCamPos + (percentDone * percentDone) * deltaPos);
            yield return null;
        }

        yield return null;

        player.gameObject.Play("Play_OBJ_teleport_depart_01");
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
        this._invulnTime = _MERCY_TIME;

        // enable shaders
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
        m_lastFrameAboutToFall = Time.frameCount;
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
        if (this._invulnTime < _FLICKER_PORTION)
            m_extantFloor.renderer.enabled = (Mathf.PingPong(_FLICKER_PORTION - this._invulnTime, _FLICKER_FREQ * 2f) < _FLICKER_FREQ);
        else
            m_extantFloor.renderer.enabled = true;
        return false;
    }
}

public class TelefragJuice : MonoBehaviour
{
    private Projectile _projectile;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.OnHitEnemy += this.OnHitEnemy;
    }

    private void OnHitEnemy(Projectile proj, SpeculativeRigidbody other, bool willKill)
    {
        if (!willKill)
            return;
        if (this._projectile.Owner is not PlayerController owner)
            return;
        if (other.gameObject.GetComponent<AIActor>() is not AIActor enemy)
            return;
        if (enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
            return; // don't teleport to bosses since they tend to have cutscenes and we don't want to interfere with those
        Vector2 targetPos = enemy.CenterPosition;
        if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
        {
            PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(owner);
            if (otherPlayer && !otherPlayer.IsGhost && !GameManager.Instance.MainCameraController.PointIsVisible(targetPos))
            {
                owner.DoEasyBlank(targetPos, EasyBlankType.MINI);
                return; // don't teleport around in co-op if we're offscreen
            }
        }
        owner.specRigidbody.RegisterTemporaryCollisionException(other, 2.0f); // could possibly be a permanent collision exception
        if (owner.CurrentGun is Gun gun && gun.gameObject.GetComponent<Telefragger>() is Telefragger telefragger)
            telefragger.TeleportPlayerToPosition(owner, targetPos);
    }
}
