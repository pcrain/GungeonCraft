namespace CwaffingTheGungy;

using static EasyLight.State;

public class EasyLight : MonoBehaviour
{
  public AdditionalBraveLight _light;
  public GameObject _parentObj;
  public float _maxLifetime;
  public float _maxBrightness;
  public float _fadeInTime;
  public float _fadeOutTime;
  public float _fadeOutStartTime;
  public bool _destroyWithParent;
  public bool _usesLifeTime;
  public bool _rotateWithParent;
  public bool _turnOnImmediately;

  private float _lifetime;
  private float _currentFadeTime;
  private State _state;
  private Gun _gun;
  private Projectile _proj;

  internal enum State
  {
    HIDDEN,
    FADEIN,
    VISIBLE,
    FADEOUT,
  }

  internal static EasyLight Create(Transform parent = null, Vector2? pos = null, Color? color = null, float maxLifeTime = 1f, float brightness = 10f, float radius = 20f,
    float fadeInTime = 0f, float fadeOutTime = 0f, bool destroyWithParent = true, bool useCone = false, float coneWidth = 30f, float coneDirection = 0f,
    bool rotateWithParent = true, bool turnOnImmediately = true)
  {
    GameObject lightObj = new GameObject("easylight");
    EasyLight e = lightObj.AddComponent<EasyLight>();

    if (pos is Vector2 posValue)
      lightObj.transform.position = posValue;
    if (parent)
    {
      e._parentObj = parent.gameObject;
      if (pos == null)
        lightObj.transform.position = e._parentObj.transform.position;
      lightObj.transform.parent = e._parentObj.transform;
    }

    e._light = lightObj.AddComponent<AdditionalBraveLight>();
    e._light.LightColor = color ?? Color.white;
    e._light.LightRadius = radius;
    if (useCone)
    {
      e._light.UsesCone = true;
      e._light.LightAngle = coneWidth; // misnomer, width of cone
      e._light.LightOrient = coneDirection;
      e._rotateWithParent = rotateWithParent;
    }
    e._light.Initialize();

    e._destroyWithParent = destroyWithParent;
    e._usesLifeTime = maxLifeTime > 0f;
    e._maxLifetime = Mathf.Max(maxLifeTime, 0f);
    e._fadeInTime = fadeInTime;
    e._fadeOutTime = Mathf.Max(fadeOutTime, 0f);
    e._fadeOutStartTime = maxLifeTime - e._fadeOutTime;
    e._maxBrightness = brightness;
    e._turnOnImmediately = turnOnImmediately;

    return e;
  }

  private void Start()
  {
    this._currentFadeTime = 0f;
    if (!this._turnOnImmediately)
    {
      this._light.LightIntensity = 0f;
      this._state = State.HIDDEN;
    }
    else if (this._fadeInTime > 0)
    {
      this._light.LightIntensity = 0f;
      this._state = State.FADEIN;
    }
    else
    {
      this._light.LightIntensity = this._maxBrightness;
      this._state = State.VISIBLE;
    }
    if (this._parentObj)
    {
      if (this._parentObj.GetComponent<Projectile>() is Projectile proj)
        this._proj = proj;
      else if (this._parentObj.GetComponentInChildren<Gun>() is Gun gun)
        this._gun = gun;
      else if (this._parentObj.transform.parent is Transform grandparent && grandparent.gameObject.GetComponentInChildren<Gun>() is Gun gun2)
        this._gun = gun2;
    }
  }

  public void TurnOn()
  {
    if (this._state == VISIBLE || this._state == FADEIN)
      return;
    if (this._fadeInTime > 0f)
    {
      this._currentFadeTime = 0f;
      this._light.LightIntensity = 0f;
      this._state = FADEIN;
      return;
    }
    this._light.LightIntensity = this._maxBrightness;
    this._state = VISIBLE;
  }

  public void TurnOff(bool immediate = false)
  {
    if (this._state == HIDDEN || this._state == FADEOUT)
      return;
    if (!immediate && this._fadeOutTime > 0f)
    {
      this._currentFadeTime = 0f;
      this._light.LightIntensity = this._maxBrightness;
      this._state = FADEOUT;
      return;
    }
    this._light.LightIntensity = 0f;
    this._state = HIDDEN;
  }

  private void OnDisable()
  {
    TurnOff(immediate: true);
  }

