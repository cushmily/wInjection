using System;

namespace wLib.Injection
{
    public class DiContext : IContext
    {
        private readonly DiContainer _container = new DiContainer();

        public object Create(Type type)
        {
            return _container.Resolve(type, true);
        }

        public T Create<T>() where T : class
        {
            return Create(typeof(T)) as T;
        }

        public object Resolve(Type type)
        {
            return _container.Resolve(type);
        }

        public T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }
    }
}