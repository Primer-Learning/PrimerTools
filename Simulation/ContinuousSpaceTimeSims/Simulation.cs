using Godot;

namespace PrimerTools.Simulation
{
    public abstract partial class Simulation : Node3D
    {
        #region Editor controls
        private bool _running;
        [Export]
        public bool Running
        {
            get => _running;
            set
            {
                if (value && !Initialized)
                {
                    Initialize();
                }
                _running = value;
            }
        }
        #endregion

        #region Simulation
        protected SimulationWorld SimulationWorld => GetParent<SimulationWorld>();
        protected int StepsSoFar;
        protected bool Initialized;

        public abstract void Initialize();
        public abstract void Reset();
        public abstract void Step();
        #endregion

        #region Visual
        protected abstract void VisualProcess(double delta);
        public override void _Process(double delta)
        {
            if (!_running) return;
            // The point of having this separate is to put performance measurement stuff here.
            VisualProcess(delta);
        }
        #endregion
    }
}
