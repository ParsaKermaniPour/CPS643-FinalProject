# VR Heist Game - Setup Guide

## Today's Goal: VR Movement with Joystick Controls

---

## PART 1: Setting Up Your VR Scene (5-10 minutes)

### Step 1: Open Unity and Your Scene
1. Open Unity Hub and launch your project
2. In Unity, go to **File → Open Scene**
3. Open `Assets/Scenes/SampleScene.unity`

### Step 2: Set Up XR Origin (Your VR Rig)
1. **Delete the Main Camera** (right-click it in Hierarchy → Delete)
   - We'll replace it with a VR camera
   
2. **Add XR Origin:**
   - Right-click in Hierarchy → **XR → XR Origin (Action-based)**
   - This creates your complete VR player rig!
   
3. **Verify the Setup:**
   - Expand "XR Origin" in the Hierarchy
   - You should see:
     - Camera Offset
       - Main Camera (this is your VR headset view)
       - LeftHand Controller
       - RightHand Controller

### Step 3: Add a Floor
1. Right-click in Hierarchy → **3D Object → Plane**
2. Name it "Floor"
3. In Inspector, set Transform Position to (0, 0, 0)
4. Set Scale to (5, 1, 5) to make it bigger

### Step 4: Add the Movement Script
1. **Select "XR Origin"** in the Hierarchy
2. In Inspector, click **Add Component**
3. Type "VRMovementSimple" and press Enter
4. The script will appear in the Inspector

### Step 5: Configure the Movement Script
1. With XR Origin selected, look at the **VRMovementSimple component**
2. Settings you'll see:
   - **Move Speed**: Leave at 2.0 (good starting speed)
   - **Main Camera**: Drag "Main Camera" from inside XR Origin here
   - **Use Left Controller**: Keep checked (left joystick = move)

---

## PART 2: Testing Your VR Movement

### If You Have a VR Headset Connected:
1. Put on your headset
2. Click **Play** button (top center of Unity)
3. Move the left joystick on your controller
4. You should move around!

### If You DON'T Have a Headset:
1. Install the **XR Device Simulator** for testing:
   - Go to **Window → Package Manager**
   - Change dropdown from "In Project" to "Unity Registry"
   - Search "XR Interaction Toolkit"
   - Install "XR Device Simulator" sample
   
2. Add the simulator to your scene:
   - Right-click Hierarchy → **XR → Device Simulator**
   
3. Press Play and use these controls:
   - **WASD** = Move camera
   - **Right-click + Mouse** = Look around
   - **Hold Shift** = Controller inputs
   - **Q/E** = Move controllers

---

## PART 3: Make it Better (Optional)

### Add Some Objects to Walk Around
1. Right-click in Hierarchy → **3D Object → Cube**
2. Position it at (2, 0.5, 2)
3. Create a few more cubes at different positions
4. Now you have obstacles to walk around!

### Adjust Movement Speed
- Select XR Origin
- In VRMovementSimple component, change **Move Speed**:
  - 1.0 = Slow walk
  - 2.0 = Normal walk (default)
  - 3.5 = Fast walk
  - 5.0 = Run

---

## TROUBLESHOOTING

### "I can't see anything when I press Play"
- Make sure Main Camera is inside XR Origin → Camera Offset
- Check that XR Origin position is at (0, 1, 0) or similar

### "Movement doesn't work"
- Make sure you dragged Main Camera into the script's "Main Camera" field
- Verify your VR controllers are connected
- Try the Device Simulator if testing without a headset

### "I'm falling through the floor"
- Select XR Origin
- Look at the CharacterController component (auto-added)
- Make sure "Center Y" is around 0.9

### "Everything is working!"
- Great! You're ready for the next step (safe lock interaction)

---

## WHAT WE BUILT TODAY

✅ Complete VR player rig (XR Origin)
✅ VR camera that follows your headset
✅ Joystick movement (left stick = move forward/backward/left/right)
✅ Movement direction based on where you're looking
✅ Collision detection (can't walk through walls)

---

## NEXT SESSION: Interactive Safe Lock

We'll create:
- A 3D safe with a combination lock
- A rotatable dial you can grab with the controller
- Number tracking (0-99)
- Visual feedback when turning
- Win condition when correct combination is entered

---

## Quick Reference

**Scripts Created:**
- `Assets/Scripts/VRMovementSimple.cs` - Easy to set up, uses basic XR input
- `Assets/Scripts/VRMovement.cs` - Advanced version with more options

**Key Components:**
- **XR Origin** - Your VR player
- **CharacterController** - Handles collision and gravity
- **Main Camera** - What you see in VR

**Controls:**
- Left Joystick = Move
- Physical movement = Look around (with real headset)

---

Good luck! 🎮
