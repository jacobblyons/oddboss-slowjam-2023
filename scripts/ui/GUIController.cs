using Godot;
using System;

public partial class GUIController : Control
{
    [Export] public Color offColor;
    [Export] public Color onColor;
    [Export] public Label partySizeLabel;
    [Export] private ColorRect[] chargeLvlIcons;
    [Export] public Button chargeLvlUpBtn;
    [Export] private ColorRect[] powerLvlIcons;
    [Export] public Button powerLvlUpBtn;

    private GameManager gameManager;

    public override void _Ready() {
        gameManager = GetNode<GameManager>("/root/GameManager/");
        foreach (ColorRect c in chargeLvlIcons) {
            c.Color = offColor;
        }
        foreach (ColorRect c in powerLvlIcons) {
            c.Color = offColor;
        }
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("menu")) {
            ToggleMouseCapture();
        }
        partySizeLabel.Text = gameManager.partySize.ToString();
    }

    public void UpdateUpgradeIconState(WeaponUpgradeData wd) {
        foreach (ColorRect c in chargeLvlIcons) { c.Color = offColor; }
        foreach (ColorRect c in powerLvlIcons) { c.Color = offColor; }

        if (wd.chargeLvl >= 1) {
            chargeLvlIcons[0].Color = onColor;
        }
        if (wd.chargeLvl >= 2) {
            chargeLvlIcons[1].Color = onColor;
        }

        if (wd.powerLvl >= 1) {
            powerLvlIcons[0].Color = onColor;
        }
        if (wd.powerLvl >= 2) {
            powerLvlIcons[1].Color = onColor;
        }
    }

    private void ToggleMouseCapture() {
        if (Input.MouseMode == Input.MouseModeEnum.Captured) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else if (Input.MouseMode == Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }
}
