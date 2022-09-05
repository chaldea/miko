using Microsoft.JSInterop;

namespace Miko;

public class AnimationService
{
    private const string ModuleName = "Miko.interop";
    private readonly IJSRuntime _jsRuntime;

    public AnimationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IAnimation> Create()
    {
        var ani = await _jsRuntime.InvokeAsync<IJSObjectReference>($"{ModuleName}.createAnimation");
        return new Animation(ani);
    }
}