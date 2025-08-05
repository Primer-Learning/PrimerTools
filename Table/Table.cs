using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimerTools;
using PrimerTools.LaTeX;

public partial class Table : Node3D
{
    // Spacing and sizing
    public int HorizontalSpacing = 2;
    public int VerticalSpacing = 2;
    
    // Column widths and row heights (if not set, uses default spacing)
    private List<float> columnWidths = new();
    private List<float> rowHeights = new();
    
    // Separate scales for header row and header column
    public Vector3 HeaderRowScale = Vector3.One;
    public Vector3 HeaderColumnScale = Vector3.One;
    public Vector3 CellLatexScale = Vector3.One;
    
    // Alignment settings per column and row
    private readonly List<LatexNode.HorizontalAlignmentOptions> _columnAlignments = new();
    private readonly List<LatexNode.VerticalAlignmentOptions> _rowAlignments = new();
    
    // Default alignments
    public LatexNode.HorizontalAlignmentOptions DefaultHorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
    public LatexNode.VerticalAlignmentOptions DefaultVerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;

    private readonly List<List<Node3D>> _cells = new();
    
    // Grid line properties
    public bool ShowGridLines = true;
    public float GridLineThickness = 0.05f;
    public Color GridLineColor = Colors.Gray;
    private readonly List<MeshInstance3D> _gridLines = new();
    
    // Auto-sizing properties
    public float AutoSizeHorizontalPadding = 0.5f; // Extra padding on left/right
    public float AutoSizeVerticalPadding = 0.5f; // Extra padding on top/bottom
    public bool AutoSizeEnabled = false;

    #region Row and column properties
    public void SetColumnWidth(int column, float width)
    {
        // Ensure the list is large enough
        while (columnWidths.Count <= column)
        {
            columnWidths.Add(HorizontalSpacing);
        }
        columnWidths[column] = width;
    }
    public void SetRowHeight(int row, float height)
    {
        // Ensure the list is large enough
        while (rowHeights.Count <= row)
        {
            rowHeights.Add(VerticalSpacing);
        }
        rowHeights[row] = height;
    }
    public void SetColumnAlignment(int column, LatexNode.HorizontalAlignmentOptions alignment)
    {
        while (_columnAlignments.Count <= column)
        {
            _columnAlignments.Add(DefaultHorizontalAlignment);
        }
        _columnAlignments[column] = alignment;
    }
    public void SetRowAlignment(int row, LatexNode.VerticalAlignmentOptions alignment)
    {
        while (_rowAlignments.Count <= row)
        {
            _rowAlignments.Add(DefaultVerticalAlignment);
        }
        _rowAlignments[row] = alignment;
    }
    
    private float GetColumnWidth(int column)
    {
        if (column < columnWidths.Count)
            return columnWidths[column];
        return HorizontalSpacing;
    }
    private float GetRowHeight(int row)
    {
        if (row < rowHeights.Count)
            return rowHeights[row];
        return VerticalSpacing;
    }
    public void SetAllColumnWidths(float width)
    {
        int maxColumn = _cells.Count;
        for (int i = 0; i < maxColumn; i++)
        {
            SetColumnWidth(i, width);
        }
    }
    public void SetAllRowHeights(float height)
    {
        int maxRow = _cells.Any() ? _cells.Max(col => col.Count) : 0;
        for (int i = 0; i < maxRow; i++)
        {
            SetRowHeight(i, height);
        }
    }
    private LatexNode.HorizontalAlignmentOptions GetColumnAlignment(int column)
    {
        if (column < _columnAlignments.Count)
            return _columnAlignments[column];
        return DefaultHorizontalAlignment;
    }
    private LatexNode.VerticalAlignmentOptions GetRowAlignment(int row)
    {
        if (row < _rowAlignments.Count)
            return _rowAlignments[row];
        return DefaultVerticalAlignment;
    }
    private float GetColumnPosition(int column)
    {
        float position = 0;
        for (int i = 0; i < column; i++)
        {
            position += GetColumnWidth(i);
        }
        return position;
    }
    private float GetRowPosition(int row)
    {
        float position = 0;
        for (int i = 0; i < row; i++)
        {
            position -= GetRowHeight(i);
        }
        return position;
    }
    private Vector3 GetAlignmentOffset(int row, int column, Node3D node)
    {
        var horizontalAlignment = GetColumnAlignment(column);
        var verticalAlignment = GetRowAlignment(row);
        
        float xOffset = 0;
        float yOffset = 0;
        
        // For horizontal alignment within the cell
        switch (horizontalAlignment)
        {
            case LatexNode.HorizontalAlignmentOptions.Left:
                xOffset = 0; // Already at left edge
                break;
            case LatexNode.HorizontalAlignmentOptions.Center:
                xOffset = GetColumnWidth(column) / 2f;
                break;
            case LatexNode.HorizontalAlignmentOptions.Right:
                xOffset = GetColumnWidth(column);
                break;
        }
        
        // For vertical alignment within the cell
        switch (verticalAlignment)
        {
            case LatexNode.VerticalAlignmentOptions.Top:
                yOffset = 0; // Already at top edge
                break;
            case LatexNode.VerticalAlignmentOptions.Center:
                yOffset = -GetRowHeight(row) / 2f;
                break;
            case LatexNode.VerticalAlignmentOptions.Bottom:
                yOffset = -GetRowHeight(row);
                break;
        }
        
        return new Vector3(xOffset, yOffset, 0);
    }
    #endregion
    
