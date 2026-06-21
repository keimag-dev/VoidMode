[Setup]
AppName=VoidMode
AppVersion=1.0.0
DefaultDirName={autopf}\VoidMode
DefaultGroupName=VoidMode
AllowNoIcons=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=VoidModeInstaller
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成する"; GroupDescription: "追加タスク:"

[Files]
; publishフォルダ内の配布物を含める。CIではSingleFile設定により基本的にVoidMode.exeのみになる。
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\VoidMode"; Filename: "{app}\VoidMode.exe"
Name: "{autodesktop}\VoidMode"; Filename: "{app}\VoidMode.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\VoidMode.exe"; Description: "VoidModeを起動する"; Flags: nowait postinstall skipifsilent
