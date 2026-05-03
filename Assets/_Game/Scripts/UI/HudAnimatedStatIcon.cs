using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anime une <see cref="Image"/> à partir d’une séquence de sprites Resources (PNG extraits d’un GIF).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class HudAnimatedStatIcon : MonoBehaviour
{
    [Tooltip("Chemin sous Assets/_Game/Resources/ (ex. OracleHUD/Frames/animated_mana_icon)")]
    public string resourcesFolder = "OracleHUD/Frames/animated_mana_icon";

    [Min(1f)] public float fps = 10f;

    [Tooltip("Décocher : animation calée sur le temps de jeu (Time.deltaTime), ex. timer de tour en combat.")]
    public bool useUnscaledTime = true;

    Image  _img;
    Sprite[] _frames;
    int    _idx;
    float  _acc;
    bool   _ready;

    void Awake()
    {
        _img = GetComponent<Image>();
    }

    void OnEnable()
    {
        RefreshFrames();
        _idx = 0;
        _acc = 0f;
        ApplyFrame();
    }

    public void RefreshFrames()
    {
        _frames = CombatAnimationResources.LoadAllSpritesSorted(resourcesFolder);
        _ready  = _frames.Length > 0;
        if (_ready && _img != null)
            _img.sprite = _frames[0];
    }

    void Update()
    {
        if (!_ready || _frames.Length <= 1 || _img == null) return;
        _acc += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float step = 1f / Mathf.Max(0.01f, fps);
        while (_acc >= step)
        {
            _acc -= step;
            _idx = (_idx + 1) % _frames.Length;
            ApplyFrame();
        }
    }

    void ApplyFrame()
    {
        if (!_ready || _img == null) return;
        _img.sprite = _frames[Mathf.Clamp(_idx, 0, _frames.Length - 1)];
    }
}
