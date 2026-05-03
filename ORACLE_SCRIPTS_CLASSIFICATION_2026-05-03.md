# Tri scripts Oracle — 2026-05-03

## Objectif

Réduire les scripts historiques redondants et garder un socle Editor clair.

## Core (conservés dans `Assets/_Game/Scripts/Editor`)

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
- `OracleHoverPatch.cs`
- `OracleHubNetworkSetup.cs`
- `OracleHubPlayerSetup.cs`
- `OracleEditorAsepriteFont.cs`
- `OracleMajGifImportTool.cs`
- `OracleDeckSlotsMajBuilder.cs`
- `InjectCombatTooltipSystem.cs`
- `InjectTurnOrderTimeline.cs`
- `IsoGridFixer.cs`
- `OracleCharacterTooltipBuilder.cs`
- `OracleChatUIBuilder.cs`
- `Sprite01SetupTool.cs`
- `OraclePassiveScreenBuilder.cs`

## Legacy (déplacés en quarantaine, réversible)

Chemin: `__cleanup_quarantine__/oracle-editor-legacy/`

- `OraclePassiveSelectionBuilder.cs`
- `OracleQuickFix.cs`
- `OracleFixV2.cs`
- `OracleFanLayoutFix.cs`
- `OracleOpponentAIPatch.cs`

## Obsolètes probables (déplacés en quarantaine, à supprimer définitivement après validation)

Chemin: `__cleanup_quarantine__/runtime-legacy/`

- `AoETooltipOverlay.cs`
- `HubAccountHeader.cs`

## Note opérationnelle

Si une régression apparaît, restaurer les fichiers depuis `__cleanup_quarantine__/` vers leur emplacement d’origine.
