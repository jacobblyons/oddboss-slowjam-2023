using Godot;
using System;

public partial class ProjectileController : Node3D
{
	[Export] float velocity;
	[Export] float spinSpeed;
	 
	private static float maxLifeTime = 10f;
	private float timeAlive;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		timeAlive = 0f;	
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//RotateZ(-1 * spinSpeed * (float)delta);
		Position += GlobalTransform.Basis.Z * velocity * (float)delta;

		// Check if projectile has been alive for too long and needs to be
		// destroyed...
		timeAlive += (float)delta;
		if (timeAlive > maxLifeTime) {
			Free();
		}
	}
}
