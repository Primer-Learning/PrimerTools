using Godot;
using PrimerTools._2D.Diagram;
using PrimerTools;

[Tool]
public partial class ShaderBracket : Node3D
{
    private BracketData _bracketData;
    private MeshInstance3D _meshInstance;
    private ShaderMaterial _shaderMaterial;
    private ShapeStyle _style;
    
    private Vector3 _leftTip3D = new Vector3(-1, -1, 0);
    private Vector3 _rightTip3D = new Vector3(1, -1, 0);
    private Vector3 _stem3D = Vector3.Zero;
    
    private float _padding = 0.5f;

    [ExportToolButton("Make")]
    private Callable MakeButton => Callable.From(Make);

    [Export]
    public Vector3 LeftTipPosition 
    { 
        get => _leftTip3D;
        set 
        { 
            _leftTip3D = value;
            UpdateFromWorld3DPositions();
        }
    }
    
    [Export]
    public Vector3 RightTipPosition 
    { 
        get => _rightTip3D;
        set 
        { 
            _rightTip3D = value;
            UpdateFromWorld3DPositions();
        }
    }
    
    [Export]
    public Vector3 StemPosition 
    { 
        get => _stem3D;
        set 
        { 
            _stem3D = value;
            UpdateFromWorld3DPositions();
        }
    }
    
    [Export]
    public float Padding
    {
        get => _padding;
        set
        {
            _padding = value;
            UpdateMeshSize();
        }
    }
    
    public ShapeStyle Style 
    { 
        get => _style;
        set
        {
            if (_style != null)
                _style.StyleChanged -= OnStyleChanged;
            
            _style = value;
            
            if (_style != null)
                _style.StyleChanged += OnStyleChanged;
            
            UpdateShaderParameters();
        }
    }

    private void Make()
    {
        foreach (var child in GetChildren())
        {
            child.Free();
        }
        _bracketData ??= new BracketData();
        if (_style == null)
        {
            var style = new ShapeStyle();
            style.Smoothness = 0.01f;
            _style = style;
        }
        CreateMesh();
        UpdateFromWorld3DPositions();
    }
    public override void _ExitTree()
    {
        if (_style != null)
        {
            _style.StyleChanged -= OnStyleChanged;
        }
    }
    
    private void OnStyleChanged()
    {
        UpdateShaderParameters();
    }
    
    private void CreateMesh()
    {
        // Create mesh instance
        _meshInstance = new MeshInstance3D();
        AddChild(_meshInstance);
        _meshInstance.Name = "BracketMesh";
        
        // Create initial plane mesh
        var planeMesh = new PlaneMesh();
        planeMesh.Size = Vector2.One * 2;
        planeMesh.SubdivideWidth = 1;
        planeMesh.SubdivideDepth = 1;
        _meshInstance.Mesh = planeMesh;
        
        // Load the curly bracket shader
        var shader = GD.Load<Shader>("res://addons/PrimerTools/2D/Diagram/ShapeShaders/curly_bracket_shader.gdshader");
        if (shader == null)
        {
            GD.PrintErr("Failed to load curly bracket shader");
            return;
        }
        
        _shaderMaterial = new ShaderMaterial();
        _shaderMaterial.Shader = shader;
        
        // Apply initial style
        _style?.ApplyToShader(_shaderMaterial);
        
        _meshInstance.SetSurfaceOverrideMaterial(0, _shaderMaterial);
        
        this.MakeSelfAndChildrenLocal();
    }
    
    private void UpdateFromWorld3DPositions()
    {
        // GD.Print("Got here");
        if (_bracketData == null || _meshInstance == null) return;
        
        // For now, ignore Z values and project to XY plane
        var leftTip2D = new Vector2(_leftTip3D.X, _leftTip3D.Y);
        var rightTip2D = new Vector2(_rightTip3D.X, _rightTip3D.Y);
        var stem2D = new Vector2(_stem3D.X, _stem3D.Y);
        
        // Update bracket data with 2D coordinates
        _bracketData.LeftTip = leftTip2D;
        _bracketData.RightTip = rightTip2D;
        _bracketData.Stem = stem2D;
        
        // Update mesh transform and size
        UpdateMeshTransform();
        
        // Update shader parameters
        UpdateShaderParameters();
    }
    
    private void UpdateMeshTransform()
    {
        if (_meshInstance == null || _bracketData == null) return;
        
        // Get bounds in 2D
        var bounds = _bracketData.GetBounds();
        
        // Add padding
        var totalPadding = _padding;
        if (_style != null)
        {
            totalPadding += _style.Thickness;
        }
        bounds = bounds.Grow(totalPadding);
        
        // Position mesh at center of bounds (ignoring Z for now)
        var center3D = new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0);
        _meshInstance.GlobalPosition = center3D;
        
        // Orient mesh to face -Z (towards camera in typical setup)
        _meshInstance.RotationDegrees = new Vector3(90, 0, 0);
        
        // Update mesh size
        UpdateMeshSize();
    }
    
    private void UpdateMeshSize()
    {
        if (_meshInstance?.Mesh is PlaneMesh planeMesh && _bracketData != null)
        {
            var bounds = _bracketData.GetBounds();
            
            // Add padding
            var totalPadding = _padding;
            if (_style != null)
            {
                totalPadding += _style.Thickness;
            }
            bounds = bounds.Grow(totalPadding);
            
            planeMesh.Size = bounds.Size;
        }
    }
    
    private void UpdateShaderParameters()
    {
        if (_shaderMaterial == null || _bracketData == null) return;
        
        _shaderMaterial.SetShaderParameter("bracket_tip1", _bracketData.LeftTip);
        _shaderMaterial.SetShaderParameter("bracket_tip2", _bracketData.RightTip);
        _shaderMaterial.SetShaderParameter("bracket_stem", _bracketData.Stem);
        
        _style?.ApplyToShader(_shaderMaterial);
    }
    
    public static ShaderBracket CreateInstance()
    {
        var bracket = new ShaderBracket();
        return bracket;
    }
}
