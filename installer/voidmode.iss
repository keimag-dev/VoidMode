[Setup]
AppName=VoidMode
AppVersion=1.0.0
DefaultDirName={autopf}\VoidMode
DefaultGroupName=VoidMode
AllowNoIcons=yes
OutputDir=userdocs:VoidMode\bin\Release
OutputBaseFilename=VoidModeInstaller
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成する"; GroupDescription: "追加タスク:"; Flags: checked

[Files]
; 以下のパスは GitHub Actions の publish ディレクトリからの相対パスとして想定
Source: "VoidMode.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "config.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\VoidMode"; Filename: "{app}\VoidMode.exe"
Name: "{autodesktop}\VoidMode"; Filename: "{app}\VoidMode.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\VoidMode.exe"; Description: "VoidModeを起動する"; Flags: nowait postinstall skipifalreadyrunning
