# Roadmap

Ordre indicatif, sans date. Les étapes « Prochaines » sont des propositions à valider.

## Fait

- Monitoring temps réel : CPU, GPU, RAM, disques, réseau, processus (PR #1)
- Audit statique : 6 vérifications en lecture seule (PR #2, #3)
- Session de jeu : 3 réglages réversibles, restauration après plantage (PR #4, #6)
- Paramètres : démarrage avec Windows, zone de notification, mises à jour Velopack (PR #7)
- Français et anglais, appliqués au redémarrage (PR #8)
- Design system v5 : Desktop Acrylic, palette Dragée, Nunito, navigation à volet, mouvement (PR #9)

## En cours : valider la v5 sur Windows

La v5 a été écrite sans pouvoir compiler le XAML ni lancer l'app. Avant toute nouvelle fonctionnalité :

- [ ] Build sans erreur ni nouveau warning
- [ ] Contraste réel au pire point, sur fond clair, très sombre et aplat noir (seuils : 4,5:1 texte, 3:1 barres)
- [ ] Perte de focus, transparence coupée, animations coupées, navigation clavier, anglais, fenêtre en 1100 × 700
- [ ] CPU et RAM au repos sur le Tableau de bord, avant et après la v5
- [ ] Police : vérifier que Nunito est bien chargée (sinon repli silencieux sur Segoe UI)
- [ ] Compatibilité de CommunityToolkit.WinUI.Media 8.2 avec le Windows App SDK 2.3.1

## Prochaines étapes (à valider)

### Qualité
- [ ] Projet de tests unitaires (formats et unités, résumé d'audit, logique d'affichage des ViewModels)
- [ ] CI GitHub Actions : build et tests à chaque PR

### Finitions de la v5
- [ ] Dialogue de mise à jour au lancement habillé au design v5
- [ ] Vrai logo (le « ✦ » est provisoire) et icône d'application
- [ ] Heure du dernier audit (« d'après l'audit de 14 h 32 »), absente faute de donnée
- [ ] Choisir le GPU dédié plutôt que le premier GPU trouvé (risque d'afficher l'iGPU)
- [ ] Réintégrer les limites des réglages de session retirées par la v5 (souris reprise par un logiciel tiers, plan USB)

### Distribution
- [ ] Première release Velopack publiée depuis la v5 (`v0.2.0`)
- [ ] Signature de code, si l'avertissement SmartScreen devient un frein
