using Godot;
using System;

[Tool]
public partial class CameraGrid : Node3D
{
    private ExportedMemberChangeChecker _exportedMemberChangeChecker;
    public override void _Process(double delta)
    {
        _exportedMemberChangeChecker ??= new ExportedMemberChangeChecker(this);
        
        if (Engine.IsEditorHint() && _exportedMemberChangeChecker.CheckForChanges())
        {
            DrawGrid();
        }
    }
    
    private bool _draw = false;
    [Export] private bool DrawButton {
        get => _draw;
        set {
            if (!value && _draw && Engine.IsEditorHint()) {
                DrawGrid();
            }
            _draw = true;
        }
    }
    
    [ExportGroup("Grid settings")]
    [Export(PropertyHint.Range, "0.001, 1")]
    private float xSpacingFraction = 0.1f;
    [Export(PropertyHint.Range, "0, 1")]
    private float xCenterFraction = 0.5f;
    [Export(PropertyHint.Range, "0.001, 1")]
    private float ySpacingFraction = 0.1f;
    [Export(PropertyHint.Range, "0, 1")]
    private float yCenterFraction = 0.5f;
    [Export(PropertyHint.Range, "1, 50")]
    private int lineWidth = 10;
    
    private int subViewPortWidth = 1920;
    private int subViewPortHeight = 1080;
    
    private float CamFov => GetParent<Camera3D>().Fov;
    private float CamNearPlane => GetParent<Camera3D>().Near;
    
    public override void _Ready()
    {
        if (!Engine.IsEditorHint()) QueueFree();
        DrawGrid();
    }
    
    private void DrawGrid()
    {
        foreach (var child in GetChildren())
        { 
            child.Free();
        }

        var subViewPortContainer = new SubViewportContainer();
        AddChild(subViewPortContainer);
        subViewPortContainer.Owner = GetTree().EditedSceneRoot;
        
        var subViewPort = new SubViewport();
        subViewPortContainer.AddChild(subViewPort);
        subViewPort.Owner = GetTree().EditedSceneRoot;
        subViewPort.Size = new Vector2I(subViewPortWidth, subViewPortHeight);
        subViewPort.TransparentBg = true;
        
        var viewPortRenderer = new MeshInstance3D();
        var viewPortRendererMesh = new PlaneMesh();
        viewPortRendererMesh.Size = new Vector2(16f/9, 1);
        viewPortRenderer.Mesh = viewPortRendererMesh;
        AddChild(viewPortRenderer);
        viewPortRenderer.Owner = GetTree().EditedSceneRoot;
        viewPortRenderer.RotationDegrees = new Vector3(90, 0, 0);
        viewPortRenderer.Position = new Vector3(0, 0, -CamNearPlane);
        var scale = 2 * CamNearPlane * Mathf.Tan(CamFov / 2 * Mathf.Pi / 180);
        GD.Print($"Field of view {CamFov}, Near plane {CamNearPlane}, scale {scale}");
        viewPortRenderer.Scale = Vector3.One * scale;

        var mat = new StandardMaterial3D();
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.AlbedoTexture = subViewPort.GetTexture();
        viewPortRenderer.Mesh.SurfaceSetMaterial(0, mat);
        
        // Loop through and make all the rects
        // Vertical
        // - lineWidth / 2 is to center the line
        var xCenter = (int) (subViewPortWidth * xCenterFraction) - lineWidth / 2;
        var xCellWidth = (int) (subViewPortWidth * xSpacingFraction);
        if (xCellWidth == 0)
        {
            GD.PrintErr("xCellWidth is 0");
            return;
        }
        
        for (var i = xCenter % xCellWidth - xCellWidth; i < subViewPortWidth; i += xCellWidth)
        {
            var vrect = new ColorRect();
            vrect.Size = new Vector2I(lineWidth, subViewPortHeight);
            vrect.Position = new Vector2I(i, 0);
            subViewPort.AddChild(vrect);
            vrect.Owner = GetTree().EditedSceneRoot;
        }
        
        // Horizontal
        var yCenter = (int) (subViewPortHeight * yCenterFraction) - lineWidth / 2;
        var yCellHeight = (int) (subViewPortHeight * ySpacingFraction);
        if (yCellHeight == 0)
        {
            GD.PrintErr("yCellHeight is 0");
            return;
        }
        for (var i = yCenter % yCellHeight - yCellHeight; i < subViewPortHeight; i += yCellHeight)
        {
            var hrect = new ColorRect();
            hrect.Size = new Vector2I(subViewPortWidth, lineWidth);
            hrect.Position = new Vector2I(0, i);
            subViewPort.AddChild(hrect);
            hrect.Owner = GetTree().EditedSceneRoot;
        }

        // Make an X at the center
        var slashLength = 10 * lineWidth;
        var slashWidth = 1 * lineWidth;
        
        var xSlash1 = new ColorRect();
        xSlash1.Size = new Vector2I(slashLength, slashWidth);
        xSlash1.RotationDegrees = 45;
        // The + lineWidth / 2 here is to undo the above correction.
        xSlash1.Position = new Vector2I(
            xCenter + lineWidth / 2 + (int)((slashWidth - slashLength) / 2f / Mathf.Sqrt2),
            yCenter + lineWidth / 2 - (int)((slashLength + slashWidth) / 2f / Mathf.Sqrt2)
        );
        subViewPort.AddChild(xSlash1);
        xSlash1.Owner = GetTree().EditedSceneRoot;
        var xSlash2 = new ColorRect();
        xSlash2.Size = new Vector2I(slashLength, slashWidth);
        xSlash2.RotationDegrees = -45;
        xSlash2.Position = new Vector2I(
            xCenter + lineWidth / 2 - (int)((slashWidth + slashLength) / 2f / Mathf.Sqrt2),
            yCenter + lineWidth / 2 + (int)((slashLength - slashWidth) / 2f / Mathf.Sqrt2)
        );
        subViewPort.AddChild(xSlash2);
        xSlash2.Owner = GetTree().EditedSceneRoot;
    }
}
