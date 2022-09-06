using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Miko;

public static class AnimationExtensions
{
    public static async Task<IAnimation> AddElement(this Task<IAnimation> animation, ElementReference el)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("addElement", el);
        return ani;
    }

    public static async Task<IAnimation> Duration(this Task<IAnimation> animation, int duration)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("duration", duration);
        return ani;
    }

    public static async Task<IAnimation> From(this Task<IAnimation> animation, string property, object value)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("from", property, value);
        return ani;
    }

    public static async Task<IAnimation> To(this Task<IAnimation> animation, string property, object value)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("to", property, value);
        return ani;
    }

    public static async Task<IAnimation> FromTo(this Task<IAnimation> animation, string property, object fromValue, object toValue)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("fromTo", property, fromValue, toValue);
        return ani;
    }

    public static async Task<IAnimation> Iterations(this Task<IAnimation> animation, int iterations)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("iterations", iterations);
        return ani;
    }

    public static async Task<IAnimation> Keyframes(this Task<IAnimation> animation, object[] keyframes)
    {
        var ani = await animation.ConfigureAwait(false);
        await ani.AnimationRef.InvokeVoidAsync("keyframes", new object[] { keyframes });
        return ani;
    }
}