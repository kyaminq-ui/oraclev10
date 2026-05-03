using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Texte flottant de combat (-X dégâts / +X soins) en espace monde, police Aseprite.
/// Usage : FloatingDamageText.SpawnDamage(amount, worldPos) / FloatingDamageText.SpawnHeal(amount, worldPos)
/// </summary>
public class FloatingDamageText : MonoBehaviour
{
    static readonly Color ColorDamage = new Color(1f, 0.38f, 0.38f);
    static readonly Color ColorHeal   = new Color(0.42f, 1f, 0.52f);

    const float Duration     = 1.1f;
    const float RiseDistance = 1.4f;
    const float PeakScale    = 1.35f;
    const float FontSize     = 5f;
    const float SpawnOffsetY = 0.6f;

    TextMeshPro _label;
    Color       _baseColor;

    // =========================================================
    // API PUBLIQUE
    // =========================================================
    public static void SpawnDamage(int amount, Vector3 worldPos) =>
        Create($"-{amount}", ColorDamage, worldPos);

    public static void SpawnHeal(int amount, Vector3 worldPos) =>
        Create($"+{amount}", ColorHeal, worldPos);

    // =========================================================
    // CRÉATION
    // =========================================================
    static void Create(string text, Color color, Vector3 worldPos)
    {
        float jitter = Random.Range(-0.25f, 0.25f);
        var go = new GameObject("FloatingDmgText");
        go.transform.position = worldPos + new Vector3(jitter, SpawnOffsetY, 0f);

        var tmp       = go.AddComponent<TextMeshPro>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = FontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        var font = OracleUIImportantFont.GetFont();
        if (font != null) tmp.font = font;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 9999;

        var fdt        = go.AddComponent<FloatingDamageText>();
        fdt._label     = tmp;
        fdt._baseColor = color;
    }

    // =========================================================
    // ANIMATION
    // =========================================================
    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        float   elapsed  = 0f;
        Vector3 startPos = transform.position;

        transform.localScale = Vector3.one * PeakScale;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;

            // Montée régulière
            transform.position = startPos + Vector3.up * (RiseDistance * t);

            // Pop-in rapide puis retour à l'échelle normale
            float scaleT = Mathf.Clamp01(elapsed / 0.12f);
            transform.localScale = Vector3.one * Mathf.Lerp(PeakScale, 1f, scaleT);

            // Fade-out sur la seconde moitié
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            _label.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}
