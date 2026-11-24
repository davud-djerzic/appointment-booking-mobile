namespace AppointmentBooking.Mobile.Models.BookableServices
{
    // Services that cannot be booked together (customer and staff booking pages):
    // while one of them is selected, the others in its group are greyed out.
    // Matched by service name as set in "Usluge"; renaming a service turns its rule off.
    public static class ServiceExclusions
    {
        private static readonly string[][] ExclusiveGroups =
        [
            ["Brijanje", "Oblikovanje brade"]
        ];

        public static void Apply(IEnumerable<ServiceSelectionItem> items)
        {
            List<ServiceSelectionItem> services = items.ToList();

            foreach (ServiceSelectionItem service in services)
            {
                service.IsBlocked =
                    !service.IsSelected &&
                    services.Any(other =>
                        !ReferenceEquals(other, service) &&
                        other.IsSelected &&
                        AreExclusive(service.Service.Name, other.Service.Name));
            }
        }

        private static bool AreExclusive(string firstName, string secondName)
        {
            string first = firstName.Trim();
            string second = secondName.Trim();

            if (string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return ExclusiveGroups.Any(group =>
                group.Contains(first, StringComparer.OrdinalIgnoreCase) &&
                group.Contains(second, StringComparer.OrdinalIgnoreCase));
        }
    }
}