  private void OnEnable()
  {
    if (this._turnOnImmediately)
      TurnOn();
  }

  private void Update()
  {
    if (!this._light)
    {
      UnityEngine.Object.Destroy(this);
      return;
    }
    this._lifetime += BraveTime.DeltaTime;
    if (this._state == HIDDEN)
      return;

    switch (this._state)
    {
      case FADEIN:
        this._currentFadeTime += BraveTime.DeltaTime;
        if (this._currentFadeTime >= this._fadeInTime)
        {
          this._light.LightIntensity = this._maxBrightness;
          this._state = VISIBLE;
          break;
        }
        float percentFadeInLeft = 1f - this._currentFadeTime / this._fadeInTime;
        this._light.LightIntensity = (1f - percentFadeInLeft * percentFadeInLeft) * this._maxBrightness; // ease in
        break;
      case VISIBLE:
        if (!this._usesLifeTime)
          break;
        if (this._lifetime < this._fadeOutStartTime)
          break;
        if (this._fadeOutTime <= 0)
        {
          DisableOrDestroy();
          return;
        }
        this._state = FADEOUT;
        this._currentFadeTime = 0f;
        break;
      case FADEOUT:
        this._currentFadeTime += BraveTime.DeltaTime;
        if (this._currentFadeTime >= this._fadeOutTime)
        {
          DisableOrDestroy();
          return;
        }
        float percentFadeOutLeft = 1f - this._currentFadeTime / this._fadeOutTime;
        this._light.LightIntensity = (percentFadeOutLeft * percentFadeOutLeft) * this._maxBrightness; // ease out
        break;
      default:
        break;
    }

    if (this._parentObj && this._light.UsesCone && this._rotateWithParent)
    {
      if (this._proj)
        this._light.LightOrient = this._proj.transform.right.XY().ToAngle();
      else if (this._gun)
        this._light.LightOrient = this._gun.CurrentAngle;
      else
        this._light.LightOrient = this._parentObj.transform.localRotation.z;
    }
  }

  private void DisableOrDestroy()
  {
    if (this._usesLifeTime)
    {
      UnityEngine.Object.Destroy(this._light.gameObject);
      this._light = null;
      UnityEngine.Object.Destroy(this);
    }
    else
    {
      this._light.LightIntensity = 0f;
      this._light.enabled = false;
      this._state = HIDDEN;
    }
  }

  private void OnDestroy()
  {
    Cleanup();
  }

  private void Cleanup()
  {
    if (!this._light)
      return;
    if (!this._parentObj || this._destroyWithParent)
    {
      UnityEngine.Object.Destroy(this._light.gameObject);
      return;
    }
    this._light.gameObject.transform.parent = null; // deparent light before destroying (unsure if this actually works, needs testing)
  }
}

public static class LightAPI
{
  public static EasyLight AddLight(this Gun gun, float brightness = 10f, Color? color = null, bool useCone = false, bool turnOnImmediately = true, float fadeInTime = 0f, float fadeOutTime = 0f, float coneWidth = 30f)
  {
    return EasyLight.Create(parent: gun.barrelOffset, brightness: brightness, fadeInTime: fadeInTime, fadeOutTime: fadeOutTime, color: color,
      maxLifeTime: -1f, useCone: useCone, coneWidth: coneWidth, turnOnImmediately: turnOnImmediately);
  }

  public static EasyLight AddLight(this Projectile projectile, float brightness = 10f, Color? color = null, bool useCone = false, bool turnOnImmediately = true, float fadeInTime = 0f,
    float fadeOutTime = 0f, float coneWidth = 30f)
  {
    return EasyLight.Create(parent: projectile.gameObject.transform, brightness: brightness, fadeInTime: fadeInTime, fadeOutTime: fadeOutTime, color: color,
      maxLifeTime: -1f, useCone: useCone, coneWidth: coneWidth, turnOnImmediately: turnOnImmediately);
  }

  public static EasyLight TemporaryLight(Vector2 position, float maxLifeTime = 1f, float brightness = 10f, float fadeInTime = 0f, float fadeOutTime = 0f, Transform parent = null)
  {
    return null;
  }

  public static EasyLight FancyLight(Vector2 position, float maxLifeTime = 1f, float brightness = 10f, float fadeInTime = 0f, float fadeOutTime = 0f, Transform parent = null)
  {
    return null;
  }
}
