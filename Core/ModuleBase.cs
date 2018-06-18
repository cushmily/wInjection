namespace wLib.Injection
{
    public abstract class ModuleBase : IModule
    {
        public DiContainer Container { get; }

        public ModuleBase(DiContainer container)
        {
            Container = container;
        }

        public abstract void ModuleBindings();
    }
}