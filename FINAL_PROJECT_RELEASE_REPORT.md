# Master Release Report — Apex Attendance .NET Project

**Project Name:** Apex Attendance — Biometric Employee Attendance Management System  
**Framework:** ASP.NET Core .NET 8 (C# 12)  
**Frontend & AI:** HTML5, Bootstrap 5.3, JavaScript, face-api.js (TinyFaceDetector, 68-Point Landmarks, 128D Descriptors)  
**Student Details:** Anubhav | Enrollment No: `92400120380`  
**Institution:** Faculty of Engineering and Technology, Department of Computer Engineering, Marwadi University  
**Subject:** .NET Programming Mini-Project  
**Evaluation:** Mini-Project Assessment  
**Timestamp:** November 22, 2026  
**Status:** **SUCCESSFULLY AUDITED, COMMITTED, AND RELEASED**

---

## 1. Executive Summary & Verification Matrix

| Milestone / Requirement | Target Specification | Achieved Status | Verification Evidence |
| :--- | :--- | :--- | :--- |
| **Project Codebase Audit** | Full audit, 0 warnings/errors | **VERIFIED** | Release build passed: `EmployeeAttendanceApp.dll` |
| **All 7 Biometric Models** | Bundled in `wwwroot/models/` | **VERIFIED** | 7 neural model files loaded and validated |
| **GitHub Repository** | `Apex-Attendance-DotNet` (Private) | **LIVE & PUSHED** | `https://github.com/Anubhavg1123/Apex-Attendance-DotNet` |
| **Active Default Branch** | `main` | **VERIFIED** | Commit `7d4f5eb` tracked on `origin/main` |
| **Security & Privacy** | No live biometric/employee leak | **VERIFIED** | `App_Data/attendance_database.json` excluded via `.gitignore` |
| **Clean Schema Template** | Sanitized sample database | **VERIFIED** | `App_Data/attendance_database.sample.json` committed |
| **Docker Packaging** | .NET 8 SDK + ASP.NET 8 Runtime | **VERIFIED** | Production multi-stage `Dockerfile` and `railway.json` |
| **Railway Persistence** | Mountable volume at `/data` | **CONFIGURED** | Dynamic `DATA_DIR` support with seamless fallback |
| **InfinityFree Compatibility** | No .NET-to-PHP rewrite; static hub | **COMPLETED** | Dedicated static landing page + PDF download link |
| **Academic PDF Report** | Marwadi University format (3–4 pgs) | **GENERATED** | `APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf` (1.87 MB) |
| **Live Route Verification** | Login, Kiosk, Dashboard, Reports | **100% PASS** | HTTP 200 verified on all endpoints via local test harness |

---

## 2. GitHub Repository Details

- **Authoritative GitHub URL:** [https://github.com/Anubhavg1123/Apex-Attendance-DotNet](https://github.com/Anubhavg1123/Apex-Attendance-DotNet)
- **Account:** `Anubhavg1123` (Authenticated via GitHub CLI `gh`)
- **Repository Visibility:** **PRIVATE** (as strictly requested)
- **Primary Branches:** `main` (Default)
- **Latest Commit Hash:** `7d4f5eb`
- **Latest Commit Message:** `docs: add official academic project report PDF, documentation assets, and updated README`

### Included Project Assets on GitHub
1. Visual Studio Solution (`EmployeeAttendanceApp.sln`) & Project (`EmployeeAttendanceApp.csproj`).
2. Controllers (`Account`, `Attendance`, `Dashboard`, `Department`, `Employee`, `Report`, `Setting`, `BiometricApi`).
3. Services (`AttendanceService`, `EmployeeService`, `ReportService`, `AuthService`).
4. Data Tier (`ApplicationDbContext`, `DbInitializer`).
5. Complete Views (Razor MVC views with mobile-responsive kiosk layouts).
6. Web Assets (`wwwroot/css/`, `wwwroot/js/biometric.js`, `wwwroot/js/site.js`).
7. Complete neural face recognition model weights:
   - `tiny_face_detector_model-shard1` & `-weights_manifest.json`
   - `face_landmark_68_model-shard1` & `-weights_manifest.json`
   - `face_recognition_model-shard1`, `-shard2` & `-weights_manifest.json`
8. Containerization & Deploy Configuration:
   - `Dockerfile` (Multi-stage .NET 8 build)
   - `railway.json` (Railway builder & restart policy)
9. Sanitized template schema (`App_Data/attendance_database.sample.json`).
10. Official Academic PDF Report (`docs/APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf` and root `APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`).
11. UI Screenshots (`docs/screenshots/`).

---

## 3. Railway Cloud Deployment Architecture

- **Official Platform:** [https://railway.com/](https://railway.com/)
- **Builder Configuration:** Dockerfile (configured in `railway.json`)
- **Port Binding:** Dynamic port detection via `PORT` environment variable (respects Railway's assigned port and binds Kestrel to `0.0.0.0:${PORT}`).

### Persistent Storage Design (`/data`)
1. **Configurable Path:** `ApplicationDbContext` checks `Environment.GetEnvironmentVariable("DATA_DIR")`.
2. **Local Fallback:** If `DATA_DIR` is not set, it automatically resolves to `Path.Combine(env.ContentRootPath, "App_Data")` without altering local behavior.
3. **Railway Volume:** Mounting a volume at `/data` with environment variable `DATA_DIR=/data` guarantees that:
   - Employee profiles and facial biometric embeddings persist across container redeployments.
   - Attendance timestamps and punches are preserved permanently.
   - Seed data is not re-inserted on restarts (guarded by `DbInitializer.cs` empty-check queries).

### Steps to Complete Railway Public URL Generation
1. Log in to [Railway.com](https://railway.com/) with GitHub.
2. Click **+ New Project** -> **Deploy from GitHub repo**.
3. Select `Anubhavg1123/Apex-Attendance-DotNet`.
4. Under **Settings** -> **Volumes**, click **Add Volume** and set mount path to `/data`.
5. Under **Variables**, add:
   - `DATA_DIR` = `/data`
   - `ASPNETCORE_ENVIRONMENT` = `Production`
6. Under **Networking**, click **Generate Domain** to activate the public HTTPS URL.

---

## 4. InfinityFree Hosting Strategy

- **Architectural Reality:** InfinityFree is a shared Apache/PHP hosting platform. It cannot execute compiled C# / ASP.NET Core Kestrel runtimes.
- **Strict Compliance:**
  - Did NOT rewrite the .NET backend into PHP.
  - Did NOT attempt an incompatible deployment.
  - Protected the existing [Business Connect](https://businessconnect.infinityfreeapp.com) website from any file collisions or overwrites.
- **Created Static Showcase Hub:**  
  A dedicated, high-performance static landing page was created at:  
  `E:\Antigravity_Workspace\Apex_Attendance_Release\InfinityFree_Landing_Page\index.html`  
  Features:
  - Responsive Bootstrap 5.3 project overview.
  - Academic metadata (Marwadi University, Anubhav, 92400120380).
  - Direct download link for `APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`.
  - Clickable button to the GitHub repository.
  - Placeholder button for the Railway deployment URL.

---

## 5. Academic Project Report (PDF) Details

- **Document Name:** `APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`
- **Output Paths:**
  - Primary Submission Copy: `E:\Anubhav_Data_Migration\Projects\Employee_Attendance_Submission\APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`
  - GitHub Repository Copy: `E:\Anubhav_Data_Migration\Projects\Employee_Attendance_Submission\docs\APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`
  - Workspace Copy: `E:\Antigravity_Workspace\Apex_Attendance_Release\Reports\APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`
  - Showcase Page Copy: `E:\Antigravity_Workspace\Apex_Attendance_Release\InfinityFree_Landing_Page\APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`
- **File Size:** **1,874,879 bytes (1.87 MB)**
- **Page Count:** 4 Pages
- **Visual Design Compliance:**
  - Formatted strictly to match the visual aesthetic and typography of the reference `Business_Connect_Mini_Project(1).pdf`.
  - Marwadi University header with NAAC A+ accredited styling.
  - Navy blue headings (`#1E3A8A` / `#0B2545`).
  - Structured technology table.
  - 4-tier system architecture diagram.
  - 6 genuine high-resolution application screenshots captured directly from the live application runtime.
  - Academic & Technical Verification Summary box.
  - Professional running header and footer with student identifier (`Anubhav_92400120380`).

---

## 6. Local Application Endpoint Test Results

Application executed via Kestrel runtime (`http://127.0.0.1:5088`) for automated integration testing:

- `POST /Account/Login`: **HTTP 200** (Authenticated with cookie `ApexAttendance.Auth`)
- `GET /Dashboard/Index`: **HTTP 200** (Rendered real-time metrics, employee attendance ratios)
- `GET /Employee/Index`: **HTTP 200** (Rendered staff directory with biometric enrollment badges)
- `GET /Attendance/Kiosk`: **HTTP 200** (Rendered full-screen webcam scanner & recognition canvas)
- `GET /Attendance/Index`: **HTTP 200** (Rendered attendance punch log & status filter)
- `GET /Report/Index`: **HTTP 200** (Rendered date-range filters & CSV export trigger)
- `GET /Setting/Index`: **HTTP 200** (Rendered shift configurations, grace periods, confidence rules)
- `GET /api/biometric/employees`: **HTTP 200** (Returned active employees for descriptor matching)
- `POST /api/biometric/check-duplicate`: **VERIFIED** (Euclidean anti-collision check functional)
