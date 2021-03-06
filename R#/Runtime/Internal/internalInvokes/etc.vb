﻿#Region "Microsoft.VisualBasic::036e0f19aa1e5c5b7e25303ac2e852ce, R#\Runtime\Internal\internalInvokes\etc.vb"

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

    '     Module etc
    ' 
    '         Function: contributors, getActivators, license, man, RVer
    '                   sessionInfo, Sys_getlocale, Sys_info
    ' 
    '         Sub: demo
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Globalization
Imports Microsoft.VisualBasic.ApplicationServices.Development
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.Utility
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Development
Imports SMRUCC.Rsharp.Development.Components
Imports Win32 = System.Environment

Namespace Runtime.Internal.Invokes

    Module etc

        ''' <summary>
        ''' # The R# License Terms
        ''' 
        ''' The license terms under which R# is distributed.
        ''' </summary>
        ''' <returns></returns>
        <ExportAPI("license")>
        Public Function license(Optional env As Environment = Nothing) As <RSuppressPrint> Object
            Call env.globalEnvironment.stdout.WriteLine(Rsharp.LICENSE.GPL3)
            Return Nothing
        End Function

        ''' <summary>
        ''' # ``R#`` Project Contributors
        ''' 
        ''' The R# Who-is-who, describing who made significant contributions to the development of R#.
        ''' </summary>
        ''' <returns></returns>
        <ExportAPI("contributors")>
        Public Function contributors(Optional env As Environment = Nothing) As <RSuppressPrint> Object
            Call env.globalEnvironment.stdout.WriteLine(My.Resources.contributions)
            Return Nothing
        End Function

        <ExportAPI("man")>
        Public Function man(symbol As Object, Optional env As Environment = Nothing) As String
            If symbol Is Nothing Then
                Return Nothing
            ElseIf TypeOf symbol Is String Then
                symbol = env.FindSymbol(symbol)?.value
            End If

            If Not TypeOf symbol Is RMethodInfo Then
                Return Nothing
            End If

            Dim xmldocs = env.globalEnvironment.packages.packageDocs
            Dim docs = xmldocs.GetAnnotations(DirectCast(symbol, RMethodInfo).GetRawDeclares)
            Dim help As UnixManPage = UnixManPagePrinter.CreateManPage(symbol, docs)

            Return UnixManPage.ToString(help, "man page create by R# package system.")
        End Function

        <ExportAPI("demo")>
        Public Sub demo()

        End Sub

        ''' <summary>
        ''' ### Extract System and User Information
        ''' 
        ''' Reports system and user information.
        ''' </summary>
        ''' <returns>
        ''' A character vector with fields
        '''
        ''' + ``sysname`` The operating system name.
        ''' + ``release`` The OS release.
        ''' + ``version`` The OS version.
        ''' + ``nodename`` A name by which the machine Is known On 
        '''     the network (If any).
        ''' + ``machine`` A concise description Of the hardware, 
        '''     often the CPU type.
        ''' + ``login`` The user 's login name, or "unknown" if it 
        '''     cannot be ascertained.
        ''' + ``user`` The name Of the real user ID, Or "unknown" If 
        '''     it cannot be ascertained.
        ''' + ``effective_user`` The name Of the effective user ID, Or 
        '''     "unknown" If it cannot be ascertained. This may differ 
        '''     from the real user In 'set-user-ID’ processes.
        '''
        ''' The last three fields give the same value On Windows.
        ''' </returns>
        <ExportAPI("Sys.info")>
        Public Function Sys_info() As list
            Return New list With {
                .slots = New Dictionary(Of String, Object)
            }
        End Function

        <ExportAPI("Sys.getlocale")>
        Public Function Sys_getlocale() As list
            Dim invariantCulture = CultureInfo.CurrentCulture

            Return New list With {
                .slots = New Dictionary(Of String, Object) From {
                    {"name", invariantCulture.Name},
                    {"ISO_name", invariantCulture.TwoLetterISOLanguageName},
                    {"en_name", invariantCulture.EnglishName},
                    {"fullName", invariantCulture.DisplayName},
                    {"LC_ID", invariantCulture.LCID},
                    {"CompareInfo", invariantCulture.CompareInfo.ToString},
                    {"TextInfo", invariantCulture.TextInfo.ToString},
                    {"NumberInfo", invariantCulture.NumberFormat.ToString}
                }
            }
        End Function

        ''' <summary>
        ''' ### Collect Information About the Current R Session
        ''' 
        ''' Print version information about R, the OS and attached or 
        ''' loaded packages.
        ''' </summary>
        ''' <returns>
        ''' sessionInfo() returns an object of class "sessionInfo" which has 
        ''' print and toLatex methods. This is a list with components
        ''' </returns>
        <ExportAPI("sessionInfo")>
        <RApiReturn(GetType(RSessionInfo))>
        Public Function sessionInfo(env As Environment) As vbObject
            Dim dev As String = env.globalEnvironment.stdout.env _
                .ToString _
                .ToLower
            Dim info As New RSessionInfo With {
                .Rversion = RVer(env),
                .loadedOnly = env.globalEnvironment.packages _
                    .EnumerateAttachedPackages _
                    .Select(Function(a) a.namespace) _
                    .ToArray,
                .locale = Sys_getlocale(),
                .matprod = "default",
                .output_device = dev,
                .activators = getActivators(env),
                .environment_variables = New list With {
                    .slots = App _
                        .GetAppVariables _
                        .ToDictionary(Function(a) a.Name,
                                      Function(a)
                                          Return CObj(a.Value)
                                      End Function)
                },
                .basePkgs = invoke.ls.slots.Keys.ToArray
            }

            Return New vbObject(info)
        End Function

        <ExportAPI("Sys.activators")>
        Public Function getActivators(env As Environment) As list
            Return New list With {
                .slots = env _
                    .globalEnvironment _
                    .types _
                    .ToDictionary(Function(a) a.Key,
                                  Function(a)
                                      Return CObj($"{a.Value.ToString}, {a.Value.raw.FullName}")
                                  End Function)
            }
        End Function

        ''' <summary>
        ''' ### Version Information
        ''' 
        ''' R.Version() provides detailed information about the version of R running.
        ''' </summary>
        ''' <param name="env"></param>
        ''' <returns>
        ''' R.Version returns a list with character-string components
        '''
        ''' + ``platform`` the platform For which R was built. A triplet Of the form 
        '''    CPU-VENDOR-OS, As determined by the configure script. E.g, 
        '''    "i686-unknown-linux-gnu" Or "i386-pc-mingw32".
        ''' + ``arch`` the architecture(CPU) R was built On/For.
        ''' + ``os`` the underlying operating system.
        ''' + ``system`` CPU And OS, separated by a comma.
        ''' + ``status`` the status Of the version (e.g., "alpha")
        ''' + ``major`` the major version number
        ''' + ``minor`` the minor version number, including the patchlevel
        ''' + ``year`` the year the version was released
        ''' + ``month`` the month the version was released
        ''' + ``day`` the day the version was released
        ''' + ``svn rev`` the Subversion revision number, which should be either "unknown" 
        '''    Or a Single number. (A range Of numbers Or a number With M Or S appended 
        '''    indicates inconsistencies In the sources used To build this version Of R.)
        ''' + ``language`` always "R".
        ''' </returns>
        <ExportAPI("R.Version")>
        Public Function RVer(env As Environment) As list
            Dim core As AssemblyInfo = GetType(Environment).Assembly.FromAssembly
            Dim version As Version = Version.Parse(core.AssemblyVersion)
            Dim built As DateTime = core.BuiltTime

            Return New list With {
                .slots = New Dictionary(Of String, Object) From {
                    {"platform", "Microsoft VisualStudio 2019/.NET Framework v4.8/Microsoft VisualBasic.NET"},
                    {"arch", "x86_64"},
                    {"os", ".NET Framework v4.8"},
                    {"system", Win32.OSVersion.VersionString},
                    {"status", Win32.OSVersion.ServicePack},
                    {"major", version.Major},
                    {"minor", version.Minor},
                    {"year", built.Year},
                    {"month", built.Month},
                    {"day", built.Day},
                    {"svn rev", version.Revision},
                    {"language", "R#"},
                    {"version.string", $"R version {core.AssemblyVersion} ({built.ToString})"},
                    {"nickname", "R-sharp"}
                }
            }
        End Function
    End Module
End Namespace
