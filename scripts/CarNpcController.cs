using Godot;
using System;

public partial class CarNpcController : AnimatableBody3D
{
    [Export]
    public float Speed = 10.0f;

    [Export]
    public PathFollow3D Path;

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // move npc
        Path.Progress += (float)(Speed * delta);
    }
}
