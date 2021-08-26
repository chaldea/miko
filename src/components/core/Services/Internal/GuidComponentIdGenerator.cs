using System;

namespace Miko
{
    internal class GuidComponentIdGenerator : IComponentIdGenerator
    {
        public string Generate(MikoDomComponentBase component) => "miko-" + Guid.NewGuid();
    }
}
