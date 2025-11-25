# CI/CD Pipeline Documentation

## Overview
Automated GitHub Actions pipeline that builds, tests, and deploys the SWGOH-Raid Scraper to an Azure VM.

## Pipeline Stages

### Build
1. Checkout code
2. Setup .NET 8.0 SDK
3. Restore dependencies
4. Build solution in Release mode
5. Publish artifacts

```bash
dotnet build SWGOH-Raid_Scraper.sln --configuration Release --no-restore
```

### Test
Runs xUnit tests to validate functionality before deployment.

**Tests:**
- Raid table HTML parsing
- Non-contributor detection
- Discord message formatting
- Error handling

```bash
dotnet test SWGOH-Raid_Scraper.sln --configuration Release --no-build
```

### Deploy
Deploys to Azure VM via SSH.

1. Copy build artifacts using SCP
2. Stop existing application
3. Start new instance

```bash
cd ~/swgoh-raid-scraper
pkill -f "SWGOH_Raid_Scraper.dll" || true
nohup dotnet SWGOH_Raid_Scraper.dll > log.txt 2>&1 &
```

## Azure VM Setup

**Configuration:**
- OS: Ubuntu LTS
- Runtime: .NET 8.0
- Deploy Path: `~/swgoh-raid-scraper`

**Authentication:**
Uses GitHub Secrets for SSH deployment:
- `AZURE_VM_HOST`
- `AZURE_VM_USERNAME`
- `AZURE_VM_KEY`
