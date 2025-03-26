using Godot;
using System;
using System.Collections.Generic;
using PrimerTools;
using PrimerTools.LaTeX;

[Tool]
public partial class LatexAnimationTest : AnimationSequence
{
    [Export] private int index;
    protected override void Define()
    {
        var latexAnimator = new LatexAnimator();
        AddChild(latexAnimator);
        
        // latexAnimator.AddExpression("$f(x) = x \\cdot x$");
        // latexAnimator.AddExpression("$f(x) = x^2$");
        
        // latexAnimator.AddExpression("$f(x) = x^2$");
        // latexAnimator.AddExpression("$f(g(x)) = (g(x))^2$");

        var firstExpression = new LatexNode("$f(x) = x^2$");
        firstExpression.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Left;
        firstExpression.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Baseline;

        var secondExpression = new LatexNode("$f(g(x)) = (g(x))^2$");
        secondExpression.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Center;
        secondExpression.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Center;
        
        var thirdExpression = new LatexNode("$f(h(x)) = (h(x))^2$");
        thirdExpression.HorizontalAlignment = LatexNode.HorizontalAlignmentOptions.Right;
        thirdExpression.VerticalAlignment = LatexNode.VerticalAlignmentOptions.Top;
        
        latexAnimator.AddExpression(firstExpression);
        latexAnimator.AddExpression(secondExpression);
        latexAnimator.AddExpression(thirdExpression);
        
        var testAnimation = latexAnimator.AnimateToExpression(
            1,
            new List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)>()
            {
                // (0, 0, 6) // For the simple test
                (0, 0, 2),
                (3, 6, 2),
                (6, 14, 1)
            }
        );
        RegisterAnimation(testAnimation);
        
        var testAnimation2 = latexAnimator.AnimateToExpression(
            2,
            new List<(int currentExpressionChunkBeginIndex, int nextExpressionChunkBeginIndex, int chunkLength)>()
            {
                (0, 0, 2),
                (3, 3, 6),
                (10, 10, 5)
            }
        );
        RegisterAnimation(testAnimation2);
        
        // RegisterAnimation(latexAnimator.AnimateValue(1, LatexAnimator.PropertyName.CurrentExpressionIndex));
        
        this.MakeSelfAndChildrenLocal();
    }
}
