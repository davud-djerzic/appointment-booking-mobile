using System.Globalization;
using AppointmentBooking.Mobile.Models.BookableServices.Responses;

namespace AppointmentBooking.Mobile.Models.BookableServices;

/// <summary>
/// Display wrapper around <see cref="ServiceResponse"/> for the staff services tab.
/// </summary>
public sealed class StaffServiceItem(ServiceResponse service)
{
    private static readonly CultureInfo BosnianCulture = CultureInfo.GetCultureInfo("bs-BA");

    public ServiceResponse Service { get; } = service;

    public long Id => Service.Id;

    public string Name => Service.Name;

    public string? Description => Service.Description;

    public int DurationMinutes => Service.DurationMinutes;

    public decimal Price => Service.Price;

    public bool IsActive => Service.IsActive;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Service.Description);

    public string DescriptionText => HasDescription ? Service.Description!.Trim() : "Bez opisa";

    public string DurationText => FormatDuration(Service.DurationMinutes);

    public string PriceText => $"{Service.Price.ToString("N2", BosnianCulture)} KM";

    public static string FormatDuration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        int hours = minutes / 60;
        int rest = minutes % 60;
        return rest == 0 ? $"{hours} h" : $"{hours} h {rest} min";
    }
}
