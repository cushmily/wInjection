using System;

namespace wLib.Injection
{
    public interface IDependencyContainer : IDisposable
    {
        #region Generic Binding

        IBinderInfo<TImplementation> Bind<TImplementation>();

        IBinderInfo<TImplementation> Bind<TImplementation>(TImplementation instance);

        IBinderInfo<TImplementation> Bind<TContract, TImplementation>() where TImplementation : TContract;

        IBinderInfo<TImplementation> Bind<TContract, TImplementation>(TContract instance)
            where TImplementation : TContract;

        #endregion

        #region Non Generic

        IBinderInfo Bind(Type contract);

        IBinderInfo Bind(Type contract, Type implementation);

        IBinderInfo Bind(Type contract, Type implementation, object instance);

        #endregion

        #region Helpers

        bool ContainsSingleton(Type type);

        void AddSingleton(Type type, object instance);

        void AddTransient(Type type);

        #endregion

        T Resolve<T>() where T : class;

        object Resolve(Type contract, bool createMode = false);

        void Inject(object target);

        IDependencyContainer MountModule(params IModule[] modules);
    }
}