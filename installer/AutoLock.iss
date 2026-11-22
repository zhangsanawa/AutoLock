; AutoLock 安装脚本 (Inno Setup 7)
; 编译: ISCC.exe AutoLock.iss

#define MyAppName "AutoLock"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "AutoLock"
#define MyAppURL "https://github.com/zhangsanawa/AutoLock"
#define MyAppExeName "AutoLock.exe"

[Setup]
AppId={{8F3C2A71-4B5D-4E92-9C16-7A2E5D8B4F31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=dist
OutputBaseFilename=AutoLock-Setup-{#MyAppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"
Name: "startup"; Description: "开机自动启动（推荐）"; GroupDescription: "附加任务:"

[Files]
Source: "..\AutoLock.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\AutoLock.exe.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "default.ini"; DestDir: "{app}"; DestName: "config.ini"; Flags: onlyifdoesntexist uninsneveruninstall

[Registry]
; 开机自启（当前用户）：不带 --elevate，开机由 explorer 直接拉起，不会弹 UAC
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AutoLock"; ValueData: """{app}\{#MyAppExeName}"" --delay"; Flags: uninsdeletevalue; Tasks: startup

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon
; 双保险：启动文件夹也放一个
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--delay"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--elevate"; Description: "立即运行 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\autolock.log"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  LnkPath: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    LnkPath := ExpandConstant('{userstartup}\{#MyAppName}.lnk');
    if FileExists(LnkPath) then
      DeleteFile(LnkPath);
  end;
end;