    public void AddNode3DToPosition(Node3D node, int row, int column)
    {
        AddChild(node);
        
        // Calculate position based on cumulative widths/heights
        float xPos = GetColumnPosition(column);
        float yPos = GetRowPosition(row);
        
        // Apply alignment offset
        var alignmentOffset = GetAlignmentOffset(row, column, node);
        
        node.Position = new Vector3(xPos, yPos, 0) + alignmentOffset;
        
        // Make sure the cells structure can hold this cell
        while (_cells.Count <= column)
        {
            _cells.Add(new List<Node3D>());
        }
        while (_cells[column].Count <= row)
        {
            _cells[column].Add(null);
        }
        
        if (_cells[column][row] != null)
        {
            GD.PrintErr("Overwriting a cell in the table");
        }
        else
        {
            _cells[column][row] = node;
        }
        
        // Update grid lines after adding a node
        UpdateGridLines();
        
        // Auto-size if enabled
        if (AutoSizeEnabled)
        {
            CallDeferred(nameof(AutoSizeColumnsAndRows));
        }
    }
    public void AddLatexNodeToPositionWithDefaultSettingsForTheTable(string latexString, int row, int column)
    {
        var newLatexNode = new LatexNode();
        newLatexNode.Latex = latexString;
        newLatexNode.UpdateCharacters();
        
        // Use separate scales for header row and header column
        if (row == 0)
        {
            newLatexNode.Scale = HeaderRowScale;
        }
        else if (column == 0)
        {
            newLatexNode.Scale = HeaderColumnScale;
        }
        else
        {
            newLatexNode.Scale = CellLatexScale;
        }
        
        // Set alignment based on column and row settings
        newLatexNode.HorizontalAlignment = GetColumnAlignment(column);
        newLatexNode.VerticalAlignment = GetRowAlignment(row);
        
        AddNode3DToPosition(newLatexNode, row, column);
    }
    
    public void SetScaleOfAllChildren(Vector3 scale)
    {
        foreach (var child in GetChildren())
        {
            if (child is Node3D node)
            {
                node.Scale = scale;
            }
        }
    }
    public Animation ScaleAllChildrenToDefault()
    {
        GD.PushWarning("Keyframed animation system is deprecated. Make a StateChange object instead.");
        var animations = new List<Animation>();
        for (var i = 0; i < _cells.Count; i++)
        {
            for (var j = 0; j < _cells[i].Count; j++)
            {
                if (_cells[i][j] is not null)
                {
                    animations.Add(ScaleCellToDefault(j, i)); // Note: j is row, i is column
                }
            }
        }
        return animations.InParallel();
    }
    public Animation ScaleCellToDefault(int row, int column)
    {
        GD.PushWarning("Keyframed animation system is deprecated. Make a StateChange object instead.");
        if (column < _cells.Count && row < _cells[column].Count && _cells[column][row] is not null)
        {
            Vector3 targetScale;
            if (row == 0)
            {
                targetScale = HeaderRowScale;
            }
            else if (column == 0)
            {
                targetScale = HeaderColumnScale;
            }
            else
            {
                targetScale = CellLatexScale;
            }
            return _cells[column][row].ScaleToAnimation(targetScale);
        }
        
        GD.PrintErr("No cell at that position");
        return null;
    }
    
