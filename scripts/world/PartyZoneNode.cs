using Godot;
using System;

public partial class PartyZoneNode : Node3D
{
    private Area3D partyArea;
    private GameManager gameManager;
    public override void _Ready()
    {
        base._Ready();
        partyArea = GetNode<Area3D>("PartyArea");
        partyArea.AreaEntered += OnNpcArrived;

        gameManager = GetNode<GameManager>("/root/GameManager");
    }

    public void OnNpcArrived(Area3D npc){
        npc.GetParent().QueueFree();
        gameManager.AddPartyGoer();
    }
}
