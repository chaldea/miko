using Miko.Components;
using Miko.Core;
using MikoApp1.Components;

namespace MikoApp1;

public class App : MikoComponent
{
    public override Element Build() => new ButtonExample().Build();
}
