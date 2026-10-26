using AppointmentBooking.Mobile.Models.Profile.Responses;
using AppointmentBooking.Mobile.Models.Salons.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.Session;

namespace AppointmentBooking.Mobile.Services.ProfileData;

public sealed class ProfileDataCache : IDisposable
{
    private readonly IApiClient apiClient;
    private readonly ISessionService sessionService;
    private readonly SemaphoreSlim loadLock = new(1, 1);

    private bool profileLoaded;
    private bool salonLoaded;
    private int sessionGeneration;

    public MyProfileResponse? Profile { get; private set; }
    public SalonResponse? Salon { get; private set; }

    public ProfileDataCache(IApiClient apiClient, ISessionService sessionService)
    {
        this.apiClient = apiClient;
        this.sessionService = sessionService;
        sessionService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public Task EnsureProfileLoadedAsync() => EnsureDataLoadedAsync(loadProfile: true);

    public Task EnsureSalonLoadedAsync() => EnsureDataLoadedAsync(loadProfile: false);

    private async Task EnsureDataLoadedAsync(bool loadProfile)
    {
        if (loadProfile ? profileLoaded : salonLoaded)
        {
            return;
        }

        await loadLock.WaitAsync();
        try
        {
            if (loadProfile ? profileLoaded : salonLoaded)
            {
                return;
            }

            int generation = Volatile.Read(ref sessionGeneration);

            if (loadProfile)
            {
                MyProfileResponse? profile =
                    await apiClient.GetAsync<MyProfileResponse>("api/profile/me");
                if (!IsCurrentSession(generation))
                {
                    return;
                }

                Profile = profile;
                profileLoaded = true;
            }
            else
            {
                SalonResponse? salon =
                    await apiClient.GetAsync<SalonResponse>("api/salons/current");
                if (!IsCurrentSession(generation))
                {
                    return;
                }

                Salon = salon;
                salonLoaded = true;
            }
        }
        finally
        {
            loadLock.Release();
        }
    }

    public void UpdateProfile(MyProfileResponse profile)
    {
        Profile = profile;
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            Interlocked.Increment(ref sessionGeneration);
            Profile = null;
            Salon = null;
            profileLoaded = false;
            salonLoaded = false;
        }
    }

    private bool IsCurrentSession(int generation) =>
        generation == Volatile.Read(ref sessionGeneration) &&
        sessionService.IsAuthenticated;

    public void Dispose()
    {
        sessionService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        loadLock.Dispose();
    }
}
