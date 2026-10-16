using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    internal sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements() => _elements.ToList();

        public void StoreElement(XElement element, string friendlyName) => _elements.Add(element);
    }
}