using Godot;
using System;

public partial class DoorNode : Node3D
{
     public override void _Ready()
    {
        base._Ready();

        var worldServer = GetNode<WorldServer>("/root/WorldServer");
        worldServer.RegisterDoor(this);
    }
}
