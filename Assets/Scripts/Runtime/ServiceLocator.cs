using System;
using System.Collections.Generic;

namespace ZomboZ.Runtime
{
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static void Register<T>(T instance)
        {
            services[typeof(T)] = instance;
        }

        public static bool TryResolve<T>(out T instance)
        {
            if (services.TryGetValue(typeof(T), out var obj) && obj is T cast)
            {
                instance = cast;
                return true;
            }
            instance = default;
            return false;
        }

        public static T Resolve<T>()
        {
            if (TryResolve<T>(out var inst)) return inst;
            throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered.");
        }
    }
}
