using Godot;
using System;
using System.Collections.Generic;

public partial class NpcSkinSelector : Node3D
{
	[Export] public Node3D[] skins;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int idx = GD.RandRange(0, skins.Length - 1);
		for (int i = 0; i < skins.Length; i++) {
			skins[i].Hide();
			if (i == idx) {
				skins[i].Show();
			}
		}
	}
}
