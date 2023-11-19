using Godot;
using System;
using System.Data;
public enum DogState { GetEmBoy, Stunned, Down }
public partial class DogController : RigidBody3D
{
    [Export]
    public float PatrolSpeed = 7.0f;
    [Export]
    public float ChaseSpeed = 15.0f;
    [Export]
    public PathFollow3D Path;
    [Export]
    public float rotateSpeed = 2.0f;
    [Export]
    public float timeUntilDespawn = 4.0f;
    [Export]
    public float timeStunned = 1.0f;
    [Export]
	public float gotHitImpulse = 50f;

    private Area3D bulletHitBoundary;
    private Area3D playerDetectionBoundary;
    private Area3D playerHitBoundary;
    private Node3D playerRef;
    private NavigationAgent3D navigationAgent;
    private bool initialized = false;
    private DogState state = DogState.GetEmBoy;
    private float timeSinceShot = 0.0f;
    private float timeSinceStunned = 0.0f;

    public override void _Ready()
    {
        base._Ready();
        bulletHitBoundary = GetNode<Area3D>("BulletBoundary");
        bulletHitBoundary.AreaEntered += OnBulletHit;
        playerDetectionBoundary = GetNode<Area3D>("PlayerDetectionBoundary");
        playerHitBoundary = GetNode<Area3D>("PlayerBoundary");
        navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        playerHitBoundary.BodyEntered += OnPlayerHit;

        // Make sure to not await during _Ready.
        Callable.From(ActorSetup).CallDeferred();
    }

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        navigationAgent.TargetPosition = Path.GlobalPosition;
        initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!initialized)
        {
            return;
        }

        if (state == DogState.Down)
        {
            if (Time.GetTicksMsec() - timeSinceShot > timeUntilDespawn * 1000)
            {
                QueueFree();
            }
        }

        if (state == DogState.Stunned)
        {
            if (Time.GetTicksMsec() - timeSinceStunned > timeStunned * 1000)
            {
                state = DogState.GetEmBoy;
                Freeze = true;
            }
        }
        

        // get target
        Vector3 target;
        float speed;

        // init player if null
        if (playerRef == null)
        {
            playerRef = GetTree().GetFirstNodeInGroup("player") as Node3D;
        }

        if (playerDetectionBoundary.OverlapsBody(playerRef))
        {
            target = playerRef.GlobalPosition;
            speed = ChaseSpeed;
        }
        else
        {
            Path.Progress += (float)(PatrolSpeed * delta);
            target = Path.GlobalPosition;
            speed = PatrolSpeed;
        }

        // move npc	
        navigationAgent.TargetPosition = target;

        Vector3 currentAgentPosition = GlobalTransform.Origin;
        Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();
        Vector3 dir = (nextPathPosition - currentAgentPosition).Normalized();
        Vector3 velocity = dir * speed;

        try
        {
            RotateTowardsNextPoint(dir, delta);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"|dir:{dir}|{e.Message}");
        }

        MoveAndCollide(velocity * (float)delta, safeMargin: 0.01f);
    }

    private void RotateTowardsNextPoint(Vector3 dir, double delta)
    {
        // Calculate target rotation
        var pointInFrontOfNpc = GlobalPosition - dir;
        var targetWithoutY = new Vector3(pointInFrontOfNpc.X, GlobalPosition.Y, pointInFrontOfNpc.Z);

        // only if there is a target to look at
        if (targetWithoutY == GlobalPosition ||
                targetWithoutY == Vector3.Zero ||
                pointInFrontOfNpc.Dot(Vector3.Up) == 1 ||  // parallel to up
                pointInFrontOfNpc.Dot(Vector3.Down) == 1) // parallel to down
            return;
        var newTransform = GlobalTransform.LookingAt(targetWithoutY, Vector3.Up);
        Quaternion targetRotation = new Quaternion(newTransform.Basis);

        // Interpolate rotation
        Quaternion currentRotation = new Quaternion(GlobalTransform.Basis);
        Quaternion newRotation = currentRotation.Slerp(targetRotation, (float)(rotateSpeed * delta));

        // Apply rotation
        GlobalRotation = newRotation.GetEuler();
    }

    private void OnPlayerHit(Node3D body)
    {
        timeSinceStunned = Time.GetTicksMsec();
        state = DogState.Stunned;
        // send the dude
        var direction = (GlobalPosition - body.GlobalPosition).Normalized();
        this.Freeze = false;
        this.ApplyCentralImpulse(direction * gotHitImpulse);
    }

    private void OnBulletHit(Node3D body)
    {
        timeSinceShot = Time.GetTicksMsec();
        state = DogState.Down;
        
        // send the dude
        this.Freeze = false;
        var direction = (GlobalPosition - body.GlobalPosition).Normalized();
        this.ApplyCentralImpulse(direction * gotHitImpulse);
    }
}
