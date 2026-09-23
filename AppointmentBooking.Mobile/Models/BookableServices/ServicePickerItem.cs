using AppointmentBooking.Mobile.Models.BookableServices.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Mobile.Models.BookableServices
{
    public sealed class ServicePickerItem(ServiceResponse service)
    {
        public ServiceResponse Service { get; } = service;

        public long Id => Service.Id;

        public string Name => Service.Name;

        public decimal Price => Service.Price;

        public string DisplayName =>
            $"{Service.Name} — {Service.Price:0.00} KM";
    }
}
