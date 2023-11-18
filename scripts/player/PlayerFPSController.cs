using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public partial class PlayerFPSController : CharacterBody3D
{
    [Export] public float moveSpeed;
    [Export] public float accelFactor;
    [Export] public float decelFactor;
    [Export] public float jumpForce;
    [Export] public float mouseSensitivity;
    [Export] public float hitstunTime;
    [Export] public float hitstunKnockbackForce;

    public PlayerMoveState moveState;
    public PlayerHitState hitState;

    private Vector2 moveInput;
    private Vector2 mouseInput;
    private static float gravity = -9.8f;       //TODO: read from proj settings.

    // Node references.
    private WorldServer worldServer;
    private Node3D fpCamera;
    private FriendGunController friendGun;
    private Area3D hurtBox;

    public override void _Ready() {
        moveState = PlayerMoveState.DEFAULT;
        hitState = PlayerHitState.DEFAULT;
        moveInput = Vector2.Zero;

        Input.MouseMode = Input.MouseModeEnum.Captured;
        fpCamera = GetNode<Node3D>("Camera3D");
        friendGun = fpCamera.GetNode<FriendGunController>("Gun");
        worldServer = GetNode<WorldServer>("/root/WorldServer");
        worldServer.RegisterPlayer(this);
        hurtBox = GetNode<Area3D>("HurtBox");
        hurtBox.AreaEntered += PlayHurtAnimation;
    }

    public override void _PhysicsProcess(double delta) {
        float fdelta = (float)delta;
        UpdateInput();
        UpdateAim();

        // Update weapon interaction.
        if (Input.IsActionPressed("shoot")) {
            friendGun.PullTrigger();
        }
        else {
            friendGun.ReleaseTrigger();
        }

        // Calculate direction player is trying to move based on direction they
        // are facing and the direction of their movement input (if any).
        Basis aim = fpCamera.GlobalTransform.Basis;
        Vector3 moveDir = new Vector3(moveInput.X, 0f, moveInput.Y).Rotated(Vector3.Up, aim.GetEuler().Y).Normalized();

        // Completely ignore effects of user movement input if player is in 
        // NONINFLUENCING state.
        if (moveState == PlayerMoveState.NONINFLUENCING) {
            moveDir = Vector3.Zero;
        }

        // Update horizontal velocity
        Vector2 horizVelocity = new Vector2(Velocity.X, Velocity.Z);
        if (moveInput != Vector2.Zero) {
            horizVelocity = horizVelocity.MoveToward(
                new Vector2(moveDir.X, moveDir.Z) * moveSpeed, 
                fdelta * accelFactor
            );
        }
        else {
            horizVelocity = horizVelocity.MoveToward(
                Vector2.Zero, fdelta * decelFactor);
        }

        // Update vertical velocity.
        // Is player touching the ground?
        float vertVelocity = Velocity.Y;
        if (!IsOnFloor()) {
            vertVelocity += gravity * fdelta;
        }
        else if (Input.IsActionPressed("jump") && moveState == PlayerMoveState.DEFAULT) {
            vertVelocity += jumpForce;
        }

        // Skip updating final velocity values if player is frozen...
        if (moveState != PlayerMoveState.FROZEN) {
            Velocity = new Vector3(horizVelocity.X, vertVelocity, horizVelocity.Y);
            MoveAndSlide();
        }

        // Reset values for next update.
        mouseInput = Vector2.Zero;
    }

    public void UpdateAim() {
        if (mouseInput.Length() > 0f) {
            RotateY(-mouseInput.X * mouseSensitivity / 180f);
            float change = -mouseInput.Y * mouseSensitivity;
            fpCamera.RotateX(change / 180f);
            fpCamera.Rotation = new Vector3(
                Mathf.Clamp(fpCamera.Rotation.X, -0.99f, 0.99f),
                fpCamera.Rotation.Y,
                fpCamera.Rotation.Z
            );
        }
    }

    // Update any varaibles that are responsible for tracking player input.
    public void UpdateInput() {
        float l = Input.IsActionPressed("move_left") ? -1 : 0;
        float r = Input.IsActionPressed("move_right") ? 1 : 0;
        float u = Input.IsActionPressed("move_forward") ? -1 : 0;
        float d = Input.IsActionPressed("move_back") ? 1 : 0;
        moveInput = new Vector2(l + r, u + d);
        if (moveInput.Length() > 0f) {
            moveInput = moveInput.Normalized();
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion motion) {
            mouseInput = motion.Relative;
        }
    }

    public void Debug_SetMoveState(PlayerMoveState state) {
        moveState = state;
    }

    public async void PlayHurtAnimation(Node3D body) {
        if (hitState == PlayerHitState.HITSTUN) {
            return;
        }
        moveState = PlayerMoveState.NONINFLUENCING;
        hitState = PlayerHitState.HITSTUN;
        Velocity = (Position - body.Position).Normalized() * hitstunKnockbackForce;
        worldServer.EmitSignal("BrainWashReleased");
        await ToSignal(GetTree().CreateTimer(hitstunTime), "timeout");
        moveState = PlayerMoveState.DEFAULT;
        hitState = PlayerHitState.DEFAULT;
    }
}

public enum PlayerMoveState {
    DEFAULT,        // Normal control over character.
    FROZEN,         // Freeze player in place (rotation ok).
    INFLUENCING,    // Player movement dictatated by external forces & player has influence
    NONINFLUENCING  // Player movement dictatated by external forces & player has no influence
}

public enum PlayerHitState {
    DEFAULT,        // Not being hurt
    HITSTUN         // In hitstun from being hurt.
}