using Godot;
using Godot.Collections;
using System;
using System.ComponentModel;
using System.Dynamic;

public partial class FPSController : CharacterBody3D
{
	[Signal] public delegate void PlayerReadyEventHandler();

	[ExportGroup("Player Stats")]
	[Export] public float speed = 6.0f;
	[Export] public float acceleration = 0.1f;
	[Export] public float deceleration = 0.25f;
	[Export] public float jumpVelocity = 8.5f;
	
	[ExportGroup("Mouse Parameters")]
	[Export] public float MouseSensitivity = 0.1f;
	[Export] public float TiltLowerLimit { get; set; } = Mathf.DegToRad(-90.0f);
	[Export] public float TiltUpperLimit { get; set; } = Mathf.DegToRad(90.0f); 

	[ExportGroup("Camera Settings")]
	[Export] public CameraController WorldCameraController;
	[Export] public Node3D cameraPivot;
	private Camera3D worldCamera;
	private Camera3D weaponCamera;
	static public float DefaultFov = 90;
	private bool InputIsMouse = false;
	private Vector3 totalMouseRotation;
	private float yawDelta;
	private float pitchDelta;
	private Vector3 horizontalRotation;
	private Vector3 verticalRotation;
	public float _currentRotation; // Used by sliding state

	[ExportGroup("Player API")]
	[Export] public AnimationPlayer ANIMATION;
	[Export] public  ShapeCast3D crouchShapeCast;
	[Export] public WeaponController WEAPON;

	[ExportGroup("Network")]
	[Export] public NetID myNetId;
	[Export] private MeshInstance3D visor;
	private PlayerInput input = new();
	private Vector3 lastPosition;
	public Vector3 DerivedVelocity { get; private set; }

	[ExportGroup("Misc")]
	[Export] private Label3D playerNameTag; 
	[Export] private MovementStateMachine stateMachine;
	private const int SERVER = 1;
	public PlayerContext context;

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		
		if (@event.IsActionPressed("pause") && myNetId.IsLocal)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

    public override void _EnterTree()
    {
        base._EnterTree();
		context = new PlayerContext
		{
			player = this,
			camera = WorldCameraController,
			weapon = WEAPON
		};

		WEAPON.SetContext(context);
		WorldCameraController.SetContext(context);
		stateMachine.SetContext(context);
    }

	public override void _Ready()
	{
		base._Ready();

		myNetId.NetIdIsReady += OnNetIdReady;

		Globals.player = this; // Make this script globally accessible
		
		crouchShapeCast.AddException(this);	// Ignore ourselves

		lastPosition = GlobalPosition;
	}

	private void OnNetIdReady()
	{
		if(myNetId.IsLocal)
			SetAsLocalPlayer();
		else
			SetAsNonLocalPlayer();
		
		SetPlayerNameTag();

		EmitSignalPlayerReady();
	}

	private void SetAsLocalPlayer()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		WorldCameraController.Camera.Fov = DefaultFov;

