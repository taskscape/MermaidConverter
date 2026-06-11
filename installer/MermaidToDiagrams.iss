#define AppName "Mermaid To Diagrams"
#define AppVersion "0.1.0"
#define Publisher "Taskscape Ltd"

[Setup]
AppId={{7B06DA3B-A9CC-48BE-8B16-963EB6A6EFC6}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={autopf}\MermaidToDiagrams
DefaultGroupName={#AppName}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
Compression=lzma2
SolidCompression=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=MermaidToDiagrams-{#AppVersion}-win-x64
PrivilegesRequired=admin
DisableProgramGroupPage=yes

[Tasks]
Name: addtopath; Description: "Add Mermaid To Diagrams to PATH"; GroupDescription: "Command line integration:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "..\artifacts\runtime\python\*"; DestDir: "{app}\runtime\python"; Flags: recursesubdirs ignoreversion
Source: "..\artifacts\runtime\graphviz\*"; DestDir: "{app}\runtime\graphviz"; Flags: recursesubdirs ignoreversion
Source: "..\samples\reference-architectures\*"; DestDir: "{app}\samples\reference-architectures"; Flags: recursesubdirs ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\plan.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Mermaid To Diagrams"; Filename: "{app}\MermaidToDiagrams.GUI.exe"
Name: "{group}\Mermaid To Diagrams CLI"; Filename: "{app}\m2d.exe"
Name: "{group}\Reference Architecture Samples"; Filename: "{app}\samples\reference-architectures"
Name: "{group}\README"; Filename: "{app}\README.md"

[Registry]
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Check: NeedsAddPath(ExpandConstant('{app}')); Tasks: addtopath

[Run]
Filename: "{app}\m2d.exe"; Parameters: "doctor --quiet"; Flags: runhidden

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', OrigPath) then
  begin
    Result := True;
    exit;
  end;

  Result := Pos(';' + Uppercase(Param) + ';', ';' + Uppercase(OrigPath) + ';') = 0;
end;
