using System;
using System.Collections.Generic;
using Godot;
using PrimerTools._2D.Diagram;
using PrimerTools;
using PrimerTools.TweenSystem;

[Tool]
public partial class ShaderArrow : Node3D
{
    private ArrowData _arrowData;
    private MeshInstance3D _meshInstance;
    private ShaderMaterial _shaderMaterial;

    private ShapeStyle _style = new ShapeStyle()
    {
        Smoothness = 0.01f,
        Thickness = 0.1f
    };
    
    private Vector3 _start3D = new Vector3(-1, 0, 0);
    private Vector3 _end3D = new Vector3(1, 0, 0);
    
    private float _padding = 0.5f;
    private float _headLength = 0.3f;
    private float _headAngle = 30.0f;

    [ExportToolButton("Make")]
    private Callable MakeButton => Callable.From(Make);

    [Export]
    public Vector3 StartPosition 
    { 
        get => _start3D;
        set 
        { 
            _start3D = value;
            UpdateFromWorld3DPositions();
        }
    }
    
    [Export]
    public Vector3 EndPosition 
    { 
        get => _end3D;
        set 
        { 
            _end3D = value;
            UpdateFromWorld3DPositions();
        }
    }
    
    private Vector3 _planeNormal = Vector3.Zero;
    private bool _hasCustomPlaneNormal = false;

    [Export]
    public Vector3 PlaneNormal
    {
        get => _planeNormal;
        set
        {
            _planeNormal = value;
            _hasCustomPlaneNormal = true;
            UpdateFromWorld3DPositions();
        }
    }

