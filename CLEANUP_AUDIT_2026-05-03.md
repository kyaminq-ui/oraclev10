# Audit nettoyage complet — 2026-05-03

## Résumé exécutif

Audit repo complet réalisé pour préparer un nettoyage massif en mode sécurisé.

- Objectif: retirer le bruit technique sans casser le runtime gameplay/réseau.
- Méthode: classification en 3 classes (`supprimable`, `quarantaine`, `à conserver`).
- Contrainte: aucune suppression directe des zones critiques sans étape intermédiaire.

## Volumétrie (fichiers)

- `Assets`: 4825
- `Assets/_Game`: 3456
- `Assets/Photon`: 973
- `Assets/TextMesh Pro/Examples & Extras`: 249
- `Library`: 36572
- `Logs`: 21
- `UserSettings`: 3

## Matrice de décision

### Supprimable immédiatement (risque faible)

- `Library/` (cache Unity régénérable)
- `Temp/` (cache temporaire)
- `Logs/` (logs locaux)
- `UserSettings/` (préférences machine locale)
- fichiers générés IDE à la racine (`*.csproj`, `*.sln`) s'ils sont présents dans le repo

### Quarantaine d'abord (risque moyen)

- `Assets/Photon/*Demos*`
- `Assets/TextMesh Pro/Examples & Extras`
- scripts editor Oracle redondants/one-shot identifiés plus bas

### À conserver (risque élevé si suppression)

- `Assets/_Game` runtime (combat, UI, réseau, hub, core)
- `ProjectSettings/`
- `Packages/manifest.json` et `Packages/packages-lock.json`
- scènes actives de `ProjectSettings/EditorBuildSettings.asset`:
  - `Assets/_Game/Scenes/MainMenu.unity`
  - `Assets/_Game/Scenes/Hub.unity`
  - `Assets/Monjeu.unity`
  - `Assets/_Game/Scenes/Ranked1v1.unity`
  - `Assets/Boot_Network.unity`

## Tri initial des scripts Oracle (pré-cleanup)

### Candidats `Legacy` (quarantaine)

- `Assets/_Game/Scripts/Editor/OraclePassiveSelectionBuilder.cs` (doublon partiel)
- `Assets/_Game/Scripts/Editor/OracleQuickFix.cs` (fix historique)
- `Assets/_Game/Scripts/Editor/OracleFixV2.cs` (fix historique)
- `Assets/_Game/Scripts/Editor/OracleFanLayoutFix.cs` (overlap avec hover/layout)
- `Assets/_Game/Scripts/Editor/OracleOpponentAIPatch.cs` (patch dev ponctuel)

### Candidats `Obsolète probable` (à valider en quarantaine)

- `Assets/_Game/Scripts/UI/AoETooltipOverlay.cs`
- `Assets/_Game/Scripts/Hub/HubAccountHeader.cs`

### `Core tooling` à conserver

- `OracleSetupWizard.cs`
- `OracleCombatHUDBuilder.cs`
- `OracleMinimalistUIRebuild.cs`
- `OracleHubSceneBuilder.cs`
- `OracleMainMenuSceneBuilder.cs`
- `OracleRanked1v1Setup.cs`
- `OracleSpellFactory.cs`
- `OracleSpellDeckPoolMenu.cs`
- `OracleSpellCarteSortSetup.cs`
- `OracleUIMajTextureSetup.cs`

## Exécution planifiée

1. Suppression zones locales/caches.
2. Création d'une quarantaine `__cleanup_quarantine__/`.
3. Déplacement des dossiers et scripts à risque moyen vers quarantaine.
4. Vérifications (lints + compilation statique possible + scan références).
5. Mise à jour roadmap/docs en source de vérité.
