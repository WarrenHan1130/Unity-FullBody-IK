# Unity Full Body IK System - User Guide

## Introduction

A Unity-based Full Body Inverse Kinematics (IK) system using the FABRIK algorithm, with auto-configuration tools and boxing pose demonstrations.

**Requirements:**
- Unity 2019.4+
- 3D character model with Humanoid rig

---

## 1. Quick IK System Setup

### Step 1: Prepare Character
1. Drag your Humanoid character model into the scene
2. Ensure the character has an `Animator` component configured as **Humanoid** type

### Step 2: Add Components
1. Select the character object
2. Add **FullBodyFABRIK** component
3. Add **FullBodyIKSetup** component
4. In `FullBodyIKSetup`, drag the `FullBodyFABRIK` component to the `Full Body IK` field

### Step 3: Auto Configuration (Click in Order)

#### 1️⃣ Click "1. Create Target Objects"
- Automatically creates 5 colored spheres (left/right hands, left/right feet, head)
- These spheres are IK targets - drag them to control the character
- **Note:** Pole Targets are NOT created automatically - see Boxing Demo section for manual setup

#### 2️⃣ Click "2. Auto Setup End Effectors"
- Automatically configures 5 IK chains
- Links target spheres to bones

#### 3️⃣ Click "3. Setup Common Constraints"
- Automatically adds 18 joint constraints
- Limits movement range of elbows, knees, etc.

### Step 4: Test
1. Click ▶️ **Play button**
2. Drag the colored spheres in Scene view
3. Observe the character's hands, feet, and head follow the movement

✅ **Setup Complete!**

---

## 2. Using Boxing Pose Demo

### Setup Boxing Demo

1. **Add Demo Component**
   - Add **BoxingPoseChanger** component to the character object

2. **Link Target Objects** (Drag from Hierarchy)
   ```
   IK Targets:
   - Left Hand Target     → LeftHandTarget (cyan sphere)
   - Right Hand Target    → RightHandTarget (cyan sphere)
   - Left Foot Target     → LeftFootTarget (green sphere)
   - Right Foot Target    → RightFootTarget (green sphere)
   - Head Target          → HeadTarget (yellow sphere)
   ```

3. **Link Reference Bones**
   ```
   Reference Bones:
   - Head Bone  → Head bone
   - Hips Bone  → Hips bone
   ```

4. **(Optional) Create Pole Targets**
   - **Important:** Pole Targets are NOT created automatically
   - Create 4 empty GameObjects manually: 
     - Right-click in Hierarchy → Create Empty
     - Name them: `LeftElbowPole`, `RightElbowPole`, `LeftKneePole`, `RightKneePole`
   - Place them 0.3-0.5m outside the elbows/knees
   - Drag them to corresponding Pole Target fields in BoxingPoseChanger
   - Purpose: Controls elbow/knee bending direction for more natural poses

### Run Demo

1. **Enter Play Mode**
2. **Press Spacebar** to cycle through poses:
   - **T-Pose** → **Guard** → **Right Punch** → **Left Punch** → **High Kick**
3. Top-left corner shows current pose and transition progress
4. Scene view displays target positions and connection lines

### Demo Parameters

| Parameter | Description | Recommended |
|-----------|-------------|-------------|
| **Transition Speed** | Pose switch speed | 2.0 (range 0.5-5.0) |
| **Pose Position Parameters** | Target coordinates for hands/feet/head | Can be fine-tuned in Inspector |

---

## 3. Parameter Adjustment

### Core Parameters (FullBodyFABRIK Component)

| Parameter | Recommended | Description |
|-----------|-------------|-------------|
| **Main Iterations** | 10-15 | Iteration count - higher is more accurate but more expensive |
| **Sub Iterations** | 3-5 | Sub-iteration count |
| **Tolerance** | 0.01 | Convergence tolerance |
| **Global Weight** | 1.0 | Overall IK strength (0-1) |
| **Rotation Damping** | 0.15 | Rotation damping - prevents jittering |
| **Root Pin Mode** | Initial Pose | Locks Hips from moving |
| **Constraint Mode** | After Convergence | Apply constraints after convergence |

