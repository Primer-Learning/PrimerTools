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

    public void AddExpression(LatexNode newLatexNode)
    {
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
        if (newIndex == _currentExpressionIndex)
        {
            GD.PushError($"Trying to animate to the current expression index: {newIndex}. Returning empty animation.");
            return new Animation().WithDuration(0);
        }
        
        // Turn the preservedCharacterMap into lists of indices, making later steps easier
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
        
        // Disappear animations
        var disappearAnimations = new List<Animation>();
        disappearAnimations.Add(_latexNodes[_currentExpressionIndex].AnimateBool(false, LatexNode.PropertyName.Visible));
        disappearAnimations.Add(intermediateNode.AnimateBool(true, Node3D.PropertyName.Visible));
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (preservedFromIndices.Contains(i)) continue;
            disappearAnimations.Add(copiesOfCurrentExpressionCharacters[i].ScaleTo(0));
        }

        // Move the preserved characters from their current position to their next position.
        var movementAnimations = new List<Animation>();
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (!preservedFromIndices.Contains(i)) continue;

            var movementIndex = preservedFromIndices.IndexOf(i);
            var indexInNextExpression = preservedToIndices[movementIndex];
            movementAnimations.Add(copiesOfCurrentExpressionCharacters[i].MoveTo(copiesOfNextExpressionCharacters[indexInNextExpression].Position));
        }

        // Scale up the new characters
        var appearanceAnimations = new List<Animation>();
        for (var i = 0; i < copiesOfNextExpressionCharacters.Count; i++)
        {
            if (preservedToIndices.Contains(i)) continue;
            appearanceAnimations.Add(copiesOfNextExpressionCharacters[i].ScaleTo(1));
        }

        // Swap visibility to new expression
        var finalSwapAnimation = AnimationUtilities.Parallel(
            _latexNodes[newIndex].AnimateBool(true, LatexNode.PropertyName.Visible),
            intermediateNode.AnimateBool(false, Node3D.PropertyName.Visible)
        );

        _currentExpressionIndex = newIndex;

        return AnimationUtilities.Series(
            disappearAnimations.InParallel(),
            movementAnimations.InParallel(),
            appearanceAnimations.InParallel(),
            finalSwapAnimation
        );
    }
}