﻿#Region "Microsoft.VisualBasic::b5341d6fba43cab77cb8ec2c03a8711f, studio\R-terminal\CLI\CLI.vb"

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

' Module CLI
' 
'     Function: ConfigStartups, Info, InitializeEnvironment, Install, SyntaxText
'               unixman, Version
' 
' /********************************************************************************/

#End Region

Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Development
Imports Microsoft.VisualBasic.ApplicationServices.Development.XmlDoc.Assembly
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.Utility
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.InteropService.SharedORM
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Language.UnixBash
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Interpreter
Imports Microsoft.VisualBasic.My
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.System
Imports SMRUCC.Rsharp.System.Configuration
Imports SMRUCC.Rsharp.System.Package
Imports RlangScript = SMRUCC.Rsharp.Runtime.Components.Rscript
Imports RProgram = SMRUCC.Rsharp.Interpreter.Program

<GroupingDefine(CLI.SystemConfig, Description:="R# language system and environment configuration util tools.")>
<CLI()>
Module CLI

    Friend Const SystemConfig As String = "R# System Utils"

    <ExportAPI("--install.packages")>
    <Description("Install new packages.")>
    <Usage("--install.packages /module <*.dll> [--verbose]")>
    <Argument("/module", False, CLITypes.File,
              Extensions:="*.dll",
              Description:=".NET Framework 4.8 assembly module file.")>
    <Group(SystemConfig)>
    Public Function Install(args As CommandLine) As Integer
        Dim module$ = args <= "/module"
        Dim config As New Options(ConfigFile.localConfigs)

        Internal.debug.verbose = args("--verbose")
        Internal.debug.write($"load config file: {ConfigFile.localConfigs}")
        Internal.debug.write($"load package registry: {config.lib}")

        If [module].StringEmpty Then
            Return "Missing '/module' argument!".PrintException
        End If

        Using pkgMgr As New PackageManager(config)
            If Not [module].ToLower.StartsWith("scan=") Then
                Call pkgMgr.InstallLocals(dllFile:=[module])
            Else
                For Each file As String In ls - l - "*.dll" <= [module].GetTagValue("=", trim:=True).Value
                    Try
                        Dim assm As Assembly = Assembly.LoadFrom(file.GetFullPath)

                        If Not assm.GetCustomAttribute(Of RPackageModuleAttribute) Is Nothing Then
                            Call pkgMgr.InstallLocals(dllFile:=file)
                        End If
                    Catch ex As Exception

                    End Try
                Next
            End If
        End Using

        Return 0
    End Function

    <ExportAPI("--startups")>
    <Usage("--startups [--add <namespaceList> --remove <namespaceList>]")>
    <Group(SystemConfig)>
    Public Function ConfigStartups(args As CommandLine) As Integer
        Dim adds As String = args("--add")
        Dim remove As String = args("--remove")
        Dim config As ConfigFile = ConfigFile.Load(ConfigFile.localConfigs)

        If config.startups Is Nothing Then
            config.startups = New StartupConfigs
        End If

        If Not adds.StringEmpty Then
            config.startups.loadingPackages = config.startups _
                .loadingPackages _
                .JoinIterates(adds.StringSplit("([;,]|\s)+")) _
                .ToArray
        End If
        If Not remove.StringEmpty Then
            Dim removePending As Index(Of String) = remove.StringSplit("([;,]|\s)+")

            config.startups.loadingPackages = config.startups _
                .loadingPackages _
                .SafeQuery _
                .Where(Function(name) Not name Like removePending) _
                .ToArray
        End If

        If adds.StringEmpty AndAlso remove.StringEmpty Then
            For Each name As String In config.startups.loadingPackages
                Call Console.WriteLine(name)
            Next
        End If

        Return config _
            .GetXml _
            .SaveTo(ConfigFile.localConfigs) _
            .CLICode
    End Function

    <ExportAPI("--version")>
    <Description("Print R# interpreter version")>
    Public Function Version(args As CommandLine) As Integer
        Console.Write(GetType(RInterpreter).Assembly.FromAssembly.AssemblyVersion)
        Return 0
    End Function

    <ExportAPI("--setup")>
    <Description("Initialize the R# runtime environment.")>
    <Group(SystemConfig)>
    Public Function InitializeEnvironment(args As CommandLine) As Integer
        Dim config As New Options(ConfigFile.localConfigs)

        Internal.debug.verbose = args("--verbose")
        Internal.debug.write($"load config file: {ConfigFile.localConfigs}")
        Internal.debug.write($"load package registry: {config.lib}")

        App.CurrentDirectory = App.HOME

        Using pkgMgr As New PackageManager(config)
            For Each file As String In {"R.base.dll", "R.graph.dll", "R.graphics.dll", "R.math.dll", "R.plot.dll"}
                If Not file.FileExists Then
                    file = "Library/" & file
                End If

                If file.FileExists Then
                    Call pkgMgr.InstallLocals(dllFile:=file)
                    Call pkgMgr.Flush()
                Else
                    Call $"missing module dll: {file}".PrintException
                End If
            Next
        End Using

        Return 0
    End Function

    <ExportAPI("/bash")>
    <Usage("/bash --script <run.R>")>
    Public Function BashRun(args As CommandLine) As Integer
        Dim script$ = args <= "--script"
        Dim bash$ = script.ParentPath & "/" & script.BaseName
        Dim utf8 As Encoding = Encodings.UTF8WithoutBOM.CodePage
        Dim dirHelper As String = UNIX.GetLocationHelper

        script = dirHelper & vbLf & "
