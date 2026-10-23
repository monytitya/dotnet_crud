using backend.Models.Data; using Microsoft.EntityFrameworkCore;
namespace backend.Data;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppPage> Pages => Set<AppPage>();
    public DbSet<AppPermission> Permissions => Set<AppPermission>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    public DbSet<AppRolePermission> RolePermissions => Set<AppRolePermission>();
    public DbSet<AppUnit> Units => Set<AppUnit>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e => { e.ToTable("APP_USERS"); e.HasIndex(x => x.Username).IsUnique(); e.HasIndex(x => x.Email).IsUnique(); });
        b.Entity<AuthAuditLog>().ToTable("AUTH_AUDIT_LOGS");
        b.Entity<AppRole>(e => { e.ToTable("APP_ROLES"); e.HasIndex(x => x.Code).IsUnique(); });
        b.Entity<AppPage>(e => { e.ToTable("APP_PAGES"); e.HasIndex(x => x.Code).IsUnique(); e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<AppPermission>(e => { e.ToTable("APP_PERMISSIONS"); e.HasIndex(x => x.Code).IsUnique(); e.HasOne(x => x.Page).WithMany(x => x.Permissions).HasForeignKey(x => x.PageId); });
        b.Entity<AppUserRole>(e => { e.ToTable("APP_USER_ROLES"); e.HasKey(x => new { x.UserId, x.RoleId }); });
        b.Entity<AppRolePermission>(e => { e.ToTable("APP_ROLE_PERMISSIONS"); e.HasKey(x => new { x.RoleId, x.PermissionId }); });
        b.Entity<AppUnit>(e => { e.ToTable("APP_UNITS"); e.HasIndex(x => x.Name).IsUnique(); e.HasIndex(x => x.Symbol).IsUnique(); e.Property(x => x.Measure).HasPrecision(18, 4); });
        b.Entity<Student>(entity =>
        {
            entity.ToTable("STUDENTS");
            entity.HasKey(student => student.Id);
            entity.Property(student => student.Name).HasMaxLength(200).IsRequired();
            entity.Property(student => student.Email).HasMaxLength(254).IsRequired();
        });
    }
}
