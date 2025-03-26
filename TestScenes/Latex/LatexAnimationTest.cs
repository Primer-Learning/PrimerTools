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
        
        latexAnimator.AddExpression("$f(x) = x^2$");
        latexAnimator.AddExpression("$f(g(x)) = (g(x))^2$");
        
        // // var latex = LatexNode.Create("$f(x) = x \\cdot x$");
        // var latex = new LatexNode("f(x) = x $\\cdot$ x");
        // AddChild(latex);
        //
        // var latex2 = new LatexNode("$f(x) = x^2$");
        // latex2.Position = Vector3.Down * 2;
        // AddChild(latex2);

        
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
        
        // RegisterAnimation(latexAnimator.AnimateValue(1, LatexAnimator.PropertyName.CurrentExpressionIndex));
        
        this.MakeSelfAndChildrenLocal();
    }
}