app=""$DIR/{script}""
cli=""$@""

R# ""$app"" $cli".Replace("{script}", script.FileName)
        script = script.LineTokens.JoinBy(vbLf)

        Return script.SaveTo(bash, utf8).CLICode
    End Function

    <ExportAPI("--man.1")>
    <Description("Exports unix man page data for current installed packages.")>
    <Usage("--man.1 [--module <module.dll> --debug --out <directory, default=./>]")>
    Public Function unixman(args As CommandLine) As Integer
        Dim out$ = args("--out") Or "./"
        Dim module$ = args("--module")
        Dim env As New RInterpreter
        Dim xmldocs As AnnotationDocs = env.globalEnvir.packages.packageDocs
        Dim utf8 As Encoding = Encodings.UTF8WithoutBOM.CodePage

        If [module].FileExists Then
            For Each pkg As Package In PackageLoader.ParsePackages(dll:=[module])
                Call pkg.unixMan(xmldocs, out)
                Call $"load: {pkg.info.Namespace}".__INFO_ECHO
            Next
        Else
            ' run build for all installed package modules
            For Each pkg As Package In env.globalEnvir.packages.AsEnumerable
                If pkg.isMissing Then
                    Call $"missing package: {pkg.namespace}...".PrintException
                Else
                    Call pkg.unixMan(xmldocs, out)
                End If
            Next
        End If

        Return 0
    End Function

    <Extension>
    Private Sub unixMan(pkg As Package, xmldocs As AnnotationDocs, out$)
        Dim annoDocs As ProjectType = xmldocs.GetAnnotations(pkg.package)
        Dim links As New List(Of NamedValue(Of String))

        For Each ref As String In pkg.ls
            Dim symbol As RMethodInfo = pkg.GetFunction(apiName:=ref)
            Dim docs As ProjectMember = xmldocs.GetAnnotations(symbol.GetRawDeclares)
            Dim help As UnixManPage = UnixManPagePrinter.CreateManPage(symbol, docs)

            links += New NamedValue(Of String) With {
                .Name = ref,
                .Value = $"{pkg.namespace}/{ref}.1",
                .Description = docs _
                    ?.Summary _
                    ?.LineTokens _
                     .FirstOrDefault
            }

            Call UnixManPage _
                .ToString(help, "man page create by R# package system.") _
                .SaveTo($"{out}/{pkg.namespace}/{ref}.1", UTF8)
        Next

        If annoDocs Is Nothing Then
            annoDocs = New ProjectType(New ProjectNamespace(New Project("n/a")))
        End If

        Using markdown As StreamWriter = $"{out}/{pkg.namespace}.md".OpenWriter
            Call markdown.WriteLine("# " & pkg.namespace)
            Call markdown.WriteLine()
            Call markdown.WriteLine(annoDocs.Summary)

            If Not annoDocs.Remarks.StringEmpty Then
                For Each line As String In annoDocs.Remarks.LineTokens
                    Call markdown.WriteLine("> " & line)
                Next
            End If

            Call markdown.WriteLine()

            For Each link As NamedValue(Of String) In links
                Call markdown.WriteLine($"+ [{link.Name}]({link.Value}) {link.Description}")
            Next
        End Using
    End Sub

    <ExportAPI("--info")>
    <Description("Print R# interpreter version information and R# terminal version information.")>
    Public Function Info(args As CommandLine) As Integer
        Dim Rterminal As AssemblyInfo = GetType(Program).Assembly.FromAssembly
        Dim RsharpCore As AssemblyInfo = GetType(RInterpreter).Assembly.FromAssembly

        Call Rterminal.AppSummary(Nothing, Nothing, App.StdOut)
        Call RsharpCore.AppSummary(Nothing, Nothing, App.StdOut)

        Call App.StdOut.value.Flush()
        Call New RInterpreter().Print("sessionInfo();")

        Return 0
    End Function

    <ExportAPI("--syntax")>
    <Description("Show syntax parser result of the input script.")>
    <Usage("--syntax /script <script.R>")>
    Public Function SyntaxText(args As CommandLine) As Integer
        Dim script$ = args <= "/script"
        Dim Rscript As RlangScript = RlangScript.FromFile(script)
        Dim program As RProgram = RProgram.CreateProgram(Rscript, debug:=False)

        Call Console.WriteLine(program.ToString)

        Return 0
    End Function
End Module
