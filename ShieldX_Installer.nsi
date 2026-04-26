; ShieldX Professional Antivirus v3.1.0 NSIS Installer Script
; This script creates a professional EXE installer with 32-bit/64-bit selection

!include "MUI2.nsh"
!include "x64.nsh"
!include "nsDialogs.nsh"

; Configuration
!define PRODUCT_NAME "ShieldX Professional Antivirus"
!define PRODUCT_VERSION "3.1.0"
!define PRODUCT_PUBLISHER "SYED HAMZA ALI SHAH"
!define PRODUCT_WEB_SITE "https://shieldx.com"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\ShieldX.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Installer attributes
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "ShieldX_Professional_Antivirus_v3.1.0_Setup.exe"
InstallDir "$PROGRAMFILES\ShieldX Professional Antivirus"
ShowInstDetails show
ShowUnInstDetails show

; Version Information
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}.0"
VIAddVersionKey "FileDescription" "ShieldX Professional Antivirus Setup"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026 ${PRODUCT_PUBLISHER}"

; Variables
Var SelectedArchitecture
Var x86BuildPath
Var x64BuildPath
Var InstallArchitecture

; Installer Sections
Section "ShieldX Professional Antivirus" SEC01
  SetOutPath "$INSTDIR"
  
  ; Determine which architecture was selected and copy files
  ${If} $SelectedArchitecture == "x64"
    SetOutPath "$INSTDIR"
    File /r "bin\Release\net8.0-windows\win-x64\*.*"
    StrCpy $InstallArchitecture "x64"
  ${Else}
    SetOutPath "$INSTDIR"
    File /r "bin\Release\net8.0-windows\win-x86\*.*"
    StrCpy $InstallArchitecture "x86"
  ${EndIf}
  
  ; Create shortcuts
  SetOutPath "$SMPROGRAMS\ShieldX"
  CreateDirectory "$SMPROGRAMS\ShieldX"
  CreateShortCut "$SMPROGRAMS\ShieldX\ShieldX Professional Antivirus.lnk" "$INSTDIR\ShieldX.exe" "" "$INSTDIR\ShieldX.exe" 0
  CreateShortCut "$SMPROGRAMS\ShieldX\Uninstall ShieldX.lnk" "$INSTDIR\Uninstall.exe"
  
  ; Desktop shortcut
  CreateShortCut "$DESKTOP\ShieldX.lnk" "$INSTDIR\ShieldX.exe" "" "$INSTDIR\ShieldX.exe" 0
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ; Registry entries
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\ShieldX.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "InstalledArchitecture" "$InstallArchitecture"
  
  ; Registry app path
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\ShieldX.exe"
  
  DetailPrint "Installation completed successfully!"
  DetailPrint "Installed Architecture: $InstallArchitecture-bit"
SectionEnd

; Architecture Selection Custom Page
Function ArchitectureSelection
  nsDialogs::Create 1018
  Pop $0
  
  ${If} $0 == error
    Abort
  ${EndIf}
  
  ; Title
  ${NSD_CreateLabel} 0 10 100% 30 "Select Installation Architecture"
  Pop $0
  CreateFont $1 "Segoe UI" 14 700
  SendMessage $0 ${WM_SETFONT} $1 0
  
  ; Description
  ${NSD_CreateLabel} 0 45 100% 20 "Choose which version to install on your system:"
  Pop $0
  
  ; 64-bit Radio Button
  ${NSD_CreateRadioButton} 10 75 90% 20 "64-bit (x64) - Recommended for modern systems"
  Pop $0
  ${NSD_OnClick} $0 OnSelect_x64
  StrCpy $SelectedArchitecture "x64"
  ${NSD_SetState} $0 ${BST_CHECKED}
  
  ; 64-bit Description
  ${NSD_CreateLabel} 20 100 80% 40 "• Better performance on modern processors$\n• Supports systems with more than 3GB RAM$\n• Recommended for Windows 10/11 64-bit systems"
  Pop $0
  CreateFont $1 "Segoe UI" 9 0
  SendMessage $0 ${WM_SETFONT} $1 0
  
  ; 32-bit Radio Button
  ${NSD_CreateRadioButton} 10 145 90% 20 "32-bit (x86) - Legacy system support"
  Pop $0
  ${NSD_OnClick} $0 OnSelect_x86
  
  ; 32-bit Description
  ${NSD_CreateLabel} 20 170 80% 40 "• Compatible with legacy systems$\n• Lower memory footprint$\n• Works on both 32-bit and 64-bit Windows"
  Pop $0
  CreateFont $1 "Segoe UI" 9 0
  SendMessage $0 ${WM_SETFONT} $1 0
  
  nsDialogs::Show
FunctionEnd

Function OnSelect_x64
  StrCpy $SelectedArchitecture "x64"
FunctionEnd

Function OnSelect_x86
  StrCpy $SelectedArchitecture "x86"
FunctionEnd

; Uninstaller
Section Uninstall
  RMDir /r "$SMPROGRAMS\ShieldX"
  Delete "$DESKTOP\ShieldX.lnk"
  RMDir /r "$INSTDIR"
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd

; Custom install page order
!insertmacro MUI_PAGE_CUSTOM ArchitectureSelection "" "^NextButtonLabel"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

; Custom finish page
!insertmacro MUI_PAGE_FINISH

; Installer initialization
Function .onInit
  StrCpy $SelectedArchitecture "x64"
  DetailPrint "ShieldX Professional Antivirus v3.1.0"
  DetailPrint "Starting installation..."
FunctionEnd

Function PageLeave
  ; Validate selection
  ${If} $SelectedArchitecture == ""
    MessageBox MB_ICONEXCLAMATION "Please select an installation architecture!"
    Abort
  ${EndIf}
FunctionEnd

; Show finish message
Function ShowFinish
  MessageBox MB_ICONINFORMATION "ShieldX Professional Antivirus v3.1.0 has been installed successfully!$\n$\nSelected Architecture: $InstallArchitecture-bit$\n$\nThe application will start shortly."
FunctionEnd
