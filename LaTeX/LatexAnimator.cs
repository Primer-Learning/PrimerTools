using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrimerTools.LaTeX;

[Tool]
public partial class LatexAnimator : Node3D
{
    private List<LatexNode> _latexNodes = [];
    private int _currentExpressionIndex = 0;

    public int CurrentExpressionIndex
    {
        get => _currentExpressionIndex;
        set
        {
            _currentExpressionIndex = value;
            for (var i = 0; i < _latexNodes.Count; i++)
            {
                _latexNodes[i].Visible = i == _currentExpressionIndex;
            }
        }
    }

    public void AddExpression(string expression)
    {
        var newLatexNode = new LatexNode(expression);
        AddChild(newLatexNode);
        if (_latexNodes.Count != 0)
        {
            newLatexNode.Visible = false;
        }
        _latexNodes.Add(newLatexNode);
    }

    public Animation AnimateToExpression(
        int newIndex, 
        List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)> preservedCharacterMap)
    {
        
        // Turn the preservedCharacterMap into lists of indices
        var preservedFromIndices = new List<int>();
        var preservedToIndices = new List<int>();
        foreach (var chunk in preservedCharacterMap)
        {
            for (var i = 0; i < chunk.chunkLength; i++)
            {
                preservedFromIndices.Add(chunk.currentExpressionChunkBeginIndex + i);
                preservedToIndices.Add(chunk.nextExpressionChunkBeginIndex + i);
            }
        }
        
        
        // Create a combined node with all the characters from both LatexNodes, with the next expression scale zero.
        var intermediateNode = new Node3D();
        intermediateNode.Visible = false;
        AddChild(intermediateNode);

        var copiesOfCurrentExpressionCharacters = new List<Node3D>();
        foreach (var character in _latexNodes[_currentExpressionIndex].GetChild(0).GetChildren().OfType<Node3D>())
        {
            var copy = (Node3D)character.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = character.GlobalPosition;
            copiesOfCurrentExpressionCharacters.Add(copy);
        }
        var copiesOfNextExpressionCharacters = new List<Node3D>();
        foreach (var character in _latexNodes[newIndex].GetChild(0).GetChildren().OfType<Node3D>())
        {
            var copy = (Node3D)character.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = character.GlobalPosition;
            copy.Scale = Vector3.Zero;
            copiesOfNextExpressionCharacters.Add(copy);
        }

        var disappearAnimations = new List<Animation>();
        disappearAnimations.Add(_latexNodes[_currentExpressionIndex].AnimateBool(false, LatexNode.PropertyName.Visible));
        disappearAnimations.Add(intermediateNode.AnimateBool(true, Node3D.PropertyName.Visible));
        // Identify the characters in the currentExpression that aren't in the preservedCharacterMap
        // Make those scale down.
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (preservedFromIndices.Contains(i)) continue;
            disappearAnimations.Add(copiesOfCurrentExpressionCharacters[i].ScaleTo(0));
        }

        var movementAnimations = new List<Animation>();
        // Move the preserved characters from their current position to their next position.
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (!preservedFromIndices.Contains(i)) continue;

            var movementIndex = preservedFromIndices.IndexOf(i);
            var indexInNextExpression = preservedToIndices[movementIndex];
            movementAnimations.Add(copiesOfCurrentExpressionCharacters[i].MoveTo(copiesOfNextExpressionCharacters[indexInNextExpression].Position));
        }

        var appearanceAnimations = new List<Animation>();
        for (var i = 0; i < copiesOfNextExpressionCharacters.Count; i++)
        {
            if (preservedToIndices.Contains(i)) continue;
            appearanceAnimations.Add(copiesOfNextExpressionCharacters[i].ScaleTo(1));
        }

        // Add any new characters not in the preserved character map
        
        // Swap visibility to new expression
        // Remake old expression

        return AnimationUtilities.Series(
            disappearAnimations.InParallel(),
            movementAnimations.InParallel(),
            appearanceAnimations.InParallel()
        );
    }
}