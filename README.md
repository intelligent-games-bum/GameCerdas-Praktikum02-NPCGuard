<div align="center">

# NPC Guard

A patrolling guard that hunts you by sight and loses you when you break cover.
Second lab of the Intelligent Games course.

**[Play it in your browser](https://play.unity.com/en/games/e2cd4937-3be9-4cda-90f3-91a958a63660/web)**

![Video demo](public/demo-video.gif)

![Unity](https://img.shields.io/badge/Unity-6.3%20LTS-000000?logo=unity)
![URP](https://img.shields.io/badge/Render-URP-2196F3)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![Input System](https://img.shields.io/badge/Input%20System-New-orange)
![AI Navigation](https://img.shields.io/badge/AI%20Navigation-2.0-4CAF50)

</div>

---

## Controls

Click the game window once before you start. Browsers hand over the mouse cursor after a click, so the camera will not respond until you give it one.

| Input | On the ground | In flight |
|---|---|---|
| WASD or arrows | Walk | Steer |
| Mouse | Orbit the camera | Orbit the camera |
| Space | Jump | Gain height |
| Space twice, quickly | Take off | Cancel and drop |
| Shift | — | Descend |

A flight runs for 10 seconds. Touching the ground ends it early, and the next one waits 3 seconds.

Tapping Space twice to climb faster will cancel the flight instead, since that gesture is also the cancel command. Leave a gap between taps.

## How the guard sees you

[`NPCSensor.cs`](Assets/Scripts/NPCSensor.cs) runs three tests every frame, ordered so the cheap ones reject you before the expensive one runs.

1. **Distance.** Are you inside the 10 metre radius?
2. **Angle.** Are you inside the 61 degree cone in front of the guard?
3. **Line of sight.** Does a ray from the guard's eyes reach yours without hitting anything on the `Obstacle` layer?

Fail any one of them and the guard perceives nothing.

That third test is what lets you hide. Crates, trees and hills sit on a layer the raycast collides with, so putting one between yourself and the guard breaks the chain. The ray leaves the guard a metre off the ground and arrives at the same height on you, which means cover shorter than a metre will not save you.

## How the guard decides

[`NPCBrain.cs`](Assets/Scripts/NPCBrain.cs) drives a `NavMeshAgent` through three states.

| State | What the guard does |
|---|---|
| Patrol | Walks the waypoint loop at speed 5 |
| Chase | Runs straight at you at speed 10 |
| Search | Goes to the spot where it last saw you, hunts for 3 seconds, gives up |

Search carries the memory. The guard records your position on every frame it can see you, so breaking line of sight never erases what it already knows. It walks to that spot first, then returns to patrol after finding nothing.

## How you move

[`PlayerController.cs`](Assets/Scripts/PlayerController.cs) moves a `CharacterController`, so walls and crates stop you. It integrates vertical motion by hand rather than leaving it to gravity, because flight needs control that gravity alone will not give.

W sends you wherever the camera faces, and the character turns to meet the direction it travels. [`CameraFollow.cs`](Assets/Scripts/CameraFollow.cs) orbits on a spring arm that shortens whenever scenery gets between the camera and your back.

[`PlayerAnimatorDriver.cs`](Assets/Scripts/PlayerAnimatorDriver.cs) writes one integer into the Animator, and three Any State transitions read it to pick the clip. A jump keeps the ground pose, since the character ships without a jump animation.

## Scripts

| File | Job |
|---|---|
| `PlayerController.cs` | Walking, jumping, and the timed flight |
| `CameraFollow.cs` | Orbit camera that dodges scenery |
| `PlayerAnimatorDriver.cs` | Turns the locomotion state into Animator parameters |
| `NPCSensor.cs` | Distance, field of view, line of sight |
| `NPCBrain.cs` | Patrol, Chase and Search over a NavMesh |
| `EnemyDetector.cs` | Colours the guard's light to match its state |

## Running it from source

Clone the repo and open the folder with Unity 6.3 LTS. Unity rebuilds the `Library` folder on first launch, which takes a few minutes.

Open `Assets/Scenes/NPCDetector.unity` and press Play.

Keep the Scene view in sight while you play. `NPCSensor` draws the detection sphere, the edges of the view cone, and a line to you that turns red the moment the guard has you and grey when it does not. Select the guard and the `Has Line Of Sight` box in the Inspector tells you the same thing at a glance.

## Notes

Anything works as cover if it carries a collider, sits on the `Obstacle` layer, and stands taller than a metre. The single crates land right on that limit, so reach for the stacked pair or the trees.

`EnemyDetector.cs` is the distance-only detector from the first lab, kept for comparison. It now consults `NPCSensor` before switching state, so the light stops turning red while you are behind cover.
