using System;
using System.Linq;
using Godot;


public partial class NpcController : CharacterBody3D
{
    [Export]
    public float walkSpeed = 2.0f;
    [Export]
    public float runSpeed = 5.0f;
    [Export]
    public float rotateSpeed = 2.0f;
    [Export]
    public float stamina = 2.0f;
    [Export]
    public TargetNode originalTarget;
    [Export]
    public float distanceToTargetOppositeOfAlien = 5.0f;
    [Export]
    public float distanceBeforeAbandoningEscape = 5.0f;

    private NavigationAgent3D navigationAgent;
    private Area3D playerDetectionBoundary;
    private Area3D escapeDetectionBoundary;
    private Area3D escapeReachedBoundary;
    private TargetNode currentTarget;
    private Node3D alienBeingFledFrom;
    private NpcState state = NpcState.Traveling;
    private float timeSinceBecameFearful = 0.0f;


    public override void _Ready()
    {
        base._Ready();
        navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        escapeDetectionBoundary = GetNode<Area3D>("EscapeDetectionBoundary");
        escapeReachedBoundary = GetNode<Area3D>("EscapeInReachBoundary");
        playerDetectionBoundary = GetNode<Area3D>("PlayerDetectionBoundary");
        playerDetectionBoundary.BodyEntered += OnPlayerDetectedInBounds;
        playerDetectionBoundary.BodyExited += OnPlayerLeftBounds;

        // Make sure to not await during _Ready.
        Callable.From(ActorSetup).CallDeferred();
    }

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        navigationAgent.TargetPosition = originalTarget.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (navigationAgent.IsNavigationFinished())
        {
            QueueFree();
        }

        navigationAgent.TargetPosition = GetTargetPosition();

        Vector3 currentAgentPosition = GlobalTransform.Origin;
        Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();
        Vector3 newVelocity = (nextPathPosition - currentAgentPosition).Normalized();

        // set speed based on state
        switch (state)
        {
            case NpcState.Fearful:
            case NpcState.FleeingFromAlien:
                Velocity = newVelocity * runSpeed;
                break;

            case NpcState.Traveling:
            case NpcState.Loitering:
            default:
                Velocity = newVelocity * runSpeed;
                break;
        }
        // move npc
        // Calculate target rotation
        var newTransform = GlobalTransform.LookingAt(GlobalPosition - newVelocity, Vector3.Up);
        Quaternion targetRotation = new Quaternion(newTransform.Basis);

        // Interpolate rotation
        Quaternion currentRotation = new Quaternion(GlobalTransform.Basis);
        Quaternion newRotation = currentRotation.Slerp(targetRotation, (float)(rotateSpeed * delta));

        // Apply rotation
        GlobalRotation = newRotation.GetEuler();

        MoveAndSlide();
        //MoveAndCollide(Velocity * (float)delta);
    }

    private Vector3 GetTargetPosition()
    {
        switch (state)
        {
            case NpcState.Fearful:
                return GetEscapeTarget() ?? originalTarget.GlobalPosition;
            case NpcState.FleeingFromAlien:
                return GetDistanceFromAlien() > distanceBeforeAbandoningEscape // if alien is far enough away, go to escape
                            ? GetEscapeTarget() ?? GetTargetAwayFromAlien() 
                            : GetTargetAwayFromAlien();
            case NpcState.Traveling:
            default:
                return originalTarget.GlobalPosition;
        }
    }

    private Vector3 GetTargetAwayFromAlien()
    {
        Vector3 characterPosition = GlobalPosition;
        Vector3 enemyPosition = alienBeingFledFrom.GlobalPosition;
        Vector3 directionFromEnemy = (characterPosition - enemyPosition).Normalized();
        return characterPosition + directionFromEnemy * distanceToTargetOppositeOfAlien;
    }

    private Vector3? GetEscapeTarget()
    {
        // look for an esape in the escape detection boundary
        var escapes = escapeDetectionBoundary.GetOverlappingAreas();
        return escapes.OrderBy(e => (e.GlobalPosition - GlobalPosition).Length()).FirstOrDefault()?.GlobalPosition;
    }

    private float GetDistanceFromAlien()
    {
        if (alienBeingFledFrom == null)
            return float.MaxValue;
        return (GlobalPosition - alienBeingFledFrom.GlobalPosition).Length();
    }

#region Events
    private void OnPlayerDetectedInBounds(Node3D body)
    {
        if (body is not PlayerFPSController)
        {
            GD.PrintErr("NpcController.OnPlayerDetected: body is not PlayerFPSController. Only players should be detected on layer 2.");
            return;
        }

        state = NpcState.FleeingFromAlien;
        alienBeingFledFrom = body;
    }

    private void OnPlayerLeftBounds(Node3D body)
    {
        if (body is not PlayerFPSController)
        {
            GD.PrintErr("NpcController.OnPlayerDetected: body is not PlayerFPSController. Only players should be detected on layer 2.");
            return;
        }

        state = NpcState.Fearful;
        timeSinceBecameFearful = Time.GetTicksMsec();
        alienBeingFledFrom = null;
    }

    private void OnEscapeReached(Node body)
    {
        if(state != NpcState.Fearful && state != NpcState.FleeingFromAlien)
            return;

        QueueFree();
    }
#endregion

}