﻿using System;

namespace wLib.Injection
{
    public class BinderInfo : IBinderInfo
    {
        public DiContainer Container { get; }
        public Type TargetType { get; }

        public BinderInfo(DiContainer container, Type targetType)
        {
            Container = container;
            TargetType = targetType;
        }

        public IBinderInfo FromInstance(object instance)
        {
            Container.AddSingleton(TargetType, instance);
            return this;
        }

        public IBinderInfo FromMethod(Func<object> getter)
        {
            Container.AddSingleton(TargetType, getter.Invoke());
            return this;
        }

        public void NonLazy()
        {
            Container.Resolve(TargetType);
        }
    }

    public class BinderInfo<TContract> : BinderInfo, IBinderInfo<TContract>
    {
        public BinderInfo(DiContainer container, Type targetType) : base(container, targetType) { }

        public IBinderInfo<TContract> FromInstance(TContract instance)
        {
            Container.AddSingleton(TargetType, instance);
            return this;
        }

        public IBinderInfo<TContract> FromMethod(Func<TContract> getter)
        {
            Container.AddSingleton(TargetType, getter.Invoke());
            return this;
        }
    }
}