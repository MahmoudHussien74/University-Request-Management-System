namespace URMS.Domain.Constants;

public static class Permissions
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
        public const string ApproveRegistration = "Permissions.Users.ApproveRegistration";
    }

    public static class Requests
    {
        public const string View = "Permissions.Requests.View";
        public const string ViewOwn = "Permissions.Requests.ViewOwn";
        public const string Create = "Permissions.Requests.Create";
        public const string ApproveAdvisor = "Permissions.Requests.ApproveAdvisor";
        public const string ConfirmAdministration = "Permissions.Requests.ConfirmAdministration";
        public const string ProcessPayment = "Permissions.Requests.ProcessPayment";
        public const string Reject = "Permissions.Requests.Reject";
    }

    public static class Advisors
    {
        public const string ImportExcel = "Permissions.Advisors.ImportExcel";
        public const string RequestAvailabilityChange = "Permissions.Advisors.RequestAvailabilityChange";
        public const string ApproveAvailabilityChange = "Permissions.Advisors.ApproveAvailabilityChange";
    }

    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string ManagePermissions = "Permissions.Roles.ManagePermissions";
    }

    /// <summary>
    /// List of all system permissions.
    /// </summary>
    public static IReadOnlyList<string> GetAllPermissions()
    {
        return typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields().Select(f => f.GetValue(null)?.ToString()))
            .OfType<string>()
            .ToList();
    }
}
