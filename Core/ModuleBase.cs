namespace wLib.Injection
{
    public abstract class ModuleBase : IModule
    {
        public IDependencyContainer Container { get; }

        protected ModuleBase(IDependencyContainer container)
        {
            Container = container;
        }

        public abstract void RegisterBindings();

        public virtual void Dispose()
        {
            Container.Dispose();
        }
    }
}