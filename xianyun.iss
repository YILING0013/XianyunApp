; -- Inno Setup Script --
[Setup]
; 定义应用程序的基本信息
AppName=Xianyun App
AppVersion=1.0
; 安装路径, {commonpf} 表示 Program Files 文件夹
DefaultDirName={commonpf}\XianyunApp 
; 开始菜单组名称
DefaultGroupName=XianyunApp 
; 输出的安装包名称
OutputBaseFilename=XianyunApp_Setup 
; 使用 lzma 压缩算法
Compression=lzma 
; 使用固体压缩
SolidCompression=yes 
; 不创建程序组
DisableProgramGroupPage=yes 

[Files]
; 将应用程序的文件添加到安装程序中
Source: "E:\Visual_Studio_project\xianyun\bin\Release\Xianyun.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "E:\Visual_Studio_project\xianyun\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; 创建桌面和开始菜单快捷方式
Name: "{commondesktop}\XianyunApp"; Filename: "{app}\Xianyun.exe"
Name: "{group}\XianyunApp"; Filename: "{app}\Xianyun.exe"

[Run]
; 安装后自动运行应用程序
Filename: "{app}\Xianyun.exe"; Description: "{cm:LaunchProgram,MyApp}"; Flags: nowait postinstall skipifsilent

