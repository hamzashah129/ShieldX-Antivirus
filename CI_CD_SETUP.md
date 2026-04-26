# CI/CD Pipeline Setup Guide

**Status**: ✅ Complete  
**Last Updated**: April 26, 2026

---

## Overview

ShieldX now includes a complete CI/CD pipeline that:
- ✅ Builds on every push
- ✅ Runs tests automatically
- ✅ Signs executables with code signing certificate
- ✅ Creates release packages
- ✅ Publishes to GitHub Releases

---

## Quick Start

### Local Testing (Before GitHub)

```powershell
# Test full CI/CD pipeline locally
.\local-ci.ps1 -Stage all

# Test individual stages
.\local-ci.ps1 -Stage build
.\local-ci.ps1 -Stage test
.\local-ci.ps1 -Stage release
```

### Version Management

```powershell
# Show current version
.\manage-version.ps1

# Increment version (patch, minor, or major)
.\manage-version.ps1 -Action increment -Type patch
# v3.1.1 → v3.1.2

# Set specific version
.\manage-version.ps1 -Action set -Version "3.2.0"

# Create git tag for release
.\manage-version.ps1 -Action tag
# Creates tag: v3.1.2
```

---

## GitHub Setup

### Step 1: Push to GitHub

```powershell
# Initialize git (already done)
git init

# Add all files
git add .

# Create initial commit
git commit -m "Initial commit: ShieldX v3.1.1"

# Add GitHub remote
git remote add origin https://github.com/YOUR_USERNAME/ShieldX_Antivirus.git

# Push to GitHub
git branch -M main
git push -u origin main
```

### Step 2: Add Secrets to GitHub

Go to: **GitHub Repository → Settings → Secrets and variables → Actions**

Add two secrets:

```
CODE_SIGNING_CERT    = [Base64-encoded PFX file]
CERT_PASSWORD        = ShieldX@2026
```

**To encode certificate:**

```powershell
$cert = [System.IO.File]::ReadAllBytes("certificates\ShieldX_CodeSigning.pfx")
$b64 = [Convert]::ToBase64String($cert)
$b64 | Set-Clipboard

# Then paste into GitHub Secrets as CODE_SIGNING_CERT
```

### Step 3: Create Release

```powershell
# Increment version locally
.\manage-version.ps1 -Action increment -Type patch

# Commit changes
git add version.json
git commit -m "Bump version to v3.1.2"

# Create tag (this triggers GitHub Actions release workflow)
.\manage-version.ps1 -Action tag

# Push commits and tags
git push
git push --tags
```

GitHub Actions will automatically:
1. Build the application
2. Run tests
3. Sign the executable
4. Create release packages
5. Publish to GitHub Releases

---

## Workflows Explained

### Build Workflow (`build.yml`)

**Trigger**: On every push to `main` or `develop`  
**Actions**:
1. Checkout code
2. Setup .NET 8.0
3. Restore NuGet packages
4. Build release configuration
5. Run tests (if available)
6. Publish application
7. Upload artifacts

**Status**: Shows in commit status checks

### Release Workflow (`release.yml`)

**Trigger**: When tag is pushed (`v*.*.*` format)  
**Actions**:
1. Build application
2. Import code signing certificate
3. Sign executable with SHA256
4. Create installer package
5. Create portable ZIP
6. Generate release notes
7. Publish to GitHub Releases
8. Create downloadable artifacts

**Outputs**: 
- `ShieldX_Professional_v3.1.1_Setup.exe` (signed)
- `ShieldX_Professional_v3.1.1_Portable.zip`

---

## Local CI/CD Script

### `local-ci.ps1` - Test Pipeline Locally

```powershell
# Run all stages (build → test → release)
.\local-ci.ps1 -Stage all

# Run specific stage
.\local-ci.ps1 -Stage build      # Just build
.\local-ci.ps1 -Stage test       # Just test
.\local-ci.ps1 -Stage release    # Build + sign + package
```

**Benefits**:
- Test before pushing to GitHub
- Verify code signing works
- Catch errors early
- No GitHub Actions consumption

---

## Version Management Script

### `manage-version.ps1` - Version Automation

**Show current version:**
```powershell
.\manage-version.ps1
# Output: Current Version: 3.1.1
#         Next Versions:
#           Patch: 3.1.2
#           Minor: 3.2.0
#           Major: 4.0.0
```

**Increment version:**
```powershell
# Patch increment (3.1.1 → 3.1.2)
.\manage-version.ps1 -Action increment -Type patch

# Minor increment (3.1.1 → 3.2.0)
.\manage-version.ps1 -Action increment -Type minor

# Major increment (3.1.1 → 4.0.0)
.\manage-version.ps1 -Action increment -Type major
```

**Set specific version:**
```powershell
.\manage-version.ps1 -Action set -Version "3.2.0"
```

**Create release tag:**
```powershell
.\manage-version.ps1 -Action tag
# Output: ✅ Tag created: v3.1.2
#         To push release to GitHub:
#           git push origin v3.1.2
```

---

## Release Process (Complete Example)

### Full Release Workflow

