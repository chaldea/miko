using Microsoft.AspNetCore.Components;

namespace Miko
{
    public abstract class MikoDomComponentBase : MikoComponentBase
    {
        // [Inject]
        // private IComponentIdGenerator ComponentIdGenerator { get; set; }

        [Parameter]
        public string Id { get; set; }

        //[Parameter(CaptureUnmatchedValues = true)]
        //public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        private ElementReference _ref;

        /// <summary>
        /// Returned ElementRef reference for DOM element.
        /// </summary>
        public virtual ElementReference Ref
        {
            get => _ref;
            set
            {
                _ref = value;
                RefBack?.Set(value);
            }
        }

        protected ClassMapper ClassMapper { get; } = new ClassMapper();

        protected MikoDomComponentBase()
        {
            ClassMapper
                .Get(() => this.Class);
        }

        protected override void OnInitialized()
        {
            // Id ??= ComponentIdGenerator.Generate(this);
            base.OnInitialized();
        }

        /// <summary>
        /// Specifies one or more class names for an DOM element.
        /// </summary>
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

        /// <summary>
        /// Specifies an inline style for an DOM element.
        /// </summary>
        [Parameter]
        public string Style
        {
            get => _style;
            set
            {
                _style = value;
                this.StateHasChanged();
            }
        }

        public string Mode { get; set; } = "md"; // md or ios

        protected virtual string GenerateStyle()
        {
            return Style;
        }

        private string _class;
        private string _style;
    }
}
