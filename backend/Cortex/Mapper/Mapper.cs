using System.Collections;
using System.Reflection;

namespace StockApp2._0.Mapper
{
    public static class Mapper
    {
        static Dictionary<(Type from, Type to), List<(MethodInfo Get, MethodInfo Set)>> _cache = new();

        public static T Map<T>(object source) where T : class, new()
        {
            var sourceType = source.GetType();
            var targetType = typeof(T);
            var key = (from: sourceType, to: targetType);

            if (!_cache.ContainsKey(key))
                PopulateCacheKey(key);

            var result = new T();
            var entries = _cache[key];
            foreach (var (Get, Set) in entries)
            {
                var value = Get.Invoke(source, null);
                var returnType = Get.ReturnType;

                if (value != null)
                {
                    if (typeof(IEnumerable).IsAssignableFrom(returnType) && returnType != typeof(string))
                    {
                        var targetPropertyType = Set.GetParameters()[0].ParameterType;
                        var elementType = targetPropertyType.IsGenericType
                            ? targetPropertyType.GetGenericArguments()[0]
                            : targetPropertyType.GetElementType();

                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var listInstance = Activator.CreateInstance(listType);

                        var addMethod = listType.GetMethod("Add");
                        var mapMethod = typeof(Mapper).GetMethod(nameof(Map)).MakeGenericMethod(elementType);

                        foreach (var item in (IEnumerable)value)
                        {
                            var mappedItem = mapMethod.Invoke(null, [item]);
                            addMethod.Invoke(listInstance, [mappedItem]);
                        }

                        Set.Invoke(result, [listInstance]);

                    }
                    else if (returnType.IsClass && !IsPrimitiveOrString(returnType))
                    {
                        var targetPropertyType = Set.GetParameters()[0].ParameterType;
                        var mapMethod = typeof(Mapper).GetMethod(nameof(Map))!.MakeGenericMethod(targetPropertyType);
                        var targetValue = mapMethod.Invoke(null, [value]);
                        Set.Invoke(result, [targetValue]);
                    }
                    else
                        Set.Invoke(result, [value]);
                }
            }

            return result;
        }

        public static void PopulateCacheKey((Type from, Type to) key)
        {
            var fromProps = key.from.GetProperties();
            var toProps = key.to.GetProperties();

            List<(MethodInfo, MethodInfo)> entry = [];

            foreach (var from in fromProps)
            {
                var to = toProps.FirstOrDefault(x => x.Name == from.Name);
                if (to == null)
                    continue;

                if (from.GetMethod != null && to.SetMethod != null)
                    entry.Add((from.GetMethod, to.SetMethod));
            }

            _cache[key] = entry;
        }

        private static bool IsPrimitiveOrString(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType;
        }
    }
}