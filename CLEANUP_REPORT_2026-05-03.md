# Rapport de nettoyage — 2026-05-03

## Résultat global

Nettoyage massif exécuté avec stratégie sécurisée:

- caches locaux supprimés
- contenu moyen-risque déplacé en quarantaine réversible
- scripts Oracle triés et rationalisés
- roadmap et docs réécrites pour refléter l’état réel

## Suppressions directes effectuées

- `Library/`
- `Temp/` (si présent)
- `Logs/`
- `UserSettings/`
- fichiers générés IDE à la racine:
  - `*.csproj`
  - `*.sln`

## Quarantaine créée

Chemin racine: `__cleanup_quarantine__/`

Sous-dossiers:

- `photon-demos/`
  - `Demos` (PhotonUnityNetworking)
  - `PhotonRealtime_Demos`
  - `PhotonChat_Demos`
- `tmp-examples/`
  - `Examples & Extras` (TextMesh Pro)
- `oracle-editor-legacy/`
  - `OraclePassiveSelectionBuilder.cs`
  - `OracleQuickFix.cs`
  - `OracleFixV2.cs`
  - `OracleFanLayoutFix.cs`
  - `OracleOpponentAIPatch.cs`
- `runtime-legacy/`
  - `AoETooltipOverlay.cs`
  - `HubAccountHeader.cs`

## Documentation mise à jour

- `README.md`
- `ROADMAP_ORACLE.txt`
- `TUTORIEL_RESEAU_ORACLE.txt`
- `TUTORIEL_TEST_2_JOUEURS_PC.txt`

Documents ajoutés:

- `CLEANUP_AUDIT_2026-05-03.md`
- `ORACLE_SCRIPTS_CLASSIFICATION_2026-05-03.md`
- `CLEANUP_REPORT_2026-05-03.md`

## Vérification post-nettoyage

- Lints IDE sur `Assets/_Game/Scripts`: aucun problème remonté.
- Scènes critiques toujours présentes:
  - `Assets/_Game/Scenes/MainMenu.unity`
  - `Assets/_Game/Scenes/Hub.unity`
  - `Assets/Monjeu.unity`
  - `Assets/_Game/Scenes/Ranked1v1.unity`
  - `Assets/Boot_Network.unity`

## Risques résiduels (connus et maîtrisés)

1. Retrait des démos Photon/TMP:
   - faible impact gameplay
   - restaurable depuis `__cleanup_quarantine__/`
2. Retrait scripts legacy:
   - possible impact sur workflows editor historiques
   - restaurable depuis `__cleanup_quarantine__/`

## Procédure de rollback

En cas de besoin, déplacer les fichiers/dossiers concernés depuis `__cleanup_quarantine__/` vers leur emplacement d’origine.
