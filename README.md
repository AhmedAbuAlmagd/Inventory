# Smart Inventory - Backend

This is the backend core of the Smart Inventory Management System, built with .NET 8/9. It provides a robust REST API for managing products, warehouses, and inventory transactions.

## Architecture

The project follows a Clean Architecture approach:
- **`SmartInventory.API`**: The entry point, containing controllers, middleware, and API configuration.
- **`SmartInventory.Application`**: Contains business logic, DTOs, service interfaces, and implementations.
- **`SmartInventory.Domain`**: Core domain entities, enums, and repository interfaces.
- **`SmartInventory.Infrastructure`**: (Consolidated into API/Application for now) Data access, EF Core configuration, and external services.

## Tech Stack

- **Framework**: .NET 8 / .NET 9
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Auth**: JWT Bearer Authentication with Refresh Tokens
- **Testing**: xUnit for service and validator tests

## Project Structure

```text
Inventory/
├── src/
│   ├── SmartInventory.API/          # Web API Layer
│   ├── SmartInventory.Application/  # Business Logic Layer
│   └── SmartInventory.Domain/       # Domain Layer
└── tests/
    └── SmartInventory.Tests/        # Unit Tests
```

## Setup & Running

1. **Prerequisites**:
   - .NET 8.0 SDK or higher
   - SQL Server (LocalDB or Express)

2. **Database Migration**:
   Navigate to `src/SmartInventory.API/` and run:
   ```bash
   dotnet ef database update
   ```

3. **Running the API**:
   From the `Inventory/` directory:
   ```bash
   dotnet run --project src/SmartInventory.API
   ```
   The API will be available at `http://localhost:5180` (or as configured in `launchSettings.json`).

4. **API Documentation**:
   Once running, access Swagger UI at `http://localhost:5180/swagger`.

## Seeded Credentials

The system comes with pre-seeded users for testing:

- **Admin**: `admin` / `Admin@123`
- **Employee**: `employee` / `Employee@123`

## Key Features

- **Product Management**: CRUD operations with search and filter capabilities.
- **Inventory Transactions**: Record stock-in and stock-out operations.
- **Warehouse Tracking**: Manage multiple storage locations.
- **Role-Based Security**: Secured endpoints based on user roles (Admin, Manager, Employee).
- **Rate Limiting**: Integrated rate limiting for API security.

## Testing

Run tests using the .NET CLI:
```bash
dotnet test
```
