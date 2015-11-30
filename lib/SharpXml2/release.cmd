@echo off

set version=1.5.0.0
set build=Release
set properties=Configuration=%build%;Optimize=true;TargetFrameworkVersion=v4.0
set nugetpath=NuGet\lib\net40

pushd SharpXml

rem build library
msbuild /p:%properties% /t:Clean,Build

rem copy assemblies into nuget folder
pushd bin\%build%
copy /Y *.dll ..\..\..\%nugetpath%
copy /Y *.xml ..\..\..\%nugetpath%

popd
popd

rem build nuget package
pushd NuGet
nuget pack SharpXml.nuspec

popd

rem build archive
pushd %nugetpath%
7z a -y ..\..\..\SharpXml-%version%.zip *

popd
