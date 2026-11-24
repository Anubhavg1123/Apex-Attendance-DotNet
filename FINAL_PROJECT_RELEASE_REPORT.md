# Final Master Release & Deployment Report — Apex Attendance .NET Project (Vercel Production)

**Project Name:** Apex Attendance — Biometric Employee Attendance Management System  
**Framework:** ASP.NET Core .NET 8 (C# 12)  
**Frontend & AI:** HTML5, Bootstrap 5.3, JavaScript, face-api.js (TinyFaceDetector, 68-Point Landmarks, 128D Descriptors)  
**Student Details:** Anubhav | Enrollment No: `92400120380`  
**Institution:** Faculty of Engineering and Technology, Department of Computer Engineering, Marwadi University  
**Subject:** .NET Programming Mini-Project  
**Evaluation:** Mini-Project Assessment  
**Timestamp:** November 24, 2026  
**Status:** **DEPLOYED TO VERCEL PRODUCTION, VERIFIED & COMMITTED TO MAIN**

---

## 1. Executive Summary: Live Production Deployment

```
========================================================================================
STATUS CLASSIFICATION & LIVE VERCEL PRODUCTION VERIFICATION
========================================================================================
[LIVE]    Vercel Production URL: https://apex-attendance-dotnet.vercel.app
[LIVE]    Vercel Project Scope: anubhavg1123s-projects / apex-attendance-dotnet
[LIVE]    Business Connect Integrity: https://business-connect-one.vercel.app UNTOUCHED
[LIVE]    Container Engine: Vercel OCI Container Engine (Buildah + Fluid Compute)
[LIVE]    Zero-Leak Biometric Guard: /api/biometric/employees masks face vectors for guests
[LIVE]    Server-Side Kiosk Face Matching: Kiosk records attendance with 0 descriptor leakage
[LIVE]    Neural Models Loaded: face_recognition & tiny_face_detector shards active (HTTP 200)
[LIVE]    Admin Authentication: Login verified, Dashboard verified (HTTP 200)
[LIVE]    Single Academic PDF Preserved: APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf (4 pages, 1.87 MB)
[LIVE]    GitHub Private Repository: https://github.com/Anubhavg1123/Apex-Attendance-DotNet
========================================================================================
```

---

## 2. Live Vercel Deployment Architecture & Verification

- **Production URL:** [https://apex-attendance-dotnet.vercel.app](https://apex-attendance-dotnet.vercel.app)
- **Deployment ID:** `dpl_F9H1oKrPzsndFbeBm42RxnUX657K`
- **Framework Specification:** `container` ([vercel.json](vercel.json))
- **Container Build Definition:** Multi-stage build in [Dockerfile](Dockerfile):
  - Stage 1: `mcr.microsoft.com/dotnet/sdk:8.0` (Build & Release publish)
  - Stage 2: `mcr.microsoft.com/dotnet/aspnet:8.0` (Runtime)
- **SSL & Proxy Termination:** Configured `ForwardedHeaders` in [Program.cs](Program.cs) to handle Vercel edge reverse-proxy HTTPS headers.
- **Static Asset Serving:** Enabled `ServeUnknownFileTypes = true` in Kestrel static file options to serve neural network binary shard weights (`-shard1`, `-shard2`).

### Verified Live Endpoints

| Endpoint | HTTP Status | Response Verification |
| :--- | :--- | :--- |
| `GET /` | `302 Found` | Correctly redirects to `https://apex-attendance-dotnet.vercel.app/Account/Login?ReturnUrl=%2F` |
| `GET /Account/Login` | `200 OK` | Login page rendered with anti-forgery token and zero credential hints |
| `POST /Account/Login` | `200 OK` | Authenticates with `ADMIN_PASSWORD` and establishes persistent cookie |
| `GET /Dashboard` | `200 OK` | Executive dashboard rendered for authenticated admin |
| `GET /Attendance/Kiosk` | `200 OK` | Live autonomous biometric kiosk interface active |
| `GET /models/tiny_face_detector_model-shard1` | `200 OK` | 193,321 bytes neural model weights delivered directly from Vercel edge |
| `GET /models/face_recognition_model-shard1` | `200 OK` | 4,194,304 bytes 128-D biometric recognition model weights delivered |
| `GET /api/biometric/policy` | `200 OK` | JSON attendance policy delivered (`shiftStartTime: 09:00`, `requiredMatchConfidence: 90`) |
| `GET /api/biometric/employees` | `200 OK` | All 5 employee records delivered with `faceDescriptorJson: null` (Biometric Leak Guard) |

---

## 3. Database Persistence Analysis (Honest Technical Disclosure)

The original application architecture utilizes a thread-safe JSON file database (`ApplicationDbContext`) managed by `ReaderWriterLockSlim`.

- **Vercel Container Storage Characteristics:** Vercel containers execute on Fluid Compute as scale-to-zero serverless instances. The container filesystem (`/tmp`) is ephemeral.
- **Active Instance Behavior:** Employee registrations and attendance punches made during an active container instance persist in `/tmp/attendance_database.json` for the duration of that instance.
- **Cold-Start / Redeployment Reset:** When the container scales to zero due to inactivity or a new deployment is promoted, the ephemeral container filesystem is recycled to the initial seeded template state.
- **Production Recommendation:** For permanent cloud database durability on Vercel without instance resets, migrate `ApplicationDbContext` to an external managed relational database (such as Vercel Postgres, Supabase, Neon, or Azure SQL). Real employee database files are strictly excluded from git tracking.

---

## 4. Production Security & Biometric Privacy

- **Dynamic Administrator Secret:** Configured via `ADMIN_PASSWORD` in Vercel environment variables. In Production, development fallbacks are disabled.
- **Zero-Leak Biometric Guard:** Unauthenticated calls to `/api/biometric/employees` receive masked `faceDescriptorJson = null` values, preventing unauthorized scraping of mathematical facial identities.
- **Server-Side Kiosk Face Matching:** Live camera descriptors captured at `/Attendance/Kiosk` are matched server-side via `/api/biometric/identify-and-punch`. No stored biometric vectors are transmitted to public browsers.
- **Duplicate Prevention:** Vector Euclidean distance threshold `<= 0.38` blocks attempts to register identical faces across different employee IDs.
- **No Secrets in Git:** Sensitive data, environment secrets, and real biometric databases are strictly excluded via `.gitignore` and `.vercelignore`.

---

## 5. Single Canonical Academic PDF Verification

- **Preservation Rule Enforced:** Exactly **ONE** academic PDF exists in the project. Zero duplicate, temporary, or redundant progress PDFs were created.
- **Canonical File:** [`APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`](APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf)
- **File Size:** **1,874,879 bytes (1.87 MB)**
- **Page Count:** Exactly **4 Pages** (Formatted per Marwadi University Department of Computer Engineering requirements).
- **Contents:** Student enrollment `92400120380`, 4-tier architecture diagram, 6 live application UI screenshots, and active project links.

---

## 6. GitHub Repository Details

- **Repository:** [https://github.com/Anubhavg1123/Apex-Attendance-DotNet](https://github.com/Anubhavg1123/Apex-Attendance-DotNet)
- **Visibility:** **PRIVATE**
- **Default Branch:** `main`
- **Scope Isolation:** Existing `business-connect` project ([https://business-connect-one.vercel.app](https://business-connect-one.vercel.app)) remains completely untouched.
