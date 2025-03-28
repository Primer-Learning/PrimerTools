using Godot;

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

    public Animation ZoomTo(float distance)
    {
        return Camera.MoveTo(new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }

    #region Manipulation in play mode
    public float RotationSensitivity { get; set; } = 0.005f;
    public float PanSensitivity { get; set; } = 0.02f;
    public float ZoomSensitivity { get; set; } = 0.1f;
    public float ZoomMin { get; set; } = 1.0f;
    public float ZoomMax { get; set; } = 50.0f;
    public bool EnableDragRotation { get; set; } = true;
    public bool EnablePanning { get; set; } = true;
    public bool EnableZooming { get; set; } = true;
    public bool InvertRotationX { get; set; } = false;
    public bool InvertRotationY { get; set; } = true;
    public bool InvertPanX { get; set; } = false;
    public bool InvertPanY { get; set; } = false;
    public bool InvertZoom { get; set; } = false;
    
    private bool _isRotating = false;
    private bool _isPanning = false;
    private Vector2 _lastMousePosition;
    public override void _Ready()
    {
        base._Ready();
        SetProcessInput(true);
    }
    
    public override void _Input(InputEvent @event)
    {
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
                if (EnableDragRotation)
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
        
        if (_isRotating && EnableDragRotation)
        {
            // Apply horizontal rotation (around Y axis)
            float yRotationDirection = InvertRotationX ? 1 : -1;
            RotateY(yRotationDirection * delta.X * RotationSensitivity);
            
            // Apply vertical rotation (around X axis)
            float xRotationDirection = InvertRotationY ? -1 : 1;
            float xRotation = xRotationDirection * delta.Y * RotationSensitivity;
            
            // Rotate around the local X axis
            RotateObjectLocal(Vector3.Right, xRotation);
        }
        
        if (_isPanning && EnablePanning)
        {
            // Get the camera's local coordinate system
            Basis cameraBasis = GlobalTransform.Basis;
            
            // Get the right and up vectors from the camera's basis
            Vector3 right = cameraBasis.X.Normalized();
            Vector3 up = cameraBasis.Y.Normalized();
            
            float xDirection = InvertPanX ? 1 : -1;
            float yDirection = InvertPanY ? -1 : 1;
            
            Vector3 panOffset =
                right * xDirection * delta.X * PanSensitivity +
                up * yDirection * delta.Y * PanSensitivity;
                
            GlobalPosition += panOffset;
        }
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
