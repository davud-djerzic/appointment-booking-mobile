using AppointmentBooking.Mobile.Models.Bookings.Requests;
using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Utilities;
using AppointmentBooking.Mobile.ViewModels.Booking;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;

namespace AppointmentBooking.Mobile.ViewModels.Staff
{
    /// <summary>
    /// "Novi termin" for staff, with the same flow as the customer booking page:
    /// services → calendar (working days) → free slots → hold → confirm.
    /// </summary>
    public partial class StaffAppointmentsViewModel
    {
        private static readonly CultureInfo BookingCalendarCulture =
            CultureInfo.GetCultureInfo("bs-BA");

        [ObservableProperty]
        private DateTime bookingDisplayedMonth = GetFirstDayOfCurrentMonth();

        [ObservableProperty]
        private DateTime? bookingSelectedDate;

        [ObservableProperty]
        private bool isLoadingBookingAvailability;

        [ObservableProperty]
        private bool isCreatingStaffHold;

        [ObservableProperty]
        private BookingAvailabilityResponse? bookingAvailability;

        [ObservableProperty]
        private BookingHoldResponse? staffBookingHold;

        [ObservableProperty]
        private bool isStaffHoldConfirmationVisible;

        private HashSet<DateOnly> bookingWorkingDates = [];
        private CalendarDayItem? selectedBookingCalendarDay;

        // Replaced as a whole when the month changes; otherwise the existing cells are updated.
        [ObservableProperty]
        private IReadOnlyList<CalendarDayItem> bookingCalendarDays = [];

        public ObservableCollection<AvailableTimeSlotResponse> BookingAvailableSlots { get; } = [];

        public string BookingDisplayedMonthTitle =>
            BookingCalendarCulture.TextInfo.ToTitleCase(
                BookingDisplayedMonth.ToString("MMMM yyyy", BookingCalendarCulture));

        public bool HasBookingAvailability =>
            BookingAvailableSlots.Count > 0;

        public bool HasNoBookingAvailability =>
            BookingAvailability is not null &&
            BookingAvailableSlots.Count == 0;

        public bool HasNoBookingSelectedDate =>
            BookingSelectedDate is null;

        public DateTimeOffset? StaffHoldStartsAtLocal =>
            StaffBookingHold is null ? null : SalonTimeZone.ToLocal(StaffBookingHold.StartsAt);

        public DateTimeOffset? StaffHoldExpiresAtLocal =>
            StaffBookingHold is null ? null : SalonTimeZone.ToLocal(StaffBookingHold.ExpiresAt);

        public string StaffHoldCustomerName =>
            $"{CustomerFirstName.Trim()} {CustomerLastName.Trim()}";

        private static DateTime GetFirstDayOfCurrentMonth() =>
            new(SalonTimeZone.Today.Year, SalonTimeZone.Today.Month, 1);

        private long[] GetSelectedBookingServiceIds() =>
            BookingServices
                .Where(x => x.IsSelected)
                .Select(x => x.Service.Id)
                .ToArray();

        // Called when "Novi termin" is opened.
        private void ResetStaffBookingCalendar()
        {
            StaffBookingHold = null;
            IsStaffHoldConfirmationVisible = false;
            BookingDisplayedMonth = GetFirstDayOfCurrentMonth();
            bookingWorkingDates.Clear();
            ClearBookingDateSelection();
            BuildBookingCalendar();
        }

        private async Task LoadStaffBookingCalendarAsync()
        {
            bookingWorkingDates.Clear();

            if (SelectedBookingEmployee is not null)
            {
                long employeeId = SelectedBookingEmployee.Id;

                try
                {
                    BookingCalendarDaysResponse? response =
                        await bookingCalendarService.GetWorkingDaysAsync(employeeId);

                    if (SelectedBookingEmployee?.Id == employeeId)
                    {
                        bookingWorkingDates = (response?.WorkingDates ?? []).ToHashSet();
                    }
                }
                catch (Exception ex)
                {
                    await errorHandler.HandleAsync(ex);
                }
            }

            BuildBookingCalendar();
        }

        [RelayCommand]
        private void PreviousBookingMonth()
        {
            DateTime previousMonth = BookingDisplayedMonth.AddMonths(-1);

            if (previousMonth < GetFirstDayOfCurrentMonth())
            {
                return;
            }

            BookingDisplayedMonth = previousMonth;
            BuildBookingCalendar();
        }

        [RelayCommand]
        private void NextBookingMonth()
        {
            DateTime nextMonth = BookingDisplayedMonth.AddMonths(1);
            DateOnly? latestWorkingDate = bookingWorkingDates.Count == 0
                ? null
                : bookingWorkingDates.Max();

            if (latestWorkingDate.HasValue &&
                nextMonth > new DateTime(latestWorkingDate.Value.Year, latestWorkingDate.Value.Month, 1))
            {
                return;
            }

            BookingDisplayedMonth = nextMonth;
            BuildBookingCalendar();
        }

