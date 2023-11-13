using System;
using Godot;

public partial class NpcController : CharacterBody3D
{
    private NavigationAgent3D _navigationAgent;

    [Export]
    public float _movementSpeed =2.0f;

    [Export]
    public DoorNode targetDoor;

    [Export]
    private Godot.Vector3 _movementTargetPosition = new Godot.Vector3(3.0f, 0.0f, 2.0f);

    public Godot.Vector3 MovementTarget
    {
        get { return _navigationAgent.TargetPosition; }
        set { _navigationAgent.TargetPosition = value; }
    }

    public override void _Ready()
    {
        base._Ready();

        _navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        // These values need to be adjusted for the actor's speed
        // and the navigation layout.
        _navigationAgent.PathDesiredDistance = 0.5f;
        _navigationAgent.TargetDesiredDistance = 0.5f;
    
        // Make sure to not await during _Ready.
        Callable.From(ActorSetup).CallDeferred();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_navigationAgent.IsNavigationFinished())
        {
            QueueFree();
        }

        _navigationAgent.TargetPosition = targetDoor.GlobalPosition;

        Godot.Vector3 currentAgentPosition = GlobalTransform.Origin;
        Godot.Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
        GD.Print($"Current: {currentAgentPosition}, Next: {nextPathPosition}");
       // GD.Print(nextPathPosition);
        Godot.Vector3 newVelocity = (nextPathPosition - currentAgentPosition).Normalized();
      
        newVelocity *= _movementSpeed;
        GD.Print($"New velocity: {newVelocity}, velocity magnitude {newVelocity.Length()}");
        
        Velocity = newVelocity;
        MoveAndSlide();
    }

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Now that the navigation map is no longer empty, set the movement target.
        MovementTarget = _movementTargetPosition;
    }
}