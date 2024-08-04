namespace CwaffingTheGungy;

using static EasyLight.State;

/// <summary>Helper class for adding nice looking lights to Guns, Projectiles, and other GameObject</summary>
public partial class EasyLight : MonoBehaviour
{
  /// <summary>Whether the light is current on or not. May still be invisible if its parent is deactivated.</summary>
  public bool IsOn => this._state == VISIBLE || this._state == FADEIN;

  /// <summary>Turns the light on if off, and turns it off if on.</summary>
  public void Toggle()
  {
    if (this.IsOn)
      TurnOff();
    else
      TurnOn();
  }

  /// <summary>Turns the light on.</summary>
  public void TurnOn()
  {
    if (this.IsOn)
      return;
    if (this._fadeInTime > 0f)
    {
      this._currentFadeTime = 0f;
      this._light.LightIntensity = 0f;
      this._state = FADEIN;
      return;
    }
    this._light.LightIntensity = this._brightness;
    this._state = VISIBLE;
  }

  /// <summary>Turns the light off, optionally skipping its fade out animation.</summary>
  public void TurnOff(bool immediate = false)
  {
    if (!this.IsOn)
      return;
    if (!immediate && this._fadeOutTime > 0f)
    {
      this._currentFadeTime = 0f;
      this._light.LightIntensity = this._brightness;
      this._state = FADEOUT;
      return;
    }
    this._light.LightIntensity = 0f;
    this._state = HIDDEN;
  }

  /// <summary>Sets the brightness of a light.</summary>
  public void SetBrightness(float brightness)
  {
    this._brightness = brightness;
  }

  /// <summary>Sets how far the light extends from its origin point.</summary>
  public void SetRadius(float radius)
  {
    this._radius = radius;
  }

  /// <summary>Sets the angle spanned by cone lights.</summary>
  public void SetConeWidth(float coneWidth)
  {
    this._light.LightAngle = coneWidth;
  }

  /// <summary>Sets the color of the light.</summary>
  public void SetColor(Color color)
  {
    this._light.LightColor = color;
  }

  /// <summary>For cone lights, automatically tracks the specified GameObject's transform.</summary>
  public void TrackObject(GameObject g)
  {
    this._trackedObject = g;
  }

  /// <summary>For cone lights, points the light in the direction specified by angle.</summary>
  public void PointInDirection(float angle)
  {
    this._light.LightOrient = angle;
  }

  /// <summary>For cone lights, points the light towards the specified position.</summary>
  public void PointAt(Vector2 position)
  {
    this._light.LightOrient = (position - this._light.transform.position.XY()).ToAngle();
  }

  /// <summary>Creates an Easy Light at the specified positinon and / or parented to the specified object</summary>
  /// <param name="pos">Where to place the light. Can be ignored if parent is non-null. Must be set if parent is null.</param>
  /// <param name="parent">The transform to parent the light to. If null, the light cannot be moved from its initial position.</param>
  /// <param name="color">The color of the light. Can be changed later using SetColor().</param>
  /// <param name="maxLifeTime">If greater than zero, determines the amount of time before the light is destroyed.</param>
  /// <param name="radius">How far the light radiates from its origin point.</param>
  /// <param name="growIn">If true, the light will grow to its radius when fading in and shrink to nothingness when fading out.</param>
  /// <param name="brightness">The brightness of the light. Setting to anything lower than 3.5f can have strange effects. Can be changed later using SetBrightness().</param>
  /// <param name="fadeInTime">If greater than 0, determines how long the light takes to reach its max brightness when turned on.</param>
  /// <param name="fadeOutTime">If greater than 0, determines how long the light takes to reach zero brightness when turned off.</param>
  /// <param name="destroyWithParent">(MIGHT NOT WORK) If true, the light is destroyed when its parent is destroyed. If false, the light persists after its parent is destroyed.</param>
  /// <param name="useCone">If true, the light will be emitted in a cone instead of in all directions.</param>
  /// <param name="coneWidth">If useCone is true, determines the angle spanned by the cone of light. Can be changed later using SetConeWidth().</param>
  /// <param name="coneDirection">If useCone is true, determines the angle the cone of light is pointed at. Can be changed later using PointInDirection() or PointAt().</param>
  /// <param name="rotateWithParent">If true, if useCone is true, and if parented to a Gun / Projectile, automatically points the cone in the direction of the gun barrel / projectile respectively.</param>
  /// <param name="turnOnImmediately">If true, the light is turned on immediately when created. If false, the light must be turned on manually with Toggle() or TurnOn().</param>
  public static EasyLight Create(Vector2? pos = null, Transform parent = null, Color? color = null, float maxLifeTime = -1f,  float radius = 20f, bool grownIn = false, float brightness = 10f,
      float fadeInTime = 0f, float fadeOutTime = 0f, bool destroyWithParent = true, bool useCone = false, float coneWidth = 30f, float coneDirection = 0f,
      bool rotateWithParent = true, bool turnOnImmediately = true)
  {
    return EasyLight.CreateInternal(
      pos               : pos,
      parent            : parent,
      color             : color,
      maxLifeTime       : maxLifeTime,
      radius            : radius,
      growIn            : grownIn,
      brightness        : brightness,
      fadeInTime        : fadeInTime,
      fadeOutTime       : fadeOutTime,
      destroyWithParent : destroyWithParent,
      useCone           : useCone,
      coneWidth         : coneWidth,
      coneDirection     : coneDirection,
      rotateWithParent  : rotateWithParent,
      turnOnImmediately : turnOnImmediately
      );
  }
}

