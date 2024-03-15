using Godot;

namespace PrimerTools;

[Tool]
public partial class CameraRig : Node3D
{
    Camera3D _camera;
    Camera3D Camera
    {
        get
        {
            // Try to get it from the scene
            _camera = GetNodeOrNull<Camera3D>("Camera3D");
            if (_camera != null) return _camera;
            
            // If the camera is not in the scene, create a new one
            _camera = new Camera3D();
            AddChild(_camera);
            _camera.Owner = GetTree().EditedSceneRoot;
            _camera.Position = Vector3.Back * 10;
            _camera.Name = "Camera3D";

            return _camera;
        }
    }

    [Export]
    private float Distance
    {
        get => Camera.Position.Z;
        set => Camera.Position = new Vector3(Camera.Position.X, Camera.Position.Y, value);
    }

    public Animation ZoomTo(float distance)
    {
        return Camera.MoveTo(new Vector3(Camera.Position.X, Camera.Position.Y, distance));
    }
        
        
//     public class CameraRig : MonoBehaviour
//     {
//         private Camera cameraCache;
//         public Camera cam => cameraCache == null ? cameraCache = GetComponent<Camera>() : cameraCache;
//
//         [SerializeField, HideInInspector]
//         private float _distance = 10;
//         [ShowInInspector]
//         public float distance {
//             get => _distance;
//             set {
//                 
//                 _distance = value;
//                 UpdateSwivel();
//             }
//         }
//         
//         [SerializeField, HideInInspector]
//         private Vector3 _swivelOrigin;
//         [ShowInInspector]
//         public Vector3 swivelOrigin {
//             get => _swivelOrigin;
//             set
//             {
//                 _swivelOrigin = value;
//                 UpdateSwivel();
//             }
//         }
//         
//         [SerializeField, HideInInspector]
//         private Vector3 _swivel;
//         [ShowInInspector]
//         public Vector3 swivel {
//             get => _swivel;
//             set
//             {
//                 _swivel = value;
//                 UpdateSwivel();
//             }
//         }
//         
//         public bool faceSwivel = true;
//         public Color backgroundColor = PrimerColor.gray;
//
//         private void OnDrawGizmos() => Gizmos.DrawSphere(swivelOrigin, 0.1f);
//
//         private void Awake()
//         {
//             if (cam != null && backgroundColor != cam.backgroundColor) {
//                 cam.clearFlags = CameraClearFlags.SolidColor;
//                 cam.backgroundColor = backgroundColor;
//             }
//         }
//
//         private void UpdateSwivel()
//         {
//             // var direction = faceSwivel ? Vector3.back : Vector3.forward;
//             transform.position = Quaternion.Euler(swivel) * Vector3.back * distance + swivelOrigin;
//             transform.rotation = Quaternion.Euler(swivel);
//         }
//
//         public Tween FocusOn(Component target, Vector3 offset, float? distance = null, Vector3? swivel = null)
//         {
//             return Travel(distance, target.transform.position + offset, swivel);
//         }
//
//         public Tween Travel(float? distance = null, Vector3? swivelOrigin = null, Vector3? swivel = null)
//         {
//             var tween = new List<Tween>();
//             var linear = LinearEasing.instance;
//
//             if (distance.HasValue)
//             {
//                 tween.Add(Tween.Value(
//                         v => this.distance = v,
//                         () => this.distance,
//                         () => distance.Value
//                     ) with
//                     {
//                         easing = linear
//                     });
//             }
//
//             if (swivelOrigin.HasValue) {
//                 {
//                     tween.Add(Tween.Value(
//                             v => this.swivelOrigin = v,
//                             () => this.swivelOrigin,
//                             () => swivelOrigin.Value
//                         ) with
//                         {
//                             easing = linear
//                         });
//                 }
//             }
//
//             if (swivel.HasValue) {
//                 {
//                     tween.Add(Tween.Value(
//                             v => this.swivel = v,
//                             () => this.swivel,
//                             () => swivel.Value
//                         ) with
//                         {
//                             easing = linear
//                         });
//                 }
//             }
//
//             // or use tween.RunInBatch() to merge all tweens into one with unified easing
//             return tween.RunInParallel() with { easing = IEasing.defaultMethod };
//         }
//
//         [PropertySpace]
//         [Button(ButtonSizes.Large)]
//         private void CopyCode()
//         {
//             GUIUtility.systemCopyBuffer = $@"
// .Travel(
//     distance: {distance}f,
//     swivelOrigin: {swivelOrigin.ToCode()},
//     swivel: {swivel.ToCode()}
// )
//             ".Trim();
//         }
//
//         public Tween TweenFieldOfView(float newFieldOfView)
//         {
//             var originalDistance = _distance;
//             var originalFieldOfView = cam.fieldOfView;
//             
//             float NewDistance(float fov)
//             {
//                 return originalDistance * Mathf.Tan(originalFieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
//             }
//
//             return Tween.Value(
//                 v => cam.fieldOfView = v,
//                 to: () => newFieldOfView,
//                 from: () => cam.fieldOfView
//             ).Observe(afterUpdate: _ =>
//             {
//                 var oldDistance = _distance;
//                 distance = NewDistance(cam.fieldOfView);
//                 foreach (var child in transform.GetChildren())
//                 {
//                     var localPosition = child.localPosition;
//                     localPosition = new Vector3(
//                         localPosition.x,
//                         localPosition.y,
//                         localPosition.z * distance / oldDistance
//                     );
//                     child.localPosition = localPosition;
//                 }
//             });
//         }
//     }
}
