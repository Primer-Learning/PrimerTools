using Godot;
using PrimerTools.TweenSystem;

namespace PrimerTools;

[Tool]
public partial class CameraRig : Node3D
{
    private const float DefaultDistance = 10;
    private const float DefaultFov = 30;
    
    private Camera3D _camera;
    public Camera3D Camera
    {
        get
        {
            if (!IsInsideTree()) return null;
            // Try to get it from the scene
            _camera = GetNodeOrNull<Camera3D>("Camera3D");
            if (_camera != null) return _camera;
            
            // If the camera is not in the scene, create a new one
            _camera = new Camera3D();
            _camera.Fov = DefaultFov;
            AddChild(_camera);
            _camera.Owner = GetTree().EditedSceneRoot;
            _camera.Position = Vector3.Back * DefaultDistance;
            _camera.Name = "Camera3D";
            
            return _camera;
        }
    }

    [Export]
    public float Distance
    {
        get => Camera is null ? DefaultDistance : Camera.Position.Z;
        set
        {
            if (Camera is null) return; 
            Camera.Position = new Vector3(Camera.Position.X, Camera.Position.Y, value);
        }
    }

    public Animation ZoomToAnimationDeprecated(float distance)
    {
        return Camera.MoveToAnimation(new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }
    
    public IStateChange ZoomTo(float distance)
    {
        return new PropertyStateChange(Camera, "position", new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }

    #region Manipulation in play mode
    [Export] public float RotationSensitivity { get; set; } = 0.005f;
    /// <summary>Unitless multiplier on top of the zoom-aware pan rate. 1.0 = mouse tracks the world 1:1 at the rig's depth.</summary>
    [Export] public float PanSensitivity { get; set; } = 1.0f;
    [Export] public float ZoomSensitivity { get; set; } = 0.1f;
    [Export] public float ZoomMin { get; set; } = 1.0f;
    [Export] public float ZoomMax { get; set; } = 1000.0f;
    [Export] public bool EnableYaw { get; set; } = true;
    [Export] public bool EnablePitch { get; set; } = true;
    /// <summary>Minimum pitch in degrees. 0 = horizontal, 90 = looking straight down.</summary>
    [Export] public float PitchMin { get; set; } = 0f;
    /// <summary>Maximum pitch in degrees. 0 = horizontal, 90 = looking straight down.</summary>
    [Export] public float PitchMax { get; set; } = 90f;
    public enum PanMode
    {
        /// <summary>Panning disabled.</summary>
        Locked,
        /// <summary>Pan on the camera's local screen plane (drag follows the cursor in screen space).</summary>
        Free,
        /// <summary>Pan along the world XZ plane (Y stays fixed).</summary>
        Flat,
        /// <summary>Pan along XZ, then snap Y to a downward physics raycast hit. Y stays put if nothing is hit.</summary>
        RaycastDown,
    }
    [Export] public PanMode PanningMode { get; set; } = PanMode.Free;
    /// <summary>Collision layers the RaycastDown pan mode tests against.</summary>
    [Export(PropertyHint.Layers3DPhysics)] public uint PanRaycastMask { get; set; } = uint.MaxValue;
    [Export] public bool EnableZooming { get; set; } = true;
    [Export] public bool InvertRotationX { get; set; } = false;
    [Export] public bool InvertRotationY { get; set; } = true;
    [Export] public bool InvertPanX { get; set; } = false;
    [Export] public bool InvertPanY { get; set; } = false;
    [Export] public bool InvertZoom { get; set; } = false;

    private bool _isRotating = false;
    private bool _isPanning = false;
    private Vector2 _lastMousePosition;
    private float _yaw;
    private float _pitch;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SceneRecorder.IsOn) return;
        if (!Camera.Current) return;
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            HandleMouseButtonEvent(mouseButtonEvent);
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            HandleMouseMotionEvent(mouseMotionEvent);
        }
    }
    
    private void HandleMouseButtonEvent(InputEventMouseButton mouseButtonEvent)
    {
        switch (mouseButtonEvent.ButtonIndex)
        {
            case MouseButton.Left:
                if (EnableYaw || EnablePitch)
                {
                    _isRotating = mouseButtonEvent.Pressed;
                    if (_isRotating)
                    {
                        _lastMousePosition = mouseButtonEvent.Position;
                        _yaw = Rotation.Y;
                        _pitch = Rotation.X;
                    }
                }
                break;
                
            case MouseButton.Middle:
                if (PanningMode != PanMode.Locked)
                {
                    _isPanning = mouseButtonEvent.Pressed;
                    if (_isPanning)
                        _lastMousePosition = mouseButtonEvent.Position;
                }
                break;
                
            case MouseButton.WheelUp:
                if (EnableZooming)
                {
                    float zoomDirection = InvertZoom ? 1 : -1;
                    Zoom(zoomDirection * ZoomSensitivity);
                }
                break;
                
            case MouseButton.WheelDown:
                if (EnableZooming)
                {
                    float zoomDirection = InvertZoom ? -1 : 1;
                    Zoom(zoomDirection * ZoomSensitivity);
                }
                break;
        }
    }
    
    private void HandleMouseMotionEvent(InputEventMouseMotion mouseMotionEvent)
    {
        Vector2 delta = mouseMotionEvent.Position - _lastMousePosition;
        _lastMousePosition = mouseMotionEvent.Position;
        
        if (_isRotating && (EnableYaw || EnablePitch))
        {
            if (EnableYaw)
            {
                float yRotationDirection = InvertRotationX ? 1 : -1;
                _yaw += yRotationDirection * delta.X * RotationSensitivity;
            }

            if (EnablePitch)
            {
                float xRotationDirection = InvertRotationY ? -1 : 1;
                _pitch += xRotationDirection * delta.Y * RotationSensitivity;
                _pitch = Mathf.Clamp(_pitch, -Mathf.DegToRad(PitchMax), -Mathf.DegToRad(PitchMin));
            }

            Rotation = new Vector3(_pitch, _yaw, 0);
        }

        if (_isPanning && PanningMode != PanMode.Locked)
        {
            // Get the camera's local coordinate system
            Basis cameraBasis = GlobalTransform.Basis;

            Vector3 right;
            Vector3 up;
            bool flatten = PanningMode == PanMode.Flat || PanningMode == PanMode.RaycastDown;
            if (flatten)
            {
                right = FlattenXZ(cameraBasis.X);
                up = FlattenXZ(-cameraBasis.Z);
            }
            else
            {
                right = cameraBasis.X.Normalized();
                up = cameraBasis.Y.Normalized();
            }

            float xDirection = InvertPanX ? 1 : -1;
            float yDirection = InvertPanY ? -1 : 1;

            float worldPerPixel = GetWorldUnitsPerPixel();

            Vector3 panOffset =
                right * xDirection * delta.X * worldPerPixel * PanSensitivity +
                up * yDirection * delta.Y * worldPerPixel * PanSensitivity;

            if (flatten) panOffset.Y = 0;

            GlobalPosition += panOffset;

            if (PanningMode == PanMode.RaycastDown)
            {
                SnapYToGround();
            }
        }
    }

    private static Vector3 FlattenXZ(Vector3 v)
    {
        v.Y = 0;
        return v.LengthSquared() > 0.0001f ? v.Normalized() : Vector3.Zero;
    }

    private const float RaycastReach = 1_000_000f;

    private void SnapYToGround()
    {
        var space = GetWorld3D()?.DirectSpaceState;
        if (space == null) return;

        Vector3 origin = GlobalPosition + Vector3.Up * RaycastReach;
        Vector3 end = GlobalPosition + Vector3.Down * RaycastReach;
        var query = PhysicsRayQueryParameters3D.Create(origin, end, PanRaycastMask);
        var hit = space.IntersectRay(query);
        if (hit.Count == 0) return;

        Vector3 hitPoint = hit["position"].AsVector3();
        Vector3 pos = GlobalPosition;
        pos.Y = hitPoint.Y;
        GlobalPosition = pos;
    }

    /// <summary>
    /// World units spanned by one screen pixel at the rig's depth. Lets pan track the cursor 1:1
    /// regardless of zoom: drag the mouse N pixels, the rig moves the same world distance the cursor
    /// would have traversed over the plane through the rig's origin.
    /// </summary>
    private float GetWorldUnitsPerPixel()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        bool useWidth = Camera.KeepAspect == Camera3D.KeepAspectEnum.Width;
        float pixelExtent = useWidth ? viewportSize.X : viewportSize.Y;
        if (pixelExtent <= 0) return 0;

        if (Camera.Projection == Camera3D.ProjectionType.Orthogonal)
        {
            return Camera.Size / pixelExtent;
        }

        float halfFovRad = Mathf.DegToRad(Camera.Fov) * 0.5f;
        return 2f * Mathf.Abs(Distance) * Mathf.Tan(halfFovRad) / pixelExtent;
    }

    private void Zoom(float amount)
    {
        float newDistance = Mathf.Clamp(
            Distance + amount * Distance,
            ZoomMin,
            ZoomMax
        );
        
        Distance = newDistance;
    }
    #endregion
}
