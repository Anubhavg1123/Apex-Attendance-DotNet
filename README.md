# Apex Attendance — Biometric Employee Attendance Management System

[![Framework](https://img.shields.io/badge/Framework-.NET%208%20ASP.NET%20Core-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%2012-239120?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Biometrics](https://img.shields.io/badge/AI%20Biometrics-face--api.js%20(TinyFace)-FF6F00)](https://github.com/justadudewhohacks/face-api.js)
[![Container](https://img.shields.io/badge/Deployment-Docker%20%7C%20Railway-0B2545?logo=docker)](https://railway.com/)
[![License](https://img.shields.io/badge/License-Academic%20Evaluation-blue)](#academic-metadata)

> **Enterprise-grade ASP.NET Core .NET 8 employee attendance management platform utilizing client-side 68-point neural facial landmark extraction, 128-dimensional Euclidean feature matching, automated kiosk punch processing, and thread-safe persistent data storage.**

---

## 🏛️ Academic Metadata

- **Institution:** Marwadi University, Rajkot
- **Faculty:** Faculty of Engineering and Technology
- **Department:** Department of Computer Engineering
- **Subject:** .NET Programming Mini-Project
- **Author:** Anubhav
- **Enrollment Number:** `92400120380`
- **Official Academic Report:** [`docs/APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`](docs/APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf)

---

## 🌟 Key Features & Architectural Capabilities

### 1. 📷 Autonomous Biometric Attendance Kiosk
- Client-side neural computer vision using **face-api.js** (`tiny_face_detector`, `face_landmark_68`, `face_recognition`).
- Sub-second 128-dimensional floating point descriptor generation directly within the browser viewport.
- Autonomous face recognition and punch dispatch without requiring physical badge contact or PIN inputs.

### 2. 🛡️ Duplicate Face Registration & Anti-Spoofing Guard
- Real-time pre-enrollment similarity validation via `/api/biometric/check-duplicate`.
- Calculates Euclidean vector distance across all registered personnel; rejects enrollments with similarity distance `< 0.45` to prevent multi-identity collusion.

### 3. ⏱️ Intelligent Shift & Punch Lifecycle
- Automatically determines punch type: **Check-In** on first daily scan; **Check-Out** on subsequent scans.
- Enforces shift parameters (09:00–18:00 default) with configurable late arrival grace periods (15 minutes).
- Automatic calculation of standard hours, overtime, and half-day rules.

### 4. 🗄️ Thread-Safe High-Performance Data Persistence
- Lock-managed singleton data context (`ApplicationDbContext`) utilizing `ReaderWriterLockSlim` for atomic read/write concurrency.
- Atomic commit file staging (`attendance_database.json.tmp` -> `attendance_database.json`) preventing data corruption.
- Cloud volume compatibility: respects `DATA_DIR` environment variable (e.g., `/data` on Railway persistent volumes).

### 5. 📊 Executive Dashboard & Audit Reports
- Live attendance overview: present/absent ratios, on-premise headcount, and real-time punch activity feed.
- Departmental segmentation and attendance percentages.
- Historical attendance audit logs with date-range filters and one-click **CSV Export**.

---

## 🛠️ Verified Technology Stack

| Layer | Component | Specification |
| :--- | :--- | :--- |
| **Backend Framework** | ASP.NET Core .NET 8 (C# 12) | Core MVC, RESTful Biometrics API, Cookie Authentication |
| **Computer Vision Engine** | face-api.js / TinyFaceDetector | 68-point facial landmarks, 128D embeddings, Euclidean matching |
| **Frontend Styling** | Razor Views + Bootstrap 5.3 | Mobile-responsive kiosk, modal overlays, analytics cards |
| **Persistence Engine** | Thread-Safe JSON Engine | `ReaderWriterLockSlim`, atomic file swap, volume-ready |
| **Containerization** | Docker Multi-Stage Build | .NET 8 SDK (`build`) + ASP.NET 8 Runtime (`final`) |
| **Cloud Deployment** | Railway Cloud | Configurable Kestrel `PORT`, persistent volume at `/data` |

---

## 🚀 Quickstart & Local Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.400 or higher)
- Modern web browser with WebRTC/camera support (Chrome, Edge, Firefox)

### 1. Clone the Repository
```bash
git clone https://github.com/Anubhavg1123/Apex-Attendance-DotNet.git
cd Apex-Attendance-DotNet
```

### 2. Verify Biometric Model Weights
The 7 neural network model weights are bundled directly in `wwwroot/models/`:
- `face_landmark_68_model-shard1` & `-weights_manifest.json`
- `face_recognition_model-shard1`, `-shard2` & `-weights_manifest.json`
- `tiny_face_detector_model-shard1` & `-weights_manifest.json`

### 3. Build & Run
```bash
# Restore dependencies and build solution in Release mode
dotnet build -c Release

# Start application server
dotnet run -c Release
```
Navigate to: `http://localhost:5000` (or `https://localhost:5001`)

### 4. Default Administrative Credentials
- **Username:** `admin`
- **Password:** `Admin@123`

---

## 🐳 Docker & Cloud Deployment (Railway)

### Multi-Stage Dockerfile Execution
```bash
docker build -t apex-attendance:latest .
docker run -d -p 8080:8080 -v attendance_data:/data -e DATA_DIR=/data apex-attendance:latest
```

### Railway Deployment Steps
1. Push your repository to GitHub: `https://github.com/Anubhavg1123/Apex-Attendance-DotNet`.
2. In [Railway.com](https://railway.com/), create a **New Project** -> **Deploy from GitHub repo**.
3. Select `Apex-Attendance-DotNet`. Railway will automatically detect `Dockerfile` and `railway.json`.
4. Under **Settings** -> **Volumes**, add a Persistent Volume mounted at `/data`.
5. Set the Environment Variable:
   - `DATA_DIR` = `/data`
   - `ASPNETCORE_ENVIRONMENT` = `Production`
6. Click **Generate Domain** under Networking to obtain your live production HTTPS URL.

---

## 🔒 Security & Biometric Data Privacy

- **Template Exclusion:** The production database containing real employee descriptors and personal records (`App_Data/attendance_database.json`) is strictly excluded from version control via `.gitignore`.
- **Sanitized Schema:** A starter template schema is provided at `App_Data/attendance_database.sample.json`.
- **Client-Side Biometrics:** Raw camera frames are processed entirely client-side in browser memory; only anonymous 128D mathematical vectors are transmitted to verification APIs.

---

## 📄 Project Documentation

- Academic Project Report (PDF): [`docs/APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf`](docs/APEX_ATTENDANCE_DOTNET_MINI_PROJECT_REPORT.pdf)
- Application Screenshots: [`docs/screenshots/`](docs/screenshots/)

---

© 2026 Anubhav (92400120380) | Department of Computer Engineering, Marwadi University
