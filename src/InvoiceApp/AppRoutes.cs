namespace InvoiceApp;

/// <summary>
/// Centralized route constants for the application.
/// Use these constants for navigation links and redirects.
/// Note: @page directives still require literal strings.
/// </summary>
public static class AppRoutes
{
    public const string Home = "/";
    public const string Invoices = "/invoices";
    public const string NotFound = "/not-found";
    public const string Error = "/error";

    public static class Account
    {
        public const string Login = "/account/login";
        public const string Register = "/account/register";
        public const string Logout = "/account/logout";
        public const string AccessDenied = "/account/accessdenied";
        public const string Lockout = "/account/lockout";
        public const string InvalidUser = "/account/invaliduser";
    }
}
