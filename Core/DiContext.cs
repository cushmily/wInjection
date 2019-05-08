using System;

namespace wLib.Injection
{
    public class DiContext : IContext
    {
        public IDependencyContainer Container { get; }

        public DiContext()
        {
            Container = new DependencyContainer();

            if (Context.GlobalContext == null) { Context.SetCurrentContext(this); }
        }

        public object Create(Type type)
        {
            return Container.Resolve(type, true);
        }

        public T Create<T>() where T : class
        {
            return Create(typeof(T)) as T;
        }

        public object Resolve(Type type)
        {
            return Container.Resolve(type);
        }

        public T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }
    }
}