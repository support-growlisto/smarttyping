; Inno Setup script for SmartTyping Desktop.
; Build the app first (self-contained single file), then compile this with the
; Inno Setup Compiler (iscc). See scripts/build-installer.ps1 and this folder's README.
;
;   iscc packaging/installer/SmartTyping.iss

#define AppName "SmartTyping"
#ifndef AppVersion
  #define AppVersion "0.1.1"
#endif
#define AppPublisher "SmartTyping"
#define AppExe "SmartTyping.exe"
; Path to the published single-file exe (overridable with iscc /DPublishDir=...).
#ifndef PublishDir
  #define PublishDir "..\..\src\SmartTyping.UI\bin\Release\net9.0-windows\win-x64\publish"
#endif

[Setup]
AppId={{E0B2C6E4-4E1D-4F0A-9C6B-5A7D3F2B1A90}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
; Per-user install (no admin): install under the user's local app data, never Program Files.
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
DisableProgramGroupPage=yes
OutputDir=..\..\dist
OutputBaseFilename=SmartTyping-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=lowest
WizardStyle=modern
SetupIconFile=..\..\assets\app.ico

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "th"; MessagesFile: "compiler:Languages\Thai.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
; The app carries its own assets/app.ico next to the exe (copied by the build).
Source: "{#PublishDir}\assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Close a running instance before install/uninstall so files aren't locked.
[InstallDelete]
Type: filesandordirs; Name: "{app}\assets"
