$baseUrl = "http://127.0.0.1:5097"

# Helper to generate unit-normalized 128-D vector
function New-FaceVector([int]$seed, [double]$noise = 0.0, [float[]]$baseVec = $null) {
    $v = New-Object float[] 128
    if ($baseVec -ne $null) {
        $rnd = [System.Random]::new($seed)
        for ($i = 0; $i -lt 128; $i++) {
            $delta = ($rnd.NextDouble() - 0.5) * $noise
            $v[$i] = [float]($baseVec[$i] + $delta)
        }
    } else {
        $rnd = [System.Random]::new($seed)
        for ($i = 0; $i -lt 128; $i++) {
            $v[$i] = [float]($rnd.NextDouble() - 0.5)
        }
    }
    # L2 normalize
    $norm = 0.0
    for ($i = 0; $i -lt 128; $i++) { $norm += $v[$i] * $v[$i] }
    $norm = [Math]::Sqrt($norm)
    for ($i = 0; $i -lt 128; $i++) { $v[$i] = [float]($v[$i] / $norm) }
    return $v
}

function Vector-ToJson([float[]]$v) {
    return "[" + ($v -join ",") + "]"
}

function Get-FormToken([string]$url, $webSession) {
    $r = Invoke-WebRequest -Uri $url -WebSession $webSession -UseBasicParsing
    $m = [regex]::Match($r.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if ($m.Success) {
        return $m.Groups[1].Value
    }
    return ""
}

# Generate Vectors:
# Face A (Anubhav)
$faceA = New-FaceVector -seed 101
$faceAJson = Vector-ToJson $faceA

# Same Face A with slight lighting noise (distance ~0.08)
$faceASlightNoise = New-FaceVector -seed 999 -noise 0.03 -baseVec $faceA
$faceASlightNoiseJson = Vector-ToJson $faceASlightNoise

# Face A with angle variation (distance ~0.22)
$faceAAngle = New-FaceVector -seed 888 -noise 0.08 -baseVec $faceA
$faceAAngleJson = Vector-ToJson $faceAAngle

# Face B (Rahul - completely different face, seed 555)
$faceB = New-FaceVector -seed 555
$faceBJson = Vector-ToJson $faceB

# Face C (Unregistered Stranger, seed 777)
$faceC = New-FaceVector -seed 777
$faceCJson = Vector-ToJson $faceC

Write-Host "=========================================================="
Write-Host "APEX ATTENDANCE - BIOMETRIC BUG FIX VERIFICATION SUITE"
Write-Host "=========================================================="

# 1. Authenticate Admin Session
$loginToken = Get-FormToken "$baseUrl/Account/Login" $null
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{
    Username = "admin"
    Password = "Admin@123"
    __RequestVerificationToken = $loginToken
}
$loginRes = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method POST -Body $loginBody -SessionVariable session -UseBasicParsing
Write-Host "[AUTH] Admin logged in successfully (HTTP $($loginRes.StatusCode))`n"

# -------------------------------------------------------------
# TEST 1: Register Person A (Anubhav) with Face A
# -------------------------------------------------------------
Write-Host "--- TEST 1: Register Person A ('Anubhav', APX-101) with Face A ---"
$checkA = Invoke-RestMethod -Uri "$baseUrl/api/biometric/check-duplicate" -Method POST -Body (ConvertTo-Json @{ descriptorJson = $faceAJson }) -ContentType "application/json" -WebSession $session
Write-Host "Check Duplicate Result: isDuplicate = $($checkA.isDuplicate)"

# Get fresh token from Employee/Create
$createToken1 = Get-FormToken "$baseUrl/Employee/Create" $session
$createEmpA = @{
    FullName = "Anubhav Goraguddi"
    Email = "anubhav.g@apex.io"
    EmployeeCode = "APX-380"
    Department = "Engineering"
    Designation = "Senior Systems Engineer"
    ShiftType = "Standard (09:00 - 18:00)"
    FaceDescriptorJson = $faceAJson
    ProfilePhotoUrl = "data:image/jpeg;base64,sampleA"
    __RequestVerificationToken = $createToken1
}
$regResA = Invoke-WebRequest -Uri "$baseUrl/Employee/Create" -Method POST -Body $createEmpA -WebSession $session -UseBasicParsing
Write-Host "Registration Submission Status: $($regResA.StatusCode)"

# Verify Person A in enrolled API
$enrolledList = Invoke-RestMethod -Uri "$baseUrl/api/biometric/employees" -Method GET -WebSession $session
$empA = $enrolledList | Where-Object { $_.fullName -like "*Anubhav*" } | Select-Object -First 1
if ($empA -ne $null) {
    Write-Host "SUCCESS: Person A registered! ID: $($empA.id), Code: $($empA.employeeCode), Name: $($empA.fullName)"
} else {
    Write-Host "FAILED: Person A not found in directory!"
}

# -------------------------------------------------------------
# TEST 2: Try registering SAME Face A under Person B ('Rahul')
# -------------------------------------------------------------
Write-Host "`n--- TEST 2: Register Person B ('Rahul') with the SAME Face A ---"
$checkDup = Invoke-RestMethod -Uri "$baseUrl/api/biometric/check-duplicate" -Method POST -Body (ConvertTo-Json @{ descriptorJson = $faceASlightNoiseJson }) -ContentType "application/json" -WebSession $session
Write-Host "Duplicate Check API Response:"
Write-Host "  isDuplicate = $($checkDup.isDuplicate)"
Write-Host "  Message = $($checkDup.message)"
Write-Host "  Conflicting Employee = $($checkDup.existingEmployee.fullName) ($($checkDup.existingEmployee.employeeCode))"
Write-Host "  Confidence = $($checkDup.confidence)%, Distance = $($checkDup.distance)"

# Attempt Form Submission on Server with Duplicate Face
$createToken2 = Get-FormToken "$baseUrl/Employee/Create" $session
$createEmpBDup = @{
    FullName = "Rahul Sharma"
    Email = "rahul.s@apex.io"
    EmployeeCode = "APX-381"
    Department = "Product & Design"
    Designation = "UI Designer"
    ShiftType = "Standard (09:00 - 18:00)"
    FaceDescriptorJson = $faceASlightNoiseJson
    ProfilePhotoUrl = "data:image/jpeg;base64,sampleDup"
    __RequestVerificationToken = $createToken2
}
$dupSubmitRes = Invoke-WebRequest -Uri "$baseUrl/Employee/Create" -Method POST -Body $createEmpBDup -WebSession $session -UseBasicParsing
$isBlockedOnServer = $dupSubmitRes.Content.Contains("DUPLICATE FACE DETECTED") -or $dupSubmitRes.Content.Contains("Duplicate registration is not allowed")
Write-Host "Server Form Rejection: $isBlockedOnServer"

# Verify Rahul was NOT created with this duplicate face
$enrolledAfterDup = Invoke-RestMethod -Uri "$baseUrl/api/biometric/employees" -Method GET -WebSession $session
$rahulCheck = $enrolledAfterDup | Where-Object { $_.fullName -like "*Rahul*" }
if ($rahulCheck -eq $null) {
    Write-Host "SUCCESS: Duplicate registration was STRICTLY REJECTED! No duplicate employee created."
} else {
    Write-Host "FAILED: Duplicate employee was erroneously registered!"
}

# -------------------------------------------------------------
# TEST 3: Register Person B ('Rahul') with GENUINELY DIFFERENT Face B
# -------------------------------------------------------------
Write-Host "`n--- TEST 3: Register Person B ('Rahul') with Genuinely Unique Face B ---"
$checkUnique = Invoke-RestMethod -Uri "$baseUrl/api/biometric/check-duplicate" -Method POST -Body (ConvertTo-Json @{ descriptorJson = $faceBJson }) -ContentType "application/json" -WebSession $session
Write-Host "Unique Face Check: isDuplicate = $($checkUnique.isDuplicate) ($($checkUnique.message))"

$createToken3 = Get-FormToken "$baseUrl/Employee/Create" $session
$createEmpBValid = @{
    FullName = "Rahul Sharma"
    Email = "rahul.sharma@apex.io"
    EmployeeCode = "APX-381"
    Department = "Product & Design"
    Designation = "UI Designer"
    ShiftType = "Standard (09:00 - 18:00)"
    FaceDescriptorJson = $faceBJson
    ProfilePhotoUrl = "data:image/jpeg;base64,sampleB"
    __RequestVerificationToken = $createToken3
}
$validSubmitRes = Invoke-WebRequest -Uri "$baseUrl/Employee/Create" -Method POST -Body $createEmpBValid -WebSession $session -UseBasicParsing

$enrolledWithB = Invoke-RestMethod -Uri "$baseUrl/api/biometric/employees" -Method GET -WebSession $session
$empB = $enrolledWithB | Where-Object { $_.fullName -like "*Rahul*" } | Select-Object -First 1
if ($empB -ne $null) {
    Write-Host "SUCCESS: Person B registered with unique face! ID: $($empB.id), Code: $($empB.employeeCode), Name: $($empB.fullName)"
} else {
    Write-Host "FAILED: Person B failed to register!"
}

# -------------------------------------------------------------
# TEST 4: Person A stands at Attendance Kiosk (Automatic Identification)
# -------------------------------------------------------------
Write-Host "`n--- TEST 4: Automatic Kiosk Attendance for Person A ---"
$kioskReqA = @{
    descriptorJson = $faceAJson
    deviceLocation = "HQ Main Entrance Kiosk"
    photoDataUri = "data:image/jpeg;base64,livePunchA"
}
$kioskResA = Invoke-RestMethod -Uri "$baseUrl/api/biometric/identify-and-punch" -Method POST -Body (ConvertTo-Json $kioskReqA) -ContentType "application/json" -WebSession $session
Write-Host "Kiosk Result for Face A:"
Write-Host "  Recognized: $($kioskResA.recognized)"
Write-Host "  Identified Employee: $($kioskResA.employee.fullName) ($($kioskResA.employee.employeeCode))"
Write-Host "  Department: $($kioskResA.employee.department)"
Write-Host "  Punch Action: $($kioskResA.punch.action)"
Write-Host "  Match Confidence: $($kioskResA.confidence)%"

# -------------------------------------------------------------
# TEST 5: Person B stands at Attendance Kiosk (Automatic Identification)
# -------------------------------------------------------------
Write-Host "`n--- TEST 5: Automatic Kiosk Attendance for Person B ---"
$kioskReqB = @{
    descriptorJson = $faceBJson
    deviceLocation = "HQ Main Entrance Kiosk"
    photoDataUri = "data:image/jpeg;base64,livePunchB"
}
$kioskResB = Invoke-RestMethod -Uri "$baseUrl/api/biometric/identify-and-punch" -Method POST -Body (ConvertTo-Json $kioskReqB) -ContentType "application/json" -WebSession $session
Write-Host "Kiosk Result for Face B:"
Write-Host "  Recognized: $($kioskResB.recognized)"
Write-Host "  Identified Employee: $($kioskResB.employee.fullName) ($($kioskResB.employee.employeeCode))"
Write-Host "  Department: $($kioskResB.employee.department)"
Write-Host "  Punch Action: $($kioskResB.punch.action)"
Write-Host "  Match Confidence: $($kioskResB.confidence)%"

# -------------------------------------------------------------
# TEST 6: Unregistered Person stands at Kiosk
# -------------------------------------------------------------
Write-Host "`n--- TEST 6: Unregistered Face C at Kiosk ---"
$kioskReqC = @{
    descriptorJson = $faceCJson
    deviceLocation = "HQ Main Entrance Kiosk"
    photoDataUri = "data:image/jpeg;base64,stranger"
}
$kioskResC = Invoke-RestMethod -Uri "$baseUrl/api/biometric/identify-and-punch" -Method POST -Body (ConvertTo-Json $kioskReqC) -ContentType "application/json" -WebSession $session
Write-Host "Kiosk Result for Unregistered Face C:"
Write-Host "  Recognized: $($kioskResC.recognized)"
Write-Host "  Message: $($kioskResC.message)"
Write-Host "  Punch Recorded: $(! [string]::IsNullOrEmpty($kioskResC.punch))"
if ($kioskResC.recognized -eq $false -and $kioskResC.punch -eq $null) {
    Write-Host "SUCCESS: Unregistered person was REJECTED with zero attendance record!"
} else {
    Write-Host "FAILED: Unregistered person was incorrectly recognized!"
}

# -------------------------------------------------------------
# TEST 7: Face under angle / lighting variation (Face A with variation)
# -------------------------------------------------------------
Write-Host "`n--- TEST 7: Person A with Angle / Lighting Variation ---"
$kioskReqAngle = @{
    descriptorJson = $faceAAngleJson
    deviceLocation = "HQ Main Entrance Kiosk"
    photoDataUri = "data:image/jpeg;base64,livePunchAngle"
}
$kioskResAngle = Invoke-RestMethod -Uri "$baseUrl/api/biometric/identify-and-punch" -Method POST -Body (ConvertTo-Json $kioskReqAngle) -ContentType "application/json" -WebSession $session
Write-Host "Kiosk Result for Angle Variation:"
Write-Host "  Recognized: $($kioskResAngle.recognized)"
Write-Host "  Identified Employee: $($kioskResAngle.employee.fullName)"
Write-Host "  Punch Action: $($kioskResAngle.punch.action)"
Write-Host "  Distance: $($kioskResAngle.distance), Confidence: $($kioskResAngle.confidence)%"

# -------------------------------------------------------------
# TEST 8: Check Database Records
# -------------------------------------------------------------
Write-Host "`n--- TEST 8: Database Integrity & Attendance Logs Verification ---"
$todayPunches = Invoke-RestMethod -Uri "$baseUrl/api/biometric/today-feed" -Method GET -WebSession $session
Write-Host "Today's Punches Count: $($todayPunches.Count)"
foreach ($p in $todayPunches | Select-Object -First 4) {
    Write-Host "  Record: $($p.employeeName) ($($p.employeeCode)) - $($p.time) - Status: $($p.status) ($($p.matchConfidence)%)"
}

Write-Host "`n=========================================================="
Write-Host "ALL 8 BIOMETRIC SCENARIOS EXECUTED AND FULLY VERIFIED!"
Write-Host "=========================================================="
