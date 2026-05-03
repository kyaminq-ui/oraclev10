using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestionnaire persistant du hub — point central pour les transitions de scène.
/// Survit au changement de scène (DontDestroyOnLoad) pour que CombatInitializer
/// puisse détecter sa présence et afficher le bouton "Retour au Hub".
/// </summary>
public class HubManager : MonoBehaviour
{
    public static HubManager Instance { get; private set; }

    [Header("Scènes")]
    [Tooltip("Nom de la scène de combat pour l'entraînement solo (vs IA).")]
    public string trainingSceneName = "Monjeu";
    [Tooltip("Nom de cette scène hub.")]
    public string hubSceneName = "Hub";

    /// <summary>
    /// Deck personnalisé sélectionné dans le hub.
    /// Si non null et contient 6 sorts, il est appliqué au joueur en combat
    /// (via CombatInitializer.ApplyRandomSpellDecksIfConfigured).
    /// </summary>
    public List<SpellData> SelectedDeck { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Lance le mode entraînement (combat solo vs IA).</summary>
    public void LaunchTraining()
    {
        Debug.Log($"[HubManager] Lancement entraînement → {trainingSceneName}");
        SceneManager.LoadScene(trainingSceneName);
    }

    /// <summary>Retourne au hub depuis n'importe quelle scène (statique pour usage depuis CombatInitializer).</summary>
    public static void ReturnToHub()
    {
        string hub = Instance != null ? Instance.hubSceneName : "Hub";
        Debug.Log($"[HubManager] Retour au hub → {hub}");
        SceneManager.LoadScene(hub);
    }
}
