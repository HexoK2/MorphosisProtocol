# 🎮 Morphosis  (Projet en developpement...)

Bienvenue sur le dépôt GitHub de **Morphosis** !

Morphosis est un jeu d'arcade-puzzle en 3D isométrique avec des éléments de plateforme et de survie, où l'adaptation est la clé. Incarnez un organisme gélatineux capable de se déformer en réaction aux obstacles pour s'échapper d'un laboratoire de biotechnologie clandestin.

---

## ✨ Aperçu du Jeu

**Titre :** Morphosis
**Type :** Jeu solo, Arcade-Puzzle 3D, Plateforme, Survie
**Plateformes Cibles :** PC (Windows, macOS, Linux), Android (optionnel)
**Durée de jeu :** Niveaux courts (3-5 min), jeu complet estimé à 2-3 heures (pour 20-30 niveaux).
**Public Visé :** Joueurs occasionnels, amateurs de puzzles et de plateformes, appréciant les concepts originaux et l'expérimentation.

### 🌟 Unique Selling Proposition (USP)

La mécanique centrale du jeu est la **déformation dynamique du personnage** en réponse aux collisions avec l'environnement. Chaque obstacle provoque une mutation qui modifie les attributs du Blob (taille, vitesse, élasticité, etc.), forçant le joueur à s'adapter constamment et à repenser sa stratégie. Une collision n'est pas toujours un échec, mais une opportunité de transformation !

---

## 📖 Concept & Scénario

Incarnez un **"Blob"**, une entité expérimentale semi-gélatineuse, qui tente de s'échapper d'un laboratoire hostile conçu pour tester et contenir ces organismes. Guidé par son instinct primaire de survie, le Blob doit naviguer à travers des salles piégées, en utilisant ses mutations forcées pour surmonter les défis.

**Core Gameplay Loop :**
1.  Entrer dans une nouvelle "salle" (niveau).
2.  Identifier les obstacles et les propriétés nécessaires pour les surmonter.
3.  Naviguer prudemment pour éviter les mutations indésirables ou les utiliser stratégiquement.
4.  Atteindre la sortie en s'adaptant à sa propre forme changeante.
5.  Collecter éventuellement des "points de mutation" ou "fragments d'ADN".

**Thème Central :** Adaptation, survie, et la nature imprévisible de l'évolution forcée.

---

## ⚙️ Mécaniques de Jeu Principales

