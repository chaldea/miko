using Miko.Components;
using Miko.Core;
using Miko.Examples.Bootstrap.Examples;

namespace MikoApp1;

public class App : MikoComponent
{
    public override Element Build() => ButtonExample.CreateDOM();
}
