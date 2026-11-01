using System.ComponentModel.DataAnnotations;

namespace TimeForCode.Reporting.Api.V1
{
    /// <summary>
    /// Marks a route or query value as a 24-character hexadecimal object identifier; emitted as <c>pattern</c> in the OpenAPI document.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class ObjectIdAttribute : RegularExpressionAttribute
    {
        internal ObjectIdAttribute()
            : base(ApiConstraints.ObjectIdPattern)
        {
        }
    }
}