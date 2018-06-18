namespace wLib.Injection
{
    public interface IModule
    {
        DiContainer Container { get; }
        
        void ModuleBindings();
    }
}