    #region Auto-sizing
    public async void AutoSizeColumnsAndRows()
    {
        // Wait a bit for LatexNodes to process
        await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        
        // Calculate required sizes
        var requiredColumnWidths = new Dictionary<int, float>();
        var requiredRowHeights = new Dictionary<int, float>();
        
        // Iterate through all cells
        for (int col = 0; col < _cells.Count; col++)
        {
            for (int row = 0; row < _cells[col].Count; row++)
            {
                var node = _cells[col][row];
                if (node == null) continue;
                
                // Get the bounding box for this cell's content
                var aabb = GetNodeBoundingBox(node);
                
                // For center-aligned content, we need the full width from center
                // For left/right aligned, we need to account for the actual position
                float contentWidth = aabb.Size.X;
                float contentHeight = aabb.Size.Y;
                
                // If the node is a LatexNode, check its alignment
                if (node is LatexNode latexNode)
                {
                    // For center alignment, the content extends equally on both sides
                    if (latexNode.HorizontalAlignment == LatexNode.HorizontalAlignmentOptions.Center)
                    {
                        // Content is centered, so we don't need extra width
                    }
                    // For left/right alignment, we might need to adjust
                }
                
                // Update required column width
                float requiredWidth = contentWidth + AutoSizeHorizontalPadding * 2;
                if (!requiredColumnWidths.ContainsKey(col) || requiredColumnWidths[col] < requiredWidth)
                {
                    requiredColumnWidths[col] = requiredWidth;
                }
                
                // Update required row height
                float requiredHeight = contentHeight + AutoSizeVerticalPadding * 2;
                if (!requiredRowHeights.ContainsKey(row) || requiredRowHeights[row] < requiredHeight)
                {
                    requiredRowHeights[row] = requiredHeight;
                }
            }
        }
        
        // Apply the calculated sizes
        foreach (var kvp in requiredColumnWidths)
        {
            SetColumnWidth(kvp.Key, kvp.Value);
        }
        
        foreach (var kvp in requiredRowHeights)
        {
            SetRowHeight(kvp.Key, kvp.Value);
        }
        
        // Reposition all nodes with new sizes
        RepositionAllNodes();
        
        // Update grid lines
        UpdateGridLines();
    }
    
    private Aabb GetNodeBoundingBox(Node3D node)
    {
        var aabb = new Aabb();
        bool first = true;
        
        // Get all MeshInstance3D nodes recursively
        var meshInstances = GetAllMeshInstances(node);
        
        foreach (var meshInstance in meshInstances)
        {
            if (meshInstance.Mesh == null) continue;
            
            // Get the mesh's AABB in world space
            var meshAabb = meshInstance.Mesh.GetAabb();
            
            // Transform to world space
            var worldTransform = meshInstance.GlobalTransform;
            meshAabb = worldTransform * meshAabb;
            
            // Transform back to the table's local space
            var tableInverseTransform = GlobalTransform.AffineInverse();
            meshAabb = tableInverseTransform * meshAabb;
            
            if (first)
            {
                aabb = meshAabb;
                first = false;
            }
            else
            {
                aabb = aabb.Merge(meshAabb);
            }
        }
        
        // If no mesh instances found, use a default size
        if (first)
        {
            aabb = new Aabb(Vector3.Zero, Vector3.One * 0.1f);
        }
        
        return aabb;
    }
    
    private List<MeshInstance3D> GetAllMeshInstances(Node node)
    {
        var meshInstances = new List<MeshInstance3D>();
        
        if (node is MeshInstance3D meshInstance)
        {
            meshInstances.Add(meshInstance);
        }
        
        foreach (var child in node.GetChildren())
        {
            meshInstances.AddRange(GetAllMeshInstances(child));
        }
        
        return meshInstances;
    }
    
