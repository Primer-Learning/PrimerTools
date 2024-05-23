using Godot;

[Tool]
public partial class CameraGrid : CanvasImitator
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
        CreateDisplayMesh();
        
        // Loop through and make all the rects
        // Vertical
        // - lineWidth / 2 is to center the line
        var xCenter = (int) (SubViewPortWidth * xCenterFraction) - lineWidth / 2;
        var xCellWidth = (int) (SubViewPortWidth * xSpacingFraction);
        if (xCellWidth == 0)
        {
            GD.PrintErr("xCellWidth is 0");
            return;
        }
        
        for (var i = xCenter % xCellWidth - xCellWidth; i < SubViewPortWidth; i += xCellWidth)
        {
            var vrect = new ColorRect();
            vrect.Size = new Vector2I(lineWidth, SubViewPortHeight);
            vrect.Position = new Vector2I(i, 0);
            SubViewPort.AddChild(vrect);
            vrect.Owner = GetTree().EditedSceneRoot;
        }
        
        // Horizontal
        var yCenter = (int) (SubViewPortHeight * yCenterFraction) - lineWidth / 2;
        var yCellHeight = (int) (SubViewPortHeight * ySpacingFraction);
        if (yCellHeight == 0)
        {
            GD.PrintErr("yCellHeight is 0");
            return;
        }
        for (var i = yCenter % yCellHeight - yCellHeight; i < SubViewPortHeight; i += yCellHeight)
        {
            var hrect = new ColorRect();
            hrect.Size = new Vector2I(SubViewPortWidth, lineWidth);
            hrect.Position = new Vector2I(0, i);
            SubViewPort.AddChild(hrect);
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
        SubViewPort.AddChild(xSlash1);
        xSlash1.Owner = GetTree().EditedSceneRoot;
        var xSlash2 = new ColorRect();
        xSlash2.Size = new Vector2I(slashLength, slashWidth);
        xSlash2.RotationDegrees = -45;
        xSlash2.Position = new Vector2I(
            xCenter + lineWidth / 2 - (int)((slashWidth + slashLength) / 2f / Mathf.Sqrt2),
            yCenter + lineWidth / 2 + (int)((slashLength - slashWidth) / 2f / Mathf.Sqrt2)
        );
        SubViewPort.AddChild(xSlash2);
        xSlash2.Owner = GetTree().EditedSceneRoot;
    }
}
