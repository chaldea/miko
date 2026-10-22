# Miko.Ionic

Ionic Framework components for [Miko](https://github.com/chaldea/miko), the native .NET UI engine
that renders Razor components with SkiaSharp: pages, toolbars, lists, forms, tabs, menus,
overlays, pickers and more, with Material Design (`md`) and iOS (`ios`) modes chosen from the
platform. Every Miko app template uses it.

```csharp
var builder = MikoAppBuilder.CreateDefault();
builder.AddIonic();
```

```razor
@using Miko.Ionic.Components

<IonPage>
    <IonHeader>
        <IonToolbar><IonTitle>Hello</IonTitle></IonToolbar>
    </IonHeader>
    <IonContent Class="ion-padding">
        <IonButton OnClick="@(() => _count++)">Clicked @_count times</IonButton>
    </IonContent>
</IonPage>

@code {
    private int _count;
}
```

Each component's parameters, events and slots are listed in the
[component reference](https://chaldea.github.io/miko/components/ionic/). Includes the
[Ionicons](https://ionic.io/ionicons) icon set; see `THIRD-PARTY-NOTICES.txt` in the package.
