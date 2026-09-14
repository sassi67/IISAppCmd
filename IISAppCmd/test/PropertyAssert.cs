using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace IISAppCmd.Tests
{
    internal static class PropertyAssert
    {
        public static void AllHavePublicGetterAndSetter(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Assert.That(properties, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                foreach (var property in properties)
                {
                    Assert.That(property.GetGetMethod(), Is.Not.Null, $"{type.Name}.{property.Name} has no public getter");
                    Assert.That(property.GetSetMethod(), Is.Not.Null, $"{type.Name}.{property.Name} has no public setter");
                }
            });
        }

        public static void AllReturnAssignedValue(Type type)
        {
            var instance = Activator.CreateInstance(type);

            Assert.Multiple(() =>
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var value = DifferentValue(property.PropertyType, property.GetValue(instance));
                    property.SetValue(instance, value);

                    var expected = property.PropertyType.IsValueType || property.PropertyType == typeof(string)
                        ? (IResolveConstraint)Is.EqualTo(value)
                        : Is.SameAs(value);
                    Assert.That(property.GetValue(instance), expected, $"{type.Name}.{property.Name}");
                }
            });
        }

        private static object DifferentValue(Type type, object current)
        {
            if (type == typeof(string)) return current + "-changed";
            if (type == typeof(bool)) return !(bool)current;
            if (type == typeof(uint)) return unchecked((uint)current + 1);
            if (type == typeof(int)) return (int)current + 1;
            if (type == typeof(long)) return (long)current + 1;
            if (type == typeof(TimeSpan)) return ((TimeSpan)current).Add(TimeSpan.FromMinutes(1));
            if (type.IsEnum) return Enum.GetValues(type).Cast<object>().First(v => !v.Equals(current));
            return Activator.CreateInstance(type);
        }
    }
}
