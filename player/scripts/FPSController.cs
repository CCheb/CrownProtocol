using Godot;
using System;

public partial class FPSController : CharacterBody3D
{
	[ExportGroup("Player Stats")]
	[Export] public float speed = 6.0f;
	
	[ExportGroup("Mouse Parameters")]
	[Export] public float MouseSensitivity = 0.1f;
	// How far down we can look
	[Export] public float TiltLowerLimit { get; set; } = Mathf.DegToRad(-90.0f);
	// How far up we can look
	[Export] public float TiltUpperLimit { get; set; } = Mathf.DegToRad(90.0f);

	[ExportGroup("Camera Settings")]
	// Camera controller that we will manipulate in script
	[Export] public CameraController WorldCameraController { get; set; }
	[Export] private InputLayer InputCameraLayer;
	static public float DefaultFov = 90;
	
	private bool InputIsMouse = false;
	private Vector3 totalMouseRotation;
	private float yawDelta;
	private float pitchDelta;
	private Vector3 horizontalRotation;
	
	// Used by sliding state
	public float _currentRotation;

	/* PLAYER API */
	//------------------------------------------
	[ExportGroup("Player API")]
	// Animation player node
	[Export] public AnimationPlayer ANIMATION;
	// Sphere shapecast above the player
	[Export] public  ShapeCast3D crouchShapeCast;
	// Reference so that the movement states are able to access the WeaponController
	// Player acts as the middle man between the movement and weapon states
	[Export] public WeaponController WEAPON;

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		
		if (@event.IsActionPressed("pause"))
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _Ready()
	{
		base._Ready();

		Globals.player = this; // Make this script globally accessible
	
		Input.MouseMode = Input.MouseModeEnum.Captured;
	
		WorldCameraController.Camera.Fov = DefaultFov;
		
		crouchShapeCast.AddException(this);	// Ignore ourselves
	}

	// _Input > UI > _UnhandledInput. We use _UnhandledInput here since we dont want any mouse movement
	// when we focus on a UI, menu, or button.
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (CurrentInputIsMouse(@event))
		{
			CalculateRotationDeltas((InputEventMouseMotion)@event);
		}
	}

	private bool CurrentInputIsMouse(InputEvent @event)
	{
		return InputIsMouse = (@event is InputEventMouseMotion) && (Input.MouseMode == Input.MouseModeEnum.Captured);
	}

	private void CalculateRotationDeltas(InputEventMouseMotion mouseMotion)
	{
		// Screen space to world space (2D -> 3D)!!!

		// Its important that we negate these values because turning right in screen space is + but in world space will
		// be negative. Thats why we take the screen space rotation and negate it over to world space rotation 
		yawDelta = -mouseMotion.Relative.X * MouseSensitivity;
		pitchDelta = -mouseMotion.Relative.Y * MouseSensitivity;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		UpdateRotations(delta); // Camera updates should be as fast as possible
	}

	private void UpdateRotations(double delta)
	{
		SetOverallRotation();
		RotatePlayer(delta);
		RotateCamera(delta);
		ResetRotationDeltas();
	}

	private void SetOverallRotation()
	{
		_currentRotation = yawDelta;
	}

	private void RotatePlayer(double delta)
	{
		// Player rotation, want horizontal rotation
		totalMouseRotation.Y += yawDelta * (float)delta; // Parse total mouse rotation.
		horizontalRotation = new Vector3(0.0f, totalMouseRotation.Y, 0.0f);
		Basis = Basis.FromEuler(horizontalRotation);
	}

	private void RotateCamera(double delta)
	{
		// Send the pitch values over to the InputCameraLayer. In this case the player owns side ways rotation while
		// the camera controller handles pitch which is the only rotation applied to the camera
		InputCameraLayer.AddPitch(pitchDelta * (float)delta);
	}

	private void ResetRotationDeltas()
	{
		// Dont want previous frame rotation inputs to affect the current frame
		yawDelta = 0.0f;
		pitchDelta = 0.0f;
	}



	//---CALLED BY OUR STATE SCRIPTS--//
	//--------------------------------//
	public void UpdateGravity(double delta)
	{
		// Only add gravity when in the air
		if (!IsOnFloor())
		{	
			// Its essential to only update the players Velocity and keep vel local 
			Vector3 velocity = Velocity;
			velocity += GetGravity() * (float)delta * 2.0f;
			Velocity = velocity;
		}
	}
	public void UpdateInput(float speed, float acceleration, float deceleration)
	{
		Vector3 velocity = Velocity;
		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		// want the direction to always be in terms of the local characters coordinate/basis. If we rotate
		// and move forward the input will rotate the same amount and move forward
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
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

		Velocity = velocity;
	}
	public void UpdateVelocity()
	{
		// Before we updated velocity here by doing Velocity = velocity for the sake of keeping
		// a global private velocity. That then caused the jump velocity to be cancelled since this
		// velocity did not know of any jumps (Y = 0) and overwrote the jump velocity

		// Called by each of the movement states
		MoveAndSlide();
		
	}


	




	
	

}