		visor.Visible = false;
		playerNameTag.Visible = false;
		
	}

	private void SetAsNonLocalPlayer()
	{
		worldCamera = GetNode<Camera3D>("CameraPivot/WorldCameraController/Camera3D");
		weaponCamera = GetNode<Camera3D>("SubViewportContainer/SubViewport/Camera3D");
		
		worldCamera.Current = false;
		weaponCamera.Current = false;

		visor.Visible = true;
	}

	private void SetPlayerNameTag()
	{
		playerNameTag.Text = GenericCore.Instance.connectedPeers[myNetId.OwnerId]["UserName"];
	}

	// _Input > UI > _UnhandledInput. We use _UnhandledInput here since we dont want any mouse movement
	// when we focus on a UI, menu, or button.
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (CurrentInputIsMouse(@event) && myNetId.IsLocal)
		{
			CalculateRotationDeltas((InputEventMouseMotion)@event);
		}
	}

	private bool CurrentInputIsMouse(InputEvent @event)
	{
		return (@event is InputEventMouseMotion) && (Input.MouseMode == Input.MouseModeEnum.Captured);
	}

	private void CalculateRotationDeltas(InputEventMouseMotion mouseMotion)
	{
		// Screen space to world space (2D -> 3D)!!!

		// Its important that we negate these values because turning right in screen space is + but in world space will
		// be negative. Thats why we take the screen space rotation and negate it over to world space rotation 
		input.yawDelta = -mouseMotion.Relative.X * MouseSensitivity;
		input.pitchDelta = -mouseMotion.Relative.Y * MouseSensitivity;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if(myNetId.IsLocal)
		{	
			GenerateAndSendMovementInput();
			ResetRotationDeltas();
		}

		if(GenericCore.Instance.IsServer)
			ApplyInput(delta);

	}

	private void GenerateAndSendMovementInput()
	{
		// Simply grab the input and pass it forward for calculation
		Vector2 move = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
    	bool jump = Input.IsActionJustPressed("jump");
		RpcId(SERVER, MethodName.SendInput, move, jump, input.yawDelta, input.pitchDelta);
	}

	private void ResetRotationDeltas()
	{
		// Dont want previous frame rotation inputs to affect the current frame
		input.yawDelta = 0.0f;
		input.pitchDelta = 0.0f;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SendInput(Vector2 move, bool jump, float yawDelta, float pitchDelta)
	{
		if(!GenericCore.Instance.IsServer)
			return;

		// From client to client representation on the server side
		input.move = move;
		input.jump = jump;
		input.yawDelta = yawDelta;
		input.pitchDelta = pitchDelta;
	}

	private void ApplyInput(double delta)
	{
		CalculateMovement(delta);
		CalculateRotations(delta);
	}

	private void CalculateDerivedVelocity(double delta)
	{
		DerivedVelocity = (GlobalPosition - lastPosition) / (float)delta;
		lastPosition = GlobalPosition;
	}

	private void CalculateMovement(double delta)
	{
		
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump. This is more of a one time thing
		if (input.jump && IsOnFloor())
		{
			velocity.Y = jumpVelocity;
		}

		Vector3 direction = (Transform.Basis * new Vector3(input.move.X, 0, input.move.Y)).Normalized();

		
		if (direction != Vector3.Zero)
		{
			// You will reach the maximum speed over time (linearly) and not instantly
			// Its important for the first argument to be changing or else it will
			// never move to its intended target speed. The current velocity will always be changing
			velocity.X = Mathf.Lerp(velocity.X, direction.X * speed, acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * speed, deceleration);
		}
		else
		{
			// MoveToward is like Lerp in that it provides movement smoothing. In this case
			// when the player stops moving it will smoothing approach 0. We provide 
			// a decelaration weight so in the case we want the player to decelerate at a different
			// speed when compared to acceleration 
			velocity.X = Mathf.MoveToward(Velocity.X, 0, deceleration);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, deceleration);
		}
		

		//velocity.X = direction.X * speed;
		//velocity.Z = direction.Z * speed;


		Velocity = velocity;

		// Calculates/simulates the movement 
		MoveAndSlide();
	}

	private void CalculateJump(float jumpVelocity)
	{   
		Vector3 velocity = Velocity;
		// Handle Jump. This is more of a one time thing
		if (input.jump && IsOnFloor())
		{
			velocity.Y = jumpVelocity;
		}
	}

	private void CalculateRotations(double delta)
	{
		RotatePlayer(delta);
		RotateCamera(delta);
	}

	private void SetOverallRotation()
	{
		_currentRotation = yawDelta;
	}

	private void RotatePlayer(double delta)
	{
		// Player rotation, want horizontal rotation
		totalMouseRotation.Y += input.yawDelta * (float)delta; // Parse total mouse rotation.
		horizontalRotation = new Vector3(0.0f, totalMouseRotation.Y, 0.0f);
		Basis = Basis.FromEuler(horizontalRotation);
	}

	private void RotateCamera(double delta)
	{
		// Camera rotation, want vertical rotation
		totalMouseRotation.X += input.pitchDelta * (float)delta; // Parse total mouse rotation
		totalMouseRotation.X = Mathf.Clamp(totalMouseRotation.X, Mathf.DegToRad(-90.0f), Mathf.DegToRad(90.0f));
		verticalRotation = new Vector3(totalMouseRotation.X, 0.0f, 0.0f);	// In Radians

		cameraPivot.Rotation = verticalRotation;
	}


	//---Single Player stuff--//
	//--------------------------------//
	public void UpdateGravity(double delta)
	{
	}

	public void UpdateInput(float speed, float acceleration, float deceleration)
	{
		
	}

	public void UpdateVelocity()
	{
		
	}
}
