; ShieldX Professional Antivirus Simplified Installer
!include "MUI2.nsh"

; Configuration
!define PRODUCT_NAME "ShieldX Professional Antivirus"
!define PRODUCT_VERSION "3.1.1"
!define PRODUCT_PUBLISHER "SYED HAMZA ALI SHAH"

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

; Installer attributes
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "output\ShieldX_Professional_v${PRODUCT_VERSION}_Setup.exe"
InstallDir "$PROGRAMFILES\ShieldX"
ShowInstDetails show

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Version Information
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}.0"

; Sections
Section "Install ShieldX"
  SetOutPath "$INSTDIR"
  
  ; Extract payload from ZIP
  File /r "payload\ShieldX.zip"
  
  ; Create Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\ShieldX.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
  
  ; Create Desktop shortcut
  CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\ShieldX.exe"
  
  ; Write uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ; Registry entries
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX" "DisplayVersion" "${PRODUCT_VERSION}"
  
  MessageBox MB_OK "ShieldX Professional Antivirus v${PRODUCT_VERSION} installed successfully!"
SectionEnd

; Uninstaller Section
Section "Uninstall"
  ; Remove Program Files
  RMDir /r "$INSTDIR"
  
  ; Remove shortcuts
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk"
  RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  
  ; Remove Registry entries
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
  
  MessageBox MB_OK "ShieldX Professional Antivirus has been uninstalled."
SectionEnd
