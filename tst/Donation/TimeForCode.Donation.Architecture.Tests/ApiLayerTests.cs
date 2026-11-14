using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace TimeForCode.Donation.Architecture.Tests
{
    [TestClass]
    public class ApiLayerTests
    {
        private static readonly Assembly ApiAssembly = typeof(TimeForCode.Donation.Api.Startup).Assembly;

        [TestMethod]
        public void ControllerActions_DeclaringAResponseType_UseOnlyTypesDefinedInTheApiProject()
        {
            var violations = ApiAssembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .SelectMany(m => m.GetCustomAttributes<ProducesResponseTypeAttribute>(), (m, a) => new { Action = $"{m.DeclaringType!.Name}.{m.Name}", a.Type })
                .SelectMany(x => Flatten(x.Type).Select(type => new { x.Action, Type = type }))
                .Where(x => IsOwnedElsewhere(x.Type))
                .Select(x => $"{x.Action} declares {x.Type.FullName}")
                .ToList();

            Assert.IsEmpty(violations, "Response types must be defined in the Donation API project:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
        }

        private static IEnumerable<Type> Flatten(Type type)
        {
            yield return type;
            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes)
            {
                foreach (var inner in Flatten(argument))
                {
                    yield return inner;
                }
            }
        }

        private static bool IsOwnedElsewhere(Type type)
        {
            var assembly = type.Assembly;
            var isFramework = assembly.GetName().Name is { } name && (name.StartsWith("System", StringComparison.Ordinal) || name.StartsWith("Microsoft", StringComparison.Ordinal));

            return !isFramework && assembly != ApiAssembly;
        }
    }
}