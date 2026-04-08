using Godot;
using System;
using System.Reflection.Metadata;

public partial class Procedural : Node
{
    private Vector2 mouseMovementDelta = Vector2.Zero;
    private Vector2 bobAmount = Vector2.Zero;
    private float time = 0.0f;
    private float idleSwaySpeed = 1.2f;
    private float idleSwayAdjustment;
    private float idleSwayRotationStrength;
    private float idleSwayX;
    private float idleSwayY;
    private float idleSwayStrength;
    private float positionSwaySpeed;
    private float rotationSwaySpeed;
    private float mouseInputPositionAmount;
    private float mouseInputRotationAmount;
    private Vector2 mouseSwayMin;
    private Vector2 mouseSwayMax;
    private Vector3 weaponViewportPos;
    private Vector3 weaponViewportRot;
    private Globals.WeaponMovementProfle currentWeaponMovementProfile;

    private NoiseTexture2D randSwayNoise;
    public void SetMouseMovementDelta(InputEventMouseMotion mouseEvent)
    {
        mouseMovementDelta = mouseEvent.Relative;
    }

    public void SetRandSwayNoise(NoiseTexture2D randSwayNoise)
    {
        this.randSwayNoise = randSwayNoise;
    }

    public void SetCurrentWeaponMovementProfile(Globals.WeaponMovementProfle currentWeaponMovementProfile)
    {
        this.currentWeaponMovementProfile = currentWeaponMovementProfile;
    }
    
    public void ParseWeaponResource(in WeaponResource weaponResource)
    {
        weaponViewportPos = weaponResource.ViewportPosition;
        weaponViewportRot = weaponResource.ViewportRotation;
        idleSwayAdjustment = weaponResource.IdleSwayAdjustment;
        idleSwayRotationStrength = weaponResource.IdleSwayRotationStength;
        idleSwayStrength = weaponResource.IdleSwayAmmount;
        idleSwaySpeed = weaponResource.IdleSwaySpeed;
        mouseInputPositionAmount = weaponResource.MouseInputPositionOffset;
        mouseInputRotationAmount = weaponResource.MouseInputRotationAmount;
        positionSwaySpeed = weaponResource.PositionSwaySpeed;
        rotationSwaySpeed = weaponResource.RotationSwaySpeed;
        mouseSwayMin = weaponResource.MouseSwayMin;
        mouseSwayMax = weaponResource.MouseSwayMax; 
    }

    public void ApplyProceduralWeaponMovement(ref Vector3 weaponPos, ref Vector3 weaponRotDeg, double delta)
    {
        InterpolateMouseMovement(delta);

        if(currentWeaponMovementProfile.BobSpeed > 0.0f)
            CalculateWeaponBob(delta);
        

        CalculateWeaponSway(ref weaponPos, ref weaponRotDeg, currentWeaponMovementProfile.IsIdle, delta);
    }

    private void InterpolateMouseMovement(double delta)
    {
        mouseMovementDelta = mouseMovementDelta.Lerp(Vector2.Zero, (float)(delta * 6.0));
		mouseMovementDelta = mouseMovementDelta.Clamp(mouseSwayMin, mouseSwayMax);
    }

    private void CalculateWeaponBob(double delta)
    {
        // Time gives us a new value always
		time += (float)delta;

		// Sin(X/Y * frequency) * amplitude
		bobAmount.X = Mathf.Sin(time * currentWeaponMovementProfile.BobSpeed) * currentWeaponMovementProfile.HorizontalBobAmount;
		bobAmount.Y = Mathf.Abs(Mathf.Cos(time * currentWeaponMovementProfile.BobSpeed) * currentWeaponMovementProfile.VerticalBobAmount);
    }

    private void CalculateWeaponSway(ref Vector3 weaponPos, ref Vector3 weaponRotDeg, bool playerIsIdle, double delta)
    {
        // Only play random sway when in idle not when moving
		if (playerIsIdle)
		{
			float RandNoiseValue = GetRandNoiseValue();

			// create time with delta and set two sine values for x and y
			time += (float)delta * (idleSwaySpeed + RandNoiseValue); // Notice how we add Randomization
															 // Create a bit of random sin wave with AdjustedRandNoiseValue
			// The + and - provide a wave shift for more added randomness
			// The stronger the RandomSwayAmount the less suttle the total sway
            // RandNoiseValue is toned down with IdleSwayAdjustment
			idleSwayX = (float)Mathf.Sin(time * 1.5 + RandNoiseValue * idleSwayAdjustment) / idleSwayStrength;
			idleSwayY = (float)Mathf.Sin(time - RandNoiseValue * idleSwayAdjustment) / idleSwayStrength;
		}

        // Lerp weapon Pos based on mouse movement.
		// If MouseMovement is 0 then the only thing left would be the currentWeapon.Position.X/Y - RandomSwayX/Y
		weaponPos.X = (float)Mathf.Lerp(weaponPos.X, weaponViewportPos.X - (mouseMovementDelta.X *
			mouseInputPositionAmount + idleSwayX + (!playerIsIdle ? bobAmount.X : 0.0f)) * delta, positionSwaySpeed);
		weaponPos.Y = (float)Mathf.Lerp(weaponPos.Y, weaponViewportPos.Y - (mouseMovementDelta.Y *
			mouseInputPositionAmount + idleSwayY + (!playerIsIdle ? bobAmount.Y : 0.0f)) * delta, positionSwaySpeed);
		// Lerp weapon rotation based on mouse movement
		// Similar concept to position. If MouseMovement.X/Y is 0 then the only thing left would be the
		// CurrentWeapon.Rotation.Y/X +/- RandomSwayY/X * IdleSwayRotationStrength. This is what causes the idle sway
		weaponRotDeg.Y = (float)Mathf.Lerp(weaponRotDeg.Y, weaponViewportRot.Y - (mouseMovementDelta.X *
			mouseInputRotationAmount + (idleSwayY * idleSwayRotationStrength)) * delta, rotationSwaySpeed);
		weaponRotDeg.X = (float)Mathf.Lerp(weaponRotDeg.X, weaponViewportRot.X - (mouseMovementDelta.Y *
			mouseInputRotationAmount + (idleSwayX * idleSwayRotationStrength)) * delta, rotationSwaySpeed); 
    }

    private float GetRandNoiseValue()
    {
        if(randSwayNoise == null || randSwayNoise.Noise == null)
            return 0.0f;

        Vector3 playerPosition = Vector3.Zero;

        // Only access Globals when in play mode. 
		if (Globals.player != null)
        {
			playerPosition = Globals.player.GlobalPosition;
        }
        
        return randSwayNoise.Noise.GetNoise2D(playerPosition.X, playerPosition.Y);
    }

}
