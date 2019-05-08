using System;

namespace wLib.Injection
{
    public interface IModule : IDisposable
    {
        IDependencyContainer Container { get; }

        void RegisterBindings();
    }
}