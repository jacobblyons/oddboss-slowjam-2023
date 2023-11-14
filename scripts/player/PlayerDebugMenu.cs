using Godot;
using System;

public partial class PlayerDebugMenu : Node
{
    [Export]
    public PlayerFPSController playerRef;

    public Button btnDefault;
    public Button btnFrozen;
    public Button btnInfluencing;
    public Button btnNoninfluencing;

    public override void _Ready() {
        btnDefault = GetNode<Button>("Btn_DEFAULT");
        btnDefault.Pressed += SetMoveStateDefault;
        btnFrozen = GetNode<Button>("Btn_FROZEN");
        btnFrozen.Pressed += SetMoveStateFrozen;
        btnInfluencing = GetNode<Button>("Btn_INFLUENCING");
        btnInfluencing.Pressed += SetMoveStateInfluencing;
        btnNoninfluencing = GetNode<Button>("Btn_NONINFLUENCING");
        btnNoninfluencing.Pressed += SetMoveStateNoninfluencing;
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
}
