using UnityEngine;

/// <summary>
/// Correcteur de profondeur isométrique — à glisser sur le prefab racine du personnage.
///
/// INSTALLATION
///   1. Glisser ce composant sur le prefab (ou GameObject) racine du personnage.
///   2. Si les tuiles d'arène utilisent un Sorting Layer nommé (ex: "Arena"),
///      renseigner exactement ce nom dans le champ "Sorting Layer Name".
///      Laisser vide si tout est sur le layer "Default".
///   3. Lancer le jeu — les personnages passent derrière/devant les obstacles
///      selon leur profondeur isométrique.
///
/// POURQUOI CE SCRIPT
///   L'ancien code utilisait un biais fixe (+1000) qui plaçait les personnages
///   TOUJOURS au-dessus de tout, y compris des obstacles situés devant eux.
///   Ce script utilise en LateUpdate la même formule que les obstacles d'arène
///   (ArenaGenerator : orderBias + baseOrder + w*h), ce qui garantit un tri correct
///   pour toute case adjacente, quel que soit la taille de la grille.
/// </summary>
[DefaultExecutionOrder(200)]   // Après TacticalCharacter (0) et PlayerAnimator (0)
public class IsometricDepthSort : MonoBehaviour
{
    [Tooltip("Nom du Sorting Layer utilisé par les tuiles d'arène.\n" +
             "Laisser vide pour ne pas modifier le layer du SpriteRenderer.")]
    public string sortingLayerName = "";

    private SpriteRenderer _sr;

    void LateUpdate()
    {
        var sr = ActiveRenderer();
        if (sr == null) return;

        // Appliquer le Sorting Layer si renseigné
        if (!string.IsNullOrEmpty(sortingLayerName) && sr.sortingLayerName != sortingLayerName)
            sr.sortingLayerName = sortingLayerName;

        var gm = GridManager.Instance;
        if (gm == null)
        {
            // Fallback sans GridManager : tri simple par Y monde
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
            return;
        }

        int w    = gm.GridWidth;
        int h    = gm.GridHeight;
        int bias = gm.config.arenaTileSortingOrderBias;
        var cfg  = gm.config;

        // Soustraire le décalage visuel du personnage pour retrouver la position logique de la case
        Vector3 logicalPos = transform.position
                           - new Vector3(cfg.characterWorldOffset.x, cfg.characterWorldOffset.y, 0f);

        Vector2Int g  = gm.WorldToGrid(logicalPos);
        int        gx = Mathf.Clamp(g.x, 0, w - 1);
        int        gy = Mathf.Clamp(g.y, 0, h - 1);

        // Même formule que les obstacles dans ArenaGenerator :
        //   sr.sortingOrder = orderBias + (isObstacle ? baseOrder + w*h : baseOrder - w*h)
        // Le personnage utilise +w*h (même couche que l'obstacle).
        // Démonstration : pour |Δrow| = 1 entre personnage et obstacle,
        //   le tri est toujours correct quel que soit la position en colonne.
        sr.sortingOrder = bias + (-(gy * w + gx)) + w * h;
    }

    /// <summary>
    /// Cherche le SpriteRenderer actif dans la hiérarchie.
    /// Gère la création tardive du child "SpriteVisual" par PlayerAnimator.
    /// </summary>
    SpriteRenderer ActiveRenderer()
    {
        if (_sr != null && _sr.enabled)
            return _sr;

        // Parcourir tous les renderers (actifs et inactifs) et prendre le premier activé
        var all = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var r in all)
        {
            if (r.enabled)
            {
                _sr = r;
                return r;
            }
        }
        return null;
    }
}
