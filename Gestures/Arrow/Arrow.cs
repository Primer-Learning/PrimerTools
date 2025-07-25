using System.Collections.Generic;
using Godot;
using PrimerTools;

[Tool]
public partial class Arrow : Node3D
{
    // TODO: Fix this up. It's confusing because tries to handle local and global positions, and also following objects.
    // Probably best to set it up so the core functionality works with head/tail positions in the Node3D's parent space.
    // Then atop that, add functionality for specifying global positions or objects follow. Need to think through the 
    // intended object following behavior. Might be best to not do that at all and instead just require manual calls
    // to animation methods using that objects position (local or global)
    // Another currently weird thing is that tailpoint is relative to headpoint, not relative to the parent space.
    
    public static readonly PackedScene ArrowScene = ResourceLoader.Load<PackedScene>("res://addons/PrimerTools/Gestures/Arrow/arrow.tscn");

    public static Arrow CreateInstance()
    {
        var arrow = ArrowScene.Instantiate<Arrow>();
        return arrow;
    }
    
    private ExportedMemberChangeChecker _exportedMemberChangeChecker;

    private const float shaftAdjustment = 0.05f;

    [Export]
    public bool EnableImmediateUpdates;
    public override void _Process(double delta)
    {
        if (!EnableImmediateUpdates) return;
        _exportedMemberChangeChecker ??= new ExportedMemberChangeChecker(this);

        if (Engine.IsEditorHint() && _exportedMemberChangeChecker.CheckForChanges())
        {
            GD.Print("Updating arrow");
            Transition();
        }
        
        // if (nodeThatHeadFollows != null || nodeThatTailFollows != null) Update();
    }

    private bool _showTailArrow;

    [Export] public bool ShowTailArrow;
    [Export] public bool ShowHeadArrow;
    [Export] public float TailPadding;
    [Export] public float HeadPadding;
    [Export] public float Chonk;

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
    [Export] public Vector3 headPoint;
    [Export] public Vector3 tailPoint;
    [Export] private Node3D shaftObject;
    [Export] private Node3D headObject;
    [Export] internal Node3D tailObject;
    
    [Export] internal Node3D nodeThatTailFollows;
    [Export] internal Node3D nodeThatHeadFollows;
    
