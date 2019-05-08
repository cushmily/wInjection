using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace wLib.Injection
{
    public class SceneContext : MonoBehaviour, IContext
    {
        public IDependencyContainer Container { get; } = new DependencyContainer();

        [SerializeField]
        private MonoModule[] _modules;

        protected virtual void Awake()
        {
            if (_modules != null)
            {
                for (var i = 0; i < _modules.Length; i++)
                {
                    var module = _modules[i];
                    if (module == null) { continue; }

                    module.SetContainer(Container);
                    module.RegisterBindings();
                }
            }

            var sw = new Stopwatch();
            sw.Start();

            foreach (var go in (GameObject[]) Resources.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                if (string.IsNullOrEmpty(go.scene.name) || go.hideFlags == HideFlags.NotEditable ||
                    go.hideFlags == HideFlags.HideAndDontSave) { continue; }

                InjectGameObject(go);
            }

            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            Debug.LogFormat("Inject scene game object finished. cost : {0} ms. ", ms);

            if (Context.GlobalContext == null) { Context.SetCurrentContext(this); }
        }

        public void InjectGameObject(GameObject targetGo)
        {
            var monos = targetGo.GetComponents<MonoBehaviour>();
            for (var i = 0; i < monos.Length; i++)
            {
                var mono = monos[i];
                if (mono == null) { continue; }

                Inject(mono);
            }
        }

        public void Inject(object obj)
        {
            Container.Inject(obj);
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