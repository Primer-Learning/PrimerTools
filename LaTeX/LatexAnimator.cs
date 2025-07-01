using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerTools.TweenSystem;

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
            disappearAnimations.Add(copiesOfCurrentExpressionCharacters[i].ScaleToAnimation(0));
        }

        // Move the preserved characters from their current position to their next position.
        var movementAnimations = new List<Animation>();
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (!preservedFromIndices.Contains(i)) continue;

            var movementIndex = preservedFromIndices.IndexOf(i);
            var indexInNextExpression = preservedToIndices[movementIndex];
            movementAnimations.Add(copiesOfCurrentExpressionCharacters[i].MoveToAnimation(copiesOfNextExpressionCharacters[indexInNextExpression].Position));
        }

        // Scale up the new characters
        var appearanceAnimations = new List<Animation>();
        for (var i = 0; i < copiesOfNextExpressionCharacters.Count; i++)
        {
            if (preservedToIndices.Contains(i)) continue;
            appearanceAnimations.Add(copiesOfNextExpressionCharacters[i].ScaleToAnimation(1));
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

    /// <summary>
    /// Animates to a different expression
    /// </summary>
    /// <param name="newIndex">The index of the new expression</param>
    /// <param name="preservedCharacterMap">Specifies the characters that should be kept during the transition</param>
    /// <returns></returns>
    public CompositeStateChange AnimateToExpressionStateChange(
        int newIndex, 
        List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)> preservedCharacterMap)
    {
        // This doesn't work if the scale is zero, which it often is at the beginning of a scene
        var oldScale = Scale;
        Scale = Vector3.One;
        
        if (newIndex == _currentExpressionIndex)
        {
            GD.PushError($"Trying to animate to the current expression index: {newIndex}. Returning empty state change.");
            return new CompositeStateChange().WithName("Empty Expression Change");
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
        intermediateNode.Name = "Intermediate ";

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
        
        var composite = new CompositeStateChange().WithName($"Latex Expression {_currentExpressionIndex} to {newIndex}");
        
        // Disappear phase
        var disappearPhase = new CompositeStateChange().WithName("Disappear Phase");
        disappearPhase.AddStateChange(new PropertyStateChange(_latexNodes[_currentExpressionIndex], "visible", false).WithDuration(0.001));
        disappearPhase.AddStateChangeInParallel(new PropertyStateChange(intermediateNode, "visible", true).WithDuration(0.001));
        
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (preservedFromIndices.Contains(i)) continue;
            disappearPhase.AddStateChangeInParallel(copiesOfCurrentExpressionCharacters[i].ScaleTo(0));
        }
        composite.AddStateChange(disappearPhase);
        
        // Movement phase
        var movementPhase = new CompositeStateChange().WithName("Movement Phase");
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (!preservedFromIndices.Contains(i)) continue;
        
            var movementIndex = preservedFromIndices.IndexOf(i);
            var indexInNextExpression = preservedToIndices[movementIndex];
            movementPhase.AddStateChangeInParallel(copiesOfCurrentExpressionCharacters[i].MoveTo(copiesOfNextExpressionCharacters[indexInNextExpression].Position));
        }
        if (movementPhase.Duration > 0) // Only add if there are movements
        {
            composite.AddStateChange(movementPhase);
        }
        
        // Appearance phase
        var appearancePhase = new CompositeStateChange().WithName("Appearance Phase");
        for (var i = 0; i < copiesOfNextExpressionCharacters.Count; i++)
        {
            if (preservedToIndices.Contains(i)) continue;
            appearancePhase.AddStateChangeInParallel(copiesOfNextExpressionCharacters[i].ScaleTo(1));
        }
        if (appearancePhase.Duration > 0) // Only add if there are appearances
        {
            composite.AddStateChange(appearancePhase);
        }
        
        // Final swap to new expression
        var finalSwap = new CompositeStateChange().WithName("Final Swap");
        finalSwap.AddStateChange(new PropertyStateChange(_latexNodes[newIndex], "visible", true).WithDuration(0.001));
        finalSwap.AddStateChangeInParallel(new PropertyStateChange(intermediateNode, "visible", false).WithDuration(0.001));
        composite.AddStateChange(finalSwap);

        _currentExpressionIndex = newIndex;

        Scale = oldScale;
        
        return composite;
    }
}
