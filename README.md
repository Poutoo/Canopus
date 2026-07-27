# PC Optimizer

Application de diagnostic et de monitoring PC gaming (Windows), en WinUI 3 / C#.

## Contexte

Projet personnel visant à remplacer les "optimiseurs PC" marketing (registre, promesses non vérifiées) par un outil basé sur une documentation de leviers réellement mesurés. Voir la documentation complète (niveaux de preuve, sources) dans le Notion du projet.

## Stack technique

- **C# / .NET 8** (`net8.0-windows10.0.19041.0`)
- **WinUI 3** (Windows App SDK 2.3.1) — déploiement *unpackaged* (pas de MSIX pour l'instant)
- **LibreHardwareMonitorLib 0.9.6** — lecture des capteurs matériels (CPU/GPU temp, charge, ventilateurs)
- **Velopack 1.2.0** — installation et mises à jour automatiques via GitHub Releases (voir [Mises à jour](#mises-à-jour-automatiques-velopack))
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

## Mises à jour automatiques (Velopack)

- **Velopack** gère l'installation et les mises à jour, avec **GitHub Releases** (`Poutoo/Canopus`) comme flux — pas d'infrastructure d'hébergement séparée.
- **Pas de certificat de signature de code payant** : l'exe reste non signé, l'avertissement SmartScreen au premier lancement est un compromis assumé.
- `VelopackApp.Build().Run()` est appelé en tout premier dans `src/App/Program.cs` (le `Main` généré automatiquement par WinUI est désactivé via `DISABLE_XAML_GENERATED_MAIN` dans le `.csproj`, et remplacé par un `Main` manuel équivalent).
- `Services/IUpdateService.cs` + `VelopackUpdateService.cs` exposent :
  - `CheckForUpdateAsync()` — vérifie seulement, n'applique rien.
  - `DownloadAndApplyUpdateAsync()` — télécharge, applique et redémarre ; appelé uniquement sur action explicite de l'utilisateur (jamais automatique/silencieux).
- Le popup actuel dans `MainWindow.xaml.cs` est un `ContentDialog` WinUI basique et temporaire (texte simple + boutons Installer/Plus tard), juste pour valider le flux bout en bout. L'habillage visuel définitif (cohérent avec `GlassStyles.xaml`) viendra dans une itération séparée.

### Publier une nouvelle release

Prérequis, une seule fois par machine :

```
dotnet tool install -g vpk
```

Il faut aussi un **token GitHub** (personal access token, scope `public_repo` suffit pour un repo public) pour publier la release — passé via `--token` ou la variable d'environnement `VPK_TOKEN`. Ce token est à créer manuellement sur GitHub (Settings → Developer settings → Personal access tokens) ; rien d'autre n'est à configurer côté GitHub, `vpk` crée la release lui-même.

Étapes pour chaque release (adapter le numéro de version à chaque fois, format semver) :

```
# 1. Build self-contained win-x64
dotnet publish src/App/App.csproj -c Release -r win-x64 --self-contained true -o publish

# 2. Récupérer la release précédente pour permettre les mises à jour delta
#    (à sauter pour la toute première release : il n'y a rien à récupérer)
vpk download github --repoUrl https://github.com/Poutoo/Canopus

# 3. Packager
vpk pack --packId Canopus --packVersion 0.1.0 --packDir publish --mainExe App.exe --packTitle "PC Optimizer"

# 4. Publier la release sur GitHub (tag + nom affiché)
vpk upload github --repoUrl https://github.com/Poutoo/Canopus --publish --releaseName "Canopus v0.1.0" --tag v0.1.0 --token <token_github>
```

## Statut

En cours d'initialisation — voir les issues/roadmap pour l'avancement détaillé.
