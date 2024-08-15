using Godot;
using System;

[Tool]
public partial class SelfBuildingAnimationTree : AnimationTree
{
	private bool _run = false;
	[Export] private bool RunButton {
		get => _run;
		set {
			if (!value && _run && Engine.IsEditorHint())
			{
				Build();
			}
			_run = true;
		}
	}

	protected virtual void Build()
	{
	}
}
