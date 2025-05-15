# Era Code - Unity Top-Down Shooter

![Era Code Banner](https://github.com/ThomasClaiborne/EraCode/blob/main/Images/PlayerPistolImage.png)

## 📝 Overview
Era Code is a top-down defense shooter where players defend against waves of diverse robot enemies in intense fixed-position combat. Players have access to various weapon categories including SMGs, ARs, pistols, rocket launchers, and LMGs, each with unique characteristics and behaviors.

## 🎮 Key Features

### Weapon System
- **Modular Weapon Framework**: Highly configurable weapon properties including damage, fire rate, and magazine capacity
- **Special Slot System**: First slot reserved for infinite-ammo pistols as reliable fallback options
- **Visual Feedback**: Distinct bullet tracers for different weapon types providing visual combat cues
- **Dynamic Weapon Shop**: Allows purchasing, upgrading, and managing your arsenal

### Economy & Progression
- **Synthium Currency**: Resource earned through combat used to purchase upgrades and ammunition
- **Persistent Inventory**: Save/load system tracking unlocked weapons, loadouts, and currency

### Enemy AI
- **Wave Spawning System**: Configurable spawn intervals and waypoints for varied enemy encounters
- **Intelligent Positioning**: Advanced attack point system with front/back row positions to prevent enemy clustering
- **Diverse Enemy Types**: Multiple enemy behaviors featuring different attack patterns and projectile types

## 🛠️ Technical Implementation

### Architecture
The game is built with a component-based architecture focusing on:
- **Data-Driven Design**: Weapon properties and wave configurations stored in ScriptableObjects
- **System Decoupling**: Clean separation between weapon, economy, and enemy AI systems
- **Optimized Spawning**: Streamlined spawn interval configuration for improved performance

### Code Highlights
- Optimized waypoint-based enemy pathing system
- Queue-based position management for enemy distribution
- Persistent save/load system for game progression
- Highly extensible weapon configuration framework

## 🎯 Development Scope
Era Code was developed as a solo project during the Devtober game jam over a 4-week period. The project demonstrates proficiency in:
- C# programming in Unity
- Gameplay systems design and implementation
- Enemy AI behavior programming
- Performance optimization techniques

## 📸 Screenshots & Media

![Gameplay Screenshot 1](https://github.com/ThomasClaiborne/EraCode/blob/main/Images/Screenshot%202025-05-15%20070921.png)
*Wave combat with multiple enemy types*

![Gameplay Screenshot 2](https://github.com/ThomasClaiborne/EraCode/blob/main/Images/Screenshot%202025-05-15%20071141.png)
*Weapon selection and shop interface*

## 🎥 Demo Video
[![Era Code Gameplay](https://img.youtube.com/vi/C6hz4RMnxmM/0.jpg)](https://youtu.be/C6hz4RMnxmM)

## 🚀 Installation & Controls
1. Clone this repository
2. Open with Unity 2021.3.x or newer
3. Play the main scene in the Scenes folder

**Controls:**
- WASD: Move
- Mouse: Aim
- Left-Click: Shoot
- Number Keys (1-5): Switch weapons
- R: Reload
- E: Interact with shop
- Tab: Open inventory

## 🧰 Tools & Technologies Used
- Unity 2021.3
- C# Programming
- Cinemachine for camera work
- Unity's NavMesh for AI pathfinding
- Custom shader development for weapon effects

## 🗺️ Future Development
- Additional weapon types and customization options
- More diverse enemy types with specialized behaviors
- Level progression system with multiple environments
- Boss battles with unique mechanics

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.

## 👨‍💻 About the Developer
Created by [Thomas Claiborne](https://thomasclaiborne.github.io) as part of his game development portfolio. Connect on [LinkedIn](https://www.linkedin.com/in/trc3/) or check out more projects on [GitHub](https://github.com/ThomasClaiborne).