```powershell
# 1. Update version
.\manage-version.ps1 -Action increment -Type minor
# v3.1.1 → v3.2.0

# 2. Test locally
.\local-ci.ps1 -Stage all
# Build, test, sign, and package locally

# 3. Review release
.\releases  # Check output files
Get-ChildItem releases

# 4. Commit version bump
git add version.json
git commit -m "Prepare v3.2.0 release"

# 5. Create git tag (triggers GitHub Actions)
.\manage-version.ps1 -Action tag
# Creates: v3.2.0

# 6. Push to GitHub (triggers release workflow)
git push
git push --tags

# 7. Monitor GitHub Actions
# GitHub automatically builds, signs, and publishes release
```

### GitHub Release Page

After pushing tag, GitHub automatically:
- Builds and signs executable
- Creates release with:
  - Release notes
  - Installer (signed)
  - Portable ZIP
  - All downloadable

---

## GitHub Actions Setup (First Time)

### Enable GitHub Actions

1. Go to **Repository → Actions**
2. Click **"I understand my workflows, go ahead and enable them"**
3. Workflows are now active

### Monitor Build Status

**In commits**: Green checkmark = build passed  
**In pull requests**: Shows test results  
**In Actions tab**: Full build logs

### Troubleshooting GitHub Actions

**Build Failed?**
- Click failed check → Details
- Scroll to "Logs" section
- Review error messages
- Common issues:
  - Test failures (fix and push again)
  - Missing certificate (add secrets)
  - .NET version mismatch

---

## Development Workflow

### Recommended Git Workflow

```
main (production releases)
  └─── develop (integration branch)
         └─── feature/feature-name (feature branches)

Branching:
1. Create feature branch from develop
   git checkout -b feature/my-feature develop

2. Develop and test locally
   .\local-ci.ps1 -Stage all

3. Commit and push
   git add .
   git commit -m "Add feature X"
   git push origin feature/my-feature

4. Create Pull Request (GitHub)
   - Automated tests run
   - Review and merge when ready

5. Merge to main for release
   - Update version
   - Create tag
   - Push to trigger release workflow
```

---

## Continuous Deployment Details

### Build Triggers

| Event | Workflow | Output |
|-------|----------|--------|
| Push to `main` | Build & Test | Artifact upload |
| Push to `develop` | Build & Test | Artifact upload |
| Tag push `v*.*.*` | **Release** | GitHub Release + Sign + Publish |
| Pull Request | Build & Test | Status check |

### Secret Requirements

| Secret | Value | Used For |
|--------|-------|----------|
| `CODE_SIGNING_CERT` | Base64 PFX | Code signing (release) |
| `CERT_PASSWORD` | Certificate password | Code signing (release) |

---

## Artifacts & Storage

### Build Artifacts
- Stored 7 days
- Available on Actions → [Workflow] → Artifacts
- Contains compiled application

### Release Artifacts
- Stored 30 days
- Published to GitHub Releases
- Permanently available in Releases tab

---

## Performance & Optimization

### Build Times

- **Build only**: ~3-5 minutes
- **With tests**: ~5-8 minutes
- **Release build**: ~8-12 minutes

### GitHub Actions Limits (Free Tier)

- 2,000 build minutes/month
- No concurrent builds
- Enough for 50+ releases per month

---

## Troubleshooting

### Issue: Build fails with certificate error

**Solution**: 
```powershell
# Re-encode certificate
$cert = [System.IO.File]::ReadAllBytes("certificates\ShieldX_CodeSigning.pfx")
$b64 = [Convert]::ToBase64String($cert)
$b64 | Set-Clipboard

# Update GitHub secret CODE_SIGNING_CERT
```

### Issue: Tests fail on GitHub but pass locally

**Causes**:
- Environmental differences
- Missing dependencies on runner
- Path issues (use `./` not `..\`)

**Solution**:
```powershell
# Check build logs
# Run locally with exact same commands
# Fix and push again
```

### Issue: Code signing fails

**Ensure**:
- `CODE_SIGNING_CERT` secret contains base64-encoded PFX
- `CERT_PASSWORD` matches certificate password
- Certificate file is valid (test locally first)

---

## Best Practices

✅ **DO**:
- Test locally with `.\local-ci.ps1` before pushing
- Use semantic versioning (X.Y.Z)
- Create descriptive commit messages
- Review changes before merging to main
- Keep certificate password in GitHub Secrets

❌ **DON'T**:
- Push directly to main (use develop + PR)
- Commit PFX files to repository
- Store passwords in code or commit messages
- Force push (affects history)

---

## Next Steps

1. **Initialize Git & Push**
   ```powershell
   git remote add origin https://github.com/YOUR_USERNAME/ShieldX_Antivirus.git
   git push -u origin main
   ```

2. **Add Secrets**
   - Go to GitHub → Settings → Secrets
   - Add `CODE_SIGNING_CERT` and `CERT_PASSWORD`

3. **Test Release**
   ```powershell
   .\manage-version.ps1 -Action increment -Type patch
   .\manage-version.ps1 -Action tag
   git push --tags
   ```

4. **Monitor**
   - Go to GitHub Actions tab
   - Watch release workflow execute
   - Download from Releases when complete

---

## References

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [softprops/action-gh-release](https://github.com/softprops/action-gh-release)
- [dotnet CLI Reference](https://learn.microsoft.com/dotnet/core/tools/)
- [SignTool Documentation](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool)

---

**Pipeline Status**: ✅ Ready for Production  
**Last Tested**: April 26, 2026
