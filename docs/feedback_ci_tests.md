---
name: feedback-ci-tests
description: Approche CI et tests pour le projet Canopus
metadata:
  type: feedback
---

Modifier le ci.yml si nécessaire sans demander la permission. Ajouter les tests au fur et à mesure de l'avancement des étapes (pas tout à la fin).

**Why:** Le projet avance étape par étape avec un push à chaque étape. Les tests doivent suivre le même rythme pour valider chaque livrable en continu.

**How to apply:** A chaque étape, écrire les tests unitaires correspondants avant de committer. Si le CI casse, le corriger directement (ci.yml ou config) sans attendre la demande.
