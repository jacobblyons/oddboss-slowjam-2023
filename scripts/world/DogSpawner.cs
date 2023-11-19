using Godot;
using System;
using System.Collections.Generic;

public partial class DogSpawner : Path3D
{
    [Export]
    public PackedScene DogScene;
    [Export]
    public PackedScene PathFollowScene;
    [Export]
    public float spawnPeriodInSeconds = 2;
    [Export]
    private float timeSinceLastSpawn = 0;
    [Export]
    private float maxDogs = 10;

    private List<DogController> dogs = new List<DogController>();
    

    public override void _Process(double delta)
    {
        base._Process(delta);
         if (Time.GetTicksMsec() - timeSinceLastSpawn > spawnPeriodInSeconds * 1000)
        {
            if (dogs.Count >= maxDogs)
            {
                return;
            }
            var dogInstance = DogScene.Instantiate<DogController>();
            var pathInstance = PathFollowScene.Instantiate<PathFollow3D>();
            AddChild(dogInstance);
            AddChild(pathInstance);
            pathInstance.ProgressRatio = (float)Random.Shared.NextDouble();
            dogInstance.GlobalPosition = pathInstance.GlobalPosition;
            dogInstance.Path = pathInstance;
            timeSinceLastSpawn = Time.GetTicksMsec();
            dogs.Add(dogInstance);
        }
    }

    public void RemoveDog(DogController dog)
    {
        dogs.Remove(dog);
    }
}
