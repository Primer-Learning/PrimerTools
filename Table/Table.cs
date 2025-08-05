using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimerTools;
using PrimerTools.LaTeX;

public partial class Table : Node3D
{
    // Spacing and sizing
    public int HorizontalSpacing = 4;
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
}
