using Godot;
using PrimerTools;

[Tool]
public partial class Arrow : Node3D
{
    public static readonly PackedScene ArrowScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Gestures/Arrow/arrow.tscn");
    
    private ExportedMemberChangeChecker _exportedMemberChangeChecker;

    private const float shaftAdjustment = 0.05f;

    public override void _Process(double delta)
    {
        _exportedMemberChangeChecker ??= new ExportedMemberChangeChecker(this);

        if (Engine.IsEditorHint() && _exportedMemberChangeChecker.CheckForChanges())
        {
            Update();
        }
        
        // if (nodeThatHeadFollows != null || nodeThatTailFollows != null) Update();
    }

    private bool _showTailArrow;

    [Export] public bool showTailArrow;
    [Export] public bool showHeadArrow;
    [Export] public float tailPadding;
    [Export] public float headPadding;
    [Export] public float width;

    // Length and rotation approach
    [Export] public float Length
    {
        get => tailPoint.Length();
        set => tailPoint = tailPoint.Normalized() * value;
    }
    [Export] public float XYPlaneRotation
    {
        get => RotationDegrees.Z;
        set
        {
            RotationDegrees = new Vector3(0, 0, value);
            tailPoint = Quaternion.FromEuler(new Vector3(0, 0, value * Mathf.Pi / 180)) * Vector3.Right * Length;
        }
    }

    // Starting point approach
    [Export] public Vector3 tailPoint;
    [Export] private Node3D shaftObject;
    [Export] private Node3D headObject;
    [Export] internal Node3D tailObject;
    
    [Export] internal Node3D nodeThatTailFollows;
    [Export] internal Node3D nodeThatHeadFollows;
    
    public void Update()
    {
        if (nodeThatHeadFollows != null) GlobalPosition = nodeThatHeadFollows.GlobalPosition;
        if (nodeThatTailFollows != null)
        {
            tailPoint = nodeThatTailFollows.GlobalPosition - GlobalPosition;
        }
        Rotation = new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X));
        
        var lengthToCutFromHead = showHeadArrow ? shaftAdjustment * width + headPadding : headPadding;
        shaftObject.Position = Vector3.Right * lengthToCutFromHead;
        
        // Not a truly robust scale correction, but should work when all the scales of parents are uniform.
        var totalLength = tailPoint.Length();
        var lengthToCutFromTail = showTailArrow
            ? shaftAdjustment * width + tailPadding
            : tailPadding;
        shaftObject.Scale = new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, width, 1);
        
        if (showHeadArrow)
        {
            headObject.Visible = true;
            headObject.Position = Vector3.Right * headPadding;
            headObject.Scale = Vector3.One * width;
        }
        else
        {
            headObject.Visible = false;
        }
        if (showTailArrow)
        {
            tailObject.Visible = true;
            tailObject.Position = Vector3.Right * (totalLength - tailPadding);
            tailObject.Scale = Vector3.One * width;
        }
        else
        {
            tailObject.Visible = false;
        }
    }
    
    // public Tween ScaleUpFromHead()
    // {
    //     var t = transform;
    //     var truePosition = t.localPosition;
    //     t.localPosition += t.right * headPadding;
    //     return ScaleAndMoveTo(1, truePosition);
    // }
    //
    // public Tween ScaleDownToHead()
    // {
    //     var fakePosition = transform.localPosition + transform.right * headPadding;
    //     return ScaleAndMoveTo(0, fakePosition);
    // }
    //
    public Animation ScaleUpFromTail()
    {
        var truePosition = Position;
        var trueScale = Scale;
        Scale = Vector3.One;
        var t = Transform;
        Position += t.Basis.X * (Length - tailPadding);
        Scale = trueScale;
        return AnimationUtilities.Parallel(
            this.ScaleTo(Vector3.One),
            this.MoveTo(truePosition)
        );
    }
    // public Tween ScaleDownToTail()
    // {
    //     var fakePosition = transform.localPosition + transform.right * (length - tailPadding);
    //     return ScaleAndMoveTo(0, fakePosition);
    // }
    //
    // private Tween ScaleAndMoveTo(float targetScale, Vector3 targetPosition)
    // {
    //     return Tween.Parallel(
    //         transform.ScaleTo(targetScale),
    //         transform.MoveTo(targetPosition)
    //     );
    // }
    //
    // public Tween PulseFromCenter(float sizeFactor = 1.2f, float attack = 0.5f, float hold = 0.5f, float decay = 0.5f, Color? color = null)
    // {
    //     var transform = this.transform;
    //     var localScale = transform.localScale;
    //     var originalPosition = transform.localPosition;
    //     var direction = transform.right;
    //     var distance = length * (sizeFactor - 1);
    //     
    //     var colorIn = Tween.noop;
    //     var colorOut = Tween.noop;
    //     if (color != null)
    //     {
    //         colorIn = TweenColor(color.Value);
    //         colorOut = TweenColor(headObject.GetChild(0).GetComponent<Renderer>().sharedMaterial.color);
    //     }
    //
    //     // Create the Tweens and add them to the list
    //     return Tween.Series(
    //         Tween.Parallel(
    //             colorIn with { duration = attack },
    //             transform.ScaleTo(localScale * sizeFactor) with { duration = attack },
    //             transform.MoveTo(originalPosition - direction * distance / 2) with {
    //                 duration = attack,
    //             }
    //         ),
    //         Tween.noop with { duration = hold },
    //         Tween.Parallel(
    //             colorOut with { duration = decay },
    //             transform.ScaleTo(localScale) with { duration = decay },
    //             transform.MoveTo(originalPosition) with {
    //                 duration = decay,
    //             }
    //         )
    //     );
    // }
    // public Tween TweenColor(Color color)
    // {
    //     return Tween.Parallel(
    //         shaftObject.GetChild(0).GetComponent<Renderer>().TweenColor(color),
    //         headObject.GetChild(0).GetComponent<Renderer>().TweenColor(color),
    //         tailObject.GetChild(0).GetComponent<Renderer>().TweenColor(color)
    //     );
    // }
}
