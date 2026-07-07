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
            SyncOrthoSize();
        }
    }

    /// <summary>
    /// Orthographic projection toggle. Switching preserves the apparent framing:
    /// the ortho size is derived from Distance and the camera's FOV so the plane
    /// through the rig's origin spans the same screen extent in both projections.
    /// Distance stays the single zoom state — wheel zoom keeps working in ortho
    /// because the ortho size tracks Distance.
    /// </summary>
    public bool Orthographic
    {
        get => Camera != null && Camera.Projection == Camera3D.ProjectionType.Orthogonal;
        set
        {
            if (Camera is null) return;
            Camera.Projection = value
                ? Camera3D.ProjectionType.Orthogonal
                : Camera3D.ProjectionType.Perspective;
            SyncOrthoSize();
        }
    }

    private void SyncOrthoSize()
    {
        if (Camera is null || Camera.Projection != Camera3D.ProjectionType.Orthogonal) return;
        Camera.Size = 2f * Mathf.Abs(Distance) * Mathf.Tan(Mathf.DegToRad(Camera.Fov) * 0.5f);
    }

    public Animation ZoomToAnimationDeprecated(float distance)
    {
        return Camera.MoveToAnimation(new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }
    
    public IStateChange ZoomTo(float distance)
    {
        return new PropertyStateChange(Camera, "position", new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }

    /// <summary>
    /// Top-down pitch that stays a hair off exact vertical so camera-relative
    /// controls (forward = camera forward projected onto XZ) keep a defined
    /// direction while looking straight down.
    /// </summary>
    public const float TopDownPitchRadians = -Mathf.Pi / 2f + 0.01f;

    private Tween _pitchTween;

    /// <summary>
    /// Tweens the rig's pitch to <paramref name="targetRadians"/> (sine in-out),
    /// killing any in-flight pitch tween. Sets rotation directly, so it can reach
    /// targets outside the drag-time pitch clamp; a later rotate drag eases back
    /// into the clamp (and kills the tween) rather than jumping.
    /// </summary>
    public void TweenPitch(float targetRadians, float seconds)
    {
        _pitchTween?.Kill();
        _pitchTween = CreateTween();
        _pitchTween.TweenProperty(this, "rotation:x", targetRadians, seconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>Walks up from a node (typically the active camera) to its owning rig, if any.</summary>
    public static CameraRig FromNode(Node node)
    {
        while (node != null)
        {
            if (node is CameraRig rig) return rig;
            node = node.GetParent();
        }
        return null;
    }

    #region Manipulation in play mode
    [Export] public float RotationSensitivity { get; set; } = 0.005f;
    /// <summary>
    /// Pixels of press-drag motion before a left-drag starts rotating. Quick
    /// clicks (press/release with less motion than this) never disturb the
    /// camera, so click-to-act game modes don't get accidental orbit nudges.
    /// </summary>
    [Export] public float RotationDragThresholdPixels { get; set; } = 5f;
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

    /// <summary>
    /// When false, the rig ignores mouse rotate and pan (button-drag). Lets a mode
    /// that owns drag input — e.g. the wall-plan editor's node dragging — suppress
    /// camera manipulation without the rig knowing about it; any in-flight
    /// rotate/pan is cancelled. Wheel zoom is unaffected (it doesn't conflict with
    /// drag) and still obeys <see cref="EnableZooming"/>.
    /// </summary>
    [Export] public bool MouseManipulationEnabled { get; set; } = true;

    private bool _isRotating = false;      // threshold crossed, actively rotating
    private bool _rotatePending = false;   // left button held, threshold not yet crossed
    private Vector2 _rotatePressPosition;
    private bool _isPanning = false;
    private Vector2 _lastMousePosition;
    private float _yaw;
    private float _pitch;
    // Per-drag pitch bounds: the configured clamp, expanded to include the pitch
    // the drag started at, so a drag beginning outside the clamp (e.g. after a
    // scripted top-down snap) eases back into range instead of jumping.
    private float _dragPitchLower;
    private float _dragPitchUpper;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SceneRecorder.IsOn) return;
        if (!Camera.Current) return;
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            // Zoom (wheel) always processes; rotate/pan obey MouseManipulationEnabled.
            bool isZoom = mouseButtonEvent.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown;
            if (!MouseManipulationEnabled && !isZoom)
            {
                _isRotating = false;
                _rotatePending = false;
                _isPanning = false;
                return;
            }
            HandleMouseButtonEvent(mouseButtonEvent);
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            if (!MouseManipulationEnabled)
            {
                _isRotating = false;
                _rotatePending = false;
                _isPanning = false;
                return;
            }
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
                    // Rotation arms on press but only starts once the cursor has
                    // moved RotationDragThresholdPixels (see HandleMouseMotionEvent),
                    // so a click never nudges the camera.
                    _rotatePending = mouseButtonEvent.Pressed;
                    if (_rotatePending) _rotatePressPosition = mouseButtonEvent.Position;
                    else _isRotating = false;
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
        if (_rotatePending && !_isRotating
            && mouseMotionEvent.Position.DistanceTo(_rotatePressPosition) >= RotationDragThresholdPixels)
        {
            BeginRotateDrag(mouseMotionEvent.Position);
        }

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
                _pitch = Mathf.Clamp(_pitch, _dragPitchLower, _dragPitchUpper);
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

    private void BeginRotateDrag(Vector2 at)
    {
        _isRotating = true;
        _lastMousePosition = at;
        _yaw = Rotation.Y;
        _pitch = Rotation.X;
        _dragPitchLower = Mathf.Min(-Mathf.DegToRad(PitchMax), _pitch);
        _dragPitchUpper = Mathf.Max(-Mathf.DegToRad(PitchMin), _pitch);
        _pitchTween?.Kill();
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
