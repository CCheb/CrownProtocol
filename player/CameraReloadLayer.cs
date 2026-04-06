using Godot;
using System;

public partial class CameraReloadLayer : CameraLayer
{
    /*
    Main concept fo the CameraReloadLayer: How far has the RealadCameraProxy moved relative to its neutral pose
    Whatever deltas are taken they are then passed over to the Position and Rotation Offsets
    */

    // ReloadProxy is a reference to the CameraReloadProxy inside of the Weapon tree which is the one that gets animated
    // Essentially we are animating a dummy object, reading its transforms and applying that forward
    public Node3D ReloadProxy;

    // Snapshot of the ReloadProxy as soon as it was assigned. This is to have a reference on its neutral position to better obtain the deltas
    // Without this the offsets would be absolute instead of relative
    private Transform3D _neutralTransform;

    public void SetProxy(Node3D proxy)
    {
        ReloadProxy = proxy;

        if (ReloadProxy != null)
        {   
            // As soon as the proxy is assigned we grab its neutral position. A holster animation would come in handy
            _neutralTransform = ReloadProxy.Transform;
        }
    }

    public override Vector3 PositionOffset
    {
        get
        {
            if (ReloadProxy == null)
                return Vector3.Zero;

            // How far has the proxy moved from where it started? If the reload animation has not started
            // then this calculation would just return 0 which means that 0 will be added as a layered offset in the CameraController
            return ReloadProxy.Transform.Origin - _neutralTransform.Origin;
        }
    }

    public override Vector3 RotationOffset
    {
        get
        {
            if (ReloadProxy == null)
                return Vector3.Zero;
            
            // Problem to solve: How much has the proxy rotated relative to its neutral pose (a very isolated rotation)
            // delta rotation relative to neutral
            // Inverse here when multiplied with the ReloadProxy basis helps extract the delta rotation between neutral and reload proxy
            // If the reload animation is not active then neutralTransform == ReloadProxy => identity matrix!
            Basis deltaBasis =
                _neutralTransform.Basis.Inverse() * ReloadProxy.Transform.Basis;

            // This would then return a Vector3.Zero
            return deltaBasis.GetEuler();
        }
    }
}
