﻿#Region "Microsoft.VisualBasic::620adfec2741c5fdd13ab3f96fd3a528, R#\System\Package\PackageFile\PackageLoader.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xie (genetics@smrucc.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2018 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.



    ' /********************************************************************************/

    ' Summaries:

    '     Module PackageLoader2
    ' 
    '         Function: CheckPackage, GetPackageDirectory, GetPackageIndex, GetPackageName, loadDependency
    '                   LoadPackage
    ' 
    '         Sub: callOnLoad
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Development.Configuration

Namespace Development.Package.File

    Public Module PackageLoader2

        ''' <summary>
        ''' 检查程序包的指纹是否和checksum中的结果相匹配
        ''' </summary>
        ''' <param name="libDir"></param>
        ''' <returns></returns>
        Public Function CheckPackage(libDir As String) As Boolean
            ' Error: package or namespace load failed for 'mzkit':
            '  .onLoad failed in loadNamespace() for 'mzkit', details:
            '   call: fun(libname, pkgname)
            '   error: 1

            Return True
        End Function

        <Extension>
        Public Function GetPackageDirectory(opt As Options, packageName$) As String
            Dim libDir As String

            If App.IsMicrosoftPlatform Then
                libDir = opt.lib_loc & $"/Library/{packageName}"
            Else
                libDir = $"{opt.lib_loc}/{packageName}"
            End If

            libDir = libDir.GetDirectoryFullPath

            Return libDir
        End Function

        Public Function GetPackageName(zipFile As String) As String
            Dim index As DESCRIPTION = GetPackageIndex(zipFile)

            If index Is Nothing Then
                Return Nothing
            Else
                Return index.Package
            End If
        End Function

        Public Function GetPackageIndex(zipFile As String) As DESCRIPTION
            If Not zipFile.FileExists Then
                Return Nothing
            End If

            Using zip As New ZipArchive(zipFile.Open(FileMode.Open, doClear:=False, [readOnly]:=True), ZipArchiveMode.Read)
                If zip.GetEntry("index.json") Is Nothing Then
                    Return Nothing
                Else
                    Using file As Stream = zip.GetEntry("index.json").Open, fs As New StreamReader(file)
                        Return fs.ReadToEnd.LoadJSON(Of DESCRIPTION)
                    End Using
                End If
            End Using
        End Function

        ''' <summary>
        ''' attach installed package
        ''' </summary>
        ''' <param name="dir"></param>
        ''' <param name="env"></param>
        Public Function LoadPackage(dir As String, env As GlobalEnvironment) As Message
            Dim [namespace] As New PackageNamespace(dir)
            Dim debugEcho As Boolean = env.debugMode

            If debugEcho Then
                Call Console.WriteLine($"load package from directory: '{dir}'.")
            End If

            For Each symbol As NamedValue(Of String) In [namespace].EnumerateSymbols
                Using bin As New BinaryReader($"{dir}/src/{symbol.Value}".Open)
                    Call BlockReader.Read(bin).Parse(desc:=[namespace].meta).Evaluate(env)
                End Using
            Next

            Call env.loadDependency(pkg:=[namespace])
            Call env.callOnLoad(pkg:=[namespace])
            Call env.attachedNamespace.Add([namespace].packageName, [namespace])

            Return Nothing
        End Function

        ''' <summary>
        ''' 这个函数会加载通过``imports...from...``语句进行显示加载的依赖程序集
        ''' </summary>
        ''' <param name="env"></param>
        ''' <param name="pkg"></param>
        <Extension>
        Private Function loadDependency(env As GlobalEnvironment, pkg As PackageNamespace) As Message
            Dim result As Object

            For Each dependency As Dependency In pkg.dependency
                If dependency.library.StringEmpty Then
                    For Each pkgName As String In dependency.packages
                        Call env.LoadLibrary(pkgName)
                    Next
                Else
                    Dim dllFile As String = pkg.FindAssemblyPath(dependency.library)

                    If Not dllFile.FileExists Then
                        result = [Imports].GetDllFile($"{dependency.library}.dll", env)

                        If TypeOf result Is Message Then
                            Return result
                        Else
                            dllFile = result
                        End If
                    End If

                    Call [Imports].LoadLibrary(dllFile, env, dependency.packages)
                End If
            Next

            Return Nothing
        End Function

        <Extension>
        Private Sub callOnLoad(env As GlobalEnvironment, pkg As PackageNamespace)
            Dim onLoad As String = $"{pkg.libPath}/.onload"

            If onLoad.FileExists Then
                Using bin As New BinaryReader(onLoad.Open)
                    Call BlockReader.Read(bin) _
                        .Parse(desc:=pkg.meta) _
                        .DoCall(Function(func)
                                    Return DirectCast(func, DeclareNewFunction)
                                End Function) _
                        .Invoke(env, params:={})
                End Using
            End If
        End Sub
    End Module
End Namespace
