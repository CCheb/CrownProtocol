using Godot;
using System;

public partial class InputLayer : CameraLayer
{
    /*
    Player input is split into two parts: horizontal and vertical rotatation.
    Horizontal is handled by the player and it essentially rotates the player itself.
    Vertical recoil strickly rotates the camera up and down and that whats handled in the InputLayer
    */

    // Since this is vertical rotation we need to set some limits on how far up and down the player rotates. 
    // In this case they can look directly up and down
    private const float TiltLowerLimit = -Mathf.Pi / 2f;
    private const float TiltUpperLimit =  Mathf.Pi / 2f;    

    // _pitch the internal variable that changes everytime the player looks around
    // _tilt IS IN RADIANS RIGHT NOW
    private float _tilt = 0.0f;

    // RotationOffset serves as the unifed property that all CameraLayers must initialize/use for the CameraController
    // In this case we create a vector with the pitch value to represent only up and down rotation (X-axis).
    // From the perspective of the CameraController we simply apply (not add) the rotation offset 
    public override Vector3 RotationOffset => new Vector3(_tilt, 0.0f, 0.0f);

    // AddPitch gets called everytime the player rotates/looks around
    public void AddPitch(float deltaTilt)
    {
        // We add the new rotation to the current base one
        // and make sure to clamp the value in the case it went overboard
        _tilt += deltaTilt;
        _tilt = Mathf.Clamp(_tilt, TiltLowerLimit, TiltUpperLimit);
        //GD.Print(Mathf.RadToDeg(_tilt));
    }
}
