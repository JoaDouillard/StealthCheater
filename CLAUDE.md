# 🤖 CLAUDE.md - État du Projet StealthCheater

**Dernière mise à jour:** 8 Janvier 2026
**Session actuelle:** Système caméra terminé - Prêt pour gameplay

---

## 📋 OBJECTIF DU PROJET

**Genre:** Jeu de stealth/puzzle
**Concept:** Un étudiant doit copier sur un camarade pendant un examen sans se faire repérer par le professeur.

**Unity Version:** Unity 6000.2.7f2
**Pipeline:** Built-in Render Pipeline
**Input System:** New Input System

---

## 🏗️ ARCHITECTURE ACTUELLE

### Structure des Scripts

```
Assets/Scripts/
├── AI/
│   ├── Student.cs
│   └── Teacher/
│       ├── TeacherAI.cs          (Orchestrateur principal)
│       ├── TeacherPatrol.cs      (Gestion points de patrouille)
│       ├── TeacherMovement.cs    (NavMeshAgent + états)
│       ├── TeacherLookAt.cs      (Rotation/regard)
│       └── TeacherDetection.cs   (Champ de vision)
│
├── Player/
│   ├── PlayerController.cs       (Déplacement WASD + Crouch)
│   └── PlayerAnimationController.cs
│
├── Managers/
│   ├── GameManager.cs            (États du jeu)
│   ├── LevelManager.cs           (Gestion configurations)
│   ├── LevelSpawner.cs           (Activation niveaux)
│   ├── LevelConfiguration.cs     (ScriptableObject configs)
│   ├── LevelGenerator.cs
│   ├── UIManager.cs
│   ├── CameraFollow.cs           (Caméra First/Third Person)
│   └── CameraOcclusionHandler.cs (Transparence murs)
│
├── Gameplay/
│   ├── CopyZone.cs
│   ├── ReturnZone.cs
│   └── PlayerSpawn.cs
│
├── Debug/
│   └── DebugFreeCameraManager.cs (NoClip - touche F)
│
└── UI/
    ├── MainMenuManager.cs
    └── PauseMenuManager.cs
```

---

## ✅ FONCTIONNALITÉS IMPLÉMENTÉES (≈40%)

### Player
- ✅ Déplacement WASD/Flèches avec CharacterController
- ✅ Système Crouch (Ctrl maintenu)
- ✅ Animations: Idle, Walk, Crouch_Walk
- ✅ Blend Tree 2D configuré
- ✅ Input System configuré

### Teacher AI (≈80% fait)
- ✅ Navigation autonome NavMesh
- ✅ Patrouille avec points aléatoires dans zone définie
- ✅ Points d'intérêt (Board, Windows) avec snap automatique
- ✅ Probabilités: 70% NavMesh / 15% Board / 15% Window
- ✅ Système anti-répétition (ne revient pas au même point)
- ✅ Arrêts aléatoires (2-5s)
- ✅ Détection joueur avec champ de vision 90°
- ✅ Système multi-zones de détection (zone1: 8m/5s, zone2: 6m/3s, zone3: 2m/immédiat)
- ✅ Modificateur crouch (distance -25%)
- ✅ Scripts séparés pour chaque responsabilité

### Système Multi-Niveaux
- ✅ LevelConfiguration (ScriptableObjects pour configs par niveau)
- ✅ LevelManager (charge et gère les configurations)
- ✅ LevelSpawner (active/désactive les props selon le niveau)
- ✅ Support de 4 niveaux (0-3)

### Caméras
- ✅ CameraFollow avec 2 modes (First/Third Person) - Toggle V
- ✅ CameraOcclusionHandler (transparence des murs)
- ✅ DebugFreeCameraManager (NoClip debug - Toggle F)

### Debug
- ✅ Gizmos visuels (zones patrol, snap points, champ vision)
- ✅ Logs détaillés
- ✅ NoClip camera pour observer

---

## ⚠️ BUGS ACTUELS

### ✅ RÉSOLU - Animator Teacher (IdleVariant)
**Problème:** BlendTree attendait float, le code utilisait int.
**Solution appliquée:** Conversion int → float (0→0.0, 1→0.5, 2→1.0)
**Fichier:** TeacherAI.cs

### ✅ RÉSOLU - First Person Camera
**Problème:** Caméra pas au niveau des yeux, mouvement inversé
**Solution appliquée:**
- Offset Vector3 avec X/Y/Z ajustables (standing/crouching)
- Mouvement relatif à la caméra en First Person
- Rotation du player suit la caméra
- Head + Hair meshes cachés en FP
**Fichiers:** CameraFollow.cs, PlayerController.cs

---

## 🔄 EN STAND-BY - Système de Shader Transparence Circulaire

### 📝 Description du besoin
**Contexte:** En Third Person, quand un objet (mur, etc.) passe entre la caméra et le player.

**Objectif:** Rendre transparent **UNIQUEMENT** la partie de l'objet qui se trouve dans un cercle de ~2m autour du nombril/torse du player.

