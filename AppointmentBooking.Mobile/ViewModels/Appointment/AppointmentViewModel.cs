using AppointmentBooking.Mobile.Messages;
using AppointmentBooking.Mobile.Models.Bookings.Responses;
using AppointmentBooking.Mobile.Models.Common;
using AppointmentBooking.Mobile.Models.Enums;
using AppointmentBooking.Mobile.Services.Api;
using AppointmentBooking.Mobile.Services.ErrorHandling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AppointmentBooking.Mobile.ViewModels.Appointment
{
    public partial class AppointmentViewModel : ObservableObject
    {
        private readonly IApiClient apiClient;
        private readonly IErrorHandler errorHandler;
        private readonly SemaphoreSlim loadLock = new(1, 1);

        // The list is loaded once (at app start) and reloaded only after the customer
        // books or cancels an appointment, not on every visit to the tab.
        private bool hasLoaded;

        [ObservableProperty]
        private bool isLoading;

        public ObservableCollection<BookingSummaryResponse> UpcomingAppointments { get; } = [];

        public ObservableCollection<BookingSummaryResponse> PreviousAppointments { get; } = [];

        public ObservableCollection<BookingSummaryResponse> CancelledAppointments { get; } = [];

        public bool HasUpcomingAppointments =>
            UpcomingAppointments.Count > 0;

        public bool HasPreviousAppointments =>
            PreviousAppointments.Count > 0;

        public bool HasCancelledAppointments =>
            CancelledAppointments.Count > 0;

        public AppointmentViewModel(
            IApiClient apiClient,
            IErrorHandler errorHandler)
        {
            this.apiClient = apiClient;
            this.errorHandler = errorHandler;

            WeakReferenceMessenger.Default.Register<AppointmentViewModel, CustomerBookingsChangedMessage>(
                this,
                static (viewModel, message) =>
                {
                    // Fire and forget: the reload shows its own errors.
                    _ = viewModel.ReloadAsync();
                });
        }

        /// <summary>Loads the list if it has not been loaded successfully yet.</summary>
        public Task EnsureLoadedAsync() =>
            hasLoaded ? Task.CompletedTask : ReloadAsync();

        /// <summary>Loads the list from the API (one load at a time).</summary>
        public async Task ReloadAsync()
        {
            await loadLock.WaitAsync();
            try
            {
                hasLoaded = await LoadAppointmentsCoreAsync();
            }
            finally
            {
                loadLock.Release();
            }
        }

        public void ResetCachedData()
        {
            hasLoaded = false;
            UpcomingAppointments.Clear();
            PreviousAppointments.Clear();
            CancelledAppointments.Clear();
            OnPropertyChanged(nameof(HasUpcomingAppointments));
            OnPropertyChanged(nameof(HasPreviousAppointments));
            OnPropertyChanged(nameof(HasCancelledAppointments));
        }

        public async Task<BookingDetailsResponse?> GetBookingDetailsAsync(long bookingId)
        {
            try
            {
                return await apiClient.GetAsync<BookingDetailsResponse>(
                    $"api/bookings/{bookingId}");
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
                return null;
            }
        }

        public async Task<bool> CancelBookingAsync(long bookingId)
        {
            try
            {
                await apiClient.PostAsync<object>(
                    $"api/bookings/{bookingId}/cancel");

                return true;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
                return false;
            }
        }

        public void MoveToCancelled(BookingSummaryResponse appointment)
        {
            UpcomingAppointments.Remove(appointment);

            CancelledAppointments.Add(
                appointment with
                {
                    Status = BookingStatus.Cancelled
                });

            SortAppointments();

            OnPropertyChanged(nameof(HasUpcomingAppointments));
            OnPropertyChanged(nameof(HasCancelledAppointments));
        }

        [RelayCommand]
        private Task LoadAppointmentsAsync() => ReloadAsync();

        private async Task<bool> LoadAppointmentsCoreAsync()
        {
            try
            {
                IsLoading = true;

                UpcomingAppointments.Clear();
                PreviousAppointments.Clear();
                CancelledAppointments.Clear();

                PagedResponse<BookingSummaryResponse>? response =
                    await apiClient.GetAsync<PagedResponse<BookingSummaryResponse>>(
                        "api/bookings/my?page=1&pageSize=50");

                foreach (BookingSummaryResponse appointment in response?.Items ?? [])
                {
                    switch (appointment.Status)
                    {
                        case BookingStatus.Scheduled:
                            if (appointment.StartsAt > DateTimeOffset.Now)
                            {
                                UpcomingAppointments.Add(appointment);
                            }
                            break;

                        case BookingStatus.Completed:
                            PreviousAppointments.Add(appointment);
                            break;

                        case BookingStatus.Cancelled:
                            CancelledAppointments.Add(appointment);
                            break;
                    }
                }

                SortAppointments();

                OnPropertyChanged(nameof(HasUpcomingAppointments));
                OnPropertyChanged(nameof(HasPreviousAppointments));
                OnPropertyChanged(nameof(HasCancelledAppointments));
                return true;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleAsync(ex);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SortAppointments()
        {
            BookingSummaryResponse[] upcoming =
                UpcomingAppointments
                    .OrderBy(x => x.StartsAt)
                    .ToArray();

            BookingSummaryResponse[] previous =
                PreviousAppointments
                    .OrderByDescending(x => x.StartsAt)
                    .ToArray();

            BookingSummaryResponse[] cancelled =
                CancelledAppointments
                    .OrderByDescending(x => x.StartsAt)
                    .ToArray();

            UpcomingAppointments.Clear();
            PreviousAppointments.Clear();
            CancelledAppointments.Clear();

            foreach (BookingSummaryResponse appointment in upcoming)
            {
                UpcomingAppointments.Add(appointment);
            }

            foreach (BookingSummaryResponse appointment in previous)
            {
                PreviousAppointments.Add(appointment);
            }

            foreach (BookingSummaryResponse appointment in cancelled)
            {
                CancelledAppointments.Add(appointment);
            }
        }
    }
}