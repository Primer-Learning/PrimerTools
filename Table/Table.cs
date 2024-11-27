using Godot;
using System;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.LaTeX;

public partial class Table : Node3D
{
    // int numColumns = 3;
    // int numRows = 3;
    // private bool hasHeaderRow = true;
    // private bool hasHeaderColumn = true;
    public int HorizontalSpacing = 4;
    public int VerticalSpacing = 2;
    
    public Vector3 HeaderLatexScale = Vector3.One * 0.5f;
    public Vector3 CellLatexScale = Vector3.One * 1f;
    public LatexNode.HorizontalAlignmentOptions DefaultHorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
    public LatexNode.VerticalAlignmentOptions DefaultVerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;

    private List<List<Node3D>> cells = new();
    
    public void AddNode3DToPosition(Node3D node, int row, int column)
    {
        AddChild(node);
        node.Position = new Vector3(column * HorizontalSpacing, -row * VerticalSpacing, 0);
        
        // Make sure the column exists
        if (cells.Count <= column)
        {
            cells.Add(new List<Node3D>());
        }
        if (cells[column].Count <= row)
        {
            cells[column].Add(null);
        }
        if (cells[column][row] != null)
        {
            GD.PrintErr("Overwriting a cell in the table");
        }
        else
        {
            cells[column][row] = node;
        }
    }
    
    public void AddLatexNodeToPositionWithDefaultSettingsForTheTable(string latexString, int row, int column)
    {
        var newLatexNode = new LatexNode();
        newLatexNode.latex = latexString;
        newLatexNode.UpdateCharacters();
        
        newLatexNode.Scale = row == 0 || column == 0 ? HeaderLatexScale : CellLatexScale;
        
        newLatexNode.HorizontalAlignment = DefaultHorizontalAlignment;
        newLatexNode.VerticalAlignment = DefaultVerticalAlignment;
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
        var animations = new List<Animation>();
        for (var i = 0; i < cells.Count; i++)
        {
            for (var j = 0; j < cells[i].Count; j++)
            {
                if (cells[i][j] is not null)
                {
                    animations.Add(ScaleCellToDefault(i, j));
                }
            }
        }
        return animations.InParallel();
    }
    
    public Animation ScaleCellToDefault(int row, int column)
    {
        if (cells[column][row] is not null)
            return cells[column][row].ScaleTo(row == 0 || column == 0 ? HeaderLatexScale : CellLatexScale);
        
        GD.PrintErr("No cell at that position");
        return null;
    }

    // public void SetSpacing(int horizontal, int vertical)
    // {
    //     horizontalSpacing = horizontal;
    //     verticalSpacing = vertical;
    // }
}
