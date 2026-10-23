# dotnet_angular

## Backend source layout

The API project file and `Program.cs` are in `backend/`. Source files are organized directly under `backend/` in `Controllers/`, `Services/`, `Models/`, `Security/`, and `Data/`.

```text
Controllers/
  AuthController.cs
  NavigationController.cs
  PermissionsController.cs
  RolesController.cs
  StudentsController.cs
  UnitsController.cs
Services/
  IUserService.cs
  UserService.cs
  IRoleService.cs
  RoleService.cs
  IUnitService.cs
  UnitService.cs
  IStudentService.cs
  StudentService.cs
  JwtService.cs
  TwoFactorService.cs
  Navigation/
  Permission/
Models/
  Data/
  Request/
Security/
Data/
```

Run the API from the repository root with `dotnet run --project backend`.

## Oracle database setup

The backend uses Entity Framework Core with Oracle. Start the Oracle container with port `1521` published, then configure the backend with a database user, password, and the Oracle service name (the value after `/` in the data source). For example, when the app runs on the same machine as the container:

```text
User Id=YOUR_USER;Password=YOUR_PASSWORD;Data Source=localhost:1521/YOUR_SERVICE_NAME
```

Set the connection string without committing credentials. From the repository root, PowerShell commands are:

```powershell
dotnet user-secrets init --project backend
dotnet user-secrets set "ConnectionStrings:OracleDb" "User Id=YOUR_USER;Password=YOUR_PASSWORD;Data Source=localhost:1521/YOUR_SERVICE_NAME" --project backend
```

Alternatively, set `ConnectionStrings__OracleDb` in the backend process environment. If the backend runs in another container, use the Oracle container's network name instead of `localhost`.

The API starts without contacting Oracle. Database-backed endpoints still require Oracle to be running and reachable at the configured host, port, and service name. A connection to `127.0.0.1:1521` requires an Oracle listener on the same machine; Docker Desktop being running does not start an Oracle database by itself.

For a disposable development schema, you can opt into table creation at startup by setting `Database:EnsureCreatedOnStartup` to `true` (or `Database__EnsureCreatedOnStartup=true`). The configured Oracle user must be allowed to create tables, indexes, and constraints. `EnsureCreated` is intended only for initial setup; use EF Core migrations when evolving a schema that already contains application tables.

The Oracle provider is configured for Oracle Database 21 compatibility, including mapping .NET `bool` values to a type supported by Oracle XE 21c. The authenticated Student endpoints are under `/api/students`.
