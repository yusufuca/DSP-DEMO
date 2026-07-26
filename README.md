# \# DSP-DEMO — Procedural Room Acoustics \& Source Occlusion Engine

# 

# A real-time system built in \*\*Unity (C#)\*\* with \*\*FMOD Studio\*\* that generates reverb and occlusion from a room's actual geometry and materials, instead of relying on pre-placed reverb zones or hand-tuned trigger volumes.

# 

# \## What it does

# 

# Every frame, the system detects the room the player is standing in, reads its volume, wall distances, and material properties, and calculates the reverb parameters from that data. There are no manually authored reverb presets per room — the room itself is the source of truth.

# 

# \## Core systems

# 

# \- \*\*Room detection\*\* — a flood-fill algorithm scans outward from the player's position, cell by cell, until it hits walls on all sides, and stores the result as a `RoomData` object (bounds, occupied cells, portals to neighboring rooms). Each room is scanned once and cached, keyed by grid cell, so re-entering a known room is a lookup rather than a recompute.

# 

# \- \*\*Reverb model\*\* — reverb time is derived from volume × material hardness × a size parameter. Diffusion comes from surface jaggedness plus a room-volume bonus. Room height is handled separately from floor area and feeds into late reflections and diffusion independently. High-cut is a function of hardness, further reduced by an air-absorption factor. Low gain has a base value from hardness plus a boundary-proximity bonus near walls. All parameters map directly to FMOD snapshot parameters (reverb time, early/late delay, diffusion, density, HF decay, high-cut, low gain), updated live.

# 

# \- \*\*Validation methodology\*\* — the model is checked against a set of controlled test houses, each isolating a single variable: same size / different materials, same material / different sizes, same footprint / different ceiling heights. Beyond the test houses, a seeded procedural house generator assembles rooms at runtime from a random seed, to confirm the model generalizes beyond hand-built scenes.

# 

# \- \*\*Source occlusion\*\* — per-source raycasting toward the listener; frequency, volume, and pan shift toward an occluded target value when a source is blocked, with early/late reflection timing derived from the speed of sound.

# 

# \- \*\*Debug tooling\*\* — a live HUD (reverb parameters, room stats) and in-editor `RoomVisualizer` gizmos showing detected cells and portals.

# 

# \## Status

# 

# Portal-based aux routing between adjacent rooms and outdoor/open-space handling are currently disabled — both have known bugs and are being reworked. Core room detection, the reverb model, and per-source occlusion are stable.

# 

# \## Stack

# 

# Unity 2022.3, C#, FMOD Studio.

# 

# \## Development notes

# 

# The system architecture, the reverb model, and the test-house validation approach are my own design. Implementation was done iteratively with AI pair-programming (mainly for Unity/C# specifics, since this was my first C# project) and reference material from DSP/audio programming forums — most heavily on the DSP math in the reverb model and on non-audio scaffolding (UI, combat, spawning) used to test the system in a playable scene. I can walk through and explain any part of this repository on request.

