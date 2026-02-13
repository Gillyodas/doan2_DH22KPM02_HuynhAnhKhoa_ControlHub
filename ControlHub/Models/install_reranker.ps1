# ==================================================================
# RERANKER MODEL INSTALLATION SCRIPT
# ==================================================================
# Purpose: Download and setup ms-marco-MiniLM-L-6-v2 reranker model
# ==================================================================

$ModelsPath = "E:\Project\ControlHub\Models"
$TempPath = "$ModelsPath\temp_reranker"

Write-Host "Starting Reranker Model Installation..." -ForegroundColor Cyan
Write-Host ""

# ==================================================================
# Step 1: Check Prerequisites
# ==================================================================

Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Yellow

# Check Python
try {
    $pythonVersion = python --version 2>&1
    Write-Host "  [OK] Python found: $pythonVersion" -ForegroundColor Green
}
catch {
    Write-Host "  [ERROR] Python not found!" -ForegroundColor Red
    Write-Host "  Please install Python from: https://www.python.org/downloads/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Alternative: Use Option 3 (copy existing model)" -ForegroundColor Cyan
    exit 1
}

# Check pip
try {
    $pipVersion = pip --version 2>&1
    Write-Host "  [OK] pip found: $pipVersion" -ForegroundColor Green
}
catch {
    Write-Host "  [ERROR] pip not found!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ==================================================================
# Step 2: Install Required Packages
# ==================================================================

Write-Host "Step 2: Installing required packages..." -ForegroundColor Yellow
Write-Host "  (This may take 2-3 minutes)" -ForegroundColor Gray

pip install --quiet optimum[onnxruntime] transformers torch

if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERROR] Package installation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "  [OK] Packages installed successfully" -ForegroundColor Green
Write-Host ""

# ==================================================================
# Step 3: Download and Convert Model
# ==================================================================

Write-Host "Step 3: Downloading reranker model..." -ForegroundColor Yellow
Write-Host "  Model: cross-encoder/ms-marco-MiniLM-L-6-v2" -ForegroundColor Gray
Write-Host "  (This may take 5-10 minutes depending on internet speed)" -ForegroundColor Gray
Write-Host ""

# Create temp directory
New-Item -ItemType Directory -Path $TempPath -Force | Out-Null

# Python script to download and convert
$pythonScript = @"
from optimum.onnxruntime import ORTModelForSequenceClassification
from transformers import AutoTokenizer
import os

print('Downloading model from Hugging Face...')
model_id = 'cross-encoder/ms-marco-MiniLM-L-6-v2'

try:
    # Download and convert to ONNX
    model = ORTModelForSequenceClassification.from_pretrained(
        model_id, 
        export=True,
        provider='CPUExecutionProvider'
    )
    tokenizer = AutoTokenizer.from_pretrained(model_id)
    
    # Save to temp directory
    output_dir = r'$TempPath'
    model.save_pretrained(output_dir)
    tokenizer.save_pretrained(output_dir)
    
    print('[OK] Model downloaded and converted successfully!')
    
except Exception as e:
    print(f'[ERROR] {e}')
    exit(1)
"@

# Save and run Python script
$scriptPath = "$TempPath\download_model.py"
$pythonScript | Out-File -FilePath $scriptPath -Encoding UTF8

python $scriptPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERROR] Model download failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Alternatives:" -ForegroundColor Yellow
    Write-Host "  1. Check internet connection" -ForegroundColor Gray
    Write-Host "  2. Try manual download from: https://huggingface.co/cross-encoder/ms-marco-MiniLM-L-6-v2" -ForegroundColor Gray
    Write-Host "  3. Use Option 3 (copy existing classifier model)" -ForegroundColor Gray
    exit 1
}

Write-Host ""

# ==================================================================
# Step 4: Copy Files to Models Folder
# ==================================================================

Write-Host "Step 4: Installing model files..." -ForegroundColor Yellow

# Find ONNX model file (might be model.onnx or model_optimized.onnx)
$onnxFile = Get-ChildItem -Path $TempPath -Filter "*.onnx" | Select-Object -First 1

if ($null -eq $onnxFile) {
    Write-Host "  [ERROR] ONNX model file not found in temp directory!" -ForegroundColor Red
    exit 1
}

# Copy files
Copy-Item -Path $onnxFile.FullName -Destination "$ModelsPath\reranker.onnx" -Force
Write-Host "  [OK] Copied: reranker.onnx" -ForegroundColor Green

Copy-Item -Path "$TempPath\vocab.txt" -Destination "$ModelsPath\reranker_vocab.txt" -Force
Write-Host "  [OK] Copied: reranker_vocab.txt" -ForegroundColor Green

Write-Host ""

# ==================================================================
# Step 5: Verify Installation
# ==================================================================

Write-Host "Step 5: Verifying installation..." -ForegroundColor Yellow

$rerankerModel = Get-Item "$ModelsPath\reranker.onnx" -ErrorAction SilentlyContinue
$rerankerVocab = Get-Item "$ModelsPath\reranker_vocab.txt" -ErrorAction SilentlyContinue

if ($null -eq $rerankerModel) {
    Write-Host "  [ERROR] reranker.onnx not found!" -ForegroundColor Red
    exit 1
}

if ($null -eq $rerankerVocab) {
    Write-Host "  [ERROR] reranker_vocab.txt not found!" -ForegroundColor Red
    exit 1
}

$modelSizeMB = [math]::Round($rerankerModel.Length / 1MB, 2)
$vocabSizeKB = [math]::Round($rerankerVocab.Length / 1KB, 2)

Write-Host "  [OK] reranker.onnx - Size: $modelSizeMB MB" -ForegroundColor Green
Write-Host "  [OK] reranker_vocab.txt - Size: $vocabSizeKB KB" -ForegroundColor Green

Write-Host ""

# ==================================================================
# Step 6: Cleanup
# ==================================================================

Write-Host "Step 6: Cleaning up..." -ForegroundColor Yellow

Remove-Item -Path $TempPath -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  [OK] Temporary files removed" -ForegroundColor Green

Write-Host ""

# ==================================================================
# Summary
# ==================================================================

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "INSTALLATION COMPLETE!" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installed Files:" -ForegroundColor Yellow
Write-Host "  - E:\Project\ControlHub\Models\reranker.onnx ($modelSizeMB MB)" -ForegroundColor White
Write-Host "  - E:\Project\ControlHub\Models\reranker_vocab.txt ($vocabSizeKB KB)" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Restart your API:" -ForegroundColor White
Write-Host "     cd E:\Project\ControlHub\src\ControlHub.API" -ForegroundColor Gray
Write-Host "     dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Check logs for:" -ForegroundColor White
Write-Host "     [OK] OnnxReranker initialized with model: E:/Project/ControlHub/Models/reranker.onnx" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Test V3 endpoint at:" -ForegroundColor White
Write-Host "     http://localhost:5000/api/audit/v3/investigate" -ForegroundColor Gray
Write-Host ""
Write-Host "==================================================================" -ForegroundColor Cyan
