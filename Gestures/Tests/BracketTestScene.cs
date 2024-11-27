using Godot;
using PrimerTools;

[Tool]
public partial class BracketTestScene : AnimationSequence
{

	private void CallTweenTransition(Bracket bracket, double duration)
	{
		bracket.TweenTransition(duration);
	}
	
	protected override void Define()
	{
		var bracket = Bracket.CreateInstance();
		AddChild(bracket);

		bracket.LeftTipPosition = new Vector3(-7, 0, 1);
		bracket.RightTipPosition = new Vector3(7, 0, 3);
		RegisterAnimation(bracket.Transition());
		
		bracket.LeftTipPosition = new Vector3(-7, 5, 1);
		bracket.RightTipPosition = new Vector3(7, -5, 3);
		RegisterAnimation(bracket.Transition());
		
		bracket.LeftTipPosition = new Vector3(7, 5, 1);
		bracket.RightTipPosition = new Vector3(-7, -5, 3);
		RegisterAnimation(bracket.Transition());
		
		bracket.LeftTipPosition = new Vector3(7, 5, -1);
		bracket.RightTipPosition = new Vector3(-7, -5, -3);
		RegisterAnimation(bracket.Transition());
		
		bracket.LeftTipPosition = new Vector3(1, 0, 1);
		bracket.RightTipPosition = new Vector3(-1, 0, 1);
		RegisterAnimation(this.MethodCall("CallTweenTransition", new Godot.Collections.Array() {bracket, 2}));
	}
}
