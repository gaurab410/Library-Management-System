# Library Management System

An enterprise-ready **ASP.NET Core MVC** web application for comprehensive library inventory management, patron circulation, multi-branch operations, self-service kiosk lookups, automated notifications, and real-time managerial analytics.

---
# Library Management System

This workspace contains the complete **Library Management System** solution.

For detailed documentation, architectural overview, demo credentials, REST API specifications, and setup instructions, please see:

👉 [**Library-Management-System/readme.md**](file:///e:/final_library/Library-Management-System/readme.md)

---

### Quick Start

```bash
cd Library-Management-System
dotnet run --project "Library Management System"
```

### Run Automated Tests (20 Passed)

```bash
cd Library-Management-System
dotnet test
```

## Table of Contents

- [Overview](#overview)
- [Demo Credentials](#demo-credentials)
- [Key Features](#key-features)
- [Architecture & Design Patterns](#architecture--design-patterns)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Clone & Run](#clone--run)
  - [Running Unit Tests](#running-unit-tests)
- [REST API Reference](#rest-api-reference)
- [Database & Seed Data](#database--seed-data)
- [Troubleshooting](#troubleshooting)

---

## Overview

The **Library Management System** is built to modern library standards, replacing paper workflows with an end-to-end digital circulation platform. The system supports multi-type item tracking (Books, Music albums, Toys & Board Games) using **Table-Per-Hierarchy (TPH)** inheritance, cookie-based role authentication, automated late-fee calculation, inter-branch transfers, waitlist reservation queues, and PDF/CSV reporting.

---

## Demo Credentials

The application includes pre-seeded demonstration accounts for all primary administrative roles:

| Role | Email | Password | Access & Responsibilities |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@library.com` | `Admin123!` | Full inventory catalog CRUD, branch administration, bulk CSV data import, item damage/destroy lifecycle |
| **Receptionist** | `reception@library.com` | `Reception123!` | Patron registration, check-out (borrow), check-in (return), fine collection, receipt generation, inter-branch transfer management |
| **Manager** | `manager@library.com` | `Manager123!` | High-level analytics dashboard, overdue audit monitoring, QuestPDF professional report exports, CSV data streaming |
| **Public / Patron** | *No login needed* | *N/A* | Public catalog search, item availability filtering, self-service barcode scanning kiosk |

---

## Key Features

### 1. Multi-Item Polymorphic Catalog (TPH)
- Supports three distinct inventory types within a single database table using Entity Framework Core discriminators:
  - **Books**: Author, Genre, ISBN, Publication Year.
  - **Music**: Artist, Album, Record Label, Release Year, Audio Format (CD, Vinyl, Digital).
  - **Toys & Games**: Toy Type, Target Age Group, Manufacturer, Piece Count.
- Full item lifecycle states: `Available`, `Borrowed`, `Damaged`, and `Destroy` (soft deletion preserving borrowing history).

### 2. Circulation & Patron Management
- **Smart Check-Out / Check-In**: Validates borrower active status, item availability, and loan limits.
- **Automated Fine Engine**: Computes late fines at **$1.50 per overdue day** upon return. Tracks fine status (`Paid`, `Unpaid`, `Waived`).
- **Patron History**: Full audit trail of past loans, active borrowings, and overdue notices per borrower.

### 3. Reservation & Waitlist Queue
- Patrons can place holds on currently borrowed items.
- Automatic FIFO queue sequencing.
- Automatically notifies the next borrower in line when an item is returned.

### 4. Inter-Branch Transfers
- Multi-branch physical inventory allocation (e.g., *City Central Library*, *Northside Branch*, *West End Library*).
- Transfer lifecycle: `Requested` &rarr; `InTransit` &rarr; `Received` / `Cancelled`.

### 5. Self-Service Kiosk & Quick Scanner
- Dedicated touch-friendly patron kiosk interface (`/Kiosk`).
- Instant barcode / library code lookup (`/Kiosk/Scan?libraryCode=BK-1001`).
- Real-time shelf location, availability indicator, and branch directory.

### 6. Automated Notification System
- Background notification service generating alerts for:
  - Upcoming due date reminders (3-day advance notice).
  - Overdue warning alerts with fine accrual estimates.
  - Reservation availability notifications when reserved items are checked in.

### 7. Manager Analytics & Reporting
- Real-time KPI summary cards: Active Loans, Overdue Books, Inventory Damage Ratio, Fine Revenue.
- **QuestPDF Engine**: Produces formatted, publication-quality PDF audit reports with headers, KPI metric cards, and transaction ledgers.
- **CsvHelper Streaming**: Fast CSV export of historical circulation logs for external spreadsheet auditing.

### 8. Bulk CSV Import
- Admin module supporting batch import for Books, Music, and Toys.
- Pre-validation preview, duplicate code checks, and transactional rollbacks.

### 9. Public REST API
- Dedicated headless endpoints (`/api/catalog/*`) enabling integration with digital signage, mobile apps, or external catalog search engines.

---

## Architecture & Design Patterns

```
                                  +-----------------------+
                                  |   Browser / Client    |
                                  +-----------+-----------+
                                              |
                     +------------------------+------------------------+
                     |                                                 |
            [ ASP.NET MVC Views ]                             [ REST API Clients ]
                     |                                                 |
+--------------------+---------------------+          +----------------+----------------+
| Controllers                             |          | API Controllers                |
| - AccountController   - AdminController  |          | - CatalogApiController         |
| - ReceptionController - ManagerController|          +----------------+----------------+
| - KioskController     - BranchController |                           |
| - TransferController  - ImportController |                           |
| - ReservationController                  |                           |
+--------------------+---------------------+                           |
                     |                                                 |
                     +------------------------+------------------------+
                                              |
                                     [ Business Services ]
                                     - NotificationService
                                     - QuestPDF Document Engine
                                     - CsvHelper Import / Export
                                              |
                                  [ EF Core DbContext ]
                               (Table-Per-Hierarchy Mapping)
                                              |
                                     [ SQLite Database ]
                                (LibraryManagementSystem.db)
```

- **Pattern**: Model-View-Controller (MVC) + Repository-style `DbContext`.
- **Inheritance Mapping**: Table-Per-Hierarchy (TPH) storing all `LibraryItem` variants in a single relational table.
- **Authentication**: Cookie-based authentication with role claims and authorization policies (`[Authorize(Roles = "...")]`).
- **Data Integrity**: Soft-delete triggers ensuring historical transaction relationships remain intact when items or borrowers are deactivated.

---

## Technology Stack

| Layer | Technologies |
| :--- | :--- |
| **Framework** | ASP.NET Core 10.0 / 8.0+ |
| **Language** | C# 12 / 13 |
| **Data Access** | Entity Framework Core 8.0 (SQLite Provider) |
| **Database** | SQLite 3 (`LibraryManagementSystem.db`) |
| **PDF Generation** | [QuestPDF](https://www.questpdf.com/) 2024.12.3 |
| **CSV Processing** | [CsvHelper](https://joshclose.github.io/CsvHelper/) 33.0.1 |
| **Front-End** | Razor Views, Bootstrap 5, FontAwesome, Vanilla CSS & JS |
| **Unit Testing** | xUnit 2.9.2, Moq 4.20.72, EF Core InMemory |

---

## Project Structure

```text
Library-Management-System/
├── Library Management System/            # Main Web Application (ASP.NET Core MVC)
│   ├── Controllers/                      # MVC & API Controllers
│   │   ├── Api/                          # REST API Endpoints (CatalogApiController)
│   │   ├── AccountController.cs          # Authentication & Cookie Session
│   │   ├── AdminController.cs            # Inventory CRUD & Lifecycle
│   │   ├── BranchController.cs           # Branch Location Management
│   │   ├── ImportController.cs           # CSV Bulk Import Engine
│   │   ├── KioskController.cs            # Patron Self-Service Terminal
│   │   ├── ManagerController.cs          # Analytics & PDF/CSV Export
│   │   ├── NotificationController.cs     # Patron & Staff Alerts
│   │   ├── PublicController.cs           # Public Catalog Search
│   │   ├── ReceptionController.cs        # Check-in/out & Patron Circulation
│   │   ├── ReservationController.cs      # Waitlist Holds & Queue
│   │   └── TransferController.cs         # Inter-Branch Transit Operations
│   ├── Data/                             # EF Core Context & Database Setup
│   │   └── LibraryManagementSystemContext.cs
│   ├── Models/                           # Entity Models & ViewModels
│   │   ├── BookItem.cs                   # Derived Book Entity
│   │   ├── MusicItem.cs                  # Derived Music Album Entity
│   │   ├── ToyItem.cs                    # Derived Toy/Board Game Entity
│   │   ├── LibraryItem.cs                # Abstract Base Catalog Entity
│   │   ├── Borrower.cs                   # Patron Information
│   │   ├── BorrowTransaction.cs          # Circulation Loans & Fines
│   │   ├── Branch.cs                     # Physical Branch Location
│   │   ├── BranchTransfer.cs             # Inter-Branch Transfer Records
│   │   ├── Reservation.cs                # Item Waitlist Hold Queue
│   │   ├── Notification.cs               # Alert Logs & Messages
│   │   ├── SeedData.cs                   # Initial Database Seeder
│   │   └── ViewModels.cs                 # Strongly Typed ViewModels
│   ├── Services/                         # Application & Domain Services
│   │   └── NotificationService.cs        # Automated Alert Engine
│   ├── Views/                            # Razor UI Views by Controller
│   ├── wwwroot/                          # Static Assets (CSS, JS, Icons)
│   ├── appsettings.json                  # SQLite Connection String & App Settings
│   ├── LibraryManagementSystem.csproj    # Project Configuration & NuGet Packages
│   └── Program.cs                        # WebApp Pipeline & DI Configuration
│
├── LibraryManagementSystem.Tests/        # Automated Test Suite (xUnit)
│   ├── BorrowReturnTests.cs              # Loans, Returns & Fine Calculations
│   ├── NotificationTriggerTests.cs       # Alert Event Verification
│   ├── PublicSearchTests.cs              # Search & Filter Logic
│   ├── ReservationQueueTests.cs          # Hold Queue FIFO & Notifications
│   └── LibraryManagementSystem.Tests.csproj
│
├── LibraryManagementSystem.slnx          # Visual Studio Solution File
├── ProjectStructureReport.md             # Technical Architecture Analysis
└── readme.md                             # Project Documentation
```

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- Modern Web Browser (Chrome, Firefox, Edge, Safari)

### Clone & Run

#### 1. Clone the Repository
```bash
git clone https://github.com/gaurab410/Library-Management-System.git
cd Library-Management-System
```

#### 2. Run from the Solution Root
```bash
dotnet run --project "Library Management System"
```

*Or navigate directly to the project folder:*
```bash
cd "Library Management System"
dotnet restore
dotnet run
```

#### 3. Access the Web Application
Open your browser and navigate to:
```
https://localhost:7143
```
*(Or the HTTP/HTTPS port displayed in your terminal output).*

> [!NOTE]
> **Zero Configuration Database**: SQLite database (`LibraryManagementSystem.db`) is automatically initialized, migrated, and seeded with realistic branches, books, music, toys, patrons, and transactions on the first launch.

---

## Running Unit Tests

The test project contains **20 comprehensive unit tests** utilizing `xUnit`, `Moq`, and `Microsoft.EntityFrameworkCore.InMemory`.

To run all automated tests:

```bash
# From the repository root
dotnet test
```

### Test Coverage Highlights

- **`BorrowReturnTests`**: Validates checkout constraints, due date calculation, return processing, and overdue fine computation ($1.50/day).
- **`ReservationQueueTests`**: Tests queue prioritization, duplicate reservation blocks, and automatic notification dispatch on return.
- **`NotificationTriggerTests`**: Verifies automated trigger mechanisms for loan reminders, overdue notices, and pick-up alerts.
- **`PublicSearchTests`**: Tests catalog query filters for polymorphic item types, availability flags, and keyword searches.

---

## REST API Reference

The application exposes a public, read-only RESTful API under `/api/catalog`:

| Method | Endpoint | Description | Query Parameters |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/catalog/items` | Retrieve list of catalog items | `type` (Book, Music, Toy, All)<br>`availability` (Available, Borrowed, All)<br>`branchId` (int)<br>`search` (string) |
| `GET` | `/api/catalog/items/{libraryCode}` | Lookup a specific item by its barcode/code | `{libraryCode}` (e.g. `BK-1001`) |
| `GET` | `/api/catalog/branches` | List all library branches and hours | *None* |
| `GET` | `/api/catalog/stats` | High-level inventory statistics | *None* |

### Sample Request: Get Available Books
```http
GET /api/catalog/items?type=Book&availability=Available
Host: localhost:7143
Accept: application/json
```

### Sample Response (`200 OK`)
```json
[
  {
    "id": 1,
    "name": "The Great Gatsby",
    "libraryCode": "BK-1001",
    "type": "Book",
    "status": "Available",
    "author": "F. Scott Fitzgerald",
    "genre": "Classic Literature",
    "isbn": "978-0743273565",
    "branchId": 1,
    "branchName": "City Central Library"
  }
]
```

---

## Database & Seed Data

On the first application run, `SeedData.Initialize()` automatically populates the SQLite database with:
- **3 Physical Branches**: City Central Library, Northside Branch, West End Library.
- **Polymorphic Catalog Items**:
  - Classic and modern Books with ISBNs, genres, and publication years.
  - Vinyl and CD Music albums with artists, record labels, and release years.
  - Educational Toys and Board Games with manufacturer and age group details.
- **Borrower Profiles**: Active and registered patrons with contact numbers and emails.
- **Transactions & Fines**: Real-world sample loans including active loans, overdue items, returned items, and fine payment records.
- **Active Reservations & Branch Transfers**: Sample waitlist holds and inter-branch transit records.

---

## Troubleshooting

### Port Conflicts or SSL Certificates
If you experience SSL certificate warnings in local development, run:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Rebuilding After Updates
If package restoration or build issues occur:
```bash
dotnet clean
dotnet restore
dotnet build
dotnet run --project "Library Management System"
```

### Database Reset
To reset the database back to clean seed data:
1. Stop the application.
2. Delete `Library Management System/LibraryManagementSystem.db`.
3. Restart the application using `dotnet run --project "Library Management System"`. The database and seed data will be recreated automatically.

---

## License

This project is open-source and available under the standard MIT License.
