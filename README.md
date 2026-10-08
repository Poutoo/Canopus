# Canopus

Application Windows de diagnostic et de monitoring pour PC gaming, en WinUI 3 / C#.

Canopus ne promet pas de « booster » ton PC. Il affiche des valeurs mesurées, vérifie des réglages documentés et applique pendant une session de jeu quelques réglages réversibles, remis comme avant à l'arrêt.

## Fonctionnalités

- **Tableau de bord** : température, fréquence, charge et nom du CPU et du GPU ; latence et gigue réseau (ping vers 1.1.1.1 toutes les 1,5 s) ; RAM et disques ; les 3 processus les plus gourmands ; résumé du dernier audit.
- **Audit** : 6 vérifications en lecture seule (plan d'alimentation, Mode Jeu Windows, profil XMP/EXPO, overlays actifs, exclusions Defender, pilote GPU). Chaque vérification affiche ses limites de détection.
- **Session de jeu** : modèle Snapshot → Apply → Verify → Revert sur 3 réglages (plan d'alimentation performant, précision du pointeur, suspension sélective USB). Si l'app plante en pleine session, les réglages sont restaurés au lancement suivant.
- **Paramètres** : lancement avec Windows, réduction dans la zone de notification, inclusion de la précision du pointeur dans la session, langue (français / anglais, appliquée au redémarrage), mises à jour.

## Stack technique

- **C# / .NET 8** (`net8.0-windows10.0.19041.0`)
- **WinUI 3** (Windows App SDK 2.3.1), déploiement *unpackaged* (pas de MSIX)
- **LibreHardwareMonitorLib 0.9.6** : capteurs matériels
- **System.Management** : requêtes WMI de l'audit
- **H.NotifyIcon.WinUI 2.3.2** : icône de la zone de notification
- **CommunityToolkit.WinUI.Media 8.2** : ombres des cards (`AttachedCardShadow`)
- **Velopack 1.2.0** : installation et mises à jour via GitHub Releases
- Architecture **MVVM**

## Structure

```
src/App/
├── Views/          Écrans, volet de navigation, fenêtre et matériau acrylique
├── ViewModels/     Logique de présentation (MVVM)
├── Models/         Données prêtes à l'affichage
├── Services/       Accès matériel et système (capteurs, audit, réglages de session, mises à jour)
├── Controls/       Contrôles maison (DataBar)
├── Animations/     Mouvement : tokens, entrées de page, retour d'appui
├── Localization/   Chargement des textes et formats des nombres et unités
├── Resources/      Textes fr.json et en.json (embarqués dans l'assembly)
├── Styles/         Tokens.xaml (design tokens) et Controls.xaml (styles)
└── Assets/Fonts/   Nunito SemiBold, Bold et ExtraBold (licence OFL)

tests/App.Tests/    Tests unitaires (xUnit)
```

## Design system (v5)

- Fenêtre entière en **Desktop Acrylic**, pilotée par un `DesktopAcrylicController` (`Views/CanopusAcrylicBackdrop.cs`) pour régler la couche de luminosité et la couleur de repli. Thème clair forcé.
- Trois niveaux de surface : **niveau 2** (l'élément principal, un par écran), **niveau 1** (cards secondaires), **niveau 0** (posé sur la fenêtre). Les barres de données sont interdites au niveau 0.
- Palette Dragée, police Nunito.
- Toute valeur visuelle (couleur, opacité, rayon, durée, easing) passe par un token de `Styles/Tokens.xaml`. Les animations passent à 0 si les animations Windows sont désactivées.

La maquette de référence (`docs/design/Canopus_DS_v5.html`) n'est pas versionnée.

## Textes et langues

- Tous les textes, unités comprises, sont dans `Resources/Strings/fr.json` et `en.json`. Toute nouvelle clé va dans les deux fichiers.
- Le français tutoie l'utilisateur.
- Une clé absente s'affiche telle quelle à l'écran, pour être repérée tout de suite.

## Prérequis pour builder

- Windows 10/11 : l'app dépend de WMI et d'APIs natives, et son compilateur XAML ne tourne que sous Windows.
- .NET SDK 8+
- L'app demande les **droits administrateur** (lecture des capteurs, voir `app.manifest`).

```
dotnet build src/App/App.csproj -c Debug -p:Platform=x64
```

## Tests et CI

```
dotnet test tests/App.Tests/App.Tests.csproj
```

- Les tests couvrent la logique sans WinUI : traductions (mêmes clés et mêmes `{0}` en français et en anglais, aucune clé utilisée dans le code qui manquerait), formats des nombres et unités, résumé de l'audit, session de jeu (capture, application, vérification, restauration, reprise après plantage) et paramètres.
- Le projet de tests compile directement ces fichiers sources (fichiers liés dans le `.csproj`) plutôt que de référencer l'app : un exe WinUI ne se charge pas dans un hôte de test. Un nouveau fichier de logique à tester doit y être ajouté.
- Les tests n'écrivent que dans des dossiers temporaires, jamais dans les vrais `session-snapshot.json` ou `settings.json`. Ils tournent aussi sous Linux.
- La CI GitHub Actions (`.github/workflows/ci.yml`) lance, à chaque PR et à chaque push sur `main` :
  - les tests, sous Ubuntu ;
  - le build de l'app, sous Windows. C'est le seul endroit hors de ta machine où le XAML est compilé.

## Mises à jour automatiques (Velopack)

- **Velopack** gère l'installation et les mises à jour, avec **GitHub Releases** (`Poutoo/Canopus`) comme flux. Il n'y a pas d'hébergement séparé.
- **Pas de certificat de signature de code** : l'exe n'est pas signé. L'avertissement SmartScreen au premier lancement est un compromis assumé.
- `VelopackApp.Build().Run()` est appelé en tout premier dans `src/App/Program.cs`. Le `Main` généré par WinUI est désactivé via `DISABLE_XAML_GENERATED_MAIN` dans le `.csproj`.
- `Services/VelopackUpdateService.cs` expose :
  - `CheckForUpdateAsync()` : vérifie seulement, n'applique rien.
  - `DownloadAndApplyUpdateAsync()` : télécharge, applique et redémarre, uniquement sur action explicite de l'utilisateur.
- Au lancement, une mise à jour disponible est proposée dans un `ContentDialog` WinUI basique, pas encore habillé au design v5. Elle peut aussi être cherchée et installée depuis les Paramètres.

### Publier une nouvelle release

Prérequis, une seule fois par machine :

```
dotnet tool install -g vpk
```

Il faut aussi un **token GitHub** (personal access token, scope `public_repo` pour un repo public), passé via `--token` ou la variable d'environnement `VPK_TOKEN`. `vpk` crée la release lui-même.

Étapes pour chaque release (adapter le numéro de version, format semver) :

```
# 1. Build self-contained win-x64
dotnet publish src/App/App.csproj -c Release -r win-x64 --self-contained true -o publish

# 2. Récupérer la release précédente pour les mises à jour delta
#    (à sauter pour la toute première release)
vpk download github --repoUrl https://github.com/Poutoo/Canopus

# 3. Packager
vpk pack --packId Canopus --packVersion 0.2.0 --packDir publish --mainExe App.exe --packTitle "Canopus"

# 4. Publier la release sur GitHub
vpk upload github --repoUrl https://github.com/Poutoo/Canopus --publish --releaseName "Canopus v0.2.0" --tag v0.2.0 --token <token_github>
```

## Statut

Seul tag existant : `v0.1.0`, posé sur le commit initial. Tout ce qui est décrit ici est arrivé après. L'avancement et les prochaines étapes sont dans [ROADMAP.md](ROADMAP.md).