    private void RepositionAllNodes()
    {
        for (int col = 0; col < _cells.Count; col++)
        {
            for (int row = 0; row < _cells[col].Count; row++)
            {
                var node = _cells[col][row];
                if (node == null) continue;
                
                // Calculate new position
                float xPos = GetColumnPosition(col);
                float yPos = GetRowPosition(row);
                
                // Apply alignment offset
                var alignmentOffset = GetAlignmentOffset(row, col, node);
                
                node.Position = new Vector3(xPos, yPos, 0) + alignmentOffset;
            }
        }
    }
    
    public void EnableAutoSizing()
    {
        AutoSizeEnabled = true;
        AutoSizeColumnsAndRows();
    }
    
    public void DisableAutoSizing()
    {
        AutoSizeEnabled = false;
    }
    #endregion
    
    #region Grid Lines
    public void UpdateGridLines()
    {
        // Clear existing grid lines
        foreach (var line in _gridLines)
        {
            line.QueueFree();
        }
        _gridLines.Clear();
        
        if (!ShowGridLines)
            return;
        
        // Calculate table dimensions
        int numColumns = _cells.Count;
        int numRows = numColumns > 0 ? _cells.Max(col => col.Count) : 0;
        
        if (numColumns == 0 || numRows == 0)
            return;
        
        // Create vertical lines (between columns and at edges)
        for (int col = 0; col <= numColumns; col++)
        {
            float xPos;
            if (col == 0)
            {
                xPos = 0; // Left edge
            }
            else if (col == numColumns)
            {
                // Right edge of the last column
                xPos = GetColumnPosition(numColumns - 1) + GetColumnWidth(numColumns - 1);
            }
            else
            {
                // Between columns
                xPos = GetColumnPosition(col);
            }
            
            float topY = 0;
            float bottomY = GetRowPosition(numRows - 1) - GetRowHeight(numRows - 1);
            
            CreateVerticalLine(xPos, topY, bottomY);
        }
        
        // Create horizontal lines (between rows and at edges)
        for (int row = 0; row <= numRows; row++)
        {
            float yPos;
            if (row == 0)
            {
                yPos = 0; // Top edge
            }
            else if (row == numRows)
            {
                // Bottom edge of the last row
                yPos = GetRowPosition(numRows - 1) - GetRowHeight(numRows - 1);
            }
            else
            {
                // Between rows
                yPos = GetRowPosition(row);
            }
            
            float leftX = 0;
            float rightX = GetColumnPosition(numColumns - 1) + GetColumnWidth(numColumns - 1);
            
            CreateHorizontalLine(leftX, rightX, yPos);
        }
    }
    
    private void CreateVerticalLine(float x, float topY, float bottomY)
    {
        var line = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        
        float height = Mathf.Abs(topY - bottomY);
        cylinder.Height = height;
        cylinder.TopRadius = GridLineThickness / 2f;
        cylinder.BottomRadius = GridLineThickness / 2f;
        cylinder.RadialSegments = 8;
        
        line.Mesh = cylinder;
        
        // Create material
        var material = new StandardMaterial3D();
        material.AlbedoColor = GridLineColor;
        line.MaterialOverride = material;
        
        // Position - cylinder's default orientation is along Y axis, so we need to rotate for vertical
        line.Position = new Vector3(x, (topY + bottomY) / 2f, 0);
        line.RotationDegrees = new Vector3(0, 0, 0); // No rotation needed for vertical
        
        AddChild(line);
        _gridLines.Add(line);
    }
    
    private void CreateHorizontalLine(float leftX, float rightX, float y)
    {
        var line = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        
        float width = Mathf.Abs(rightX - leftX);
        cylinder.Height = width;
        cylinder.TopRadius = GridLineThickness / 2f;
        cylinder.BottomRadius = GridLineThickness / 2f;
        cylinder.RadialSegments = 8;
        
        line.Mesh = cylinder;
        
        // Create material
        var material = new StandardMaterial3D();
        material.AlbedoColor = GridLineColor;
        line.MaterialOverride = material;
        
        // Position and rotate - cylinder's default orientation is along Y axis, so rotate 90 degrees on Z for horizontal
        line.Position = new Vector3((leftX + rightX) / 2f, y, 0);
        line.RotationDegrees = new Vector3(0, 0, 90); // Rotate to be horizontal
        
        AddChild(line);
        _gridLines.Add(line);
    }
    #endregion
}
