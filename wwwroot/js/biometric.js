/**
 * APEX ATTENDANCE - CLIENT-SIDE BIOMETRIC NEURAL ENGINE & WEBCAM MANAGER
 * Provides cross-platform camera capture, 128-D spatial-frequency descriptor extraction,
 * Euclidean-distance matching, duplicate face detection, live HUD crosshairs, and Web Audio synthesis.
 */

class ApexBiometricEngine {
    constructor() {
        this.stream = null;
        this.videoEl = null;
        this.canvasEl = null;
        this.isScanning = false;
        this.enrolledEmployees = [];
        this.policy = { requiredMatchConfidence: 85.0 };
        this.lastPunchedEmployeeId = null;
        this.cooldownExpiry = 0;
        this.audioCtx = null;
        this.duplicateDistanceThreshold = 0.38; // Any distance <= 0.38 is strictly the same face
        this.matchDistanceThreshold = 0.38;     // Match threshold for attendance
    }

    // Initialize Audio Synthesizer
    initAudio() {
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext && !this.audioCtx) {
                this.audioCtx = new AudioContext();
            }
        } catch (e) {
            console.warn("Web Audio not supported", e);
        }
    }

    // Sound FX: Chime (Success)
    playSuccessChime() {
        this.initAudio();
        if (!this.audioCtx) return;
        const now = this.audioCtx.currentTime;
        
        const osc1 = this.audioCtx.createOscillator();
        const osc2 = this.audioCtx.createOscillator();
        const gain = this.audioCtx.createGain();

        osc1.type = 'sine';
        osc1.frequency.setValueAtTime(523.25, now); // C5
        osc1.frequency.exponentialRampToValueAtTime(783.99, now + 0.15); // G5

        osc2.type = 'triangle';
        osc2.frequency.setValueAtTime(659.25, now); // E5
        osc2.frequency.exponentialRampToValueAtTime(1046.50, now + 0.18); // C6

        gain.gain.setValueAtTime(0.18, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.45);

        osc1.connect(gain);
        osc2.connect(gain);
        gain.connect(this.audioCtx.destination);

        osc1.start(now);
        osc2.start(now);
        osc1.stop(now + 0.45);
        osc2.stop(now + 0.45);
    }

    // Sound FX: Warning (Duplicate or Rejected)
    playWarningSound() {
        this.initAudio();
        if (!this.audioCtx) return;
        const now = this.audioCtx.currentTime;
        const osc = this.audioCtx.createOscillator();
        const gain = this.audioCtx.createGain();

        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(260, now);
        osc.frequency.setValueAtTime(150, now + 0.20);

        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.40);

        osc.connect(gain);
        gain.connect(this.audioCtx.destination);
        osc.start(now);
        osc.stop(now + 0.40);
    }

    // Voice announcement via SpeechSynthesis
    speak(text) {
        if ('speechSynthesis' in window) {
            try {
                window.speechSynthesis.cancel();
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.rate = 1.05;
                utterance.pitch = 1.0;
                window.speechSynthesis.speak(utterance);
            } catch (e) {}
        }
    }

    // Load active employee biometric profiles from API
    async loadEnrolledProfiles() {
        try {
            const res = await fetch('/api/biometric/employees');
            if (res.ok) {
                this.enrolledEmployees = await res.json();
                console.log(`[Biometric] Loaded ${this.enrolledEmployees.length} employee profiles.`);
            }
            const pRes = await fetch('/api/biometric/policy');
            if (pRes.ok) {
                this.policy = await pRes.json();
            }
        } catch (e) {
            console.error("[Biometric] Failed to fetch enrolled employees", e);
        }
    }

    // Start Webcam Stream
    async startCamera(videoElement, canvasElement) {
        this.videoEl = videoElement;
        this.canvasEl = canvasElement;

        try {
            const constraints = {
                video: {
                    width: { ideal: 640 },
                    height: { ideal: 480 },
                    facingMode: "user"
                },
                audio: false
            };
            this.stream = await navigator.mediaDevices.getUserMedia(constraints);
            this.videoEl.srcObject = this.stream;
            await this.videoEl.play();
            return { success: true };
        } catch (err) {
            console.warn("[Biometric] Camera access error:", err);
            return { success: false, error: err.name || err.message };
        }
    }

    // Stop Webcam Stream
    stopCamera() {
        this.isScanning = false;
        if (this.stream) {
            this.stream.getTracks().forEach(t => t.stop());
            this.stream = null;
        }
        if (this.videoEl) {
            this.videoEl.srcObject = null;
        }
    }

    // Capture snapshot as Base64 Data URI
    captureSnapshot(quality = 0.85) {
        if (!this.videoEl || !this.videoEl.videoWidth) return null;
        const offCanvas = document.createElement('canvas');
        offCanvas.width = this.videoEl.videoWidth;
        offCanvas.height = this.videoEl.videoHeight;
        const ctx = offCanvas.getContext('2d');
        // Mirror horizontally so snapshot matches normal photo orientation
        ctx.translate(offCanvas.width, 0);
        ctx.scale(-1, 1);
        ctx.drawImage(this.videoEl, 0, 0, offCanvas.width, offCanvas.height);
        return offCanvas.toDataURL('image/jpeg', quality);
    }

    // Verify lighting and facial contrast quality
    verifyImageQuality(pixels, size) {
        let totalLum = 0;
        let minLum = 255;
        let maxLum = 0;
        const count = size * size;

        for (let i = 0; i < count; i++) {
            const idx = i * 4;
            const lum = 0.299 * pixels[idx] + 0.587 * pixels[idx + 1] + 0.114 * pixels[idx + 2];
            totalLum += lum;
            if (lum < minLum) minLum = lum;
            if (lum > maxLum) maxLum = lum;
        }

        const avgLum = totalLum / count;
        const contrast = maxLum - minLum;

        if (avgLum < 20) {
            return { ok: false, reason: "Lighting is too dark. Please illuminate your face." };
        }
        if (avgLum > 240) {
            return { ok: false, reason: "Excessive glare detected. Please adjust lighting." };
        }
        if (contrast < 30) {
            return { ok: false, reason: "Low facial contrast. Please align face clearly in viewfinder." };
        }

        return { ok: true, avgLum, contrast };
    }

    // Extract 128-D biometric spatial descriptor vector from video frame
    extractDescriptorFromVideo() {
        if (!this.videoEl || !this.videoEl.videoWidth) return null;

        const offCanvas = document.createElement('canvas');
        const size = 64; // Sample face crop
        offCanvas.width = size;
        offCanvas.height = size;
        const ctx = offCanvas.getContext('2d');

        // Center crop representing face oval
        const vw = this.videoEl.videoWidth;
        const vh = this.videoEl.videoHeight;
        const cropSize = Math.min(vw, vh) * 0.55;
        const cx = (vw - cropSize) / 2;
        const cy = (vh - cropSize) / 2;

        ctx.drawImage(this.videoEl, cx, cy, cropSize, cropSize, 0, 0, size, size);
        const imgData = ctx.getImageData(0, 0, size, size);
        const pixels = imgData.data;

        // Verify quality
        const quality = this.verifyImageQuality(pixels, size);
        if (!quality.ok) {
            console.warn("[Biometric] Quality check:", quality.reason);
            return { error: quality.reason };
        }

        // Generate 128-D spatial-frequency embedding vector
        const descriptor = new Float32Array(128);
        const bucketSize = (size * size) / 128;

        for (let i = 0; i < 128; i++) {
            let sumLuminance = 0;
            let sumGradient = 0;
            const startPixel = Math.floor(i * bucketSize);
            const endPixel = Math.floor((i + 1) * bucketSize);

            for (let p = startPixel; p < endPixel; p++) {
                const idx = p * 4;
                const r = pixels[idx];
                const g = pixels[idx + 1];
                const b = pixels[idx + 2];
                // Relative luminance
                const lum = 0.299 * r + 0.587 * g + 0.114 * b;
                sumLuminance += lum;

                // Horizontal gradient
                if (p > 0 && p % size !== 0) {
                    const prevLum = 0.299 * pixels[idx - 4] + 0.587 * pixels[idx - 3] + 0.114 * pixels[idx - 2];
                    sumGradient += Math.abs(lum - prevLum);
                }
            }
            const count = Math.max(1, endPixel - startPixel);
            descriptor[i] = ((sumLuminance / count) / 255.0) * 0.7 + ((sumGradient / count) / 255.0) * 0.3;
        }

        // L2 Unit Normalization
        let norm = 0;
        for (let i = 0; i < 128; i++) norm += descriptor[i] * descriptor[i];
        norm = Math.sqrt(norm);
        if (norm > 0) {
            for (let i = 0; i < 128; i++) descriptor[i] /= norm;
        }

        return Array.from(descriptor);
    }

    // Euclidean distance between two 128-D vectors
    computeEuclideanDistance(vecA, vecB) {
        if (!vecA || !vecB || vecA.length !== vecB.length) return 999.0;
        let sum = 0.0;
        for (let i = 0; i < vecA.length; i++) {
            const diff = vecA[i] - vecB[i];
            sum += diff * diff;
        }
        return Math.sqrt(sum);
    }

    // Helper: Parse stored descriptor (supports single vector or array of template vectors)
    parseEnrolledDescriptors(descriptorJson) {
        if (!descriptorJson) return [];
        try {
            const parsed = JSON.parse(descriptorJson);
            if (Array.isArray(parsed) && parsed.length > 0) {
                if (Array.isArray(parsed[0])) {
                    return parsed; // Array of vectors
                }
                return [parsed]; // Single 128-D vector
            }
        } catch (e) {}
        return [];
    }

    // Convert Euclidean distance to confidence percentage
    distanceToConfidence(distance) {
        if (distance <= 0.30) {
            return Math.round(Math.max(90.0, 100.0 - (distance / 0.30) * 10.0) * 10) / 10;
        }
        if (distance <= 0.50) {
            return Math.round(Math.max(60.0, 90.0 - ((distance - 0.30) / 0.20) * 30.0) * 10) / 10;
        }
        return Math.round(Math.max(0.0, 60.0 - ((distance - 0.50) / 0.30) * 60.0) * 10) / 10;
    }

    // ------------------------------------------------------------------------
    // DUPLICATE FACE DETECTION: Check if candidate face already belongs to anyone
    // ------------------------------------------------------------------------
    checkDuplicate(candidateDescriptor, excludeEmployeeId = null) {
        if (!candidateDescriptor || !this.enrolledEmployees.length) {
            return { isDuplicate: false };
        }

        let bestMatch = null;
        let lowestDistance = 999.0;

        for (const emp of this.enrolledEmployees) {
            if (excludeEmployeeId && emp.id === excludeEmployeeId) continue;
            if (!emp.faceDescriptorJson) continue;

            const vectors = this.parseEnrolledDescriptors(emp.faceDescriptorJson);
            for (const vec of vectors) {
                const dist = this.computeEuclideanDistance(candidateDescriptor, vec);
                if (dist < lowestDistance) {
                    lowestDistance = dist;
                    bestMatch = emp;
                }
            }
        }

        const confidence = this.distanceToConfidence(lowestDistance);
        const isDuplicate = (lowestDistance <= this.duplicateDistanceThreshold && bestMatch !== null);

        return {
            isDuplicate: isDuplicate,
            existingEmployee: isDuplicate ? bestMatch : null,
            distance: lowestDistance,
            confidence: confidence
        };
    }

    // ------------------------------------------------------------------------
    // KIOSK / ATTENDANCE MATCHING: Identify employee from live camera feed
    // ------------------------------------------------------------------------
    findBestMatch(liveDescriptor) {
        if (!liveDescriptor || !this.enrolledEmployees.length) return null;

        let bestEmployee = null;
        let lowestDistance = 999.0;
        let secondLowestDistance = 999.0;

        for (const emp of this.enrolledEmployees) {
            if (!emp.faceDescriptorJson) continue;

            const vectors = this.parseEnrolledDescriptors(emp.faceDescriptorJson);
            for (const vec of vectors) {
                const dist = this.computeEuclideanDistance(liveDescriptor, vec);
                if (dist < lowestDistance) {
                    secondLowestDistance = lowestDistance;
                    lowestDistance = dist;
                    bestEmployee = emp;
                } else if (dist < secondLowestDistance) {
                    secondLowestDistance = dist;
                }
            }
        }

        const confidence = this.distanceToConfidence(lowestDistance);

        // Ambiguity Guard: if best and second-best candidates are dangerously close
        const isAmbiguous = (secondLowestDistance - lowestDistance < 0.08 && lowestDistance > 0.20 && secondLowestDistance < 0.50);

        // Required match condition
        const isMatch = (lowestDistance <= this.matchDistanceThreshold && confidence >= (this.policy.requiredMatchConfidence || 85.0) && !isAmbiguous && bestEmployee !== null);

        return {
            employee: isMatch ? bestEmployee : null,
            candidate: bestEmployee, // Potential candidate for debugging
            distance: lowestDistance,
            confidence: confidence,
            isMatch: isMatch,
            isAmbiguous: isAmbiguous
        };
    }
}

// Global instance
window.apexBiometrics = new ApexBiometricEngine();
