using Microsoft.JSInterop;

namespace Miko;

public interface IAnimation
{
    IJSObjectReference AnimationRef { get; }
    Task Play();
    Task Pause();
    Task Stop();
    Task Destroy(bool? clearStyleSheets);
}