using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class WorldServer : Node
{
    [Signal]
    public delegate void BrainWashReleasedEventHandler();

    [Export]
    public int doorCount { get => targets.Count; set { } }

    [Export]
    public int spawnerCount { get => spawners.Count; set { } }

    [Export]
    public Node3D PlayerRef { get; set; }

    [Export]
    public float npcSpawnPeriodInSeconds = 2;

    [Export]
    public float MaxNpcs = 20;

    private Dictionary<Vector3, TargetNode> targets = new Dictionary<Vector3, TargetNode>();
    private Dictionary<Vector3, SpawnerNode> spawners = new Dictionary<Vector3, SpawnerNode>();
    private List<NpcController> npcs = new List<NpcController>();
    private ulong lastSpawnTime = 0;


    public override void _Ready()
    {
        base._Ready();
        lastSpawnTime = Time.GetTicksMsec();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Time.GetTicksMsec() - lastSpawnTime > npcSpawnPeriodInSeconds * 1000)
        {
            if (doorCount == 0 || spawnerCount == 0)
                return;
            
            if(npcs.Count >= MaxNpcs)
                return;

            // set a random target door
            var doorIndex = Random.Shared.Next(0, doorCount - 1);
            var door = targets.Values.ToList()[doorIndex];

            // spawn at a random spawner
            var spawnIndex = Random.Shared.Next(0, spawnerCount - 1);
            if (spawnerCount > 0)
                spawners.Values.ToList()[spawnIndex].Spawn(door);
            lastSpawnTime = Time.GetTicksMsec();
        }
    }

    public void RegisterPlayer(Node3D player)
    {
        PlayerRef = player;
    }

    public void RegisterSpawner(SpawnerNode spawner)
    {
        if (spawners.ContainsKey(spawner.GlobalPosition))
            return;

        spawners.Add(spawner.GlobalPosition, spawner);
    }

    public void RegisterTarget(TargetNode target)
    {
        if (targets.ContainsKey(target.GlobalPosition))
            return;

        targets.Add(target.GlobalPosition, target);
    }

    public void RegisterNpc(NpcController npc)
    {
        npcs.Add(npc);

    }

    public void DeregisterNpc(NpcController npc)
    {
        npcs.Remove(npc);
    }
}
