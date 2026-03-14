Write-Host "Starting Docker Deployment..." -ForegroundColor Green
# Get current date for version (yy.m.d.number)
$currentDate = Get-Date
$year = $currentDate.ToString("yy")
$month = $currentDate.Month
$day = $currentDate.Day
$datePrefix = "$year.$month.$day"

Write-Host "Date: $datePrefix" -ForegroundColor Cyan

# Read and increment version from csproj file
$csprojPath = "SoftMax.Accounting.csproj"
$csprojContent = Get-Content $csprojPath -Raw
if ($csprojContent -match '<Version>(.*?)</Version>') {
    $currentVersion = $matches[1]
    Write-Host "Current version: $currentVersion" -ForegroundColor Cyan
    
    # Parse version (yy.m.d.number)
    $versionParts = $currentVersion -split '\.'
    if ($versionParts.Length -eq 4) {
        $existingDatePrefix = "$($versionParts[0]).$($versionParts[1]).$($versionParts[2])"
        
        # Check if date matches
        if ($existingDatePrefix -eq $datePrefix) {
            # Same day, increment the number
            $buildNumber = [int]$versionParts[3] + 1
        } else {
            # New day, reset to 1
            $buildNumber = 1
        }
        
        $newVersion = "$datePrefix.$buildNumber"
        
        # Update all version tags in csproj
        $csprojContent = $csprojContent -replace "<Version>$currentVersion</Version>", "<Version>$newVersion</Version>"
        $csprojContent = $csprojContent -replace "<AssemblyVersion>$currentVersion</AssemblyVersion>", "<AssemblyVersion>$newVersion</AssemblyVersion>"
        $csprojContent = $csprojContent -replace "<FileVersion>$currentVersion</FileVersion>", "<FileVersion>$newVersion</FileVersion>"
        
        # Save updated csproj
        Set-Content -Path $csprojPath -Value $csprojContent -NoNewline
        
        Write-Host "Version updated to: $newVersion" -ForegroundColor Green
        $VERSION = $newVersion
    } else {
        # Invalid format, create new version with current date
        $newVersion = "$datePrefix.1"
        Write-Host "Creating new version: $newVersion" -ForegroundColor Yellow
        $VERSION = $newVersion
    }
} else {
    $VERSION = "1.0.0"
    Write-Host "Could not read version from csproj, using: $VERSION" -ForegroundColor Yellow
}
$TAG = "latest"
$REGISTRY = "devopsr.soft-max.com"
$IMAGE_NAME = "softmax/softmax.accounting/softmax"
$FULL_IMAGE = "$REGISTRY/$IMAGE_NAME" + ":" + "$TAG"

try {
    Write-Host "Checking for existing image..." -ForegroundColor Cyan
    $existingImage = docker images $FULL_IMAGE --format "{{.Repository}}:{{.Tag}}" | Where-Object { $_ -eq $FULL_IMAGE }
    if ($existingImage) {
        Write-Host "Removing existing image: $FULL_IMAGE" -ForegroundColor Yellow
        docker rmi $FULL_IMAGE --force
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Existing image removed successfully" -ForegroundColor Green
        }
    } else {
        Write-Host "No existing image found" -ForegroundColor Gray
    }

    Write-Host "Building Docker image with registry name..." -ForegroundColor Cyan
    Write-Host "Building: $FULL_IMAGE" -ForegroundColor White
    
    docker build -f Dockerfile -t $FULL_IMAGE .
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed"
    }
    
    $size = docker images $FULL_IMAGE --format "{{.Size}}"
    Write-Host "Image size: $size" -ForegroundColor Yellow
    
    Write-Host "Pushing to registry..." -ForegroundColor Cyan
    Write-Host "Registry: $REGISTRY" -ForegroundColor White
    Write-Host "Image: $IMAGE_NAME" + ":" + "$TAG" -ForegroundColor White
    
    docker push $FULL_IMAGE
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker push failed"
    }
    
    Write-Host "Successfully deployed!" -ForegroundColor Green
    Write-Host "Image: $FULL_IMAGE" -ForegroundColor White
    
} catch {
    Write-Host "Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure you're logged in: docker login $REGISTRY" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
