# PC Optimizer

Application de diagnostic et de monitoring PC gaming (Windows), en WinUI 3 / C#.

## Contexte

Projet personnel visant à remplacer les "optimiseurs PC" marketing (registre, promesses non vérifiées) par un outil basé sur une documentation de leviers réellement mesurés. Voir la documentation complète (niveaux de preuve, sources) dans le Notion du projet.

## Stack technique

- **C# / .NET 8** (`net8.0-windows10.0.19041.0`)
- **WinUI 3** (Windows App SDK 2.3.1) — déploiement *unpackaged* (pas de MSIX pour l'instant)
- **LibreHardwareMonitorLib 0.9.6** — lecture des capteurs matériels (CPU/GPU temp, charge, ventilateurs)
- Architecture **MVVM**

Ces versions ont été validées par un spike technique avant le démarrage du projet (voir `docs/spike-notes.md` si présent).

## Structure

```
src/app/
├── Views/          Écrans de l'application
├── ViewModels/     Logique de présentation (MVVM)
├── Models/         Structures de données (métriques, leviers d'audit)
├── Services/       Accès matériel/système (HardwareMonitorService, etc.)
├── Styles/         Design tokens (Tokens.xaml) — traduits depuis le design system Claude Design
└── Assets/
```

## Niveaux fonctionnels (roadmap)

1. **Audit statique** — checklist de leviers vérifiés (driver GPU, XMP, power plan, etc.)
2. **Application "session de jeu"** — pattern Snapshot → Apply → Verify → Revert
3. **Monitoring temps réel** — dashboard (températures, stockage, charge système, réseau)

## Prérequis pour builder

- .NET SDK 8+ installé
- Windows 10/11 (l'app est Windows-only, dépend de WMI et d'APIs natives)
- L'app nécessite des **droits administrateur** pour lire les capteurs matériels (voir `app.manifest`)

## Statut

En cours d'initialisation — voir les issues/roadmap pour l'avancement détaillé.
