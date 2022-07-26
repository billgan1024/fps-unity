## FPS Game
An FPS game I'm prototyping in Unity. 

### Features

* There is an implementation for wallriding and wallrunning which uses a general state system tracking the player's velocity, acceleration, and drag.

* The platformer component of this project describes a template for a 3D platformer with theoretically perfect collision detection as we avoid using raycasts in favour of `OnCollision` events.

* Implementation of projectile weapons automatically aims bullets at the center of the screen despite the bullet origin not being there.

### Screenshots


### June 30, 2022

Fixed a bug where the player would sometimes randomly disengage from walls. This was due to unpredictable behaviour with a rigidbody's velocity
when the player hits a wall; sometimes the player could bounce off the wall due to the velocity vector pointing slightly away from the wall.
To fix this, I have made the player align their velocity to the current wall the instant `TransitionWall()` is called.

### Player.cs implementation standards

The player has a list of states which have functions that implement the code that runs when you want to transition to that state, regardless
of the current state.

Some variables (like ground/wall normals) automatically update every `FixedUpdate` and thus there is no need to update them as soon 
as possible in `ChangeStateFromCollisions`.
