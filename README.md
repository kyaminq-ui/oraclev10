# Oracle — Tactical RPG Isométrique (Unity)

Projet Unity de combat tactique tour par tour (1v1), vue isométrique, système de sorts + passifs, HUD combat, matchmaking hub et pont réseau Photon.

Ce dossier est la racine Unity du projet.

## Stack

- Unity `2022.3.62f3`
- URP
- Photon PUN 2 (`Assets/Photon`)
- TextMesh Pro
- DOTween
- ParrelSync (tests multi-instance)

## Ordre de build actuel (source de vérité)

Référence: `ProjectSettings/EditorBuildSettings.asset`

1. `Assets/_Game/Scenes/MainMenu.unity`
2. `Assets/_Game/Scenes/Hub.unity`
3. `Assets/Monjeu.unity`
4. `Assets/_Game/Scenes/Ranked1v1.unity`
5. `Assets/Boot_Network.unity`

## État actuel

### Déjà en place

- Grille iso, pathfinding, génération d’arène, highlights.
- Combat au tour par tour (initiative, timer, PA/PM, sorts, cooldowns, logs).
- Deck de 6 sorts, passifs, écran de sélection des passifs.
- Pipeline de combat (`CombatInitializer`): passifs -> placement -> combat.
- Réseau 1v1: room/matchmaking (`HubMatchmaker`) + bridge RPC (`OracleCombatNetBridge`) pour déplacement, cast, fin de tour et placement.

### Priorités MVP maintenant

1. Synchronisation autoritaire de la sélection des passifs.
2. Gestion propre des déconnexions/reconnexions.
3. Durcissement des cas limite réseau (timeout, migration MasterClient, conflits d’actions).

## Nettoyage 2026-05-03

Nettoyage majeur réalisé en mode sécurisé:

- suppression des caches locaux (`Library`, `Temp`, `Logs`, `UserSettings`)
- retrait des fichiers générés IDE (`*.csproj`, `*.sln`)
- mise en quarantaine des démos/legacy dans `__cleanup_quarantine__/`

Rapports:

- `CLEANUP_AUDIT_2026-05-03.md`
- `ORACLE_SCRIPTS_CLASSIFICATION_2026-05-03.md`

## Documentation projet

- `ROADMAP_ORACLE.txt` (roadmap active)
- `TUTORIEL_RESEAU_ORACLE.txt` (flux réseau actuel)
- `TUTORIEL_TEST_2_JOUEURS_PC.txt` (tests 2 joueurs sur même PC)
