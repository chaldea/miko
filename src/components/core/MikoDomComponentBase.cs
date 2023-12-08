using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Miko
{
    public abstract class MikoDomComponentBase : MikoComponentBase
    {
        private ElementReference _ref;
        private string _class;
        private string _style;

        [Inject]
        private IOptions<MikoOptions> Options { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        [Parameter]
        public string Class
        {
            get => _class;
            set
            {
                _class = value;
                ClassMapper.OriginalClass = value;
            }
        }

        [Parameter]
        public string Style
        {
            get => _style;
            set
            {
                _style = value;
                StateHasChanged();
            }
        }

        public virtual ElementReference Ref
        {
            get => _ref;
            set
            {
                _ref = value;
                RefBack?.Set(value);
            }
        }

        public string Mode => Options == null ? "md" : Options.Value.Mode;

        protected ClassMapper ClassMapper { get; } = new();

        protected MikoDomComponentBase()
        {
            ClassMapper
                .Get(() => Class);
        }

        protected virtual string GenerateStyle()
        {
            return Style;
        }
    }
}
