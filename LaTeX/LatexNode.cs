using System;
using System.ComponentModel;
using Godot;
using System.IO;
using System.Reflection;

namespace PrimerTools.LaTeX;
[Tool]
public partial class LatexNode : Node3D
{
	private bool run = true;
	[Export]
	public bool Run {
		get => run;
		set {
			var oldRun = run;
			run = value;
			if (run && !oldRun) { // Avoids running on build
				RunTest();
			}
		}
	}

	[Export] public bool openBlender = false;
	[Export] public string _latex = "$z^2 + y^2 = 1$";
	
	private readonly LatexToSvg latexToSvg = new();
	
	public override void _Ready() {
	}
	
	private async void RunTest() {
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		var path = await latexToSvg.MeshFromExpression(_latex, openBlender);
		var newNode = ResourceLoader.Load<PackedScene>(path).Instantiate();
		
		AddChild(newNode);
		newNode.Owner = GetTree().EditedSceneRoot;
	}
}
