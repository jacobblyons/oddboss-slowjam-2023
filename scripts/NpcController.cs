
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;


public partial class NpcController : RigidBody3D
{
	[Export]
	public float walkSpeed = 2.0f;

	[Export]
	public float brainwashedSpeed = 2.0f;
	[Export]
	public float runSpeed = 5.0f;
	[Export]
	public float rotateSpeed = 2.0f;
	[Export]
	public float gotHitImpulse = 50f;
	[Export]
	public float lifePostSplatterInSec = 1f;
	[Export]
	public float stunTimeInSec = 3f;

	[Export]
	public float stamina = 2.0f;
	[Export]
	public TargetNode originalTarget;
	[Export]
	public float distanceToTargetOppositeOfAlien = 5.0f;
	[Export]
	public float distanceInFrontOfPlayerToLookAt = 0.5f;
	[Export]
	public float distanceBeforeAbandoningEscape = 5.0f;

	private NavigationAgent3D navigationAgent;
	private Area3D playerDetectionBoundary;
	private Area3D escapeDetectionBoundary;
	private Area3D escapeReachedBoundary;
	private Area3D partyReachedBoundary;
	private Area3D carHitBoundary;
	private Area3D bulletHitBoundary;
	private TargetNode currentTarget;
	private Node3D alienBeingFledFrom;
	private NpcState state = NpcState.Traveling;
	private float timeSinceBecameFearful = 0.0f;
	private float timeSinceSplatted = 0.0f;
	private float timeSinceShot = 0.0f;
	private WorldServer worldServer;


	public override void _Ready()
	{
		base._Ready();
		navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

		escapeDetectionBoundary = GetNode<Area3D>("EscapeDetectionBoundary");
		escapeReachedBoundary = GetNode<Area3D>("EscapeInReachBoundary");
		escapeReachedBoundary.AreaEntered += OnEscapeReached;
		partyReachedBoundary = GetNode<Area3D>("PartyBoundary");
		partyReachedBoundary.AreaEntered += OnEnterPartyZone;
		playerDetectionBoundary = GetNode<Area3D>("PlayerDetectionBoundary");
		carHitBoundary = GetNode<Area3D>("CarBoundary");
		carHitBoundary.BodyEntered += OnCarHit;
		bulletHitBoundary = GetNode<Area3D>("BulletBoundary");
		bulletHitBoundary.AreaEntered += OnBulletHit;
		playerDetectionBoundary.BodyEntered += OnPlayerDetectedInBounds;
		playerDetectionBoundary.BodyExited += OnPlayerLeftBounds;
		worldServer = GetNode<WorldServer>("/root/WorldServer");
		worldServer.BrainWashReleased += OnBrainwashReleased;

		// Make sure to not await during _Ready.
		Callable.From(ActorSetup).CallDeferred();
	}

	private async void ActorSetup()
	{
		// Wait for the first physics frame so the NavigationServer can sync.
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		navigationAgent.TargetPosition = originalTarget.GlobalPosition;
		returnToNormal();
	}

	private void DestroyMe()
	{
		GD.Print($"NPC: Destroyed : {state.ToString()}");
		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (state == NpcState.Splattered)
		{
			if (Time.GetTicksMsec() - timeSinceSplatted > lifePostSplatterInSec * 1000)
			{
				GD.Print("NPC: Splatted, despawning.");
				DestroyMe();
			}

			return;
		}

		if (state == NpcState.Stunned)
		{
			if (Time.GetTicksMsec() - timeSinceShot > stunTimeInSec * 1000)
			{
				state = NpcState.Brainwashed;
				alienBeingFledFrom = worldServer.PlayerRef;
				returnToNormal();
			}
		}

		navigationAgent.TargetPosition = GetTargetPosition();

		Vector3 currentAgentPosition = GlobalTransform.Origin;
		Vector3 nextPathPosition = navigationAgent.GetNextPathPosition();
		Vector3 dir = (nextPathPosition - currentAgentPosition).Normalized();

		// set speed based on state
		Vector3 velocity;
		switch (state)
		{
			case NpcState.Fearful:
			case NpcState.FleeingFromAlien:
				velocity = dir * runSpeed;
				break;
			case NpcState.Brainwashed:
				velocity = dir * brainwashedSpeed;
				break;
			case NpcState.Stunned:
				velocity = Vector3.Zero;
				break;
			case NpcState.Traveling:
			case NpcState.Loitering:
			default:
				velocity = dir * runSpeed;
				break;
		}

		// don't apply any movements when knocked
		if (state == NpcState.Splattered || state == NpcState.Stunned)
			return;

		try
		{
			RotateTowardsNextPoint(dir, delta);
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"{state.ToString()}|dir:{dir}|vel:{velocity}|{e.Message}");
		}

		MoveAndCollide(velocity * (float)delta, safeMargin: 0.01f);
	}

	private void RotateTowardsNextPoint(Vector3 dir, double delta)
	{
		// Calculate target rotation
		var pointInFrontOfNpc = GlobalPosition - dir;
		var targetWithoutY = new Vector3(pointInFrontOfNpc.X, GlobalPosition.Y, pointInFrontOfNpc.Z);

		// only if there is a target to look at
		if (	targetWithoutY == GlobalPosition || 
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
			case NpcState.Brainwashed:
				return GetBrainwashedTarget();
			case NpcState.Traveling:
			default:
				return originalTarget.GlobalPosition;
		}
	}

	private Vector3 GetBrainwashedTarget()
	{
		return (GetTree().GetFirstNodeInGroup("player") as Node3D).GlobalPosition;
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
		if (state == NpcState.Splattered || state == NpcState.Brainwashed)
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
		if (state == NpcState.Splattered  || state == NpcState.Stunned || state == NpcState.Brainwashed)
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
		GD.Print("NPC: escape reached");
		if (state == NpcState.Splattered || 
			state == NpcState.Stunned || 
			state == NpcState.Brainwashed ||
			state == NpcState.Traveling)
			return;
		
		GD.Print("NPC: Escaped");
		DestroyMe();

	}

	private void OnCarHit(Node3D body)
	{
		GD.Print("NPC: Hit and run");
		var direction = (GlobalPosition - body.GlobalPosition).Normalized();

		goLimp();

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
		if (state == NpcState.Splattered)
			return;

		GD.Print("NPC: Brain blasted");
		var direction = (GlobalPosition - body.GlobalPosition).Normalized();

		goLimp();

		// send the dude
		this.ApplyCentralImpulse(direction * gotHitImpulse);

		timeSinceShot = Time.GetTicksMsec();
		state = NpcState.Stunned;
	}

	private void OnBrainwashReleased()
	{
		state = NpcState.FleeingFromAlien;
		alienBeingFledFrom = worldServer.PlayerRef;
		returnToNormal();
	}

	private void OnEnterPartyZone(Area3D area)
	{
		if (state == NpcState.Brainwashed)
		{
			GD.Print("NPC: Party Zone Reached");
			DestroyMe();
		}
	}
	#endregion

	private void goLimp()
	{
		this.Freeze = false;
	}

	private void returnToNormal()
	{
		this.Freeze = true;
		GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
	}
}
