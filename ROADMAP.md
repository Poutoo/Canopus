# Roadmap

Ordre indicatif, sans date. Les étapes « Prochaines » sont des propositions à valider.

## Fait

- Monitoring temps réel : CPU, GPU, RAM, disques, réseau, processus (PR #1)
- Audit statique : 6 vérifications en lecture seule (PR #2, #3)
- Session de jeu : 3 réglages réversibles, restauration après plantage (PR #4, #6)
- Paramètres : démarrage avec Windows, zone de notification, mises à jour Velopack (PR #7)
- Français et anglais, appliqués au redémarrage (PR #8)
- Design system v5 : Desktop Acrylic, palette Dragée, Nunito, navigation à volet, mouvement (PR #9), interface validée sur Windows

## Prochaines étapes (à valider)

### Qualité
- [x] Projet de tests unitaires (traductions, formats et unités, résumé d'audit, session de jeu, paramètres)
- [ ] Tester aussi la logique d'affichage des ViewModels (aujourd'hui liée à WinUI)
- [x] CI GitHub Actions : tests et build Windows à chaque PR
- [ ] Mesurer le contraste réel au pire point (fond clair, très sombre, aplat noir) pour confirmer les chiffres de la maquette
- [x] Mesurer CPU et RAM au repos sur le Tableau de bord : environ 90 à 100 Mo (mémoire privée, comme le Gestionnaire des tâches) et moins de 1 % de CPU

### Finitions de la v5
- [x] Dialogue de mise à jour au lancement habillé au design v5
- [x] Vrai logo et icône d'application
- [x] Heure du dernier audit (« d'après l'audit de 14 h 32 »), absente faute de donnée
- [x] Choisir le GPU dédié plutôt que le premier GPU trouvé (risque d'afficher l'iGPU)
- [x] Réintégrer les limites des réglages de session retirées par la v5 (souris reprise par un logiciel tiers, plan USB)

### Distribution
- [ ] Première release Velopack publiée depuis la v5 (`v0.2.0`)
- [ ] Signature de code, si l'avertissement SmartScreen devient un frein
