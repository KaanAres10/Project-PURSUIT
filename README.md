*An mixed-reality experience combining VR and real world projection.*

---

## Concept

**Project Pursuit** is an immersive **two-player mixed-reality car chase** experience.

- **Player 1 – The Driver** sits inside a **futuristic car** wearing an **HTC Vive headset**, using a **physical steering wheel** to escape a pursuing helicopter.  
- **Player 2 – The Helicopter Operator** walks around the room with a **tracked projector**, searching the environment from above.

The game merges **virtual and physical worlds**, projecting live visuals into real space.

---

## Video
<video muted loop playsinline controls style="width:100%; height:auto; border-radius:8px;">
  <source src="https://github.com/user-attachments/assets/1abfa2cb-0950-45c6-86e7-18bad82fd586" type="video/mp4">
  Your browser does not support the video tag.
</video>
---

## Core Features

### Dual-Player Gameplay
- **Driver (VR):** Immersed in a car cockpit through the HTC Vive headset.

- **Helicopter Operator:** Walks physically, using a **Vive Tracker**-attached **projector** to “scan” the room.
![Projector with Tracker](https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/projector_tracker.jpg)

### Custom Volumetric Fog 
Implemented with Unity’s **URP Render Feature**

The **Volumetric Fog** combines **baked lighting** and **raymarching** to simulate light scattering inside fog volumes.
1. During baking, lighting data is sampled from **Unity’s Light Probes** using **spherical harmonics (SH)** evaluation.  
2. This lighting information is stored inside a **3D texture** (the *Baked Light Volume*), representing the ambient and directional illumination at each voxel in world space.  
3. At runtime, the fog shader **raymarches** through this volume, sampling the precomputed lighting values while accumulating **scattering** and **transmittance** along the camera ray.  
4. The result is a volumetric fog that reacts to light depth and color, and it remains efficient for **real-time VR rendering**.
![Volumetric Fog](https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/Volumetric_Fog.png)


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

| Component | Role | Image |
|------------|------|-------|
| **PC** | Runs Unity (VR and projector rendering). |  |
| **HTC Vive Headset** | Main VR experience for the driver. | <img src="https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/vive.png" width="100"/> |
| **HTC Vive Tracker** | Tracks projector’s position/orientation. | <img src="https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/tracker.png" width="100"/> |
| **Acer Projector** | Projects the helicopter spotlight onto real surfaces. | <img src="https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/projector.png" width="100"/> |
| **Steering Wheel** | Provides physical driving control. | <img src="https://media.githubusercontent.com/media/KaanAres10/Project-PURSUIT/web/media/steer.png" width="100"/> |
| **Base Stations** | Enable accurate spatial tracking. |  |
---

**Technologies:** Unity URP, SteamVR, HLSL  
**Hardware:** HTC Vive, Vive Tracker, Acer Projector, Logitech Steering Wheel  

----
## BTS
<video muted loop playsinline controls style="width:100%; height:auto; border-radius:8px;">
  <source src="https://github.com/user-attachments/assets/7fa8ed8e-74ae-4a9c-ba31-6f6ea94fd514" type="video/mp4">
  Your browser does not support the video tag.
</video>