public static class EasyLightExtensions
{
  /// <summary>Attaches an EasyLight to a gun.</summary>
  /// <param name="brightness">The brightness of the light. Setting to anything lower than 3.5f can have strange effects. Can be changed later using SetBrightness().</param>
  /// <param name="color">The color of the light. Can be changed later using SetColor().</param>
  /// <param name="useCone">If true, the light will be emitted in a cone instead of in all directions.</param>
  /// <param name="turnOnImmediately">If true, the light is turned on immediately when created. If false, the light must be turned on manually with Toggle() or TurnOn().</param>
  /// <param name="fadeInTime">If greater than 0, determines how long the light takes to reach its max brightness when turned on.</param>
  /// <param name="fadeOutTime">If greater than 0, determines how long the light takes to reach zero brightness when turned off.</param>
  /// <param name="coneWidth">If useCone is true, determines the angle spanned by the cone of light. Can be changed later using SetConeWidth().</param>
  /// <param name="rotateWithParent">If true, if useCone is true, and if parented to a Gun / Projectile, automatically points the cone in the direction of the gun barrel / projectile respectively.</param>
  /// <param name="radius">How far the light radiates from its origin point.</param>
  /// <param name="growIn">If true, the light will grow to its radius when fading in and shrink to nothingness when fading out.</param>
  public static EasyLight AddLight(this Gun gun, float brightness = 10f, Color? color = null, bool useCone = false, bool turnOnImmediately = true, float fadeInTime = 0f,
    float fadeOutTime = 0f, float coneWidth = 30f, bool rotateWithParent = true, float radius = 20f, bool grownIn = false)
  {
    return EasyLight.CreateInternal(parent: gun.barrelOffset, brightness: brightness, fadeInTime: fadeInTime, fadeOutTime: fadeOutTime, color: color, radius: radius,
      maxLifeTime: -1f, useCone: useCone, coneWidth: coneWidth, turnOnImmediately: turnOnImmediately, rotateWithParent: rotateWithParent, growIn: grownIn);
  }

