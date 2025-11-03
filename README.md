# Project Pursuit  
*An mixed-reality experience combining VR and real world projection.*

---

## Concept

**Project Pursuit** is an immersive **two-player mixed-reality car chase** experience.

- **Player 1 – The Driver** sits inside a **futuristic car** wearing an **HTC Vive headset**, using a **physical steering wheel** to escape a pursuing helicopter.  
- **Player 2 – The Helicopter Operator** walks around the room with a **tracked projector**, searching the environment from above.

The game merges **virtual and physical worlds**, projecting live visuals into real space.

---

## Core Features

### Dual-Player Gameplay
- **Driver (VR):** Immersed in a car cockpit through the HTC Vive headset.  
- **Helicopter Operator:** Walks physically, using a **Vive Tracker**-attached **projector** to “scan” the room.

### Custom Volumetric Fog 
Implemented with Unity’s **URP Render Feature**

The **Volumetric Fog** combines **baked lighting** and **raymarching** to simulate light scattering inside fog volumes.
1. During baking, lighting data is sampled from **Unity’s Light Probes** using **spherical harmonics (SH)** evaluation.  
2. This lighting information is stored inside a **3D texture** (the *Baked Light Volume*), representing the ambient and directional illumination at each voxel in world space.  
3. At runtime, the fog shader **raymarches** through this volume, sampling the precomputed lighting values while accumulating **scattering** and **transmittance** along the camera ray.  
4. The result is a volumetric fog that reacts to light depth and color, and it remains efficient for **real-time VR rendering**.

### Virtual Projection Mapping
- Real-time **projection alignment** with physical tracking.  
- Handles **spatial calibration**, and **depth-based visibility**.  
- Ensures consistent visuals between VR headset and projector output.

###  Mixed Input & Output Modalities
- **Inputs:** VR headset tracking, steering wheel, projector motion.  
- **Outputs:** Head-mounted display, real-world floor projection.  
- **Cross-Reality Feedback:** Both players influence the same virtual environment.

---

## Hardware Setup

| Component | Role |
|------------|------|
| PC | Runs Unity (VR, projector rendering). |
| HTC Vive Headset | Main VR experience for the driver. |
| HTC Vive Tracker | Tracks projector’s position/orientation. |
| Acer Projector | Projects the helicopter spotlight onto real surfaces. |
| Steering Wheel | Provides physical driving control. |
| Base Stations | Enable accurate spatial tracking. |

---

**Technologies:** Unity URP, SteamVR, HLSL  
**Hardware:** HTC Vive, Vive Tracker, Acer Projector, Logitech Steering Wheel  


