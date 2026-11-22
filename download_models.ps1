$dest = Join-Path $PSScriptRoot "wwwroot\models"
if (-not (Test-Path $dest)) {
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
}

$baseUrl = "https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"
$files = @(
    "tiny_face_detector_model-weights_manifest.json",
    "tiny_face_detector_model-shard1",
    "face_landmark_68_model-weights_manifest.json",
    "face_landmark_68_model-shard1",
    "face_recognition_model-weights_manifest.json",
    "face_recognition_model-shard1",
    "face_recognition_model-shard2"
)

foreach ($f in $files) {
    $targetPath = Join-Path $dest $f
    if (-not (Test-Path $targetPath)) {
        Write-Host "Downloading $f..."
        try {
            Invoke-WebRequest -Uri "$baseUrl/$f" -OutFile $targetPath -UseBasicParsing -TimeoutSec 15
            Write-Host "Saved $f ($((Get-Item $targetPath).Length) bytes)"
        } catch {
            Write-Warning "Failed to download $f from GitHub: $_"
        }
    } else {
        Write-Host "Already exists: $f"
    }
}
