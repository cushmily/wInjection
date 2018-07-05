using System;

namespace wLib.Injection
{
    public interface IContext
    {
        object Create(Type type);

        T Create<T>() where T : class;
        
        object Resolve(Type type);

        T Resolve<T>() where T : class;

        //TODO Add hirerachy contexts
//        IContext Parent { get; }
    }
}