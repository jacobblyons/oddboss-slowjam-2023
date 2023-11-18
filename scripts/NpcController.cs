using System;
using System.Linq;
using Godot;


public partial class NpcController : RigidBody3D
{
    [Export]
    public float walkSpeed = 2.0f;
    [Export]
    public float runSpeed = 5.0f;
    [Export]
    public float rotateSpeed = 2.0f;
    [Export]
    public float gotHitImpulse = 50f;
    [Export]
    public float lifePostSplatter = 1f;

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
    private Area3D carHitBoundary;
    private Area3D bulletHitBoundary;
    private TargetNode currentTarget;
    private Node3D alienBeingFledFrom;
    private NpcState state = NpcState.Traveling;
    private float timeSinceBecameFearful = 0.0f;
    private float timeSinceSplatted = 0.0f;


    public override void _Ready()
    {
        base._Ready();
        navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        escapeDetectionBoundary = GetNode<Area3D>("EscapeDetectionBoundary");
        escapeReachedBoundary = GetNode<Area3D>("EscapeInReachBoundary");
        playerDetectionBoundary = GetNode<Area3D>("PlayerDetectionBoundary");
        carHitBoundary = GetNode<Area3D>("CarBoundary");
        carHitBoundary.BodyEntered += OnCarHit;
        bulletHitBoundary = GetNode<Area3D>("BulletBoundary");
        bulletHitBoundary.AreaEntered += OnBulletHit;
        playerDetectionBoundary.BodyEntered += OnPlayerDetectedInBounds;
        playerDetectionBoundary.BodyExited += OnPlayerLeftBounds;

        //GlobalRotationDegrees = new Vector3(0, GlobalRotationDegrees.Y, 0);
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

        if (state == NpcState.Splattered)
        {
            if (Time.GetTicksMsec() - timeSinceSplatted > lifePostSplatter * 1000)
            {
                QueueFree();
            }

            return;
        }

        navigationAgent.TargetPosition = GetTargetPosition();

        Vector3 currentAgentPosition = GlobalTransform.Origin;
        Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();
        Vector3 dir = (nextPathPosition - currentAgentPosition).Normalized();
        Vector3 velocity;
        // set speed based on state
        switch (state)
        {
            case NpcState.Fearful:
            case NpcState.FleeingFromAlien:
                velocity = dir * runSpeed;
                break;
            case NpcState.Traveling:
            case NpcState.Loitering:
            default:
                velocity = dir * runSpeed;
                break;
        }
        // move npc
        // Calculate target rotation
        var newTransform = GlobalTransform.LookingAt(GlobalPosition - dir, Vector3.Up);
        Quaternion targetRotation = new Quaternion(newTransform.Basis);

        // Interpolate rotation
        Quaternion currentRotation = new Quaternion(GlobalTransform.Basis);
        Quaternion newRotation = currentRotation.Slerp(targetRotation, (float)(rotateSpeed * delta));

        // Apply rotation
        GlobalRotation  = newRotation.GetEuler();

        MoveAndCollide(velocity * (float)delta);
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
        if(state == NpcState.Splattered)
            return;

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
        if(state == NpcState.Splattered)
            return;
            
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
        if(state == NpcState.Splattered)
            return;
            
        if (state != NpcState.Fearful && state != NpcState.FleeingFromAlien)
            return;

        QueueFree();
    }

    private void OnCarHit(Node3D body)
    {
        GD.Print("Ouchie!");
        var direction = (GlobalPosition - body.GlobalPosition).Normalized();

        // remove axis lock
        this.AxisLockAngularX = false;
        this.AxisLockAngularY = false;
        this.AxisLockAngularZ = false;

        // send the dude
        this.ApplyCentralImpulse(direction * gotHitImpulse);

        if (state != NpcState.Splattered)
        {
            state = NpcState.Splattered;
            timeSinceSplatted = Time.GetTicksMsec();
        }
    }

    private void OnBulletHit(Node3D body)
    {
        GD.Print("Ouchie!");
        var direction = (GlobalPosition - body.GlobalPosition).Normalized();

        // remove axis lock
        this.AxisLockAngularX = false;
        this.AxisLockAngularY = false;
        this.AxisLockAngularZ = false;

        // send the dude
        this.ApplyCentralImpulse(direction * gotHitImpulse);

        if (state != NpcState.Splattered)
        {
            state = NpcState.Splattered;
            timeSinceSplatted = Time.GetTicksMsec();
        }
    }
    #endregion

}