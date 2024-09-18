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
        public abstract void ClearDeadEntities();
        #endregion

        #region Visual
        public abstract void VisualProcess(double delta);
        #endregion
    }
}
