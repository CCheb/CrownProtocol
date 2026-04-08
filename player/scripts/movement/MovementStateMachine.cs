using Godot;
using Godot.Collections;
using System;

public partial class MovementStateMachine : PlayerMovementState
{// Specify the default state to be current. Its an export
    [Export] private State CURRENT_STATE;
    // Dictionary to hold any states that are children of the state machine
    // Keys are strings, values are Nodes that we need to cast over to State
    private Dictionary states = new Dictionary();
    private bool notFired = true;

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
                transitionSignal.Transition += OnChildTransition;
                transitionSignal.Init();
            }
            else
                GD.PushWarning("State machine contains incompatible child node");
        }

        // Wait for the owner (Player) to get ready before proceeding
        await ToSignal(Owner, "ready");
        // After initial setup, Enter the default state
        // CURRENT_STATE.Enter(null);
    }

    private void OnPlayerReady()
    {
        SetProcess(true);
        SetPhysicsProcess(true);

        GD.Print("MovementStateMachine Ready!");

        CURRENT_STATE.Enter(null);
    }


    // Call the state's process function
    public override void _Process(double delta)
    {
        base._Process(delta);
        CURRENT_STATE.Update(delta);    // The Update also contains the player update
        //Globals.debug.AddDebugProperty("Current State", CURRENT_STATE.Name, 1); 
    }

    // Call the states physics process function. This is whats constantly running along with _Process
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        CURRENT_STATE.PhysicsUpdate(delta);
    }
    
    // Call back when transition signal is emitted by any state
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
                WEAPON_CONTROLLER.EmitSignal(WeaponController.SignalName.MovementChanged, CURRENT_STATE);
            }
        }
        else
        {
            GD.PushWarning("State does not exist");
        }
    }
}
