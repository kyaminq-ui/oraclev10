using UnityEngine;

/// <summary>
/// Repr�sente une cellule de la grille isom�trique
/// Contient toutes les donn�es d'une case
/// </summary>
[System.Serializable]  // Permet de voir la classe dans l'Inspector si besoin
public class Cell
{
    // =========================================================
    // PROPRI�T�S DE POSITION
    // =========================================================

    /// <summary>Position X dans la grille (colonne)</summary>
    public int GridX { get; private set; }

    /// <summary>Position Y dans la grille (ligne)</summary>
    public int GridY { get; private set; }

    /// <summary>Position en coordonn�es monde Unity</summary>
    public Vector3 WorldPosition { get; private set; }

    // =========================================================
    // PROPRI�T�S D'�TAT
    // =========================================================

    /// <summary>La cellule peut-elle �tre travers�e ?</summary>
    public bool IsWalkable { get; set; } = true;

    /// <summary>Murs Pilier / Balise : tours restants avant r�tablissement de la case ; 0 = pas un mur temporaire.</summary>
    public int TemporaryWallTurns { get; set; }

    /// <summary>Type visuel et fonctionnel de cette cellule (sol, obstacle, spawn�)</summary>
    public CellTileType TileType { get; set; } = CellTileType.Ground;

    /// <summary>La cellule est-elle occup�e par quelque chose ?</summary>
    public bool IsOccupied => Occupant != null;

    /// <summary>L'objet qui occupe cette cellule (null si vide)</summary>
    public GameObject Occupant { get; private set; } = null;

    // =========================================================
    // PROPRI�T�S DE HIGHLIGHT
    // =========================================================

    /// <summary>Type de highlight actuel sur cette cellule</summary>
    public HighlightType CurrentHighlight { get; private set; } = HighlightType.None;

    /// <summary>La cellule est-elle survol�e par la souris ?</summary>
    public bool IsHovered { get; set; } = false;

    /// <summary>La cellule est-elle s�lectionn�e ?</summary>
    public bool IsSelected { get; set; } = false;

    /// <summary>R�f�rence au GameObject visuel de cette cellule</summary>
    public GameObject VisualObject { get; set; } = null;

    // =========================================================
    // CONSTRUCTEUR
    // =========================================================

    /// <summary>
    /// Cr�er une nouvelle cellule
    /// </summary>
    /// <param name="gridX">Position colonne dans la grille</param>
    /// <param name="gridY">Position ligne dans la grille</param>
    /// <param name="worldPosition">Position dans le monde Unity</param>
    public Cell(int gridX, int gridY, Vector3 worldPosition)
    {
        GridX = gridX;
        GridY = gridY;
        WorldPosition = worldPosition;
    }

    // =========================================================
    // M�THODES DE GESTION DE L'OCCUPANT
    // =========================================================

    /// <summary>
    /// Placer un GameObject sur cette cellule
    /// </summary>
    public void SetOccupant(GameObject occupant)
    {
        Occupant = occupant;
    }

    /// <summary>
    /// Lib�rer cette cellule
    /// </summary>
    public void ClearOccupant()
    {
        Occupant = null;
    }

    // =========================================================
    // M�THODES DE HIGHLIGHT
    // =========================================================

    /// <summary>
    /// Changer le type de highlight
    /// </summary>
    public void SetHighlight(HighlightType type)
    {
        CurrentHighlight = type;
    }

    /// <summary>
    /// Supprimer le highlight
    /// </summary>
    public void ClearHighlight()
    {
        CurrentHighlight = HighlightType.None;
        IsHovered = false;
        IsSelected = false;
    }

    // =========================================================
    // UTILITAIRES
    // =========================================================

    /// <summary>
    /// Repr�sentation texte pour le debug
    /// </summary>
    public override string ToString()
    {
        return $"Cell({GridX},{GridY}) | " +
               $"Walkable:{IsWalkable} | " +
               $"Occupied:{IsOccupied} | " +
               $"Highlight:{CurrentHighlight}";
    }
}

// =========================================================
// ENUM � Types de highlight
// =========================================================

/// <summary>
/// Tous les types de mise en �vidence possibles pour une cellule
/// </summary>
public enum HighlightType
{
    None,       // Pas de highlight
    Move,       // D�placement possible (bleu)
    Attack,     // Zone d'attaque (rouge)
    AoE,        // Zone d'effet (orange)
    Selected,   // S�lectionn� (jaune)
    Hover       // Survol souris (gris)
}