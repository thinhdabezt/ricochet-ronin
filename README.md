# Ricochet Ronin

## 1. High Concept
**"Billiards meets Samurai"**.

Ricochet Ronin is a high-speed action game with a tactical layer. The player controls a samurai who cannot run and can only launch themselves like a billiard ball to slice through enemies.

- **Genre:** Top-down Action / Puzzle / Physics-based
- **Platform:** PC (mouse) and Mobile (touch), with support planned for both
- **Suggested Art Style:** Neon Noir or Minimalist

## 2. Core Gameplay Loop
The gameplay rhythm is: **Still (Aim) -> Motion (Dash) -> Still (Aim)**.

### Aim Phase
- Hold mouse/finger to enter slow-motion (or full time-stop).
- Drag in the opposite direction of the intended movement (slingshot mechanic).
- Show a `Trajectory Line` to predict the movement path and wall ricochets using Raycast.

### Dash Phase
- Release input to launch at high speed.
- Hit a wall: the character ricochets, preserving most or all momentum.
- Hit an enemy: instant one-hit kill with Slash VFX.

### Reset Phase
- The character stops when friction drains momentum, or after a fixed dash duration.
- Ready for the next slash.

## 3. Key Features

### A. Physics & Movement
- **Drag & Shoot:** Launch direction and power are controlled by drag input.
- **Bouncing:** Use `PhysicsMaterial2D` to control bounce behavior.
- **Friction:** Use `Linear Drag` so the player slides and slows naturally instead of stopping abruptly.

### B. Combat
- **Slice Through:** Enemies die when the player passes through them (`Trigger Collider`).
- **Multi-Kill Combo:** Killing 2 or more enemies in a single dash grants Combo score (Double Kill, Triple Kill, etc.).
- **Hazards:** Spikes or lasers cause immediate death and level restart.

### C. Expansion Mechanics (Later Phase)
- **Mid-air Correction:** During a dash, an extra click can rotate movement direction by 90 degrees.
- **Enemy Types:**
  - Dummy: stationary enemy
  - Patrol: moves back and forth
  - Shielded: only dies when hit from behind

## 4. Technical Architecture
To keep the game scalable, responsibilities are split across modules instead of placing everything in `Player.cs`:

- **InputManager:** Reads mouse/touch input (drag start, drag hold, release) and contains no game logic.
- **PlayerMovement:** Receives input commands and applies force to `Rigidbody2D`.
- **TrajectoryPredictor:** Draws predicted ricochet paths using `LineRenderer` and vector reflection math.
- **GameManager:** Manages game states (`Playing`, `Victory`, `GameOver`).

## References
1. Font: QuinqueFive - https://ggbot.itch.io/quinquefive-font