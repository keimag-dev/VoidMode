[Setup]
AppName=VoidMode
AppVersion=1.0.0
DefaultDirName={autopf}\VoidMode
DefaultGroupName=VoidMode
AllowNoIcons=yes
OutputDir=.\publish
OutputBaseFilename=VoidModeInstaller
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにアイコンを作成する"; GroupDescription: "追加タスク:"

[Files]
; .issファイルから見た相対パス (installer/ -> ../publish/) を指定
Source: "..\publish\VoidMode.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\VoidMode"; Filename: "{app}\VoidMode.exe"
Name: "{autodesktop}\VoidMode"; Filename: "{app}\VoidMode.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\VoidMode.exe"; Description: "VoidModeを起動する"; Flags: nowait postinstall
