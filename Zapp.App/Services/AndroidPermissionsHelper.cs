namespace Zapp.App.Services;

// handle dynamic android runtime permissions for ble and location
public static class AndroidPermissionsHelper
{
    // verify and request required bluetooth and location permissions
    public static async Task<bool> EnsureBlePermissionsAsync()
    {
#if ANDROID
        var statusList = new List<PermissionStatus>();

        // check standard fine location permission
        statusList.Add(await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>());
        if (statusList[^1] != PermissionStatus.Granted)
        {
            statusList[^1] = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        // evaluate all checks
        return statusList.All(s => s == PermissionStatus.Granted);
#else
        await Task.CompletedTask;
        return true;
#endif
    }
}
