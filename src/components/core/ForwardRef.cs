using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Miko
{
    public class ForwardRef
    {
        private ElementReference _current;

        public ElementReference Current
        {
            get => _current;
            set => Set(value);
        }

        public void Set(ElementReference value)
        {
            _current = value;
        }
    }

    public class ElementRefList
    {
        public IList<ElementReference> Refs { get; } = new List<ElementReference>();

        public ElementReference this[int i]
        {
            get => Refs[i];
            set => Refs.Insert(i, value);
        }
    }
}