using Godot;
using Godot.Collections;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class MovementStateMachine : PlayerMovementState
{
    [Export] private State CURRENT_STATE;
    // Dictionary to hold any states that are children of the state machine
    // Keys are strings, values are Nodes that we need to cast over to State
    private Dictionary states = new Dictionary();
    private bool notFired = true;
    private PlayerContext context;

    public void SetContext(PlayerContext ctx)
    {
        context = ctx;
    }

    // Setup available states in _Ready()
    public override async void _Ready()
    {
        base._Ready();

        
        var player = GetParent<FPSController>();
        player.PlayerReady += OnPlayerReady;
        

        SetProcess(false);
        SetPhysicsProcess(false);


        // Grab any state children and determine if they are of state type (extend State class)
        // They are technically of type PlayerMovementState but it inherits from State
        foreach (Node child in GetChildren())
        {
            // If so then add them to the dictionary
            if (child is State)
            {
                states[child.Name] = child;
                // Make sure to subscribe the call back to each of the states transition signal
                // For each identified state and call their Init functions
                State transitionSignal = (State)child;
                transitionSignal.Transition += BroadcastStateTransition; // server only
                transitionSignal.Init();
            }
            else
                GD.PushWarning("State machine contains incompatible child node");
        }

        await ToSignal(Owner, "ready"); // Wait for player
    }

    private void OnPlayerReady()
    {
        SetProcess(true);
        SetPhysicsProcess(true);

        GD.Print("MovementStateMachine Ready!");

        // Enter the default state
        CURRENT_STATE.Enter(null);
    }


    // Call the state's process function
    public override void _Process(double delta)
    {
        base._Process(delta);
        CURRENT_STATE.Update(delta);    // The Update also contains the player update
    }

    // Call the states physics process function. This is whats constantly running along with _Process
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        CURRENT_STATE.PhysicsUpdate(delta);
    }
    
    // Call back when transition signal is emitted by any state
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void OnChildTransition(string newStateName)
    {
        // Try to find if passed state name is in the states dictionary
        if (states.ContainsKey(newStateName))
        {
            // if so then grab the state and check if its already the current state
            // if not then we gracefully exit the current state and enter the new state
            State newState = (State)states[newStateName];
            if (newState != CURRENT_STATE)
            {
                // "Load the new cartridge"
                CURRENT_STATE.Exit();
                newState.Enter(CURRENT_STATE);
                // So as to execute its update funcition in process
                CURRENT_STATE = newState;
                // Notify the Weapon Controller that the Current Movement State has changed
                context.weaponController.OnMovementStateChange(CURRENT_STATE);
            }
        }
        else
        {
            GD.PushWarning("State does not exist");
        }
    }

    public Globals.MovementStates GetCurrentStateName()
    {
        return CURRENT_STATE.GetStateName();
    }

    public bool IsShootingAnimationPlaying()
    {
        bool isShooting = (bool)context.player.characterAnimations.Get("parameters/conditions/shooting");
        bool isRunningShooting = (bool)context.player.characterAnimations.Get("parameters/conditions/runShoot");
        bool isJumpingShooting = (bool)context.player.characterAnimations.Get("parameters/conditions/jumpShoot");

        return isShooting || isRunningShooting || isJumpingShooting;
    }

    public void ToogleStateAnimation(bool condition)
    {
        CURRENT_STATE.ToggleAnimation(condition);
    }
    
    private void BroadcastStateTransition(string newStateName)
    {
        // Server is only one to call this
        Rpc(MethodName.OnChildTransition, newStateName);
    }
}
