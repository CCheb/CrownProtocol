using Godot;
using Godot.Collections;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;

public partial class FPSController : CharacterBody3D, IEnemy
{
	[Signal] public delegate void PlayerReadyEventHandler();

	[ExportGroup("Player Stats")]
	[Export] public float health = 100.0f;
	[Export] public float score = 0.0f;
	[Export] public int deaths = 0;
	[Export] public int kills = 0;
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
	[Export] public AnimationTree characterAnimations;

	[ExportGroup("Network")]
	[Export] public NetID myNetId;
	[Export] private Node3D characterModel;
	private PlayerInput input = new();
	private Vector3 lastPosition;
	public Vector3 DerivedVelocity { get; private set; }

	[ExportGroup("Misc")]
	[Export] private Label3D playerNameTag; 
	[Export] private MovementStateMachine stateMachine;
	[Export] private FPSPauseMenu pauseMenu;
	[Export] private CollisionShape3D hitBox;
	[Export] private AudioStreamPlayer3D hitMarkerPing;
	[Export] private AudioStreamPlayer3D killBell;
	[Export] public bool deathConfirmed = false;
	private Node3D lookAtRef;
	private bool isInPauseMenu;
	private const int SERVER = 1;
	public PlayerContext context;

	// UI Additions
	[Export] public ProgressBar healthBar;
	[Export] private Color segmentColors;
	[Export] public ProgressBar xpBar;
	[Export] public TextureProgressBar captureBar;
	[Export] private int captureTime = 60;
	private bool isCapturing = false;
	[Export] private GridContainer leaderboard;
	[Export] private Label[] playerNameLabels;
	[Export] private Label[] pointsLabels;
	[Export] private Label[] killsLabels;
	[Export] private Label[] deathsLabels;
	[Export] private AnimationPlayer hitFlashUI;

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		
		if (@event.IsActionPressed("pause") && myNetId.IsLocal)
		{
			//Input.MouseMode = Input.MouseModeEnum.Visible;
			GD.Print($"client kills {kills}");
			pauseMenu.UnHideMenu();
			isInPauseMenu = true;
		}
	}

    public override void _EnterTree()
    {
        base._EnterTree();
		context = new PlayerContext
		{
			player = this,
			cameraController = WorldCameraController,
			weaponController = WEAPON,
			movementStateMachine = stateMachine
		};

		WEAPON.SetContext(context);
		WorldCameraController.SetContext(context);
		stateMachine.SetContext(context);
    }

	public override void _Ready()
	{
		base._Ready();

		myNetId.NetIdIsReady += OnNetIdReady;

		//Globals.player = this; // Make this script globally accessible
		
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

	private void OnResumeButtonClicked()
	{
		isInPauseMenu = false;
	}

	private void SetAsLocalPlayer()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		WorldCameraController.Camera.Fov = DefaultFov;

		characterModel.Visible = false;
		playerNameTag.Visible = false;

		pauseMenu.ResumeButtonClicked += OnResumeButtonClicked;

		// set segment color to current color of the Fill StyleBox of sec1
		StyleBoxFlat fillStyle = (StyleBoxFlat)healthBar.GetThemeStylebox("fill");
		segmentColors = fillStyle.BgColor;

		captureBar.Visible = false;
		captureBar.MaxValue = captureTime;

		healthBar.Value = health;

		xpBar.Value = score;

		// initialize leaderboard
		UpdateLeaderboard();
	}

	private void SetAsNonLocalPlayer()
	{
		worldCamera = GetNode<Camera3D>("CameraPivot/WorldCameraController/Camera3D");
		weaponCamera = GetNode<Camera3D>("SubViewportContainer/SubViewport/Camera3D");
		
		worldCamera.Current = false;
		weaponCamera.Current = false;

		characterModel.Visible = true;
	}

	private void SetPlayerNameTag()
	{
		playerNameTag.Text = GenericCore.Instance.connectedPeers[myNetId.OwnerId]["UserName"];
	}

	private void UpdateLeaderboard()
	{
		if (!myNetId.IsLocal || leaderboard == null)
			return;

		// collect all ready players
		Godot.Collections.Array<Node> allPlayerNodes = GetTree().GetNodesInGroup("Enemies");
		var allPlayers = new System.Collections.Generic.List<FPSController>();

		foreach (var node in allPlayerNodes)
		{
			if (node is FPSController player && player.myNetId != null &&
				GenericCore.Instance.connectedPeers.ContainsKey(player.myNetId.OwnerId))
			{
				allPlayers.Add(player);
			}
		}

		// sort by OwnerId so every client sees the exact same order
		allPlayers.Sort((a, b) => a.myNetId.OwnerId.CompareTo(b.myNetId.OwnerId));

		// populate the 4 rows
		for (int i = 0; i < 4; i++)
		{
			if (i < allPlayers.Count)
			{
				FPSController p = allPlayers[i];
				long ownerId = p.myNetId.OwnerId;

				playerNameLabels[i].Text = (string)GenericCore.Instance.connectedPeers[ownerId]["UserName"];
				pointsLabels[i].Text = p.score.ToString("0");
				killsLabels[i].Text = p.kills.ToString();
				deathsLabels[i].Text = p.deaths.ToString();
			}
			else
			{
				// empty slot
				playerNameLabels[i].Text = "-";
				pointsLabels[i].Text = "-";
				killsLabels[i].Text = "-";
				deathsLabels[i].Text = "-";
			}
		}
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

		if(myNetId.IsLocal && !isInPauseMenu && !deathConfirmed)
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

	// PLAYER INTERACTIONS 
	public void Hit(float damageRecieved, long senderId, long receiverId)
	{
		// Hit is handled by the server

		if (deathConfirmed)
		{
			return;
		}

		if (health - damageRecieved <= 0)
		{
		
			foreach(var enemy in GetTree().GetNodesInGroup("Enemies"))
			{
				if(enemy is FPSController player && player.myNetId.OwnerId == senderId)
				{
					player.kills++;
					player.score += 5;
					GD.Print($"Player should have {player.kills} kills");
					lookAtRef = player.hitBox;
				}
			}
			
			// Server sets these variables and MultiplayerSynchronizer handles the rest
			health = 0.0f;
			deaths++;
			deathConfirmed = true;	

			StartRespawnSequence();
		}
		else
		{
			health -= damageRecieved;
		}	

		// Send Rpc's to both sender and receiver players to update their UI
		if (receiverId == Multiplayer.GetUniqueId())
		{
			//OnReceiverHitUpdateUI();
		}
		else
		{
			RpcId(receiverId, MethodName.OnReceiverHitUpdateUI, health, deathConfirmed);
		}
		if (senderId == Multiplayer.GetUniqueId())
		{
			//OnSenderHitUpdateUI(deathConfirmed);
		}
		else if (senderId != -1)
		{
			RpcId(senderId, MethodName.OnSenderHitUpdateUI, deathConfirmed);
		}

		if (GenericCore.Instance.IsServer)
		{
			Godot.Collections.Array<Node> allPlayerNodes = GetTree().GetNodesInGroup("Enemies");
			foreach (var node in allPlayerNodes)
			{
				if (node is FPSController player)
				{
					player.Rpc(MethodName.RefreshLeaderboard);
				}
			}
		}
	}

	public void HitByEnemy(float damageRecieved, long receiverId)
	{
		if(deathConfirmed)
		{
			return;
		}

		if (health - damageRecieved <= 0)
		{			
			// Server sets these variables and MultiplayerSynchronizer handles the rest
			health = 0.0f;
			deaths++;
			deathConfirmed = true;	

			StartRespawnSequence();
		}
		else
		{
			GD.Print(health);
			health -= damageRecieved;
		}	
		if (receiverId == Multiplayer.GetUniqueId())
		{
			OnReceiverHitUpdateUI(health, deathConfirmed);
		}
		else
		{
			RpcId(receiverId, MethodName.OnReceiverHitUpdateUI, health, deathConfirmed);
		}
		if (GenericCore.Instance.IsServer)
		{
			Godot.Collections.Array<Node> allPlayerNodes = GetTree().GetNodesInGroup("Enemies");
			foreach (var node in allPlayerNodes)
			{
				if (node is FPSController player)
				{
					player.Rpc(MethodName.RefreshLeaderboard);
				}
			}
		}
	}

	private async void StartRespawnSequence()
	{
		await ToSignal(GetTree().CreateTimer(5), "timeout");

		var gameManager = GetTree().CurrentScene as GameManager;
		Node3D randomSpawn = gameManager.RequestRandomPlayerSpawn();

		deathConfirmed = false;
		health = 100.0f;

		GlobalTransform = randomSpawn.GlobalTransform;
	}

	// Timer
	private async void Capture()
	{
		if (!isCapturing || !myNetId.IsLocal) return;
		if(captureBar.Visible == false) captureBar.Visible = true;
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		// lerp capture bar up 1
		captureBar.Value = Mathf.Lerp(captureBar.Value, captureBar.Value + 1, 0.8f);
		if (captureBar.Value >= captureBar.MaxValue)
		{
			GD.Print("I WIN!");

			// TODO:
			// send win signal to server
			// server handles end logic and sends winner to all clients
			// clients show end game screen with winning player and disables player movement
			// buttons to continue or leave game show and function accordingly

			return;
		}
		// recursive call to keep checking isCapturing each second
		Capture();

	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void OnReceiverHitUpdateUI(float newHealth, bool deathCon)
	{
		if(!myNetId.IsLocal)
			return;

		if (deathCon)
		{
			// Maybe start timer here and show death screen
			hitFlashUI.Play("die");
		}
			
		GD.Print("I got hit! Update UI here!");
		GD.Print(newHealth);

		if (!hitFlashUI.IsPlaying())
			hitFlashUI.Play("hit");

		healthBar.Value = newHealth;
		GD.Print("NEW HEALTH BAR VALUE: " + healthBar.Value);
		
		// color lerp system based on curent health percentage
		if(newHealth > 0)
		{
			float ratio = newHealth / 100.0f;
			StyleBoxFlat fillStyle;
			fillStyle = healthBar.GetThemeStylebox("fill").Duplicate() as StyleBoxFlat;
			// Lerp from Red (0%) to Green (100%)
			fillStyle.BgColor = Colors.Red.Lerp(segmentColors, ratio);

			healthBar.AddThemeStyleboxOverride("fill", fillStyle);
		}
		

	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void OnSenderHitUpdateUI(bool deathConfirmed)
	{

		if (deathConfirmed)
		{
			killBell?.Play();
			xpBar.Value = score;
			GD.Print("XP value:" + xpBar.Value);
			GD.Print("score:" + score);
		}
		else
		{
			hitMarkerPing?.Play();
		}

	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RefreshLeaderboard()
	{
		if (myNetId.IsLocal)
		{
			UpdateLeaderboard();
		}
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