  /// <summary>Attaches an EasyLight to a projectile.</summary>
  /// <param name="brightness">The brightness of the light. Setting to anything lower than 3.5f can have strange effects. Can be changed later using SetBrightness().</param>
  /// <param name="color">The color of the light. Can be changed later using SetColor().</param>
  /// <param name="useCone">If true, the light will be emitted in a cone instead of in all directions.</param>
  /// <param name="turnOnImmediately">If true, the light is turned on immediately when created. If false, the light must be turned on manually with Toggle() or TurnOn().</param>
  /// <param name="fadeInTime">If greater than 0, determines how long the light takes to reach its max brightness when turned on.</param>
  /// <param name="fadeOutTime">If greater than 0, determines how long the light takes to reach zero brightness when turned off.</param>
  /// <param name="coneWidth">If useCone is true, determines the angle spanned by the cone of light. Can be changed later using SetConeWidth().</param>
  /// <param name="rotateWithParent">If true, if useCone is true, and if parented to a Gun / Projectile, automatically points the cone in the direction of the gun barrel / projectile respectively.</param>
  /// <param name="radius">How far the light radiates from its origin point.</param>
  /// <param name="growIn">If true, the light will grow to its radius when fading in and shrink to nothingness when fading out.</param>
  public static EasyLight AddLight(this Projectile projectile, float brightness = 10f, Color? color = null, bool useCone = false, bool turnOnImmediately = true, float fadeInTime = 0f,
    float fadeOutTime = 0f, float coneWidth = 30f, bool rotateWithParent = true, float radius = 20f, bool grownIn = false)
  {
    return EasyLight.CreateInternal(parent: projectile.gameObject.transform, brightness: brightness, fadeInTime: fadeInTime, fadeOutTime: fadeOutTime, color: color, radius: radius,
      maxLifeTime: -1f, useCone: useCone, coneWidth: coneWidth, turnOnImmediately: turnOnImmediately, rotateWithParent: rotateWithParent, growIn: grownIn);
  }
}

// Private API for EasyLight
public partial class EasyLight : MonoBehaviour
{
  [SerializeField]
  private AdditionalBraveLight _light;
  [SerializeField]
  private GameObject _parentObj;
  [SerializeField]
  private float _maxLifetime;
  [SerializeField]
  private float _brightness;
  [SerializeField]
  private float _fadeInTime;
  [SerializeField]
  private float _fadeOutTime;
  [SerializeField]
  private float _fadeOutStartTime;
  [SerializeField]
  private bool _destroyWithParent;
  [SerializeField]
  private bool _usesLifeTime;
  [SerializeField]
  private bool _rotateWithParent;
  [SerializeField]
  private bool _turnOnImmediately;
  [SerializeField]
  private bool _growIn;
  [SerializeField]
  private float _radius;

  private float _lifetime;
  private float _currentFadeTime;
  private State _state;
  private Gun _gun;
  private Projectile _proj;
  private GameObject _trackedObject;

  internal enum State
  {
    HIDDEN,
    FADEIN,
    VISIBLE,
    FADEOUT,
  }

  internal static EasyLight CreateInternal(Vector2? pos = null, Transform parent = null, Color? color = null, float maxLifeTime = 1f, float brightness = 10f, float radius = 20f,
    float fadeInTime = 0f, float fadeOutTime = 0f, bool destroyWithParent = true, bool useCone = false, float coneWidth = 30f, float coneDirection = 0f,
    bool rotateWithParent = true, bool turnOnImmediately = true, bool growIn = false)
  {
    if (pos == null && parent == null)
    {
      ETGModConsole.Log($"can't create light without parent or position");
      return null;
    }
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
    e._radius = e._light.LightRadius = radius;
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
    e._brightness = brightness;
    e._turnOnImmediately = turnOnImmediately;
    e._growIn = growIn;

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
      this._light.LightIntensity = this._brightness;
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
          this._light.LightIntensity = this._brightness;
          this._light.LightRadius = this._radius;
          this._state = VISIBLE;
          break;
        }
        float percentFadeInLeft = 1f - this._currentFadeTime / this._fadeInTime;
        float easeInFraction = 1f - percentFadeInLeft * percentFadeInLeft;
        this._light.LightIntensity = easeInFraction * this._brightness;
        if (this._growIn)
          this._light.LightRadius = easeInFraction * this._radius;
        break;
      case VISIBLE:
        this._light.LightRadius = this._radius;
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
        float easeOutFraction = percentFadeOutLeft * percentFadeOutLeft;
        this._light.LightIntensity = easeOutFraction * this._brightness; // ease out
        if (this._growIn)
          this._light.LightRadius = easeOutFraction * this._radius;
        break;
      default:
        break;
    }

    if (this._light.UsesCone)
    {
      if (this._trackedObject)
      {
        this._light.LightOrient = (this._trackedObject.transform.position - this._light.transform.position).XY().ToAngle();
      }
      else if (this._parentObj && this._rotateWithParent)
      {
        if (this._proj)
          this._light.LightOrient = this._proj.transform.right.XY().ToAngle();
        else if (this._gun)
          this._light.LightOrient = this._gun.CurrentAngle;
        else
          this._light.LightOrient = this._parentObj.transform.localRotation.z;
      }
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