**Contraintes importantes:**
- ❌ **PAS** tout l'objet qui devient transparent
- ✅ **SEULEMENT** la partie dans le cercle (transparence pixel par pixel)
- ✅ Transition douce entre opaque et transparent
- ✅ Pas de "rémanence" ou effet bizarre
- ✅ Shader propre et performant

### 📊 État actuel
**Statut:** ⏸️ DÉSACTIVÉ TEMPORAIREMENT - Cause des crashes Unity

**Raison:** Priorité au gameplay fonctionnel d'abord.

**Fichiers concernés:**
- `/Assets/Shaders/TransparentWithMask.shader` (commenté)
- `/Assets/Scripts/Managers/CameraOcclusionAdvanced.cs` (commenté)

### 🛠️ Implémentation tentée

**Approche:** Shader custom avec masque sphérique

**Principe:**
1. Shader reçoit la position du centre du cercle (nombril) via `_MaskCenter`
2. Pour chaque pixel, calcule la distance au centre
3. Si pixel < `_MaskRadius` (2m) → Rend transparent avec fade
4. Sinon → Opaque normal

**Paramètres du shader:**
- `_MaskCenter` (Vector3) - Position world du nombril
- `_MaskRadius` (Float) - Rayon du cercle (défaut: 2.0)
- `_MinAlpha` (Float 0-1) - Transparence min dans le cercle (défaut: 0.3)
- `_FadeDistance` (Float) - Distance de transition douce (défaut: 0.5)

**Script C#:**
- Détecte objets entre caméra et player (RaycastAll)
- Applique dynamiquement le shader avec matériaux temporaires
- Update `_MaskCenter` chaque frame vers position du nombril
- Restaure matériaux originaux quand objet n'est plus occlusif

### ⚠️ Problèmes rencontrés
1. **Violet/Rose:** Material apparaissait violet (shader ne compile pas correctement)
2. **Crash Unity:** Cause non identifiée, peut-être lié au shader ou aux matériaux dynamiques
3. **Tout l'objet transparent:** Malgré le shader, tout l'objet devenait transparent (problème non résolu)

### 🔍 À investiguer plus tard
- Vérifier compatibilité shader avec Built-in Render Pipeline
- Peut-être utiliser URP/HDRP avec Shader Graph pour plus de stabilité
- Alternative: Decal Projector system
- Alternative simple: Découper manuellement les gros objets en sections de 5-10m

### 📅 Quand réimplémenter
**Après:** Gameplay core fonctionnel (copier, détection, game over, win, etc.)

**Note:** Ce système est un "nice to have" pour le polish visuel, pas critique pour le gameplay.

---

### 🟡 PROBLÈME - Third Person Camera

**Problème rapporté:** "Je ne sais pas comment bien la positionner"

**Configuration actuelle** (`CameraFollow.cs:14`):
```csharp
[SerializeField] private Vector3 thirdPersonOffset = new Vector3(0, 17.3f, 10f);
```

**Analyse:**
- Hauteur: 17.3m (très élevé)
- Recul: 10m
- Angle résultant: ≈60° (quasi-isométrique)

