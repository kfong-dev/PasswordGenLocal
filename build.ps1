# Target: Intel Core i7-13700K on Z690-F chipset (Raptor Lake, Intel 64)
# ISA: MMX, SSE through SSE4.2, SSSE3, AVX, AVX2, FMA3, SHA, AES-NI, PCLMUL,
#       F16C, BMI1, BMI2, ABM, ADX, RDRAND  —  NO AVX-512
# The .NET 8 JIT auto-detects these via CPUID; AVX-512 is disabled in runtimeconfig.template.json.
$msvcPath = "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.50.35717\bin\Hostx64\x64"
$vcToolsInstallDir = "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.50.35717\"
$env:PATH = "$msvcPath;$env:PATH"
$env:VCToolsInstallDir = $vcToolsInstallDir

$binPath = Join-Path $PSScriptRoot "bin"
if (-not (Test-Path $binPath)) { New-Item -ItemType Directory -Path $binPath | Out-Null }

$existing = Get-ChildItem -Path $binPath -Filter "PGLv*.exe" -ErrorAction SilentlyContinue
$highest = 0
foreach ($f in $existing) {
    if ($f.BaseName -match "PGLv(\d+)$") {
        $n = [int]$Matches[1]
        if ($n -gt $highest) { $highest = $n }
    }
}
$next = $highest + 1
$assemblyName = "PGLv$next"

Write-Host "Building $assemblyName (local/enterprise, target: i7-13700K / win-x64)..."

dotnet publish PasswordGenLocal.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $binPath `
    -p:AssemblyName=$assemblyName `
    -p:VCToolsInstallDir=$vcToolsInstallDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}

Write-Host "Build succeeded: bin\$assemblyName.exe" -ForegroundColor Green
