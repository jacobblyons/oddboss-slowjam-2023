using Godot;
using System;

public partial class PlayerDebugMenu : Node
{
    [Export]
    public PlayerFPSController playerRef;
    private FriendGunController gunRef;

    public Button btnDefault;
    public Button btnFrozen;
    public Button btnInfluencing;
    public Button btnNoninfluencing;

    public Button btnIncrementWeaponChargeLvl;
    public Button btnDecrementWeaponChargeLvl;
    public Button btnIncrementWeaponPowerLvl;
    public Button btnDecrementWeaponPowerLvl;

    public override void _Ready() {
        gunRef = playerRef.GetNode<FriendGunController>("Camera3D/Gun");

        btnDefault = GetNode<Button>("Btn_DEFAULT");
        btnDefault.Pressed += SetMoveStateDefault;
        btnFrozen = GetNode<Button>("Btn_FROZEN");
        btnFrozen.Pressed += SetMoveStateFrozen;
        btnInfluencing = GetNode<Button>("Btn_INFLUENCING");
        btnInfluencing.Pressed += SetMoveStateInfluencing;
        btnNoninfluencing = GetNode<Button>("Btn_NONINFLUENCING");
        btnNoninfluencing.Pressed += SetMoveStateNoninfluencing;
        btnIncrementWeaponChargeLvl = GetNode<Button>("Btn_IncWeaponCharge");
        btnIncrementWeaponChargeLvl.Pressed += IncWeaponCharge;
        btnDecrementWeaponChargeLvl = GetNode<Button>("Btn_DecWeaponCharge");
        btnDecrementWeaponChargeLvl.Pressed += DecWeaponCharge;
        btnIncrementWeaponPowerLvl = GetNode<Button>("Btn_IncWeaponPower");
        btnIncrementWeaponPowerLvl.Pressed += IncWeaponPower;
        btnDecrementWeaponPowerLvl = GetNode<Button>("Btn_DecWeaponPower");
        btnDecrementWeaponPowerLvl.Pressed += DecWeaponPower;

    }

    private void SetMoveStateDefault() {
        playerRef.Debug_SetMoveState(PlayerMoveState.DEFAULT);
    }

    private void SetMoveStateFrozen() {        
        playerRef.Debug_SetMoveState(PlayerMoveState.FROZEN);
    }

    private void SetMoveStateInfluencing() {
        playerRef.Debug_SetMoveState(PlayerMoveState.INFLUENCING);
    }

    private void SetMoveStateNoninfluencing() {
        playerRef.Debug_SetMoveState(PlayerMoveState.NONINFLUENCING);
    }

    private void IncWeaponCharge() {
        gunRef.upgradeData.IncrementChargeLvl();
    }

    private void DecWeaponCharge() {
        gunRef.upgradeData.DecrementChargeLvl();
    }

    private void IncWeaponPower() {
        gunRef.upgradeData.IncrementPowerLvl();
    }

    private void DecWeaponPower() {
        gunRef.upgradeData.DecrementPowerLvl();
    }
}
