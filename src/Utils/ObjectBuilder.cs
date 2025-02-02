using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Herta.Utils.ObjectBuilder
{
    public static class ObjectBuilder
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new();

        public static T Build<T>(Dictionary<string, object> properties) where T : new()
        {
            T obj = new T();
            Type type = typeof(T);

            if (!_propertyCache.TryGetValue(type, out var propertyInfos))
            {
                propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, p => p);
                _propertyCache[type] = propertyInfos;
            }

            foreach (var property in properties)
            {
                if (propertyInfos.TryGetValue(property.Key, out var propInfo))
                {
                    object? value = property.Value;

                    // Handle nested objects
                    if (value is Dictionary<string, object> nestedProperties)
                    {
                        value = BuildNestedObject(propInfo.PropertyType, nestedProperties);
                    }
                    // Handle lists and arrays
                    else if (value is IList list)
                    {
                        value = BuildListOrArray(propInfo.PropertyType, list);
                    }
                    else
                    {
                        value = ConvertValue(value, propInfo.PropertyType);
                    }

                    propInfo.SetValue(obj, value);
                }
                else
                {
                    throw new ArgumentException($"Property '{property.Key}' not found in type '{type.Name}'");
                }
            }

            return obj;
        }

        public static T BuildFromJson<T>(string json) where T : new()
        {
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return Build<T>(properties!);
        }

        public static bool TryBuild<T>(Dictionary<string, object> properties, out T obj) where T : new()
        {
            obj = new T();
            try
            {
                obj = Build<T>(properties);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryBuildFromJson<T>(string json, out T obj) where T : new()
        {
            obj = new T();
            try
            {
                var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return TryBuild(properties!, out obj);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static object? BuildNestedObject(Type type, Dictionary<string, object> properties)
        {
            MethodInfo? method = typeof(ObjectBuilder).GetMethod(nameof(Build), BindingFlags.Public | BindingFlags.Static);
            MethodInfo? genericMethod = method!.MakeGenericMethod(type) ?? null;
            return genericMethod?.Invoke(null, new object[] { properties }) ?? Activator.CreateInstance(type);
        }

        private static object BuildListOrArray(Type type, IList list)
        {
            Type? elementType = type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0];
            var GenericListType = typeof(List<>).MakeGenericType(elementType!);
#nullable disable
            var convertedList = (IList)Activator.CreateInstance(GenericListType);
#nullable enable

            foreach (var item in list)
            {
                if (item is Dictionary<string, object> nestedProperties)
                {
                    convertedList!.Add(BuildNestedObject(elementType!, nestedProperties));
                }
                else
                {
                    convertedList!.Add(ConvertValue(item, elementType!));
                }
            }

            if (type.IsArray)
            {
                var array = Array.CreateInstance(elementType!, convertedList!.Count);
                convertedList.CopyTo(array, 0);
                return array;
            }

            return convertedList!;
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
            {
                if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    return null;
                }

                throw new InvalidCastException($"Cannot cast null to {targetType}");
            }

            if (Nullable.GetUnderlyingType(targetType) != null)
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? typeof(object); // 默认为object
            }

            return Convert.ChangeType(value, targetType);
        }
    }
}
