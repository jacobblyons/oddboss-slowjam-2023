using Godot;
using System;

public partial class FriendGunUpgrader : Node3D
{
    [Export] public FriendGunController friendGun;
    private GameManager gameManager;
    private GUIController guiController;

    public override void _Ready() {
        gameManager = GetNode<GameManager>("/root/GameManager");
        guiController = GetNode<GUIController>("/root/Game/GUI");
        guiController.chargeLvlUpBtn.Pressed += TryUpgradeGunCharge;
        guiController.powerLvlUpBtn.Pressed += TryUpgradeGunPower;
    }

    public void TryUpgradeGunCharge() {
        if (gameManager.partySize >= 3) {
            GD.Print("LVl up charge?");
            gameManager.RemovePartyGoers(3);
            friendGun.upgradeData.IncrementChargeLvl();
            guiController.UpdateUpgradeIconState(friendGun.upgradeData);
        }
    }

    public void TryUpgradeGunPower() {        
        if (gameManager.partySize >= 5) {
            GD.Print("LVl up power?");
            gameManager.RemovePartyGoers(5);
            friendGun.upgradeData.IncrementPowerLvl();
            guiController.UpdateUpgradeIconState(friendGun.upgradeData);
        }
    }
}
