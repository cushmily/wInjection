using System;

namespace wLib.Injection
{
    public interface IContext
    {
        IDependencyContainer Container { get; }
        
        object Create(Type type);

        T Create<T>() where T : class;

        object Resolve(Type type);

        T Resolve<T>() where T : class;
    }
}