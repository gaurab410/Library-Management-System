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


