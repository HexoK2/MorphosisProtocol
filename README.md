# 🎮 Morphosis  *(Project in development...)*

Welcome to the GitHub repository of **Morphosis**!

Morphosis is a 3D isometric arcade-puzzle game with platforming and survival elements, where adaptation is key. You play as a gelatinous organism that can deform in reaction to obstacles to escape from a secret biotechnology lab.

---

## ✨ Game Overview

**Title:** Morphosis  
**Genre:** Single-player, 3D Arcade-Puzzle, Platformer, Survival  
**Target Platforms:** PC (Windows, macOS, Linux), Android (optional)  
**Playtime:** Short levels (3–5 minutes), full game estimated at 2–3 hours (20–30 levels)  
**Target Audience:** Casual players, puzzle and platforming fans, people who enjoy unique mechanics and experimentation

### 🌟 Unique Selling Proposition (USP)

The core mechanic of Morphosis is the **dynamic deformation of the character** in response to collisions with the environment. Each obstacle triggers a mutation that alters the Blob's attributes (size, speed, elasticity, etc.), forcing the player to adapt constantly and rethink their strategy.  
A collision isn’t always a failure — it's often an opportunity for transformation!

---

## 📖 Concept & Story

You play as a **"Blob"**, a semi-gelatinous experimental entity trying to escape from a hostile lab built to test and contain such organisms. Guided by its primal survival instinct, the Blob must make its way through trap-filled rooms, using forced mutations to overcome challenges.

**Core Gameplay Loop:**
1. Enter a new "room" (level).  
2. Identify obstacles and the required properties to overcome them.  
3. Move strategically to avoid or take advantage of mutations.  
4. Reach the exit by adapting to your changing form.  
5. Optionally collect "mutation points" or "DNA fragments".

**Main Theme:** Adaptation, survival, and the unpredictable nature of forced evolution.

---

## ⚙️ Core Mechanics

### 3.1. Character Movement (The Blob)
The Blob is controlled using **arrow keys / ZQSD / WASD** for 2D movement (up to jump/climb, down to crouch, left/right to move horizontally). Touch controls (swipe, virtual joystick, tap-to-move) are planned for Android.  
The Blob uses **soft body physics**, allowing it to squash, stretch, and bounce realistically.

### 3.2. Deformation & Mutations
The core mechanic. Each collision with a specific obstacle triggers a mutation that alters the Blob's attributes. These mutations may be temporary or persistent for the current level.

**Examples of mutable attributes:**
* **Size:** Shrink (to crawl through vents) or Enlarge (to activate pressure plates)
* **Speed:** Slow down (danger) or accelerate (to escape traps)
* **Elasticity / Bounce:** Stick to walls or bounce higher
* **Viscosity / Grip:** Lose grip or gain wall-clinging abilities
* **Density / Weight:** Float or become heavy enough to trigger weight switches
* **Special Traits:** Luminescence, Absorption, Resistance (planned for advanced puzzles)

Visual and audio cues (color, particles, sounds) inform the player of the Blob’s current state.

### 3.3. Evolving Rooms (Levels)
Each level is a "lab room" introducing new mechanics and combining them in complex ways.  
**Examples:** shape-sensitive doors, timed traps, spontaneous mutation zones, lasers, moving platforms, light-based puzzles, wind/water currents, and hostile security drones.

### 3.4. Save & Progression
* **Auto-save** at the end of each completed level  
* **Checkpoints ("Recombination Stations")** per level, allowing respawn in the Blob’s current form  
* Option to reset/restart the level at any time

---

## 🎨 Art Direction & Audio Atmosphere

### Art Direction
* **World:** Dark, sterile futuristic lab, contrasting with the vivid organic colors of the Blob  
* **Visuals:** Containment tubes, cables, glitchy screens, neon lighting  
* **Color Palette:** Cold grays, deep blues, off-whites for the environment; bright colors (acid green, pinks, oranges) for interactive elements and Blob forms  
* **Graphics:** Minimalist, vector-based style with geometric environments and smooth, organic Blob animations  
* **Visual Effects:** Squishy Blob animations, mutation particles, dynamic lighting, and clear interaction feedback  
* **UI:** Clean, futuristic, minimalist — simple icons and a clear HUD

### Sound Design
* **Music:** Nervous, glitchy electro with cold synths and irregular rhythms, intensifying or relaxing based on the situation  
* **SFX:** "Squish" and "splat" sounds for the Blob, distinct mutation audio cues, trap/door/laser sounds, satisfying collectible effects  
* **Robotic Voices:** Lab system announcements ("Anomaly detected...", "Containment protocol activated...") to reinforce the dystopian atmosphere

---

## 🛠️ Technical Architecture

* **Game Engine:** Unity 2022+  
* **Language:** C#  
* **Project Structure:** Clean folder structure (`Assets/Scenes`, `Assets/Scripts/Player`, `Assets/Prefabs/Environment`, `Assets/Audio/Music`, etc.)  
* **Version Control:** Git with a Unity-optimized `.gitignore` (excluding `Library/`, `Temp/`, etc.)  
* **Optimization:** Object pooling, draw call reduction  
* **Design Patterns:** Singleton, Observer / Event-driven architecture

---

## 🗺️ Level Design

* **Progressive Learning:** Gradual introduction of mechanics  
* **Visual Clarity:** Clear communication of interactions and hazards  
* **Balance:** Each level mixes puzzle-solving, platforming, and survival  
* **Structure:** Each level is a closed room with an entry, exit, main path, optional/secret paths, mutation zones, traps, puzzles, and checkpoints  
* **Progression:** Levels grouped into themed zones (e.g. "Basic Containment", "Advanced Mutagenesis Lab") with increasing complexity and mutation combinations

**Sample Levels:**
* "The Retractable Hallway" (size-based navigation)  
* "The Time Trial" (speed mutation mechanics)  
* "The Luminous Maze" (use of special properties)  
* "Controlled Freefall" (density mutation control)

---

## 🚀 Simplified Roadmap (MVP)

* **Phase 1 (Minimal Prototype):** Unity project setup, basic Blob movement/jump, simple test level  
* **Phase 2 (Mutations & Simple Obstacles):** Attribute system, 2–3 mutation types and obstacle mechanics, 2–3 test levels  
* **Phase 3 (Graphics & UI):** Polishing Blob/environments, VFX, UI (menus, HUD), first music and sound effects  
* **Phase 4 (Save System & Menu):** Save progression, advanced checkpoints, level selection, options menu  
* **Final Phase (Testing & Build):** Finish remaining levels (10–15 for MVP), full testing, balancing, optimization, audio finalization, PC (and Android) build

---

## 🎁 Optional Bonuses (Post-MVP)

* **Skin System:** Unlockable Blob appearances  
* **Endless Mode:** Randomly generated survival room with increasing difficulty  
* **Local Leaderboard:** Record best times/scores  
* **New Mutation & Obstacle Types:** Ongoing gameplay expansion  
* **Challenge Mode:** Levels with unique constraints or objectives

---

## 🤝 Contributing

This project is being developed by [K2 (me.)].

If you’d like to contribute or have any questions, feel free to open an issue or reach out.

---

**Developed with Unity.**
