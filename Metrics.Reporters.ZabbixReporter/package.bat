@echo off
%~d0
cd %~dp0
echo 命令说明：
echo [pack=创建包，文件存放在Packages目录]
echo [push=上传Packages目录的所有包]
echo [exit=退出]
:start
set /p var=请输入命令:
if "%var%"=="pack" goto pack
if "%var%"=="push" goto push
if "%var%"=="exit" goto end
echo 输入命令有误，请重新输入
goto start

:pack
if not exist Packages mkdir Packages
@del Packages\*.nupkg /f /q
nuget pack -Build -Properties Configuration=Release -OutputDirectory Packages
goto start

:push 
nuget push Packages\*.nupkg -s http://10.12.8.250:81/ dacai
goto start

:end