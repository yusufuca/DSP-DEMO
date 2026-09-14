DSP-DEMO
A real-time procedural acoustics and sound propagation engine built in Unity (C#) and FMOD Studio. It dynamically calculates room volume, material properties, and occlusion on the fly, eliminating the need for manual reverb zones.

Core Systems
Room Detection: Flood-fill grid scanning that caches room bounds, dimensions, and material data into RoomData objects.

-Procedural Reverb Model: Derives FMOD parameters (reverb time, diffusion, HF decay, EQ) mathematically from room volume, surface hardness, and wall proximity.

-Source Occlusion: Real-time raycasting that dynamically shifts frequency, volume, and pan when obstacles block the audio source.

-Validation & Testing: Tested against controlled test houses and a runtime procedural house generator.

Tech Stack
Engine: Unity 2022.3 (C#)

Middleware: FMOD Studio
