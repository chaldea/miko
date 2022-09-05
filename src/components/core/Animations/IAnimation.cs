using Microsoft.JSInterop;

namespace Miko;

public interface IAnimation
{
    IJSObjectReference AnimationRef { get; }
    Task Play();
}