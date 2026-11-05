; Night Tech Lifeline installer - Inno Setup 6.3 or newer (https://jrsoftware.org/isinfo.php)
; Build with ..\build.ps1 rather than compiling this directly; it publishes the app first
; and passes the version from NightTech.Lifeline.csproj.

#define AppName "Night Tech Lifeline"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define Publisher "Night Tech Solutions"
#define PublisherUrl "https://nighttech.solutions/"
#define HelpUrl "https://nighttech.solutions/security-apps/#lifeline"
#define ExeName "NightTechLifeline.exe"

[Setup]
; Never change AppId after the first public release: Windows uses it to match upgrades.
AppId={{9C2E6B41-3F7D-4A8E-B5C1-6D0E2F8A7B93}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#PublisherUrl}
AppSupportURL={#HelpUrl}
AppUpdatesURL={#HelpUrl}
AppCopyright=© 2026 {#Publisher}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#Publisher}
VersionInfoDescription={#AppName} installer

; Lifeline never needs administrator rights, so it installs just for the person signed in (no admin prompt).
PrivilegesRequired=lowest
DefaultDirName={autopf}\Night Tech Lifeline
DisableProgramGroupPage=yes
DisableDirPage=yes

; Windows 10 version 2004 or newer, 64-bit
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Asks people to close Lifeline before upgrading or uninstalling (matches App.MutexName)
AppMutex=NightTechSolutions.Lifeline
CloseApplications=yes

LicenseFile=..\LICENSE.txt
SetupIconFile=..\src\NightTech.Lifeline\Assets\lifeline.ico
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
WizardImageFile=branding\wizard-100.bmp,branding\wizard-200.bmp
WizardSmallImageFile=branding\small-100.bmp,branding\small-200.bmp

OutputDir=..\dist
OutputBaseFilename=NightTechLifeline-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes

#ifdef Sign
SignTool=ntsign
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel2=This installs [name/ver] on your computer.%n%nIf a scary warning ever takes over your screen, or you think a scammer is on your computer, Lifeline helps you get out of it and reach someone you trust. It's free, with no ads, accounts or tracking.%n%nClose other apps before you continue.
FinishedLabel=Night Tech Lifeline is installed. Its round icon sits by the clock (you may need to click the ^ arrow to see it), and Ctrl+Alt+H opens it.

[Tasks]
Name: "startup"; Description: "Start Lifeline when I sign in to Windows (recommended)"; GroupDescription: "Options:"
Name: "desktopicon"; Description: "Put a ""Scam help"" icon on the desktop"; GroupDescription: "Options:"

[Files]
Source: "..\publish\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NightTechLifeline"; ValueData: """{app}\{#ExeName}"" --background"; Flags: uninsdeletevalue; Tasks: startup

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#ExeName}"; Comment: "Help getting out of a scam screen"
Name: "{autodesktop}\Scam help - Night Tech Lifeline"; Filename: "{app}\{#ExeName}"; Comment: "Help getting out of a scam screen"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Unticking "Start Lifeline when I sign in" on an upgrade removes the entry an earlier install made.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not WizardIsTaskSelected('startup') then
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'NightTechLifeline');
end;

// Lifeline's folder holds the browser tabs it moved aside and the family contact.
// Uninstalling asks before deleting it, so saved tabs aren't lost by surprise.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\Night Tech Solutions\Lifeline');
    if DirExists(DataDir) and not UninstallSilent then
    begin
      if MsgBox('Also delete Lifeline''s settings and the browser tabs it saved?' + #13#10 + #13#10 +
                'Choose No to keep them.', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
