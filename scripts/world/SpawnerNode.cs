using Godot;
using System;

public partial class SpawnerNode : Node3D
{
    [Export]
    public PackedScene SceneToSpawn;

    public override void _Ready()
    {
        base._Ready();

        var worldServer = GetNode<WorldServer>("/root/WorldServer");
        worldServer.RegisterSpawner(this);
    }

    public void Spawn(TargetNode target)
    {
        var sceneInstance = SceneToSpawn.Instantiate<NpcController>();
        GetParent().AddChild(sceneInstance);
        sceneInstance.GlobalTransform = GlobalTransform;
        sceneInstance.originalTarget = target;
    }
}
