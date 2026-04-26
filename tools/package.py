#!/usr/bin/env python3
"""
ShieldX Antivirus - Package all project files into a zip
"""
import zipfile, os, shutil

project_dir = "/home/claude/ShieldX_Antivirus"
output_zip  = "/mnt/user-data/outputs/ShieldX_Antivirus_Complete.zip"

os.makedirs("/mnt/user-data/outputs", exist_ok=True)

with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk(project_dir):
        # Skip unwanted dirs
        dirs[:] = [d for d in dirs if d not in ['__pycache__', '.git', 'bin', 'obj']]
        for file in files:
            filepath = os.path.join(root, file)
            arcname  = os.path.relpath(filepath, os.path.dirname(project_dir))
            zf.write(filepath, arcname)
            print(f"  + {arcname}")

print(f"\n✅ ZIP created: {output_zip}")
size_mb = os.path.getsize(output_zip) / 1024 / 1024
print(f"   Size: {size_mb:.2f} MB")
