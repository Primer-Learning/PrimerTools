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
    /// Instant orthographic projection toggle (see <see cref="TweenProjection"/>
    /// for the animated switch). Switching preserves the apparent framing: the
    /// ortho size is derived from Distance and the camera's FOV so the plane
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
            _projectionTween?.Kill();
            _orthoTarget = value;
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

    private Tween _yawTween;
    private float _yawTweenTarget;

    /// <summary>
    /// Tweens yaw by a relative amount (sine in-out). Rapid presses accumulate:
    /// a press mid-tween retargets from the in-flight destination, so two 45°
    /// presses always land 90° from where the first started. A rotate drag
    /// kills the tween.
    /// </summary>
    public void TweenYawBy(float deltaRadians, float seconds)
    {
        bool inFlight = _yawTween != null && _yawTween.IsValid() && _yawTween.IsRunning();
        _yawTweenTarget = (inFlight ? _yawTweenTarget : Rotation.Y) + deltaRadians;
        _yawTween?.Kill();
        _yawTween = CreateTween();
        _yawTween.TweenProperty(this, "rotation:y", _yawTweenTarget, seconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// FOV (degrees) narrow enough that snapping between a matched-framing
    /// perspective and true orthographic is visually negligible.
    /// </summary>
    private const float OrthoSnapFovDegrees = 5f;

    private Tween _projectionTween;
    private bool? _orthoTarget;
    private float _perspectiveFov = DefaultFov;

    /// <summary>
    /// The projection state the rig is at or animating toward. Poll this (not
    /// <see cref="Orthographic"/>) for UI toggles so they don't flicker during
    /// a projection tween, which stays in perspective until the final snap.
    /// </summary>
    public bool OrthographicTarget => _orthoTarget ?? Orthographic;

    /// <summary>
    /// Animated projection switch. To ortho: dolly the camera out while
    /// narrowing the FOV (the framing extent through the rig's origin held
    /// constant), then snap to true orthographic once perspective distortion is
    /// negligible. To perspective: snap to the matched narrow-FOV perspective
    /// (visually identical), then tween FOV/distance back to the FOV captured
    /// on the way out. Distance ends where the current framing implies, so zoom
    /// done while in ortho carries back to perspective.
    /// </summary>
    public void TweenProjection(bool orthographic, float seconds)
    {
        if (Camera is null) return;
        bool wasTransitioning = _projectionTween != null && _projectionTween.IsValid();
        _projectionTween?.Kill();
        _orthoTarget = orthographic;

        if (orthographic)
        {
            if (Orthographic) return;
            // Mid-transition FOV is scratch state, not the one to come back to.
            if (!wasTransitioning) _perspectiveFov = Camera.Fov;
            float extent = FramingExtent();
            _projectionTween = CreateTween();
            _projectionTween.TweenMethod(
                    Callable.From<float>(fov => SetPerspectiveFraming(fov, extent)),
                    Camera.Fov, OrthoSnapFovDegrees, seconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _projectionTween.TweenCallback(Callable.From(() =>
            {
                Camera.Projection = Camera3D.ProjectionType.Orthogonal;
                SyncOrthoSize();
            }));
        }
        else
        {
            float extent = FramingExtent();
            if (Orthographic)
            {
                Camera.Fov = OrthoSnapFovDegrees;
                Camera.Projection = Camera3D.ProjectionType.Perspective;
                SetPerspectiveFraming(OrthoSnapFovDegrees, extent);
            }
            _projectionTween = CreateTween();
            _projectionTween.TweenMethod(
                    Callable.From<float>(fov => SetPerspectiveFraming(fov, extent)),
                    Camera.Fov, _perspectiveFov, seconds)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
    }

    /// <summary>
    /// World extent spanned by the viewport (along the keep-aspect axis) at the
    /// plane through the rig's origin — the quantity both projections share.
    /// </summary>
    private float FramingExtent()
        => Orthographic
            ? Camera.Size
            : 2f * Mathf.Abs(Distance) * Mathf.Tan(Mathf.DegToRad(Camera.Fov) * 0.5f);

    /// <summary>Sets FOV and back-solves Distance so the framing extent stays fixed.</summary>
    private void SetPerspectiveFraming(float fovDegrees, float framingExtent)
    {
        Camera.Fov = fovDegrees;
        Distance = framingExtent / (2f * Mathf.Tan(Mathf.DegToRad(fovDegrees) * 0.5f));
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

    private bool _isRotating = false;
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
                    if (mouseButtonEvent.Pressed) BeginRotateDrag(mouseButtonEvent.Position);
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
        _yawTween?.Kill();
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
