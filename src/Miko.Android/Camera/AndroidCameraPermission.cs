using Android;
using Android.App;
using Android.Content.PM;

namespace Miko.Android.Camera;

/// <summary>
/// Asks for the <c>CAMERA</c> runtime permission without the app having to forward
/// <c>Activity.OnRequestPermissionsResult</c>: the request is made, then the outcome is read back with
/// <c>CheckSelfPermission</c>. A grant is seen as soon as it happens; a denial is concluded once the
/// permission dialog has come and gone (the Activity lost and regained window focus) with the
/// permission still missing, or when no dialog appears at all (the user chose "don't ask again", or a
/// policy forbids it).
/// </summary>
internal sealed class AndroidCameraPermission
{
    private const int RequestCode = 0x4DCA; // "M", camera
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(150);
    // No dialog within this time after the request means the system answered without asking.
    private static readonly TimeSpan DialogGrace = TimeSpan.FromSeconds(4);
    // A user who leaves the dialog up longer than this gets a denial; Restart() asks again.
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    public static bool IsGranted()
        => global::Android.App.Application.Context.CheckSelfPermission(Manifest.Permission.Camera) == Permission.Granted;

    /// <summary>Requests the permission and resolves to whether it was granted. Never throws.</summary>
    /// <param name="paused">
    /// Whether the session has been paused: the permission dialog is an Activity of its own, so the
    /// host Activity pauses while it shows — a sign that the dialog appeared, like losing focus.
    /// </param>
    public static async Task<bool> RequestAsync(Activity activity, Func<bool> paused, Func<bool> cancelled)
    {
        if (IsGranted()) return true;

        activity.RunOnUiThread(() =>
        {
            try { activity.RequestPermissions([Manifest.Permission.Camera], RequestCode); }
            catch (Exception) { }
        });

        var start = DateTime.UtcNow;
        // The dialog is an Activity: it pauses the host (the session sees Pause()) and takes the window
        // focus. Focus counts only once the host has been seen in front, since the window may not have
        // its focus yet when the camera opens during the first frames.
        bool front = false, dialogSeen = false;
        int settledPolls = 0;
        while (!cancelled())
        {
            await Task.Delay(PollInterval).ConfigureAwait(false);
            if (IsGranted()) return true;

            bool isPaused = paused();
            bool focused = activity.HasWindowFocus;
            if (isPaused || (front && !focused))
            {
                dialogSeen = true;
                settledPolls = 0;
                continue;
            }
            if (focused) front = true;

            var elapsed = DateTime.UtcNow - start;
            if (elapsed > Timeout) return false;
            if (dialogSeen)
            {
                // Back in front with no grant: dismissed (one more poll lets a late grant land).
                if (focused && ++settledPolls >= 2) return IsGranted();
                continue;
            }
            if (elapsed > DialogGrace) return IsGranted();
        }
        return false;
    }
}
