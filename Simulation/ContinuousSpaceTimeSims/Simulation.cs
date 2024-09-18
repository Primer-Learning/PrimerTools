using System;
using System.Diagnostics;
using Godot;
using PrimerTools.Simulation;

namespace PrimerTools.Simulation
{
    public abstract partial class Simulation : Node3D, ISimulation
    {
        #region Editor controls
        protected bool _running;
        public bool Running
        {
            get => _running;
            set
            {
                if (value && !_initialized)
                {
                    Initialize();
                }
                _running = value;
            }
        }
        #endregion

        #region Simulation
        protected SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
        protected int _stepsSoFar;
        protected bool _initialized;

        public abstract void Initialize();
        public abstract void Reset();
        public abstract void Step();

        public override void _Process(double delta)
        {
            if (!_running) return;

            if (SimulationWorld.PerformanceTest)
            {
                _processStopwatch.Restart();
            }

            // ProcessSimulation(delta);

            if (SimulationWorld.PerformanceTest)
            {
                _processStopwatch.Stop();
                _totalProcessTime += _processStopwatch.Elapsed.TotalMilliseconds;
                _processCount++;
            }
        }

        // protected abstract void ProcessSimulation(double delta);
        #endregion

        #region Performance testing 
        protected Stopwatch _stepStopwatch = new Stopwatch();
        protected Stopwatch _processStopwatch = new Stopwatch();
        protected double _totalStepTime;
        protected double _totalProcessTime;
        protected int _stepCount;
        protected int _processCount;

        public virtual void PrintPerformanceStats()
        {
            if (_stepCount > 0 && _processCount > 0)
            {
                GD.Print($"{GetType().Name} Performance Stats:");
                GD.Print($"  Average Step Time: {_totalStepTime / _stepCount:F3} ms");
                GD.Print($"  Average Process Time: {_totalProcessTime / _processCount:F3} ms");
            }
        }
        #endregion
    }
}
