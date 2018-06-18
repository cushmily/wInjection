using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace wLib.Injection
{
    public class DiContainer
    {
        public readonly Dictionary<Type, Type> Types = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, List<MemberInfo>> _memberCaches = new Dictionary<Type, List<MemberInfo>>();

        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private static IEnumerable<Type> _modules;

        #region Helper

        public DiContainer()
        {
            if (_modules == null)
            {
                _modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                    .Where(x => typeof(IModule).IsAssignableFrom(x) && !x.IsAbstract);
            }

            foreach (var moduleType in _modules)
            {
                var construtor = moduleType.GetConstructor(new[] {typeof(DiContainer)});
                if (construtor != null)
                {
                    var moduleInstance = construtor.Invoke(new object[] {this}) as IModule;
                    if (moduleInstance != null) { moduleInstance.ModuleBindings(); }
                }
            }

            Register<DiContainer, DiContainer>(this);
        }

        public void Register<TContract, TImplementation>() where TImplementation : TContract
        {
            Types.Add(typeof(TContract), typeof(TImplementation));
        }

        public void Register<TContract, TImplementation>(TContract instance) where TImplementation : TContract
        {
            Types.Add(typeof(TContract), typeof(TImplementation));
            _singletons.Add(typeof(TContract), instance);
        }

        public T Resolve<T>()
        {
            var instance = InternalResolve<T>();
            Inject(instance);
            return instance;
        }

        public object Resolve(Type contract)
        {
            var instance = InternalResolve(contract);
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
                    switch (memberInfo.MemberType)
                    {
                        case MemberTypes.Field:
                            var fieldInfo = memberInfo as FieldInfo;
                            fieldInfo.SetValue(target, InternalResolve(fieldInfo.FieldType));
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
                                propertyInfo.SetValue(target, InternalResolve(propertyInfo.PropertyType));
                            }

                            break;
                    }

                    injectMembers.Add(memberInfo);
                }
            }
        }

        #endregion

        #region Internal

        private T InternalResolve<T>()
        {
            return (T) InternalResolve(typeof(T));
        }

        private object InternalResolve(Type contract)
        {
            Type implementation;
            if (!Types.TryGetValue(contract, out implementation))
            {
                throw new ApplicationException("Can't resolve a unregistered type: " + contract);
            }

            object instance;
            if (!_singletons.TryGetValue(contract, out instance))
            {
                var constructor = implementation.GetConstructors()[0];
                var parameterInfos = constructor.GetParameters();

                if (parameterInfos.Length == 0) { return Activator.CreateInstance(implementation); }

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