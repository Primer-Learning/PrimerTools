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
    [Export] public float PanSensitivity { get; set; } = 0.02f;
    [Export] public float ZoomSensitivity { get; set; } = 0.1f;
    [Export] public float ZoomMin { get; set; } = 1.0f;
    [Export] public float ZoomMax { get; set; } = 1000.0f;
    [Export] public bool EnableYaw { get; set; } = true;
    [Export] public bool EnablePitch { get; set; } = true;
    [Export] public bool EnablePanning { get; set; } = true;
    /// <summary>When true, panning glides along the world XZ plane instead of the camera-local screen plane.</summary>
    [Export] public bool FlatPan { get; set; } = false;
    [Export] public bool EnableZooming { get; set; } = true;
    [Export] public bool InvertRotationX { get; set; } = false;
    [Export] public bool InvertRotationY { get; set; } = true;
    [Export] public bool InvertPanX { get; set; } = false;
    [Export] public bool InvertPanY { get; set; } = false;
    [Export] public bool InvertZoom { get; set; } = false;

    private bool _isRotating = false;
    private bool _isPanning = false;
    private Vector2 _lastMousePosition;

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
                        _lastMousePosition = mouseButtonEvent.Position;
                }
                break;

            case MouseButton.Middle:
                if (EnablePanning)
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

        if (_isRotating)
        {
            if (EnableYaw)
            {
                float yRotationDirection = InvertRotationX ? 1 : -1;
                RotateY(yRotationDirection * delta.X * RotationSensitivity);
            }

            if (EnablePitch)
            {
                float xRotationDirection = InvertRotationY ? -1 : 1;
                float xRotation = xRotationDirection * delta.Y * RotationSensitivity;
                RotateObjectLocal(Vector3.Right, xRotation);
            }
        }

        if (_isPanning && EnablePanning)
        {
            Basis cameraBasis = GlobalTransform.Basis;

            Vector3 right;
            Vector3 up;
            if (FlatPan)
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

            Vector3 panOffset =
                right * xDirection * delta.X * PanSensitivity +
                up * yDirection * delta.Y * PanSensitivity;

            if (FlatPan) panOffset.Y = 0;

            GlobalPosition += panOffset;
        }
    }

    private static Vector3 FlattenXZ(Vector3 v)
    {
        v.Y = 0;
        return v.LengthSquared() > 0.0001f ? v.Normalized() : Vector3.Zero;
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
