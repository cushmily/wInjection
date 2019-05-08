using System;

namespace wLib.Injection
{
    public interface IBinderInfo
    {
        IDependencyContainer Container { get; }

        Type TargetType { get; }

        IBinderInfo FromInstance(object instance);

        IBinderInfo FromMethod(Func<object> getter);

        void NonLazy();
        
        void AsSingleton();

        void AsTransient();
    }

    public interface IBinderInfo<in TContract> : IBinderInfo
    {
        IBinderInfo<TContract> FromInstance(TContract instance);

        IBinderInfo<TContract> FromMethod(Func<TContract> getter);
    }
}