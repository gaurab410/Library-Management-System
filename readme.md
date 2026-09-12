# Library Management System

An ASP.NET Core MVC web application for managing library items, user authentication, and admin/manager operations.

## Demo Login Credentials

You can sign in using any of the pre-configured accounts:

| Role | Email | Password | Access Level |
| --- | --- | --- | --- |
| **Admin** | `admin@library.com` | `Admin123!` | System Settings, User Management, Full Access |
| **Receptionist** | `reception@library.com` | `Reception123!` | Member Registration, Book Checkouts & Returns, Fine Processing |
| **Manager** | `manager@library.com` | `Manager123!` | Inventory Management, Reports, Overdue Monitoring |

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or higher (Support for .NET 10)
- [Git](https://git-scm.com/)

---

## How to Clone and Run

### 1. Clone the Repository
```bash
git clone https://github.com/gaurab410/Library-Management-System.git
cd Library-Management-System
```

### 2. Navigate to the Web App Folder
```bash
cd "Library Management System"
```

### 3. Run the Application
```bash
dotnet run
```
if problem arises this is the fix :
```cd "E:\new\Library-Management-System\Library Management System"
dotnet restore
dotnet build
```
Then
```bash
dotnet run
```

> **Note:** The database uses **SQLite** (`LibraryManagementSystem.db`) and is automatically created and seeded with default data when the application starts for the first time.

### 4. Open in Browser
Open your browser and navigate to the localhost URL shown in the terminal (e.g., `https://localhost:7143` or `http://localhost:5000`).

---

## Command Line Quick Reference

| Action | Command |
| --- | --- |
| Clone Repo | `git clone https://github.com/gaurab410/Library-Management-System.git` |
| Run from Root | `dotnet run --project "Library Management System"` |
| Run from Project Dir | `cd "Library Management System"` then `dotnet run` |
| Build Project | `dotnet build` |


# Library Management System - Technical Report

This report provides a comprehensive analysis of the Library Management System codebase, detailing the roles, folder structure, CRUD functions, and the purpose of key files and major functions.

## 1. Project Overview and Architecture

The project is an **ASP.NET Core MVC (Model-View-Controller)** application. It uses **Entity Framework Core (EF Core)** for Object-Relational Mapping (ORM) and data access, connected to a **SQLite** database. 

It implements a **Table-Per-Hierarchy (TPH)** inheritance pattern for inventory items, allowing `BookItem`, `MusicItem`, and `ToyItem` to inherit from a base `LibraryItem` class and be stored in a single table with a discriminator column.

## 2. Folder Structure

The project follows a standard ASP.NET Core MVC structure:

*   **`Controllers/`**: Contains the C# classes that handle incoming HTTP requests, process user input, interact with the models, and return the appropriate views. Organized by user roles (`AdminController.cs`, `ReceptionController.cs`, etc.).
*   **`Models/`**: Defines the data entities (e.g., `LibraryItem.cs`, `Borrower.cs`) that map to database tables, as well as ViewModels (e.g., `ViewModels.cs`) which are used to pass specific data shapes between the controllers and views.
*   **`Data/`**: Contains the `LibraryManagementSystemContext.cs` which manages the database connection and EF Core configurations.
*   **`Views/`**: Contains the Razor pages (`.cshtml` files) that generate the HTML user interface.
*   **`wwwroot/`**: Houses static files served directly to the client, such as CSS styles, JavaScript files, and images.

## 3. User Roles and Their Responsibilities

The system defines four distinct access levels, managed via cookie-based authentication:

1.  **Admin (`AdminController`)**: 
    *   **Responsibility**: Inventory Management.
    *   **Work**: Full control over library items. They can add new books, music, and toys to the catalog, update item details, and manage the item's physical status (Available, Damaged, Destroy). They can also remove items from the system.
2.  **Reception / Front Desk (`ReceptionController`)**: 
    *   **Responsibility**: Daily Operations & Patron Management.
    *   **Work**: Handles the interaction with library patrons (Borrowers). They register new borrowers, update borrower profiles, and most importantly, process the borrowing (check-out) and returning (check-in) of items, including the calculation of late fines.
3.  **Manager (`ManagerController`)**: 
    *   **Responsibility**: Analytics and Oversight.
    *   **Work**: Has access to a read-only dashboard that provides statistics on library operations. This includes active vs. overdue loans, item status counts, and financial statistics (paid, unpaid, and pending fines).
4.  **Public (`PublicController`)**: 
    *   **Responsibility**: Browsing.
    *   **Work**: Unauthenticated users can search the library catalog to see what items are available or currently checked out.

## 4. CRUD Functions (Create, Read, Update, Delete)

The system is heavily CRUD-based, divided primarily between the Admin and Reception roles.

### Inventory Items (Admin)
*   **Create**: Specialized creation forms for different types (`CreateBook()`, `CreateMusic()`, `CreateToy()`).
*   **Read**: Viewing the catalog (`Index()`) and viewing specific item details (`Details()`).
*   **Update**: Modifying item information (`Edit()`) and quickly changing an item's status (`ChangeStatus()`).
*   **Delete**: Removing items (`DeleteConfirmed()`). *Note: The system implements a soft-delete ("Destroy" status) if an item has a borrowing history, ensuring past transaction records are not broken.*

### Borrowers (Reception)
*   **Create**: Registering new patrons (`CreateBorrower()`).
*   **Read**: Listing patrons (`Borrowers()`) and viewing their borrowing history (`BorrowerDetails()`).
*   **Update**: Modifying patron contact details (`EditBorrower()`).
*   **Delete**: Removing patrons (`DeleteBorrowerConfirmed()`). *Note: Similar to items, patrons with history are "deactivated" rather than permanently deleted.*

### Borrowing Transactions (Reception)
*   **Create**: Checking out an item creates a new transaction (`Borrow()`).
*   **Update**: Returning an item updates the existing transaction with a return date and calculates any applicable fines (`Return()`).

## 5. Key Files and Major Functions

Here is a breakdown of the critical files that drive the application:

### `Data/LibraryManagementSystemContext.cs`
*   **Purpose**: The heart of the data layer. It inherits from `DbContext`.
*   **Major Functions**:
    *   `DbSet<...>` properties: Expose the tables for `LibraryItems`, `Borrowers`, and `BorrowTransactions`.
    *   `OnModelCreating(...)`: Configures the database schema. Crucially, it sets up the TPH inheritance for Library Items (differentiating Books, Music, and Toys) and enforces referential integrity (preventing the deletion of items/borrowers if they are linked to transactions).

### `Controllers/AccountController.cs`
*   **Purpose**: Handles user authentication and authorization.
*   **Major Functions**:
    *   `Login(...)`: Verifies credentials (using hardcoded demo accounts like `admin@library.com`, `reception@library.com`) and issues an authentication cookie with the respective user role. Redirects the user to their specific dashboard based on their role.
    *   `Logout()`: Clears the authentication cookie.

### `Controllers/AdminController.cs`
*   **Purpose**: Manages the inventory CRUD logic.
*   **Major Functions**:
    *   `Index(...)`: Fetches library items with support for search terms, item type filtering, and status filtering.
    *   `Create[Type](...)`: Validates and saves new specific items (e.g., ensuring `LibraryCode` uniqueness).
    *   `DeleteConfirmed(...)`: Handles the logic of deciding whether to permanently delete an item from the database or just mark its status as `Destroy` to preserve history.

### `Controllers/ReceptionController.cs`
*   **Purpose**: Manages patrons and the transaction workflow.
*   **Major Functions**:
    *   `Borrow(...)`: Complex logic that validates if an item is `Available` and if a borrower is `Active` before creating a `BorrowTransaction` and updating the item's status to `Borrowed`.
    *   `Return(...)`: Calculates the difference between the current date and the `DueDate`. If overdue, it generates a fine ($1.50 per day late), updates the transaction, and returns the item's status to `Available`.

### `Controllers/ManagerController.cs`
*   **Purpose**: Aggregates data for reporting.
*   **Major Functions**:
    *   `Index()`: Queries all transactions and items to compute aggregate numbers: total active loans, overdue loans, inventory health (damaged/destroyed counts), and financial metrics (total accrued fines vs. paid fines).

### `Models/ViewModels.cs` (and other model files)
*   **Purpose**: Data transport and validation.
*   **Major Functions**: Defines classes like `BorrowItemViewModel`, `ReturnItemViewModel`, and `ManagerDashboardViewModel`. These ensure that views only receive the data they need and that incoming POST requests are strongly typed and validated before hitting the controllers.
