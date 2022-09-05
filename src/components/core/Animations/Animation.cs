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
    }
}
