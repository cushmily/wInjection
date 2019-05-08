using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace wLib.Injection
{
    public class DependencyContainer : IDependencyContainer
    {
        public readonly Dictionary<Type, Type> Types = new Dictionary<Type, Type>();

        public readonly Dictionary<Type, List<MemberInfo>> _memberCaches = new Dictionary<Type, List<MemberInfo>>();

        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private readonly List<Type> _transientTypes = new List<Type>();

        private readonly List<IModule> _modules = new List<IModule>();

        #region Helper

        public DependencyContainer()
        {
            Bind<IDependencyContainer, DependencyContainer>(this);
        }

        #region Generic Binding

        public IBinderInfo<TImplementation> Bind<TImplementation>()
        {
            return Bind<TImplementation, TImplementation>();
        }

        public IBinderInfo<TImplementation> Bind<TImplementation>(TImplementation instance)
        {
            return Bind<TImplementation, TImplementation>(instance);
        }

        public IBinderInfo<TImplementation> Bind<TContract, TImplementation>() where TImplementation : TContract
        {
            var binder = new BinderInfo<TImplementation>(this, typeof(TContract));
            CheckBindingCache(typeof(TContract));
            Types.Add(typeof(TContract), typeof(TImplementation));

            return binder;
        }

        public IBinderInfo<TImplementation> Bind<TContract, TImplementation>(TContract instance)
            where TImplementation : TContract
        {
            var binder = new BinderInfo<TImplementation>(this, typeof(TContract));
            CheckBindingCache(typeof(TContract));
            Types.Add(typeof(TContract), typeof(TImplementation));
            AddSingleton(typeof(TContract), instance);

            return binder;
        }

        #endregion

        #region Non Generic

        public IBinderInfo Bind(Type contract)
        {
            var binder = new BinderInfo(this, contract);
            CheckBindingCache(contract);
            Types.Add(contract, contract);
            return binder;
        }

        public IBinderInfo Bind(Type contract, Type implementation)
        {
            var binder = new BinderInfo(this, contract);
            CheckBindingCache(contract);
            Types.Add(contract, implementation);
            return binder;
        }

        public IBinderInfo Bind(Type contract, Type implementation, object instance)
        {
            var binder = new BinderInfo(this, contract);
            CheckBindingCache(contract);
            Types.Add(contract, implementation);
            AddSingleton(contract, instance);
            return binder;
        }

        #endregion

        public T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }

        public object Resolve(Type contract, bool createMode = false)
        {
            var instance = InternalResolve(contract, createMode);
            Inject(instance);
            return instance;
        }

        public void Inject(object target)
        {
            var objType = target.GetType();
            List<MemberInfo> injectedMembers;

            if (!_memberCaches.TryGetValue(objType, out injectedMembers))
            {
                injectedMembers = new List<MemberInfo>();
                var members = objType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(Inject)));

                var memberInfos = members as MemberInfo[] ?? members.ToArray();
                injectedMembers.AddRange(memberInfos);

                _memberCaches.Add(objType, injectedMembers);
            }

            var memberInfoCaches = _memberCaches[objType];

            foreach (var memberInfo in memberInfoCaches)
            {
                object instance = null;
                switch (memberInfo.MemberType)
                {
                    case MemberTypes.Field:
                        var fieldInfo = memberInfo as FieldInfo;
                        instance = InternalResolve(fieldInfo.FieldType);
                        fieldInfo.SetValue(target, instance);
                        break;
                    case MemberTypes.Method:
                        var methodInfo = memberInfo as MethodInfo;
                        var parameters = methodInfo.GetParameters();
                        var invokeParameter = new object[parameters.Length];
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var parameter = parameters[i];
                            invokeParameter[i] = InternalResolve(parameter.ParameterType);
                        }

                        methodInfo.Invoke(target, invokeParameter);
                        break;
                    case MemberTypes.Property:
                        var propertyInfo = memberInfo as PropertyInfo;
                        if (propertyInfo.SetMethod != null)
                        {
                            instance = InternalResolve(propertyInfo.PropertyType);
                            propertyInfo.SetValue(target, instance);
                        }

                        break;
                }
            }
        }

        public IDependencyContainer MountModule(params IModule[] modules)
        {
            _modules.AddRange(modules);
            
            foreach (var module in modules) { module.RegisterBindings(); }

            return this;
        }

        #endregion

        #region Helpers

        public bool ContainsSingleton(Type type)
        {
            return _singletons.ContainsKey(type);
        }

        public void AddSingleton(Type type, object instance)
        {
            if (_singletons.ContainsKey(type))
            {
                throw new ApplicationException($"Singleton of type: {type} already registered.");
            }

            _singletons[type] = instance;
        }

        public void AddTransient(Type type)
        {
            if (_singletons.ContainsKey(type)) { _singletons.Remove(type); }

            if (!_transientTypes.Contains(type)) { _transientTypes.Add(type); }
        }

        #endregion

        #region Internal

        private void CheckBindingCache(Type boundType)
        {
            if (Types.ContainsKey(boundType)) { throw new ApplicationException($"{boundType} already bound."); }
        }

        private T InternalResolve<T>(bool createMode = false)
        {
            return (T) InternalResolve(typeof(T), createMode);
        }

        private object InternalResolve(Type contract, bool createMode = false)
        {
            if (!Types.TryGetValue(contract, out var implementation))
            {
                if (createMode) { implementation = contract; }
                else { throw new ApplicationException("Can't resolve a unregistered type: " + contract); }
            }

            if (_transientTypes.Contains(contract) || !_singletons.TryGetValue(contract, out var instance))
            {
                var constructor = implementation.GetConstructors()[0];
                var parameterInfos = constructor.GetParameters();

                if (parameterInfos.Length == 0)
                {
                    instance = Activator.CreateInstance(implementation);
                    _singletons.Add(contract, instance);
                    Inject(instance);
                    return instance;
                }

                var parameters = new List<object>(parameterInfos.Length);
                foreach (var parameterInfo in parameterInfos)
                {
                    parameters.Add(InternalResolve(parameterInfo.ParameterType));
                }

                instance = constructor.Invoke(parameters.ToArray());

                _singletons.Add(contract, instance);
            }

            return instance;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) { return; }

            Types.Clear();
            _memberCaches.Clear();
            _singletons.Clear();
            _transientTypes.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}