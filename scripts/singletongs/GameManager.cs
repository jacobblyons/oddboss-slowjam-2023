using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    [Export]
    public int partySize {get; set;} 

    public override void _Ready()
    {
        base._Ready();
    }

    public void AddPartyGoer()
    {
        partySize++;
    }

    public void RemovePartyGoers(int num) {
        if (partySize >= num) {
            partySize -= num;
        }
    }
}