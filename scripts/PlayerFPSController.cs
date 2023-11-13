using Godot;
using System;

public partial class PlayerFPSController : CharacterBody3D
{
    [Export]
    public float moveSpeed;
    [Export]
    public float accelFactor;
    [Export]
    public float decelFactor;
    [Export]
    public float jumpForce;
    [Export]
    public float mouseSensitivity;

    private Node3D fpCamera;

    private Vector2 moveInput;
    private Vector2 mouseInput;
    private static float gravity = -9.8f;

    public override void _Ready() {
        moveInput = Vector2.Zero;

        Input.MouseMode = Input.MouseModeEnum.Captured;
        fpCamera = GetNode<Node3D>("Camera3D");
    }

    public override void _PhysicsProcess(double delta) {
        float fdelta = (float)delta;
        UpdateInput();
        UpdateAim();

        // Calculate direction player is trying to move based on direction they
        // are facing and the direction of their movement input (if any).
        Basis aim = fpCamera.GlobalTransform.Basis;
        Vector3 aimDir = aim.X;
        Vector3 moveDir = new Vector3(moveInput.X, 0f, moveInput.Y).Rotated(Vector3.Up, aim.GetEuler().Y).Normalized();

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
        else if (Input.IsActionPressed("jump")) {
            vertVelocity += jumpForce;
        }

        Velocity = new Vector3(horizVelocity.X, vertVelocity, horizVelocity.Y);

        mouseInput = Vector2.Zero;
        MoveAndSlide();
    }

    public void UpdateAim() {
        if (mouseInput.Length() > 0f) {
            RotateY(-mouseInput.X * mouseSensitivity / 180f);
            float change = -mouseInput.Y * mouseSensitivity;
            fpCamera.RotateX(change / 180f);
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
}
