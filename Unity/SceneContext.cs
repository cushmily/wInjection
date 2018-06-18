using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace wLib.Injection
{
    public class SceneContext : MonoBehaviour, IContext
    {
        private readonly DiContainer _container = new DiContainer();

        [SerializeField]
        private MonoModule[] _modules;

        private void Awake()
        {
            if (_modules != null)
            {
                for (var i = 0; i < _modules.Length; i++)
                {
                    var module = _modules[i];
                    module.SetContainer(_container);
                }
            }

            var sw = new Stopwatch();
            sw.Start();
            var monos = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (var i = 0; i < monos.Length; i++)
            {
                var mono = monos[i];
                _container.Inject(mono);
            }

            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            Debug.LogFormat("Inject scene game object finised. cost : {0} ms. ", ms);
        }

        public T Create<T>()
        {
            return _container.Resolve<T>();
        }
    }
}