using Godot;
using PrimerTools.AnimationSequence;
using PrimerTools.Graph;

namespace ToolsTestingPlayground.TestScenes;

[Tool]
public partial class GraphTest : AnimationSequence
{
	protected override void Define()
	{
		#region MyRegion

		var graph = Graph.CreateAsOwnedChild(this);
		
		graph.Position = Vector3.Left;
		graph.XAxis.length = 5;
		graph.XAxis.showTicCylinders = false;
		graph.XAxis.showArrows = false;
		graph.XAxis.showRod = false;
		graph.YAxis.max = 100;
		graph.YAxis.ticStep = 20;
		graph.YAxis.length = 2;
		graph.YAxis.Visible = false;
		// graph.YAxis.showTicCylinders = false;
		// graph.YAxis.showArrows = false;
		graph.ZAxis.length = 0;
		graph.Transition();

		var barPlot = graph.AddBarPlot();
		barPlot.ShowValuesOnBars = true;
		barPlot.SetData(10, 20, 30);
		RegisterAnimation(graph.Transition());
		
		graph.XAxis.length = 4;
		barPlot.SetData(20, 30, 10);
		RegisterAnimation(graph.Transition());

		// Point data test
		// var data = new PointData();
		// graph.AddChild(data);
		// data.AddPoints(
		// 	new Vector3(4, 50, 0),
		// 	new Vector3(1, 80, 0)
		// );
		// data.SetData(
		// 	new Vector3(6, 90, 0),
		// 	new Vector3(3, 20, 0)
		// );
		// RegisterAnimation(data.Transition());
		
		graph.XAxis.length = 7;
		graph.YAxis.max = 120;
		graph.XAxis.max = 12;
		graph.ZAxis.length = 0;
		
		// var line = graph.AddLine();
		// line.SetInitialData(
		// 	new Vector3(0, 10, 0)
		// );
		
		// RegisterAnimation(graph.Transition(0.5f));
		// line.SetColor(PrimerColor.blue);
		// graph.XAxis.length = 7;
		// graph.YAxis.max = 80;
		// graph.XAxis.max = 12;
		// graph.ZAxis.length = 0;

		// line.SetData(
		// 	new Vector3(0, 70, 0),
		// 	new Vector3(1, 50, -10),
		// 	new Vector3(2, 0, -20),
		// 	new Vector3(3, 30, -30),
		// 	new Vector3(3, 100, -40),
		// 	new Vector3(2, 100, -50)
		// );
		// line.SetData(
		// 	new Vector3(0, 10, 0),
		// 	new Vector3(1, 50, 0)
		// 	// new Vector3(3, 50, 0)
		// );
		
		// RegisterAnimation(graph.Transition(duration: 5));
		
		// line.SetData(
		// 	new Vector3(0, 0, 0),
		// 	new Vector3(1, 50, 0),
		// 	new Vector3(3, 100, 0)
		// );
		//
		// RegisterAnimation(graph.Update());
		
		#endregion
	}
}