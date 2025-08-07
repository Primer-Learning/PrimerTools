using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimerTools;
using PrimerTools.LaTeX;
using PrimerTools.TweenSystem;

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

    private readonly Dictionary<Node3D, (int row, int column)> _nodePositions = new();
    
    // Track which columns and rows actually contain content
    private readonly HashSet<int> _occupiedColumns = new();
    private readonly HashSet<int> _occupiedRows = new();
    
    // Grid line properties
    public bool ShowGridLines = true;
    public float GridLineThickness = 0.05f;
    public Color GridLineColor = Colors.Gray;
    // private readonly List<MeshInstance3D> _gridLines = new();
    private readonly List<MeshInstance3D> _horizontalGridLines = new();
    private readonly List<MeshInstance3D> _verticalGridLines = new();
    
    // Auto-sizing properties
    public float AutoSizeHorizontalPadding = 1; // Extra padding on left/right
    public float AutoSizeVerticalPadding = 1; // Extra padding on top/bottom
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
        
        // If setting a non-zero width, mark as occupied
        if (width > 0)
        {
            _occupiedColumns.Add(column);
        }
    }
    public void SetRowHeight(int row, float height)
    {
        // Ensure the list is large enough
        while (rowHeights.Count <= row)
        {
            rowHeights.Add(VerticalSpacing);
        }
        rowHeights[row] = height;
        
        // If setting a non-zero height, mark as occupied
        if (height > 0)
        {
            _occupiedRows.Add(row);
        }
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
        // Empty columns have zero width
        if (!_occupiedColumns.Contains(column))
            return 0;
            
        if (column < columnWidths.Count)
            return columnWidths[column];
        return HorizontalSpacing;
    }
    private float GetRowHeight(int row)
    {
        // Empty rows have zero height
        if (!_occupiedRows.Contains(row))
            return 0;
            
        if (row < rowHeights.Count)
            return rowHeights[row];
        return VerticalSpacing;
    }
    public void SetAllColumnWidths(float width)
    {
        int maxColumn = GetMaxColumn();
        for (int i = 0; i <= maxColumn; i++)
        {
            SetColumnWidth(i, width);
        }
    }
    public void SetAllRowHeights(float height)
    {
        int maxRow = GetMaxRow();
        for (int i = 0; i <= maxRow; i++)
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
        // GD.Print($"Row {row} is at y = {position}");
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
        
        // Check if there's already a node at this position
        var existingNode = GetNodeAt(row, column);
        if (existingNode != null)
        {
            GD.PrintErr("Overwriting a cell in the table");
            _nodePositions.Remove(existingNode);
        }
        
        _nodePositions[node] = (row, column);
        
        // Mark this row and column as occupied
        _occupiedColumns.Add(column);
        _occupiedRows.Add(row);
        
        if (AutoSizeEnabled)
        {
            CalculateAutoSizes();
        }
        
        // Calculate position based on cumulative widths/heights
        var xPos = GetColumnPosition(column);
        var yPos = GetRowPosition(row);
        
        // Apply alignment offset
        var alignmentOffset = GetAlignmentOffset(row, column, node);
        node.Position = new Vector3(xPos, yPos, 0) + alignmentOffset;
    }
    public void AddLatexNodeToPositionWithDefaultSettingsForTheTable(string latexString, int row, int column)
    {
        var newLatexNode = new LatexNode();
        newLatexNode.Latex = latexString;
        newLatexNode.UpdateCharacters();

        // Determine target scale
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

        // Store target scale in metadata
        newLatexNode.SetMeta("target_scale", targetScale);

        // Start at zero scale (invisible)
        newLatexNode.Scale = Vector3.Zero;

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
    
    #region Auto-sizing
    private void CalculateAutoSizes()
    {
        // GD.Print("Calculating auto-size");
        // Calculate required sizes
        var requiredColumnWidths = new Dictionary<int, float>();
        var requiredRowHeights = new Dictionary<int, float>();
        
        // Iterate through all cells
        foreach (var kvp in _nodePositions)
        {
            var node = kvp.Key;
            var (row, col) = kvp.Value;
                
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
        
        // Apply the calculated sizes
        foreach (var kvp in requiredColumnWidths)
        {
            SetColumnWidth(kvp.Key, kvp.Value);
        }
        
        foreach (var kvp in requiredRowHeights)
        {
            SetRowHeight(kvp.Key, kvp.Value);
        }
    }
    
    private Aabb GetNodeBoundingBox(Node3D node)
    {
        // Store original scale
        var originalScale = node.Scale;
        
        // Temporarily set to target scale if node is at zero scale
        if (node.Scale.Length() < 0.001)
        {
            // Find the target scale for this node
            if (_nodePositions.TryGetValue(node, out var position))
            {
                node.Scale = GetTargetScaleForCell(node, position.row, position.column);
            }
        }
        
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
        
        // Restore original scale
        node.Scale = originalScale;
        
        // If no mesh instances found, use a default size
        if (first)
        {
            aabb = new Aabb(Vector3.Zero, Vector3.One * 0.1f);
        }
        
        // GD.Print($"Bounding box for {node.Name} is {aabb.Size}");
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
    
    public void RepositionAllNodes()
    {
        foreach (var kvp in _nodePositions)
        {
            var node = kvp.Key;
            var (row, col) = kvp.Value;
            
            // Calculate new position
            float xPos = GetColumnPosition(col);
            float yPos = GetRowPosition(row);
            
            // Apply alignment offset
            var alignmentOffset = GetAlignmentOffset(row, col, node);
            
            node.Position = new Vector3(xPos, yPos, 0) + alignmentOffset;
        }
    }
    #endregion
    
    #region Transition System
    public IStateChange TransitionStateChange(double duration = 0.5)
    {
        var composite = new CompositeStateChange().WithName("Table Transition");

        // 1. Update grid lines based on current table dimensions
        composite.AddStateChange(TransitionGridLines(duration));

        // 2. Scale up any cells that are at zero scale
        foreach (var kvp in _nodePositions)
        {
            var node = kvp.Key;
            var (row, col) = kvp.Value;
    
            // Check if node needs to scale up
            if (node.Scale.IsZeroApprox())
            {
                Vector3 targetScale = GetTargetScaleForCell(node, row, col);
                composite.AddStateChangeInParallel(
                    node.ScaleTo(targetScale).WithDuration(duration)
                );
            }
    
            // Check if node is in the right position
            var targetPos = GetTargetPositionForCell(row, col, node);
            if (!node.Position.IsEqualApprox(targetPos))
            {
                composite.AddStateChangeInParallel(
                    node.MoveTo(targetPos).WithDuration(duration)
                );
            }
        }

        return composite;
    }
    private IStateChange TransitionGridLines(double duration)
    {
        var composite = new CompositeStateChange().WithName("Grid Line Transition");

        if (!ShowGridLines)
            return composite;

        // Calculate what grid lines should exist
        var maxColumn = GetMaxColumn();
        var maxRow = GetMaxRow();

        if (maxColumn < 0 || maxRow < 0)
            return composite;
        
        // Draw horizontal lines from 0 to maxRow + 1
        for (var i = 0; i <= maxRow + 1; i++)
        {
            float leftX = GetColumnPosition(0);
            float rightX = GetColumnPosition(maxColumn) + GetColumnWidth(maxColumn);
            
            MeshInstance3D line;
            if (i >= _horizontalGridLines.Count)
            {
                float lastY;
                if (i == 0)
                {
                    lastY = 0;
                }
                else lastY = GetRowPosition(i); 
                
                line = CreateHorizontalLine(leftX, rightX, lastY);
                ((CylinderMesh)line.Mesh).Height = 0;
            }
            else
            {
                line = _horizontalGridLines[i];
            }
            
            var targetY = i > maxRow ? GetRowPosition(maxRow) - GetRowHeight(maxRow) : GetRowPosition(i);
            var targetX = (rightX - leftX) / 2;
            var targetPos = new Vector3(targetX, targetY, 0);
            
            // Animate to target position if different
            if ((line.Position - targetPos).LengthSquared() > 0.001f)
            {
                composite.AddStateChangeInParallel(
                    line.MoveTo(targetPos)
                        .WithDuration(duration)
                );
            }
            if (!Mathf.IsEqualApprox(((CylinderMesh)line.Mesh).Height, rightX - leftX))
            {
                composite.AddStateChangeInParallel(
                    new PropertyStateChange((CylinderMesh)line.Mesh, "height", rightX - leftX).WithDuration(duration)
                );
            }
        }
        
        // Draw vertical lines from 0 to maxColumn + 1
        for (var i = 0; i <= maxColumn + 1; i++)
        {
            float topY = GetRowPosition(0);
            float bottomY = GetRowPosition(maxRow) - GetRowHeight(maxRow);
            
            MeshInstance3D line;
            if (i >= _verticalGridLines.Count)
            {
                float lastX;
                if (i == 0)
                {
                    lastX = 0;
                }
                else lastX = GetColumnPosition(i); 
                
                line = CreateVerticalLine(lastX, topY, bottomY);
                ((CylinderMesh)line.Mesh).Height = 0;
            }
            else
            {
                line = _verticalGridLines[i];
            }
            
            var targetX = i > maxColumn ? GetColumnPosition(maxColumn) + GetColumnWidth(maxColumn) : GetColumnPosition(i);
            var targetY = (topY + bottomY) / 2;
            var targetPos = new Vector3(targetX, targetY, 0);
            
            // Animate to target position if different
            if ((line.Position - targetPos).LengthSquared() > 0.001f)
            {
                composite.AddStateChangeInParallel(
                    line.MoveTo(targetPos)
                        .WithDuration(duration)
                );
            }
            if (!Mathf.IsEqualApprox(((CylinderMesh)line.Mesh).Height, topY - bottomY))
            {
                composite.AddStateChangeInParallel(
                    new PropertyStateChange((CylinderMesh)line.Mesh, "height", topY - bottomY).WithDuration(duration)
                );
            }
        }

        return composite;
    }
    private Vector3 GetTargetScaleForCell(Node3D node, int row, int col)
    {
        // Check if node has stored target scale
        if (node.HasMeta("target_scale"))
            return node.GetMeta("target_scale").AsVector3();

        // Otherwise use default logic
        if (row == 0)
            return HeaderRowScale;
        else if (col == 0)
            return HeaderColumnScale;
        else
            return CellLatexScale;
    }
    private Vector3 GetTargetPositionForCell(int row, int col, Node3D node)
    {
        float xPos = GetColumnPosition(col);
        float yPos = GetRowPosition(row);
        var alignmentOffset = GetAlignmentOffset(row, col, node);
        return new Vector3(xPos, yPos, 0) + alignmentOffset;
    }
    private float GetTableWidth()
    {
        int maxColumn = GetMaxColumn();
        if (maxColumn < 0) return 0;
        return GetColumnPosition(maxColumn) + GetColumnWidth(maxColumn);
    }
    
    // Helper methods for the new structure
    public Node3D GetNodeAt(int row, int column)
    {
        foreach (var kvp in _nodePositions)
        {
            if (kvp.Value.row == row && kvp.Value.column == column)
                return kvp.Key;
        }
        return null;
    }
    private int GetMaxRow()
    {
        if (_nodePositions.Count == 0) return -1;
        return _nodePositions.Values.Max(pos => pos.row);
    }
    private int GetMaxColumn()
    {
        if (_nodePositions.Count == 0) return -1;
        return _nodePositions.Values.Max(pos => pos.column);
    }
    
    // Public method to update node position in the table
    public void UpdateNodePosition(Node3D node, int newRow, int newColumn)
    {
        if (_nodePositions.ContainsKey(node))
        {
            _nodePositions[node] = (newRow, newColumn);
        }
        else
        {
            GD.PrintErr($"Node {node.Name} is not in the table");
        }
    }
    
    // Public method to swap two nodes' positions
    public void SwapNodePositions(Node3D node1, Node3D node2)
    {
        if (_nodePositions.TryGetValue(node1, out var pos1) && 
            _nodePositions.TryGetValue(node2, out var pos2))
        {
            _nodePositions[node1] = pos2;
            _nodePositions[node2] = pos1;
        }
        else
        {
            GD.PrintErr("One or both nodes are not in the table");
        }
    }
    
    // Remove a node from the table
    public void RemoveNode(Node3D node)
    {
        if (_nodePositions.TryGetValue(node, out var position))
        {
            _nodePositions.Remove(node);
            
            // Check if this was the last node in its row/column
            bool columnStillOccupied = false;
            bool rowStillOccupied = false;
            
            foreach (var kvp in _nodePositions)
            {
                if (kvp.Value.column == position.column)
                    columnStillOccupied = true;
                if (kvp.Value.row == position.row)
                    rowStillOccupied = true;
            }
            
            if (!columnStillOccupied)
                _occupiedColumns.Remove(position.column);
            if (!rowStillOccupied)
                _occupiedRows.Remove(position.row);
                
            node.QueueFree();
        }
    }
    #endregion
    
    #region Grid Lines
    private MeshInstance3D CreateVerticalLine(float x, float topY, float bottomY)
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
        _verticalGridLines.Add(line);
        return line;
    }
    private MeshInstance3D CreateHorizontalLine(float leftX, float rightX, float y)
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
        _horizontalGridLines.Add(line);
        return line;
    }
    #endregion
}
