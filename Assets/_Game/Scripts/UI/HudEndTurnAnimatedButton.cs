using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// HUD « fin de tour » : première frame au repos ; la séquence joue après un clic, puis déclenche <see cref="onSequenceFinished"/>.
/// </summary>
[DisallowMultipleComponent]
public class HudEndTurnAnimatedButton : MonoBehaviour
{
    [Tooltip("Chemin sous Resources (ex. OracleHUD/Frames/end_turn_animated_button)")]
    public string resourcesFolder = "OracleHUD/Frames/end_turn_animated_button";

    [Min(1f)] public float fps = 12f;

    [Tooltip("Invoqué à la fin du clip (ou immédiatement si 0–1 frame).")]
    public UnityEvent onSequenceFinished = new UnityEvent();

    [Tooltip("Si ce délégué renvoie false, le clic est ignoré (pas d’animation).")]
    public Func<bool> canStartAnimation;

    [Tooltip("Masque les TextMeshPro enfants (ex. libellé « Fin de tour »).")]
    public bool hideTMPChildren = true;

    Image     _graphic;
    Button    _button;
    Sprite[]  _frames = Array.Empty<Sprite>();
    Coroutine _playback;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            enabled = false;
            return;
        }

        var tg = _button.targetGraphic as Image;
        _graphic = tg != null ? tg : GetComponent<Image>();

        RefreshFrames();
        HideLabelsIfNeeded();

        _button.onClick.RemoveListener(OnButtonClicked);
        _button.onClick.AddListener(OnButtonClicked);
    }

    void Start()
    {
        HideLabelsIfNeeded();
    }
    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    void OnEnable()
    {
        RefreshFrames();
        ResetToIdleFrame();
        HideLabelsIfNeeded();
    }

    public void RefreshFrames()
    {
        _frames = CombatAnimationResources.LoadAllSpritesSorted(resourcesFolder);
        if (_graphic == null) return;

        if (_frames.Length > 0)
            _graphic.sprite = _frames[0];

        _graphic.type           = Image.Type.Simple;
        _graphic.fillAmount     = 1f;
        _graphic.preserveAspect = true;
        _graphic.raycastTarget = true;
        _graphic.color         = Color.white;
    }

    void HideLabelsIfNeeded()
    {
        if (!hideTMPChildren) return;
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.gameObject.SetActive(false);
    }

    void OnButtonClicked()
    {
        if (canStartAnimation != null && !canStartAnimation())
            return;

        if (_frames.Length <= 1 || _graphic == null)
        {
            onSequenceFinished?.Invoke();
            return;
        }

        if (_playback != null)
            return;

        _playback = StartCoroutine(CoPlayOnce());
    }

    IEnumerator CoPlayOnce()
    {
        bool wasInteractable = _button != null && _button.interactable;
        if (_button != null)
            _button.interactable = false;

        float step = 1f / Mathf.Max(0.01f, fps);
        if (_frames.Length > 0 && _frames[0] != null)
            _graphic.sprite = _frames[0];

        for (int i = 0; i < _frames.Length; i++)
        {
            if (_frames[i] != null)
                _graphic.sprite = _frames[i];

            float t = 0f;
            while (t < step)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        onSequenceFinished?.Invoke();

        if (_frames.Length > 0 && _frames[0] != null)
            _graphic.sprite = _frames[0];

        CombatHUD hud = GetComponentInParent<CombatHUD>();
        if (hud != null)
            hud.RefreshEndTurnButtonInteractable();
        else if (_button != null)
            _button.interactable = wasInteractable;

        _playback = null;
    }

    public void ResetToIdleFrame()
    {
        if (_playback != null)
        {
            StopCoroutine(_playback);
            _playback = null;
        }

        if (_graphic != null && _frames.Length > 0 && _frames[0] != null)
            _graphic.sprite = _frames[0];
    }
}