### Constraint Angle Parameters (FullBodyIKSetup Component)

Adjustable before clicking "3. Setup Common Constraints":

| Parameter | Recommended | Description |
|-----------|-------------|-------------|
| **Elbow Max Angle** | 160° | Maximum elbow bend angle |
| **Knee Max Angle** | 130° | Maximum knee bend angle |
| **Upper Arm Max Angle** | 85° | Upper arm swing range |
| **Upper Leg Max Angle** | 65° | Upper leg swing range |
| **Spine Max Angle** | 30° | Spine bend angle |
| **Neck Max Angle** | 30° | Neck rotation range |

---

## 4. Important Notes

### ⚠️ Common Issues

**1. No effect after setup?**
- ✅ IK only works in **Play Mode**
- ✅ Confirm character is **Humanoid** type
- ✅ Check if target spheres are properly linked to End Effectors

**2. Character jittering or unstable?**
- Increase `Rotation Damping` to 0.2-0.3
- Decrease `Max Rotation Speed`
- Check for conflicting IK chains

**3. Wrong elbow/knee bending direction?**
- Add Pole Target and place it in the desired bending direction
- Adjust `Pole Weight` to 0.8-1.0
- Check Hinge constraint axis settings

**4. Hands/feet cannot reach target?**
- IK chain length is shorter than target distance (physical limitation)
- Move target closer
- Increase `Main Iterations` for better accuracy

**5. Root bone (Hips) moving randomly?**
- Set `Root Pin Mode` to `Initial Pose` to lock position

### 📋 Debug Tools

**Visual Gizmos (Scene View):**
- 🔴 Red wireframe sphere - IK target position
- 🟡 Yellow line - Connection from bone to target
- 🟣 Magenta line - Pole Target connection
- 🟢 Green arrow - Initial joint direction (enable Show Constraints)
- 🔴 Red marker - Joint exceeding constraint limits

**Debug Options (FullBodyFABRIK Component):**
- `Show Gizmos` - Display visualizations
- `Show Pole Targets` - Show Pole Targets
- `Show Constraints` - Show constraint ranges
- `Log Iterations` - Output iteration info to Console

### 💡 Usage Tips

**Performance Optimization:**
- Reduce `Main Iterations` and `Sub Iterations`
- Disable unnecessary End Effectors
- Turn off debug Gizmos

**More Natural Motion:**
- Use Pole Targets to control elbow/knee direction (must be created manually - not auto-generated)
- Adjust constraint angles to match human body structure
- Increase `Rotation Damping` for smoother motion

**Blending with Animation:**
- Lower `Global Weight` to show partial animation
- Dynamically enable/disable specific End Effectors
- IK executes in `LateUpdate` and overrides animation results

---

## 5. Quick Reference

### Recommended Configuration Presets

**High Accuracy (Important Characters)**
```
Main Iterations: 15
Sub Iterations: 5
Rotation Damping: 0.1
Constraint Passes: 3
```

**Balanced (Recommended)**
```
Main Iterations: 10
Sub Iterations: 3
Rotation Damping: 0.15
Constraint Passes: 2
```

**High Performance (Background Characters)**
```
Main Iterations: 8
Sub Iterations: 2
Rotation Damping: 0.2
Constraint Passes: 1
```

### Project Files

```
Assets/
├── Scripts/
│   ├── FullBodyFABRIK.cs         # IK Solver
│   ├── FullBodyIKSetup.cs        # Setup Tool
│   └── BoxingPoseChanger.cs      # Boxing Demo
├── Scenes/
│   └── SampleScene.unity         # Sample Scene
└── X Bot.fbx                      # Sample Character
```

---

## Summary

**Complete Workflow:**
1. ✅ Add components (FullBodyFABRIK + FullBodyIKSetup)
2. ✅ Click three buttons to complete auto-configuration
3. ✅ (Optional) Add BoxingPoseChanger for pose demonstration
4. ✅ Test and adjust parameters in Play Mode

**Need Help?**
- Check Console for error messages
- Enable debug Gizmos for visualization
- Reference the sample scene: SampleScene.unity

Enjoy! 🎮