### 3.1. Déplacement du Personnage (Le Blob)
Le Blob est contrôlé via les **flèches directionnelles / ZQSD / WASD** sur PC pour le mouvement 2D (haut pour sauter/grimper, bas pour s'accroupir, gauche/droite pour le déplacement horizontal). Des contrôles tactiles (swipe, joystick virtuel, tap-to-move) sont envisagés pour Android. Le Blob bénéficie d'une **physique molle (soft body physics)**, lui permettant de s'écraser, s'étirer et rebondir de manière réaliste.

### 3.2. Déformation et Mutations
La mécanique phare. Chaque collision avec un obstacle spécifique déclenche une mutation modifiant les attributs du Blob. Ces mutations peuvent être temporaires ou permanentes pour le niveau en cours.

**Exemples d'attributs mutables :**
* **Taille :** Réduction (pour conduits étroits) ou Grossissement (pour activer plateformes).
* **Vitesse :** Ralentissement (danger) ou Accélération (échapper aux pièges).
* **Élasticité / Rebond :** Adhérence aux murs ou rebond plus haut.
* **Viscosité / Adhérence :** Perte d'adhérence ou capacité à s'accrocher aux murs.
* **Densité / Poids :** Lévitation ou capacité à couler/activer interrupteurs lourds.
* **Propriétés Spéciales :** Luminosité, Absorption, Résistance (envisagées pour des défis avancés).

Des indicateurs visuels (couleur, particules) et sonores informent le joueur de l'état du Blob.

### 3.3. Salles Évolutives (Niveaux)
Chaque niveau est une "salle" de laboratoire, introduisant de nouvelles mécaniques et les combinant de manière complexe.
**Exemples de mécaniques de niveau :** Portes à forme, pièges temporels, zones de mutation spontanée, lasers, plateformes mouvantes, puzzles lumineux, courants d'air/eau, et des drones de sécurité ennemis.

### 3.4. Sauvegarde et Progression
* **Sauvegarde automatique** à la fin de chaque niveau réussi.
* **Checkpoints ("Stations de Recombinaison")** par niveau, permettant de réapparaître avec la forme du Blob au moment de l'activation.
* Option de réinitialiser le niveau.

---

## 🎨 Direction Artistique & Ambiance Sonore

### Direction Artistique
* **Univers :** Laboratoire futuriste sombre et stérile, contrastant avec les couleurs vives et organiques du Blob.
* **Visuels :** Tubes de confinement, câbles, écrans glitchés, lumières néon.
* **Palette de Couleurs :** Dominance de gris froids, bleus profonds, blancs cassés pour l'environnement ; touches de couleurs vives (verts acides, roses, oranges) pour les éléments interactifs et les différentes formes du Blob.
* **Graphismes :** Style minimaliste, vectoriel, avec des formes géométriques simples pour l'environnement et des formes organiques/fluides pour le Blob. Le Blob est semi-transparent avec un cœur lumineux changeant de couleur.
* **Effets Visuels :** Animations molles du Blob, particules lors des mutations, effets de lumière dynamiques, feedback visuel clair des interactions.
* **UI :** Épurée, futuriste, minimaliste, avec des icônes simples et un HUD clair.

### Ambiance Sonore
* **Musique :** Ambiance électro-nerveuse et glitchée, avec des synthétiseurs froids et des rythmes irréguliers, s'intensifiant ou s'adoucissant selon la situation.
* **Effets Sonores (SFX) :** Bruits de "squish" et "splat" pour le Blob, sons distincts pour les mutations, bruits des pièges, portes, lasers, et un son satisfaisant pour les collectibles.
* **Voix Robotiques :** Annonces automatisées du laboratoire ("Anomalie détectée...", "Protocole de confinement activé...") pour renforcer l'ambiance dystopique.

---

## 🛠️ Architecture Technique

* **Moteur de Jeu :** Unity 2022+
* **Langage :** C#
* **Organisation du Projet :** Structure de dossiers logique (`Assets/Scenes`, `Assets/Scripts/Player`, `Assets/Prefabs/Environment`, `Assets/Audio/Music` etc.).
* **Gestion de Version :** Git avec un fichier `.gitignore` adapté pour Unity (excluant `Library/`, `Temp/`, etc.).
* **Optimisation :** Utilisation de pools d'objets, optimisation des appels de dessin.
* **Design Patterns :** Singleton, Observer/Event-driven.

---

## 🗺️ Conception des Niveaux (Level Design)

* **Apprentissage Progressif :** Introduction graduelle des mécaniques.
* **Clarté Visuelle :** Communication claire des interactions et dangers.
* **Équilibre :** Chaque niveau équilibre puzzle, plateforme et survie.
* **Structure :** Chaque niveau est une salle fermée avec entrée, sortie, chemin principal, chemins alternatifs/secrets, obstacles de mutation, pièges, éléments de puzzle et checkpoints.
* **Progression :** Niveaux regroupés en zones thématiques (ex: "Zone de Confinement Basique", "Laboratoire de Mutagénèse Avancée"), augmentant la complexité et combinant les mutations.

**Exemples de Niveaux :**
* "Le Couloir Rétractable" (gestion de la taille)
* "La Course Contre la Montre" (gestion de la vitesse)
* "Le Labyrinthe Lumineux" (utilisation de propriétés spéciales)
* "La Chute Libre Contrôlée" (ajustement de la densité)

---

## 🚀 Roadmap Simplifiée (MVP)

* **Phase 1 (Prototype Minimal) :** Création projet Unity, Blob basique (mouvement, saut), niveau test simple.
* **Phase 2 (Mutations & Obstacles Simples) :** Gestion des attributs, 2-3 types de mutations et obstacles, 2-3 niveaux.
* **Phase 3 (Style Graphique & UI) :** Affinage visuel du Blob et environnement, VFX, UI (écrans, HUD), premières musiques/SFX.
* **Phase 4 (Système de Sauvegarde & Menu) :** Sauvegarde progression, checkpoints avancés, sélection de niveau, options.
* **Phase Finale (Tests, Équilibrage, Build) :** Conception niveaux restants (10-15 pour MVP), tests approfondis, équilibrage, optimisation, finalisation audio, build PC (et Android).

---

## 🎁 Bonus Optionnels (Post-MVP)

* **Système de Skins :** Débloquer de nouvelles apparences pour le Blob.
* **Mode Infini :** Salle générée aléatoirement avec difficulté croissante.
* **Leaderboard Local :** Enregistrer les meilleurs temps/scores.
* **Nouveaux Types de Mutations et Obstacles :** Extension continue du gameplay.
* **Mode "Challenge" :** Niveaux avec des contraintes spécifiques.

---

## 🤝 Contribution

Ce projet est développé par [Ton Nom/Nom de l'équipe].

Si vous souhaitez contribuer ou avez des questions, n'hésitez pas à ouvrir une issue ou à nous contacter.

---

## 📄 Licence

[À ajouter si tu as choisi une licence spécifique, par exemple : MIT, GPL, etc.]

---

**Développé avec Unity.**
