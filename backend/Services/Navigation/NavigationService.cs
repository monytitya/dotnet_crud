using backend.Data;
using backend.Models.Request;
using backend.Security;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Navigation;

public sealed class NavigationService(AppDbContext db) : INavigationService
{
    public async Task<IReadOnlyList<NavigationItemResponse>> GetUserNavigationAsync(
        int userId, CancellationToken cancellationToken)
    {
        var pages = await db.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == userId && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Where(rolePermission =>
                rolePermission.Permission.IsActive &&
                rolePermission.Permission.Action == AppPermissionActions.View &&
                rolePermission.Permission.Page.IsActive)
            .Select(rolePermission => rolePermission.Permission.Page)
            .Distinct()
            .Select(page => new NavigationPageData(
                page.Id, page.Code, page.Name, page.Route, page.Icon, page.ParentId, page.SortOrder))
            .ToArrayAsync(cancellationToken);

        // A user may have permission to a child page without permission to its parent.
        // Keep those accessible pages visible by promoting them to the root of the menu.
        var pageIds = pages.Select(page => page.Id).ToHashSet();
        return BuildTree(pages, page => page.ParentId is null || !pageIds.Contains(page.ParentId.Value));
    }

    private static IReadOnlyList<NavigationItemResponse> BuildTree(
        IReadOnlyList<NavigationPageData> pages, Func<NavigationPageData, bool> isRoot) =>
        pages.Where(isRoot)
            .OrderBy(page => page.SortOrder)
            .ThenBy(page => page.Label)
            .Select(page => new NavigationItemResponse(
                page.Id, page.Code, page.Label, page.Route, page.Icon, page.SortOrder,
                BuildTree(pages, child => child.ParentId == page.Id)))
            .ToArray();

    private sealed record NavigationPageData(
        int Id, string Code, string Label, string? Route, string? Icon, int? ParentId, int SortOrder);
}
