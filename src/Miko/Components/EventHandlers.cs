using Miko.Events;

namespace Miko.Components;

[EventHandler("onclick",      typeof(MouseEventArgs),    true, true)]
[EventHandler("onmouseenter", typeof(MouseEventArgs))]
[EventHandler("onmouseleave", typeof(MouseEventArgs))]
[EventHandler("onmousedown",  typeof(MouseEventArgs),    true, true)]
[EventHandler("onmouseup",    typeof(MouseEventArgs),    true, true)]
[EventHandler("onfocus",      typeof(FocusEventArgs))]
[EventHandler("onblur",       typeof(FocusEventArgs))]
[EventHandler("onchange",     typeof(ChangeEventArgs))]
[EventHandler("onscroll",     typeof(ScrollEventArgs))]
[EventHandler("onkeydown",    typeof(KeyboardEventArgs), true, true)]
[EventHandler("oninput",      typeof(InputEventArgs))]
public static class EventHandlers { }