**Questions à poser à l'utilisateur:**
- Quel type de vue voulez-vous ?
  - Vue isométrique (actuelle) ?
  - Vue plus proche et dynamique (type jeu d'action) ?
  - Vue intermédiaire ?

**Suggestions selon le type de jeu:**
- **Stealth classique:** Offset `(0, 8, -6)` - angle ~53°, plus proche
- **Isométrique gaming:** Offset `(0, 12, -8)` - angle ~56°
- **Action/Adventure:** Offset `(0, 3, -4)` - angle ~37°, derrière l'épaule

---

## ❌ FONCTIONNALITÉS MANQUANTES (≈60%)

### Priorité 1 - Actions Player
- ❌ Animation Se lever / S'asseoir
- ❌ Interaction Copier (E) avec:
  - Timer 3-5 secondes
  - Barre de progression
  - Animation Reading
  - Feedback UI
- ❌ Interaction Écrire (E) avec:
  - Timer 3-5 secondes
  - Barre de progression
  - Animation Writing
  - Condition de victoire

### Priorité 2 - Élèves/Obstacles
- ❌ Placement élèves dans classe
- ❌ Tag "Student" configuré
- ❌ Colliders et materials
- ❌ Animations variées élèves
- ❌ Système d'animation autonome

### Priorité 3 - UI/HUD
- ❌ Barre progression copie/écriture
- ❌ Indicateurs d'action "Press E"
- ❌ Messages de guidance
- ❌ Écran Game Over avec options
- ❌ Écran Victory avec stats
- ❌ Timer examen (optionnel)

### Priorité 4 - Animations avancées
- ❌ 3 variantes Idle Teacher (animations importées, pas configurées dans Animator)
- ❌ Animation Scolding Teacher (détection)
- ❌ Animation Defeat Player
- ❌ Animations Reading/Writing Player

### Priorité 5 - Polish
- ❌ Audio (musique, SFX pas, voix)
- ❌ Tests et équilibrage
- ❌ Optimisations
- ❌ Build final

---

## 📊 AVANCEMENT GLOBAL: ~40%

| Composant | État | %  |
|-----------|------|-----|
| Player Base | ✅ Complet | 100% |
| Teacher AI | 🟡 Fonctionnel, bugs à fixer | 80% |
| Actions Gameplay | ❌ Non implémenté | 0% |
| Élèves/Obstacles | ❌ Non implémenté | 0% |
| UI/HUD | ❌ Minimal | 10% |
| Animations | 🟡 Basiques faites | 50% |
| Caméras | 🟡 Implémenté, bugs | 70% |
| Audio | ❌ Rien | 0% |
| Level Design | 🟡 Structure faite | 30% |

---

## 🎯 TÂCHE ACTUELLE (5 Jan 2026)

### Objectif: Corriger le système de caméra

**Problèmes à résoudre:**
1. ✅ First Person - Ne donne pas vraiment la vision du player
2. ✅ Third Person - Positionnement à améliorer

**Prochaines étapes:**
1. Analyser les préférences utilisateur pour Third Person
2. Corriger First Person (offset Z à 0)
3. Proposer plusieurs presets pour Third Person
4. Tester les deux modes
5. (Optionnel) Implémenter "Head" Transform pour FP

---

## 📚 DOCUMENTATION DISPONIBLE

### Dans `/Ressources/`
- **CahierDesCharges/**
  - `00_RESUME_PROJET.md` - Vue d'ensemble
  - `01_TEACHER_SPECIFICATIONS.md` - Specs complètes Teacher
  - `01_TEACHER_INTERACTIONS.md`
  - `Cahier des chargeN1.txt` - Cahier initial
  - `Cahier_des_charges_V2_DETAILLE.md` - Version détaillée

- **guide/**
  - `QUICK_START_GUIDE.md` - Setup rapide 1 niveau
  - `ANIMATOR_SETUP_GUIDE.md`
  - `CAMERA_SETUP_GUIDE.md`
  - `MIXAMO_ANIMATION_FIX.md`
  - `SETUP_MULTI_LEVELS.md`
  - `TESTING_GUIDE.md`
  - `AGENTS.md`

- **Autres:**
  - `ROADMAP.md` - Features TODO et envisagées
  - `ERREUR.png` / `Erreur1.png` - Captures bugs

---

## 🔧 CONFIGURATIONS IMPORTANTES

### Tags requis
- `Player` - Le joueur
- `Teacher` - Le professeur
- `Student` - Les élèves
- `Board` - Le tableau
- `Window` - Les fenêtres

### Input Actions (InputSystem_Actions.inputactions)
- **Player/Move** - WASD déplacement
- **Player/Crouch** - Ctrl s'accroupir
- **Player/Look** - Souris (first person)
- **Player/ToggleCamera** - V (toggle FP/TP)
- **Debug/NoClip** - F (caméra libre)

### Layers
- Default
- (À définir selon besoins)

---

## 💡 NOTES POUR MOI (CLAUDE)

### Points de vigilance
1. **Ne jamais détruire le code existant** - L'utilisateur a insisté dessus
2. **Fixer les bugs d'abord** avant d'ajouter des features
3. **Utiliser le système de configuration** - Tout passe par LevelConfiguration
4. **Respecter l'architecture séparée** - Ne pas tout mettre dans un seul script
5. **Documenter dans `/Ressources/`** - Ne pas polluer la racine

### Organisation documentation
- **`CLAUDE.md`** - Uniquement à la racine (ce fichier)
- **Cahiers des charges** → `/Ressources/CahierDesCharges/`
- **Guides utilisateur** → `/Ressources/guide/`
- **Notes techniques** → Créer `/Ressources/technical/` si besoin

### Système multi-niveaux
- LevelConfiguration = ScriptableObject avec toutes les configs
- LevelManager charge la config active
- Teacher, Detection, Patrol lisent depuis LevelManager.Instance
- Chaque niveau peut avoir ses propres paramètres

### Pattern de code observé
- Scripts séparés par responsabilité
- Initialisation dans `Start()` après que les managers soient prêts
- Logs détaillés avec `[NomScript]` prefix
- Gizmos pour debug visuel
- Configuration centralisée via ScriptableObjects

---

## 🚀 PROCHAINES SESSIONS

### Après correction caméras
1. Fixer bug Animator Teacher (IdleVariant)
2. Implémenter actions Player (Copier/Écrire)
3. Placer et configurer les élèves
4. Implémenter UI de base
5. Tests complets de la boucle de gameplay

---

**Créé par:** Claude (Assistant IA)
**Pour:** Tracking de l'avancement et compréhension du projet
**Mis à jour:** À chaque session de travail significative
