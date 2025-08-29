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
    public int CurrentExpressionIndex => _currentExpressionIndex;

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

        // Use GetCharacterContainers instead of GetCharacters for proper movement
        var copiesOfCurrentExpressionCharacters = new List<Node3D>();
        foreach (var container in _latexNodes[_currentExpressionIndex].GetCharacterContainers())
        {
            var copy = (Node3D)container.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = container.GlobalPosition;
            copiesOfCurrentExpressionCharacters.Add(copy);
        }
        var copiesOfNextExpressionCharacters = new List<Node3D>();
        foreach (var container in _latexNodes[newIndex].GetCharacterContainers())
        {
            var copy = (Node3D)container.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = container.GlobalPosition;
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
    /// Creates the given expression and animates to it
    /// </summary>
    /// <param name="latex">The expression to make a LatexNode from</param>
    /// <param name="preservedCharacterChunks">Specifies the characters that should be kept during the transition</param>
    /// <param name="anchorChunkIndex">If preservedCharacterChunks are defined, setting this to an index of one of them will
    ///  cause that chunk to act as an anchor for the animation, keeping the chunk in place and moving everything else around it. </param>
    /// <returns></returns>
    public CompositeStateChange AnimateToExpressionStateChange(
        string latex,
        List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)> preservedCharacterChunks,
        int anchorChunkIndex = -1
        )
    {
        var nextLatex = LatexNode.Create(latex);
        // Copy alignment of current expression. If you want different alignment, don't use this overload.
        nextLatex.HorizontalAlignment = _latexNodes[_currentExpressionIndex].HorizontalAlignment;
        nextLatex.VerticalAlignment = _latexNodes[_currentExpressionIndex].VerticalAlignment;
        AddExpression(nextLatex);
        return AnimateToExpressionStateChange(_currentExpressionIndex + 1, preservedCharacterChunks, anchorChunkIndex);
    }

    /// <summary>
    /// Animates to the next expression index
    /// </summary>
    /// <param name="preservedCharacterChunks">Specifies the characters that should be kept during the transition</param>
    /// <param name="anchorChunkIndex">If preservedCharacterChunks are defined, setting this to an index of one of them will
    ///  cause that chunk to act as an anchor for the animation, keeping the chunk in place and moving everything else around it. </param>
    /// <returns></returns>
    public CompositeStateChange AnimateToExpressionStateChange(
        List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)> preservedCharacterChunks,
        int anchorChunkIndex = -1
        )
    {
        return AnimateToExpressionStateChange(_currentExpressionIndex + 1, preservedCharacterChunks, anchorChunkIndex);
    }

    /// <summary>
    /// Animates to a different expression
    /// </summary>
    /// <param name="newIndex">The index of the new expression</param>
    /// <param name="preservedCharacterChunks">Specifies the characters that should be kept during the transition</param>
    /// <param name="anchorChunkIndex">If preservedCharacterChunks are defined, setting this to an index of one of them will
    ///  cause that chunk to act as an anchor for the animation, keeping the chunk in place and moving everything else around it. </param>
    /// <returns></returns>
    public CompositeStateChange AnimateToExpressionStateChange(
        int newIndex, 
        List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)> preservedCharacterChunks,
        int anchorChunkIndex = -1
        )
    {
        // Check if we have containers instead of characters
        if (_latexNodes[_currentExpressionIndex].GetCharacterContainers().Count == 0 
            || _latexNodes[newIndex].GetCharacterContainers().Count == 0)
        {
            GD.Print("One or more LaTeX expressions has no characters. Skipping LaTeX animation.");
            return new CompositeStateChange();
        }

        if (anchorChunkIndex >= 0 && anchorChunkIndex < preservedCharacterChunks.Count)
        {
            // The below won't work with non-unit scales, so some not-very-robust warning
            if (Math.Abs(_latexNodes[_currentExpressionIndex].Scale.X - 1) > 0.001)
            {
                GD.Print("Anchored LaTeX animations likely won't work properly when the LatexNodes have non-unit scales. You could scale the LatexAnimator itself, or make LatexAnimator no longer assume unit scales.");
            }
            
            // Find the first character container in the anchor chunk
            var anchorCharacterIndex = preservedCharacterChunks[anchorChunkIndex].currentExpressionChunkBeginIndex;
            var anchorContainer = _latexNodes[_currentExpressionIndex].GetCharacterContainers()[anchorCharacterIndex];
            // Find the corresponding character container in destination
            var anchoredCharacterIndex = preservedCharacterChunks[anchorChunkIndex].nextExpressionChunkBeginIndex;
            var anchoredContainer = _latexNodes[newIndex].GetCharacterContainers()[anchoredCharacterIndex];

            // Move every destination character container's position by the difference between the anchor and anchored positions
            // But since these are each in container that are used for alignment, we need to apply the container's
            // position to get the position in the grandparent LatexNode space.
            // This would disrupt alignment calculations, but I don't expect that will matter if we're using anchors
            // in a LatexAnimator.
            // If it ends up mattering, it might be possible to move the containers and then move the characters
            // so they align individually.
            var anchorCharacterTransformRelativeToOutNode =
                _latexNodes[_currentExpressionIndex].GetChild<Node3D>(0).Transform * anchorContainer.Position; 
            var anchoredCharacterTransformRelativeToOutNode =
                _latexNodes[newIndex].GetChild<Node3D>(0).Transform * anchoredContainer.Position;
            
            var displacement = anchorCharacterTransformRelativeToOutNode - anchoredCharacterTransformRelativeToOutNode;
            
            foreach (var container in _latexNodes[newIndex].GetCharacterContainers())
            {
                container.Position += displacement;
            }
        }
        
        // This doesn't work if the scale is zero, which it often is at the beginning of a scene
        var oldScale = Scale;
        Scale = Vector3.One;
        
        if (newIndex == _currentExpressionIndex)
        {
            GD.PushError($"Trying to animate to the current expression index: {newIndex}. Returning empty state change.");
            return new CompositeStateChange().WithName("Empty Expression Change");
        }
        
        // Turn the preservedCharacterChunks into lists of indices, making later steps easier
        var preservedFromIndices = new List<int>();
        var preservedToIndices = new List<int>();
        foreach (var chunk in preservedCharacterChunks)
        {
            for (var i = 0; i < chunk.chunkLength; i++)
            {
                preservedFromIndices.Add(chunk.currentExpressionChunkBeginIndex + i);
                preservedToIndices.Add(chunk.nextExpressionChunkBeginIndex + i);
            }
        }
        
        // Create a combined node with all the character containers from both LatexNodes, with the next expression scale zero.
        var intermediateNode = new Node3D();
        intermediateNode.Visible = false;
        AddChild(intermediateNode);
        intermediateNode.Name = "Intermediate ";
        
        // Use GetCharacterContainers for proper movement
        var copiesOfCurrentExpressionCharacters = new List<Node3D>();
        foreach (var container in _latexNodes[_currentExpressionIndex].GetCharacterContainers())
        {
            var copy = (Node3D)container.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = container.GlobalPosition;
            copiesOfCurrentExpressionCharacters.Add(copy);
        }
        var copiesOfNextExpressionCharacters = new List<Node3D>();
        foreach (var container in _latexNodes[newIndex].GetCharacterContainers())
        {
            var copy = (Node3D)container.Duplicate();
            intermediateNode.AddChild(copy);
            copy.GlobalPosition = container.GlobalPosition;
            copy.Scale = Vector3.Zero;
            copiesOfNextExpressionCharacters.Add(copy);
        }
        
        var composite = new CompositeStateChange().WithName($"Latex Expression {_currentExpressionIndex} to {newIndex}");
        
        // Disappear phase
        var disappearPhase = new CompositeStateChange().WithName("Disappear Phase");
        disappearPhase.AddStateChangeWithDelay(new PropertyStateChange(_latexNodes[_currentExpressionIndex], "visible", false).WithDuration(0.001));
        disappearPhase.AddStateChangeInParallel(new PropertyStateChange(intermediateNode, "visible", true).WithDuration(0.001));
        
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (preservedFromIndices.Contains(i)) continue;
            disappearPhase.AddStateChangeInParallel(copiesOfCurrentExpressionCharacters[i].ScaleTo(0));
        }
        composite.AddStateChangeWithDelay(disappearPhase);
        
        // Movement phase
        var movementPhase = new CompositeStateChange().WithName("Movement Phase");
        for (var i = 0; i < copiesOfCurrentExpressionCharacters.Count; i++)
        {
            if (!preservedFromIndices.Contains(i)) continue;
            var movementIndex = preservedFromIndices.IndexOf(i);
            var indexInNextExpression = preservedToIndices[movementIndex];

            if (copiesOfNextExpressionCharacters.Count <= indexInNextExpression)
            {
                GD.PushWarning("Ran out of next expression character copies.");
                continue;
            }
            
            var diff = copiesOfCurrentExpressionCharacters[i].Position -
                       copiesOfNextExpressionCharacters[indexInNextExpression].Position;
            if (diff.Length() > 0.001)
            {
                movementPhase.AddStateChangeInParallel(copiesOfCurrentExpressionCharacters[i].MoveTo(copiesOfNextExpressionCharacters[indexInNextExpression].Position));
            }
        }
        if (movementPhase.Duration > 0) // Only add if there are movements
        {
            composite.AddStateChangeWithDelay(movementPhase);
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
            composite.AddStateChangeWithDelay(appearancePhase);
        }
        
        // Final swap to new expression
        var finalSwap = new CompositeStateChange().WithName("Final Swap");
        finalSwap.AddStateChangeWithDelay(new PropertyStateChange(_latexNodes[newIndex], "visible", true).WithDuration(0.001));
        finalSwap.AddStateChangeInParallel(new PropertyStateChange(intermediateNode, "visible", false).WithDuration(0.001));
        composite.AddStateChangeWithDelay(finalSwap);

        _currentExpressionIndex = newIndex;

        Scale = oldScale;
        
        return composite;
    }
}
