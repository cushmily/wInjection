using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace wLib.Injection
{
    public class DiContainer
    {
        public readonly Dictionary<Type, Type> Types = new Dictionary<Type, Type>();

        public readonly Dictionary<Type, List<MemberInfo>> _memberCaches = new Dictionary<Type, List<MemberInfo>>();

        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private static IEnumerable<Type> modules;

        #region Helper

        public DiContainer()
        {
            if (modules == null)
            {
                modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                    .Where(x => typeof(IModule).IsAssignableFrom(x) && !x.IsAbstract);
            }

            foreach (var moduleType in modules)
            {
                var construtor = moduleType.GetConstructor(new[] {typeof(DiContainer)});
                if (construtor != null)
                {
                    var moduleInstance = construtor.Invoke(new object[] {this}) as IModule;
                    if (moduleInstance != null) { moduleInstance.ModuleBindings(); }
                }
            }

            Bind<DiContainer, DiContainer>(this);
        }

        #region Generic Binding

        public IBinderInfo Bind<TImplementation>()
        {
            return Bind<TImplementation, TImplementation>();
        }

        public IBinderInfo Bind<TImplementation>(TImplementation instance)
        {
            return Bind<TImplementation, TImplementation>(instance);
        }

        public IBinderInfo Bind<TContract, TImplementation>() where TImplementation : TContract
        {
            var binder = new BinderInfo(this, typeof(TContract));
            CheckBindingCache(typeof(TContract));
            Types.Add(typeof(TContract), typeof(TImplementation));

            return binder;
        }

        public IBinderInfo Bind<TContract, TImplementation>(TContract instance)
            where TImplementation : TContract
        {
            var binder = new BinderInfo(this, typeof(TContract));
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
            List<MemberInfo> injectMembers;
            if (!_memberCaches.TryGetValue(objType, out injectMembers))
            {
                injectMembers = new List<MemberInfo>();
                var memebers = objType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.CustomAttributes.Any(a => a.AttributeType == typeof(Inject)));

                foreach (var memberInfo in memebers)
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

                    injectMembers.Add(memberInfo);
                }

                _memberCaches.Add(objType, injectMembers);
            }
        }

        #endregion

        #region Helpers

        public void AddSingleton(Type type, object instance)
        {
            if (_singletons.ContainsKey(type))
            {
                throw new ApplicationException(string.Format("Sigleton of type: {0} already registered.", type));
            }

            _singletons[type] = instance;
        }

        #endregion

        #region Internal

        private void CheckBindingCache(Type bindedType)
        {
            if (Types.ContainsKey(bindedType)) { throw new ApplicationException($"{bindedType} already binded."); }
        }

        private T InternalResolve<T>(bool createMode = false)
        {
            return (T) InternalResolve(typeof(T), createMode);
        }

        private object InternalResolve(Type contract, bool createMode = false)
        {
            Type implementation;
            if (!Types.TryGetValue(contract, out implementation))
            {
                if (createMode) { implementation = contract; }
                else { throw new ApplicationException("Can't resolve a unregistered type: " + contract); }
            }

            object instance;
            if (!_singletons.TryGetValue(contract, out instance))
            {
                var constructor = implementation.GetConstructors()[0];
                var parameterInfos = constructor.GetParameters();

                if (parameterInfos.Length == 0)
                {
                    instance = Activator.CreateInstance(implementation);
                    _singletons.Add(contract, instance);
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
    }
}