    // Todo: Make this an animation, like Transition. This will make it easier to 
    // move the arrow while keeping its tail point, for example. Or to change multiple parameters at once.
    // Currently, these need to manually be animated.
    public Animation Transition(double duration = AnimationUtilities.DefaultDuration, Animation.InterpolationType rotationInterpolationType = Animation.InterpolationType.CubicAngle)
    {
        var animations = new List<Animation>();

        var finalPosition = Position;
        
        if (nodeThatHeadFollows != null)
        {
            GD.PushWarning("Node following disabled for Arrow.");
            // GlobalPosition = nodeThatHeadFollows.GlobalPosition;
            // animations.Add(this.MoveTo(nodeThatHeadFollows.GlobalPosition, global: true));
        }
        else
        {
            finalPosition = headPoint;
            // animations.Add(this.MoveTo(headPoint));
        }
        
        // if (nodeThatTailFollows != null)
        // {
        //     // This assumes global scale is Vector3.one
        //     tailPoint = nodeThatTailFollows.GlobalPosition - GlobalPosition;
        // }

        var finalRotation = new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X) * 180 / Mathf.Pi);
        // animations.Add(this.RotateTo(new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X) * 180 / Mathf.Pi)));
        
        // Rotation = new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X));
        
        var lengthToCutFromHead = ShowHeadArrow ? shaftAdjustment * Chonk + HeadPadding : HeadPadding;

        var finalShaftPosition = Vector3.Right * lengthToCutFromHead;
        // animations.Add(shaftObject.MoveTo(Vector3.Right * lengthToCutFromHead));
        
        // shaftObject.Position = Vector3.Right * lengthToCutFromHead;
        
        // Not a truly robust scale correction, but should work when all the scales of parents are uniform.
        var totalLength = tailPoint.Length();
        var lengthToCutFromTail = ShowTailArrow
            ? shaftAdjustment * Chonk + TailPadding
            : TailPadding;

        var finalShaftScale = new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, Chonk, 1);
        // animations.Add(shaftObject.ScaleTo(new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, Chonk, 1)));
        // shaftObject.Scale = new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, Chonk, 1);

        var finalHeadObjectPosition = Vector3.Right * HeadPadding;
        var finalHeadScale = Vector3.One * Chonk;
        if (ShowHeadArrow)
        {
            headObject.Visible = true;
            // headObject.Position = Vector3.Right * HeadPadding;
            // animations.Add(headObject.MoveTo(Vector3.Right * HeadPadding));
            // headObject.Scale = Vector3.One * Chonk;
            // animations.Add(headObject.ScaleTo(Vector3.One * Chonk));
        }
        else
        {
            headObject.Visible = false;
        }

        var finalTailObjectPosition = Vector3.Right * (totalLength - TailPadding);
        var finalTailObjectScale = Vector3.One * Chonk;
        if (ShowTailArrow)
        {
            tailObject.Visible = true;
            // tailObject.Position = Vector3.Right * (totalLength - TailPadding);
            // animations.Add(tailObject.MoveTo(Vector3.Right * (totalLength - TailPadding)));
            // tailObject.Scale = Vector3.One * Chonk;
            // animations.Add(tailObject.ScaleTo(Vector3.One * Chonk));
        }
        else
        {
            tailObject.Visible = false;
        }

        // return animations.InParallel().WithDuration(duration);
        return AnimationUtilities.Parallel(
            this.MoveToAnimation(finalPosition),
            this.RotateToAnimation(finalRotation, interpolationType: rotationInterpolationType),
            shaftObject.MoveToAnimation(finalShaftPosition),
            shaftObject.ScaleToAnimation(finalShaftScale),
            headObject.MoveToAnimation(finalHeadObjectPosition),
            headObject.ScaleToAnimation(finalHeadScale),
            tailObject.MoveToAnimation(finalTailObjectPosition),
            tailObject.ScaleToAnimation(finalTailObjectScale)
        ).WithDuration(duration);
    }
    
    
    public Tween TweenTransition(double duration = AnimationUtilities.DefaultDuration)
    {
        var tween = CreateTween();
        tween.SetParallel();
        
        if (nodeThatHeadFollows != null)
        {
            // GlobalPosition = nodeThatHeadFollows.GlobalPosition;
            // animations.Add(this.MoveTo(nodeThatHeadFollows.GlobalPosition, global: true));

            tween.TweenProperty(
                this,
                "global_position",
                nodeThatHeadFollows.GlobalPosition,
                duration
            );
        }
        else
        {
            tween.TweenProperty(
                this,
                "position",
                headPoint,
                duration
            );
        }
        
        if (nodeThatTailFollows != null)
        {
            // This assumes global scale is Vector3.one
            tailPoint = nodeThatTailFollows.GlobalPosition - GlobalPosition;
            
            // animations.Add(this.RotateTo(new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X) * 180 / Mathf.Pi)));
            tween.TweenProperty(
                this,
                "rotation",
                new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X)),
                duration
            );
        }
        else
        {
            tween.TweenProperty(
                this,
                "rotation",
                new Vector3(0, 0, Mathf.Atan2(tailPoint.Y, tailPoint.X)),
                duration
            );
        }
        
        
        var lengthToCutFromHead = ShowHeadArrow ? shaftAdjustment * Chonk + HeadPadding : HeadPadding;
        // animations.Add(shaftObject.MoveTo(Vector3.Right * lengthToCutFromHead));
        tween.TweenProperty(
            shaftObject,
            "position",
            Vector3.Right * lengthToCutFromHead,
            duration
        );
        
        // shaftObject.Position = Vector3.Right * lengthToCutFromHead;
        
        // Not a truly robust scale correction, but should work when all the scales of parents are uniform.
        var totalLength = tailPoint.Length();
        var lengthToCutFromTail = ShowTailArrow
            ? shaftAdjustment * Chonk + TailPadding
            : TailPadding;
        
        // animations.Add(shaftObject.ScaleTo(new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, Chonk, 1)));
        tween.TweenProperty(
            shaftObject,
            "scale",
            new Vector3(totalLength - lengthToCutFromHead - lengthToCutFromTail, Chonk, 1),
            duration
        );
        
        if (ShowHeadArrow)
        {
            headObject.Visible = true;
            // animations.Add(headObject.MoveTo(Vector3.Right * HeadPadding));
            tween.TweenProperty(
                headObject,
                "position",
                Vector3.Right * HeadPadding,
                duration
            );
            // animations.Add(headObject.ScaleTo(Vector3.One * Chonk));
            tween.TweenProperty(
                headObject,
                "scale",
                Vector3.One * Chonk,
                duration
            );
        }
        else
        {
            headObject.Visible = false;
        }
        if (ShowTailArrow)
        {
            tailObject.Visible = true;
            // animations.Add(tailObject.MoveTo(Vector3.Right * (totalLength - TailPadding)));
            tween.TweenProperty(
                tailObject,
                "position",
                Vector3.Right * (totalLength - TailPadding),
                duration
            );
            // animations.Add(tailObject.ScaleTo(Vector3.One * Chonk));
            tween.TweenProperty(
                tailObject,
                "scale",
                Vector3.One * Chonk,
                duration
            );
        }
        else
        {
            tailObject.Visible = false;
        }

        return tween;
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
        // Ensure the arrow reflects recent property changes
        Transition();
        
        var finalPosition = Position;
        Scale = Vector3.One;
        Position += Transform.Basis.X * (Length - TailPadding);
        Scale = Vector3.Zero;
        return AnimationUtilities.Parallel(
            this.ScaleToAnimation(Vector3.One),
            this.MoveToAnimation(finalPosition)
        );
    }
    public Animation ScaleDownToTail()
    {
        return AnimationUtilities.Parallel(
            this.ScaleToAnimation(Vector3.Zero),
            this.MoveToAnimation(Position + tailPoint)
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