    [Export]
    public bool UseCustomPlaneNormal
    {
        get => _hasCustomPlaneNormal;
        set
        {
            _hasCustomPlaneNormal = value;
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
    
    [Export]
    public float HeadLength
    {
        get => _headLength;
        set
        {
            _headLength = value;
            if (_arrowData != null)
            {
                _arrowData.HeadLength = value;
                UpdateShaderParameters();
            }
        }
    }
    
    [Export]
    public float HeadAngle
    {
        get => _headAngle;
        set
        {
            _headAngle = value;
            if (_arrowData != null)
            {
                _arrowData.HeadAngle = value;
                UpdateShaderParameters();
            }
        }
    }

    public ShapeStyle Style
    {
        get => _style;
        private set
        {
            if (_style != null)
                _style.StyleChanged -= OnStyleChanged;

            _style = value;

            if (_style != null)
                _style.StyleChanged += OnStyleChanged;

            UpdateShaderParameters();
        }
    }

    public override void _Ready()
    {
        Make();
    }

    private void Make()
    {
        foreach (var child in GetChildren())
        {
            child.Free();
        }
        _arrowData ??= new ArrowData();
        _arrowData.HeadLength = _headLength;
        _arrowData.HeadAngle = _headAngle;
        
        if (_style != null)
            _style.StyleChanged += OnStyleChanged;
        
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
        _meshInstance.Name = "ArrowMesh";
        
        // Create initial plane mesh
        var planeMesh = new PlaneMesh();
        planeMesh.Size = Vector2.One * 2;
        planeMesh.SubdivideWidth = 1;
        planeMesh.SubdivideDepth = 1;
        _meshInstance.Mesh = planeMesh;
        
        // Load the arrow shader
        var shader = GD.Load<Shader>("res://addons/PrimerTools/2D/Diagram/ShapeShaders/arrow_shader_solo.gdshader");
        if (shader == null)
        {
            GD.PrintErr("Failed to load arrow shader");
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
        if (_arrowData == null || _meshInstance == null) return;
        
        // Calculate the basis vectors for the arrow plane
        var localTransform = CalculateLocalTransform();
        
        // Update arrow data with 2D coordinates in local space
        var localStart = localTransform.Inverse() * _start3D;
        var localEnd = localTransform.Inverse() * _end3D;
        
        _arrowData.Start = new Vector2(localStart.X, localStart.Y);
        _arrowData.End = new Vector2(localEnd.X, localEnd.Y);
        
        // Update mesh transform
        UpdateMesh(localTransform);
        
        // Update shader parameters
        UpdateShaderParameters();
    }
    
    private Transform3D CalculateLocalTransform()
    {
        var arrowDirection = (_end3D - _start3D);

        // Handle degenerate case where start and end are the same
        if (arrowDirection.LengthSquared() < 0.0001f)
        {
            // Default to identity transform at start position
            return new Transform3D(Basis.Identity, _start3D);
        }

        Vector3 normal;

        // Use provided normal or calculate from camera
        if (_hasCustomPlaneNormal && _planeNormal.LengthSquared() > 0.0001f)
        {
            normal = _planeNormal.Normalized();
        }
        else
        {
            // Get camera direction as default normal
            var viewport = GetViewport();
            if (viewport != null && viewport.GetCamera3D() != null)
            {
                var camera = viewport.GetCamera3D();
                normal = -camera.GlobalTransform.Basis.Z;
            }
            else
            {
                // Fallback: use a perpendicular vector
                var up = Vector3.Up;
                if (Mathf.Abs(arrowDirection.Normalized().Dot(up)) > 0.99f)
                    up = Vector3.Right;
                normal = arrowDirection.Cross(up).Normalized();
            }
        }

        // Ensure normal is perpendicular to arrow direction
        normal = (normal - normal.Dot(arrowDirection.Normalized()) *
            arrowDirection.Normalized()).Normalized();

        // Calculate basis vectors
        var localX = arrowDirection.Normalized();
        var localZ = normal;
        var localY = localZ.Cross(localX).Normalized();

        var basis = new Basis(localX, localY, localZ);

        return new Transform3D(basis, _start3D);
    }
    
    private void UpdateMesh(Transform3D worldTransformOfStart)
    {
        if (_meshInstance == null || _arrowData == null) return;
        
        // Get bounds in 2D, local space
        var bounds = _arrowData.GetBounds();
        
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
        var worldCenter = worldTransformOfStart * localCenter3D;
        
        _meshInstance.GlobalPosition = worldCenter;
        _meshInstance.GlobalBasis = worldTransformOfStart.Basis.Rotated(worldTransformOfStart.Basis.X, Mathf.Tau / 4f);
        
        // Update mesh size
        UpdateMeshSize();
    }
    
    private void UpdateMeshSize()
    {
        if (_meshInstance?.Mesh is PlaneMesh planeMesh && _arrowData != null)
        {
            var bounds = _arrowData.GetBounds();
            
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
        if (_shaderMaterial == null || _arrowData == null) return;

        var center = _arrowData.GetBounds().GetCenter();
        
        // The arrow data already contains local coordinates relative to the mesh center
        _shaderMaterial.SetShaderParameter("arrow_start", _arrowData.Start - center);
        _shaderMaterial.SetShaderParameter("arrow_end", _arrowData.End - center);
        _shaderMaterial.SetShaderParameter("head_length", _arrowData.HeadLength);
        _shaderMaterial.SetShaderParameter("head_angle", _arrowData.HeadAngle);
        
        // Pass mesh size to shader
        if (_meshInstance.Mesh is PlaneMesh planeMesh)
        {
            _shaderMaterial.SetShaderParameter("mesh_size", planeMesh.Size);
        }
        
        _style?.ApplyToShader(_shaderMaterial);
    }
    
    public static ShaderArrow CreateInstance()
    {
        var arrow = new ShaderArrow();
        return arrow;
    }

    public CompositeStateChange TransitionHeadAndTail(Vector3 headPosition, Vector3 tailPosition)
    {
        return CompositeStateChange.Parallel(
            new PropertyStateChange(this, "EndPosition", headPosition),
            new PropertyStateChange(this, "StartPosition", tailPosition)
        );
    }
}
