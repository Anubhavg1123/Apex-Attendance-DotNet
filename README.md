# Apex Attendance - Biometric Face Authorization & Employee Time Tracking System

An enterprise-grade, production-style **Business Employee Attendance & Time Tracking System** built with **ASP.NET Core (.NET 8.0 / C#)**, **MVC Architecture**, and **Real-Time Client-Side Biometric Face Recognition (face-api.js / TensorFlow.js)**.

---

## 🌟 Key Features

### 1. Real Biometric Facial Recognition & Camera Access
- **Cross-Platform Camera Integration**: Accesses laptop webcams, desktop USB cameras, and mobile front/rear cameras via HTML5 `navigator.mediaDevices.getUserMedia`.
- **Neural Network Embedding Extraction**: Uses `face-api.js` client-side neural networks (Tiny Face Detector, 68-Point Facial Landmarks, MobileNet Face Recognition) to compute 128-dimensional biometric embedding vectors.
- **Biometric Enrollment**: Admin scans and enrolls staff faces with real-time quality detection, 3-second alignment guides, and vector persistence.
- **Biometric Punch Authorization**: Employee check-in/check-out strictly requires face verification against enrolled biometric descriptors.
- **Strict Anti-Proxy Protection**: Computes Euclidean distance between live face and enrolled descriptor with $\ge 90\%$ match confidence threshold. Rejects unauthorized proxies.
- **Biometric Entrance Kiosk (`/Attendance/Kiosk`)**: Dedicated full-screen automated terminal for tablet or entrance-mounted cameras. Automatically identifies staff, verifies identity, and punches them in/out with audio-visual feedback.

### 2. Administrator Management & Security
- **Clean Slate / Auto-Seeded Database**: Thread-safe database with seed data for 5 departments, 5 staff members, policy settings, and 7 days of attendance history.
- **Administrative Control**: Only the System Administrator (`admin` / `Admin@123`) can log in to manage policy settings, departments, employee registration, and audit reports.
- **Cookie Authentication & Anti-Forgery Protection**: Production-grade security preventing CSRF attacks.

### 3. Executive Analytics & Reporting Engine
- **Live Analytics Dashboard**: Real-time metrics for Total Staff, Present, Late, Half-Days, Absent, and Attendance Rate.
- **Interactive Visualizations**: 7-Day Attendance Volume stacked bar chart and Department Presence distribution donut chart using Chart.js.
- **Master Attendance Logs**: Multi-parameter search, date filters, department filters, status filters, and pagination.
- **Reporting Hub**: Daily, Monthly, Employee-wise, and Department-wise reports with **CSV / Excel export** and Print view.

---

## 🛠️ Technology Stack

| Layer | Technology |
|---|---|
| **Backend Framework** | ASP.NET Core MVC (.NET 8.0) |
| **Language** | C# 12 / .NET 8 |
| **Biometric Neural Engine** | face-api.js / TensorFlow.js (128-D Biometric Embeddings) |
| **Biometric Weight Shards** | Tiny Face Detector, 68-Point Landmarks, Face Recognition ResNet |
| **Camera Access** | HTML5 `getUserMedia` API (Laptops, Tablets, Smartphones) |
| **Data Layer** | Thread-safe, schema-modeled document storage with atomic persistence |
| **Authentication** | ASP.NET Core Cookie Authentication & Role-Based Authorization |
| **Password Security** | SHA-256 with cryptographic salt |
| **Frontend UI** | HTML5, CSS3, JavaScript (ES6+), Bootstrap 5.3.3, SweetAlert2 |
| **Visualizations** | Chart.js 4.4 |

---

## 🔐 Default Administrator Login

| Role | Username | Initial Password | Purpose |
|---|---|---|---|
| **System Admin** | `admin` | `Admin@123` | System Administrator (Full Access to Register Staff, Configure Policy, View Reports) |

---

## 🚀 How to Run the Application

### Option 1: Terminal / Command Prompt
1. **Navigate to the Submission Directory**:
   ```bash
   cd "C:\Users\Anubhav\Documents\Employee_Attendance_Submission"
   ```

2. **Build and Run**:
   ```bash
   dotnet build EmployeeAttendanceApp.sln
   dotnet run --urls "http://localhost:5097"
   ```

3. **Access the Portal**:
   - Web Application: `http://localhost:5097`
   - Biometric Entrance Kiosk: `http://localhost:5097/Attendance/Kiosk`

### Option 2: Visual Studio 2022
1. Double-click `EmployeeAttendanceApp.sln` inside `Documents/Employee_Attendance_Submission`.
2. Press `F5` to start debugging or `Ctrl + F5` to run without debugging.

---

## 📷 Biometric Face Punch Workflow

```
1. Admin logs in as 'admin' -> Navigates to Employees -> Add New Employee.
2. Fills employee details -> Clicks "Scan & Enroll Face via Camera".
3. Webcam activates -> Live alignment guides detect face -> Captures 128-D biometric descriptor.
4. Admin saves employee -> User account and biometric profile are created.
5. Employee logs in -> Clicks "Face Punch In" -> Webcam opens -> Matches face with enrolled vector.
6. Upon >= 90% confidence match -> Attendance is recorded with server time, status, match score, and photo snapshot.
```

---

## 📂 Submission Package Structure

```
Employee_Attendance_Submission/
├── EmployeeAttendanceApp.sln       <- Visual Studio Solution file
├── EmployeeAttendanceApp.csproj    <- .NET Project configuration (.NET 8.0)
├── Program.cs                      <- ASP.NET Core application entry point & DI configuration
├── appsettings.json                <- Configuration settings
├── README.md                       <- Project setup & documentation
├── PROJECT_STATUS_REPORT.md        <- Complete Senior Developer Audit Report
├── App_Data/
│   └── attendance_database.json    <- Thread-safe JSON database with seeded records
├── Controllers/
│   ├── AccountController.cs        <- Login / Logout / Cookie Authentication
│   ├── AttendanceController.cs     <- Attendance history & Check-in / Check-out actions
│   ├── DashboardController.cs      <- Executive Analytics
│   ├── DepartmentController.cs     <- Department CRUD
│   ├── EmployeeController.cs       <- Employee CRUD & Biometric enrollment
│   ├── ReportController.cs         <- Report filters & CSV export
│   ├── SettingController.cs        <- Company shift policy configuration
│   └── Api/
│       └── BiometricApiController.cs <- REST API endpoints for biometric verification
├── Data/
│   ├── ApplicationDbContext.cs     <- Thread-safe database context & concurrency locking
│   └── DbInitializer.cs            <- Database seeder & password hasher
├── Models/
│   ├── DomainModels.cs             <- Employee, Attendance, Department, Policy, User entities
│   └── ViewModels/                 <- View models for Dashboard, Reports, Login, etc.
├── Services/
│   ├── AttendanceService.cs        <- Check-in / Check-out / Punctuality logic
│   ├── EmployeeService.cs          <- Employee CRUD service
│   └── ReportAndAuthServices.cs    <- Reporting, CSV exporter & Authentication service
├── Views/                          <- Razor Views (Bootstrap 5.3 styled UI)
└── wwwroot/
    ├── css/                        <- Custom glassmorphism stylesheets
    ├── js/                         <- Biometric neural network scripts & UI handlers
    ├── lib/                        <- Bootstrap, jQuery, SweetAlert2
    └── models/                     <- face-api.js neural network weight shards (All 7 models)
```
