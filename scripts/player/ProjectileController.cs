using Godot;
using System;

public partial class ProjectileController : Node3D
{
	[Export] float velocity;
	[Export] float spinSpeed;
	 
	private static float maxLifeTime = 10f;
	private float timeAlive;
	private Area3D hitBoundary;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		timeAlive = 0f;	
		hitBoundary = GetNode<Area3D>("HitBoundary");
		hitBoundary.AreaEntered += OnHit;
		hitBoundary.BodyEntered += OnHit;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		RotateObjectLocal(Vector3.Up, spinSpeed * (float)delta);
		TranslateObjectLocal(new Vector3(0f, -1f * velocity * (float)delta, 0f));

		// Check if projectile has been alive for too long and needs to be
		// destroyed...
		timeAlive += (float)delta;
		if (timeAlive > maxLifeTime) {
			QueueFree();
		}
	}

	public void OnHit(Node body)
	{
		QueueFree();
	}
}
