namespace backend.Security;

public static class AppPermissionActions
{
    public const string View = "VIEW";
    public const string Create = "CREATE";
    public const string Edit = "EDIT";
    public const string Delete = "DELETE";

    public static readonly string[] All = [View, Create, Edit, Delete];

    public static bool IsValid(string action) =>
        All.Contains(action, StringComparer.OrdinalIgnoreCase);
}
