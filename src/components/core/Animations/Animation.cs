using Microsoft.JSInterop;

namespace Miko
{
    internal class Animation: IAnimation
    {
        public IJSObjectReference AnimationRef { get; }

        public Animation(IJSObjectReference animationRef)
        {
            AnimationRef = animationRef;
        }

        public async Task Play()
        {
            await AnimationRef.InvokeVoidAsync("play");
        }

        public async Task Pause()
        {
            await AnimationRef.InvokeVoidAsync("pause");
        }

        public async Task Stop()
        {
            await AnimationRef.InvokeVoidAsync("stop");
        }

        public async Task Destroy(bool? clearStyleSheets)
        {
            await AnimationRef.InvokeVoidAsync("destroy", clearStyleSheets);
        }
    }
}