        [RelayCommand]
        private async Task SelectBookingDateAsync(CalendarDayItem day)
        {
            if (day is null || !day.IsSelectable || SelectedBookingEmployee is null)
            {
                return;
            }

            if (SelectedBookingServiceCount == 0)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Odaberite uslugu",
                    "Prije odabira datuma morate odabrati najmanje jednu uslugu.",
                    "U redu");

                return;
            }

            BookingSelectedDate = day.Date;

            if (selectedBookingCalendarDay != day)
            {
                if (selectedBookingCalendarDay is not null)
                {
                    selectedBookingCalendarDay.IsSelected = false;
                }

                day.IsSelected = true;
                selectedBookingCalendarDay = day;
            }

            await LoadBookingAvailabilityAsync(day.Date);
        }

        private async Task LoadBookingAvailabilityAsync(DateTime date)
        {
            ClearBookingAvailability();

            long[] serviceIds = GetSelectedBookingServiceIds();

            if (SelectedBookingEmployee is null || serviceIds.Length == 0)
            {
                return;
            }

            try
            {
                IsLoadingBookingAvailability = true;

                string serviceQuery = string.Join(
                    "&",
                    serviceIds.Select(serviceId => $"serviceIds={serviceId}"));

                string endpoint =
                    "api/bookings/availability" +
                    $"?employeeId={SelectedBookingEmployee.Id}" +
                    $"&{serviceQuery}" +
                    $"&date={date:yyyy-MM-dd}";

                BookingAvailabilityResponse? response =
                    await apiClient.GetAsync<BookingAvailabilityResponse>(endpoint);

                BookingAvailability = response;

                foreach (AvailableTimeSlotResponse slot in response?.Slots ?? [])
                {
                    BookingAvailableSlots.Add(slot);
                }

                OnPropertyChanged(nameof(HasBookingAvailability));
                OnPropertyChanged(nameof(HasNoBookingAvailability));
            }
            catch (Exception ex)
            {
                ClearBookingAvailability();

                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsLoadingBookingAvailability = false;
            }
        }

        private Task ReloadBookingAvailabilityAsync() =>
            BookingSelectedDate is DateTime date
                ? LoadBookingAvailabilityAsync(date)
                : Task.CompletedTask;

        [RelayCommand]
        private async Task CreateStaffHoldAsync(AvailableTimeSlotResponse slot)
        {
            if (slot is null || IsCreatingStaffHold)
            {
                return;
            }

            string? validationMessage = GetStaffBookingValidationMessage();

            if (validationMessage is not null || SelectedBookingEmployee is null)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Novi termin",
                    validationMessage ?? "Nije odabran zaposlenik za kojeg se dodaje termin.",
                    "U redu");

                return;
            }

            try
            {
                IsCreatingStaffHold = true;

                CreateStaffBookingRequest request = new(
                    CustomerFirstName.Trim(),
                    CustomerLastName.Trim(),
                    IsAdmin ? SelectedBookingEmployee.Id : null,
                    slot.StartsAt,
                    GetSelectedBookingServiceIds(),
                    string.IsNullOrWhiteSpace(StaffBookingNotes)
                        ? null
                        : StaffBookingNotes.Trim());

                BookingHoldResponse? hold =
                    await apiClient.PostAsync<CreateStaffBookingRequest, BookingHoldResponse>(
                        "api/bookings/staff/hold",
                        request);

                if (hold is null)
                {
                    throw new InvalidOperationException("API nije vratio zadržani termin.");
                }

                StaffBookingHold = hold;
                IsStaffHoldConfirmationVisible = true;
            }
            catch (ApiException ex) when (
                ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Termin nije dodan",
                    GetStaffBookingApiMessage(ex),
                    "U redu");

                await ReloadBookingAvailabilityAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsCreatingStaffHold = false;
            }
        }

        [RelayCommand]
        private async Task CancelStaffHoldAsync()
        {
            if (StaffBookingHold is null || IsCreatingStaffHold)
            {
                return;
            }

            Guid holdToken = StaffBookingHold.HoldToken;

            try
            {
                IsCreatingStaffHold = true;

                await apiClient.DeleteAsync($"api/bookings/staff/hold/{holdToken}");

                StaffBookingHold = null;
                IsStaffHoldConfirmationVisible = false;

                await ReloadBookingAvailabilityAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                await HandleExpiredStaffHoldAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsCreatingStaffHold = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmStaffBookingAsync()
        {
            if (StaffBookingHold is null || IsCreatingStaffHold)
            {
                return;
            }

            Guid holdToken = StaffBookingHold.HoldToken;
            DateTime bookingDate = SalonTimeZone.ToLocal(StaffBookingHold.StartsAt).Date;

            try
            {
                IsCreatingStaffHold = true;

                EmployeeBookingDetailsResponse? created =
                    await apiClient.PostAsync<EmployeeBookingDetailsResponse>(
                        $"api/bookings/staff/hold/{holdToken}/confirm");

                if (created is null)
                {
                    throw new InvalidOperationException("API nije vratio kreirani termin.");
                }

                StaffBookingHold = null;
                IsStaffHoldConfirmationVisible = false;

                wasStaffBookingCreated = true;
                OnPropertyChanged(nameof(WasStaffBookingCreated));
                SelectedDate = bookingDate;

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Termin dodan",
                    "Termin je uspješno dodan.",
                    "U redu");
            }
            catch (ApiException ex) when (
                ex.StatusCode == HttpStatusCode.Conflict &&
                ReadProblemDetail(ex.ResponseBody).Contains("expired or no longer exists", StringComparison.OrdinalIgnoreCase))
            {
                await HandleExpiredStaffHoldAsync();
            }
            catch (ApiException ex) when (
                ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                // The server released the hold (break added or time booked in the meantime).
                StaffBookingHold = null;
                IsStaffHoldConfirmationVisible = false;

                await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                    "Termin nije dodan",
                    GetStaffBookingApiMessage(ex),
                    "U redu");

                await ReloadBookingAvailabilityAsync();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
            }
            finally
            {
                IsCreatingStaffHold = false;
            }
        }

        private async Task HandleExpiredStaffHoldAsync()
        {
            StaffBookingHold = null;
            IsStaffHoldConfirmationVisible = false;

            await AppointmentBooking.Mobile.Services.Dialogs.AlertPanelService.DisplayAlertAsync(
                "Termin više nije zadržan",
                "Privremeni hold je istekao ili termin više nije dostupan. Odaberite novi termin.",
                "U redu");

            await ReloadBookingAvailabilityAsync();
        }

        private void ClearBookingDateSelection()
        {
            BookingSelectedDate = null;

            if (selectedBookingCalendarDay is not null)
            {
                selectedBookingCalendarDay.IsSelected = false;
                selectedBookingCalendarDay = null;
            }

            ClearBookingAvailability();
        }

        private void ClearBookingAvailability()
        {
            BookingAvailability = null;
            BookingAvailableSlots.Clear();

            OnPropertyChanged(nameof(HasBookingAvailability));
            OnPropertyChanged(nameof(HasNoBookingAvailability));
        }

        // Same layout as the customer calendar: Monday–Friday, 5 columns.
        // The cells are created once per month. Reopening "Novi termin" or loading the
        // working days only updates the existing cells, so the calendar views are not rebuilt.
        private void BuildBookingCalendar()
        {
            selectedBookingCalendarDay = null;

            List<CalendarDayItem> cells = CreateBookingCalendarCells();
            IReadOnlyList<CalendarDayItem> currentCells = BookingCalendarDays;

            bool isSameMonth =
                currentCells.Count == cells.Count &&
                currentCells.Zip(cells).All(pair => pair.First.Date == pair.Second.Date);

            if (!isSameMonth)
            {
                selectedBookingCalendarDay = cells.FirstOrDefault(x => x.IsSelected);
                BookingCalendarDays = cells;
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                currentCells[i].IsSelectable = cells[i].IsSelectable;
                currentCells[i].IsSelected = cells[i].IsSelected;

                if (currentCells[i].IsSelected)
                {
                    selectedBookingCalendarDay = currentCells[i];
                }
            }
        }

        private List<CalendarDayItem> CreateBookingCalendarCells()
        {
            List<CalendarDayItem> cells = [];

            DateTime firstDay = new(BookingDisplayedMonth.Year, BookingDisplayedMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(BookingDisplayedMonth.Year, BookingDisplayedMonth.Month);

            int firstDayOffset = firstDay.DayOfWeek switch
            {
                DayOfWeek.Monday => 0,
                DayOfWeek.Tuesday => 1,
                DayOfWeek.Wednesday => 2,
                DayOfWeek.Thursday => 3,
                DayOfWeek.Friday => 4,
                _ => 0
            };

            for (int i = 0; i < firstDayOffset; i++)
            {
                cells.Add(new CalendarDayItem());
            }

            DateTime today = SalonTimeZone.Today.ToDateTime(TimeOnly.MinValue);

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new(BookingDisplayedMonth.Year, BookingDisplayedMonth.Month, day);

                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                CalendarDayItem calendarDay = new()
                {
                    Date = date,
                    IsSelectable = date >= today && bookingWorkingDates.Contains(DateOnly.FromDateTime(date)),
                    IsSelected = BookingSelectedDate.HasValue &&
                        BookingSelectedDate.Value.Date == date.Date
                };

                cells.Add(calendarDay);
            }

            while (cells.Count % 5 != 0)
            {
                cells.Add(new CalendarDayItem());
            }

            return cells;
        }

        partial void OnBookingDisplayedMonthChanged(DateTime value) =>
            OnPropertyChanged(nameof(BookingDisplayedMonthTitle));

        partial void OnBookingSelectedDateChanged(DateTime? value) =>
            OnPropertyChanged(nameof(HasNoBookingSelectedDate));

        partial void OnStaffBookingHoldChanged(BookingHoldResponse? value)
        {
            OnPropertyChanged(nameof(StaffHoldStartsAtLocal));
            OnPropertyChanged(nameof(StaffHoldExpiresAtLocal));
            OnPropertyChanged(nameof(StaffHoldCustomerName));
        }
    }
}
