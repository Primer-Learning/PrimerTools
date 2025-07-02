using System;
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
        if (_bracketData == null || _meshInstance == null) return;
        
        // Calculate the basis vectors for the bracket plane
        var localTransform = CalculateLocalTransform();
        
        // Update bracket data with 2D coordinates in local space
        var localPoint0 = localTransform.Inverse() * _leftTip3D;
        var localPoint1 = localTransform.Inverse() * _rightTip3D;
        var localPoint2 = localTransform.Inverse() * _stem3D;
        
        _bracketData.LeftTip = new Vector2(localPoint0.X, localPoint0.Y);
        _bracketData.RightTip = new Vector2(localPoint1.X, localPoint1.Y);
        _bracketData.Stem = new Vector2(localPoint2.X, localPoint2.Y);
        
        // Update mesh transform
        UpdateMesh(localTransform);
        
        // Update shader parameters
        UpdateShaderParameters();
    }
    
    private Transform3D CalculateLocalTransform()
    {
        // Calculate basis vectors
        var tipToTip = (_rightTip3D - _leftTip3D);
        var tipToStem = (_stem3D - _leftTip3D);

        Vector3 localX;
        Vector3 localY;
        Vector3 localZ;
        Basis basis;
        
        // Handle degenerate case where points are collinear
        if (tipToTip.Cross(tipToStem).LengthSquared() < 0.0001f)
        {
            // Fall back to a default orientation
            var up = Vector3.Up;
            if (Mathf.Abs(tipToTip.Dot(up)) > 0.99f)
                up = Vector3.Right;
            
            localZ = tipToTip.Cross(up).Normalized();
            localY = localZ.Cross(tipToTip).Normalized();
            basis = new Basis(tipToTip.Normalized(), localY, localZ);
            
            return new Transform3D(basis, _leftTip3D);
        }
        
        // Normal case: points form a proper triangle
        localX = tipToTip.Normalized();
        localZ = tipToTip.Cross(tipToStem).Normalized();
        localY = localZ.Cross(localX).Normalized();
        
        basis = new Basis(localX, localY, localZ);
        
        return new Transform3D(basis, _leftTip3D);
    }
    
    private void UpdateMesh(Transform3D worldTransformOfLeftTip)
    {
        if (_meshInstance == null || _bracketData == null) return;
        
        // Get bounds in 2D, local space
        var bounds = _bracketData.GetBounds();
        
        // Add padding
        var totalPadding = _padding;
        if (_style != null)
        {
            totalPadding += _style.Thickness;
        }
        bounds = bounds.Grow(totalPadding);
        
        // Calculate world position for mesh center
        var localCenter2D = bounds.GetCenter();
        var localCenter3D = new Vector3(localCenter2D.X, localCenter2D.Y, 0);
        
        // Transform to world space
        var worldCenter = worldTransformOfLeftTip * localCenter3D;
        GD.Print(localCenter3D);
        GD.Print(worldCenter);
        
        _meshInstance.GlobalPosition = worldCenter;
        _meshInstance.GlobalBasis = worldTransformOfLeftTip.Basis.Rotated(worldTransformOfLeftTip.Basis.X, Mathf.Tau / 4f);
        
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

        var center = _bracketData.GetBounds().GetCenter();
        
        // The bracket data already contains local coordinates relative to the mesh center
        _shaderMaterial.SetShaderParameter("bracket_tip1", _bracketData.LeftTip - center);
        _shaderMaterial.SetShaderParameter("bracket_tip2", _bracketData.RightTip - center);
        _shaderMaterial.SetShaderParameter("bracket_stem", _bracketData.Stem - center);
        
        // Pass mesh size to shader
        if (_meshInstance.Mesh is PlaneMesh planeMesh)
        {
            _shaderMaterial.SetShaderParameter("mesh_size", planeMesh.Size);
        }
        
        _style?.ApplyToShader(_shaderMaterial);
    }
    
    public static ShaderBracket CreateInstance()
    {
        var bracket = new ShaderBracket();
        return bracket;
    }
}
