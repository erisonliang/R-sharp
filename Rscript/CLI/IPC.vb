﻿#Region "Microsoft.VisualBasic::3150cf44846f41da91cfc83a155a574b, Rscript\CLI\IPC.vb"

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
    '     Function: postResult, slaveMode
    ' 
    ' /********************************************************************************/

#End Region

Imports System.ComponentModel
Imports System.Net
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Parallel
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.Rsharp.Development
Imports SMRUCC.Rsharp.Development.Configuration
Imports SMRUCC.Rsharp.Interpreter
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Serialize
Imports IPEndPoint = Microsoft.VisualBasic.Net.IPEndPoint

Partial Module CLI

    <ExportAPI("--slave")>
    <Usage("--slave /exec <script.R> /args <json_base64> /request-id <request_id> /PORT=<port_number> [/timeout=<timeout in ms, default=1000> /retry=<retry_times, default=5> /MASTER=<ip, default=localhost> /entry=<function_name, default=NULL>]")>
    <Description("Create a R# cluster node for run background or parallel task. This IPC command will run a R# script file that specified by the ``/exec`` argument, and then post back the result data json to the specific master listener.")>
    <Argument("/exec", False, CLITypes.File, AcceptTypes:={GetType(String)}, Extensions:="*.R", Description:="a specific R# script for run")>
    <Argument("/args", False, CLITypes.Base64, PipelineTypes.std_in,
              AcceptTypes:={GetType(Dictionary(Of String, String))},
              Extensions:="*.json",
              Description:="The base64 text of the input arguments for running current R# script file, this is a json encoded text of the arguments. the json object should be a collection of [key => value[]] pairs.")>
    <Argument("/entry", True, CLITypes.String, AcceptTypes:={GetType(String)},
              Description:="the entry function name, by default is running the script from the begining to ends.")>
    <Argument("/request-id", False, CLITypes.String,
              AcceptTypes:={GetType(String)},
              Description:="the unique id for identify current slave progress in the master node when invoke post data callback.")>
    <Argument("/MASTER", True, CLITypes.String,
              AcceptTypes:={GetType(IPAddress)},
              Description:="the ip address of the master node, by default this parameter value is ``localhost``.")>
    <Argument("/PORT", False, CLITypes.Integer,
              AcceptTypes:={GetType(Integer)},
              Description:="the port number for master node listen to this callback post data.")>
    <Argument("/retry", False, CLITypes.Integer,
              AcceptTypes:={GetType(Integer)},
              Description:="How many times that this cluster node should retry to send callback data if the TCP request timeout.")>
    Public Function slaveMode(args As CommandLine) As Integer
        Dim script As String = args <= "/exec"
        Dim arguments As Dictionary(Of String, String()) = args("/args") _
            .Base64Decode _
            .LoadJSON(Of Dictionary(Of String, String()))
        Dim port As Integer = args <= "/PORT"
        Dim master As String = args("/MASTER") Or "localhost"
        Dim entry As String = args <= "/entry"
        Dim request_id As String = args <= "/request-id"
        Dim retryTimes As Integer = args("/retry") Or 5
        Dim timeout As Double = args("/timeout") Or 1000
        Dim R As RInterpreter = RInterpreter.FromEnvironmentConfiguration(ConfigFile.localConfigs)
        Dim parameters As NamedValue(Of Object)() = arguments _
            .Select(Function(a)
                        Return New NamedValue(Of Object) With {
                            .Name = a.Key,
                            .Value = a.Value
                        }
                    End Function) _
            .ToArray

        R.debug = args("--debug")

        For Each pkgName As String In R.configFile.GetStartupLoadingPackages
            Call R.LoadLibrary(packageName:=pkgName)
        Next

        Dim result As Object = R.Source(script, parameters)
        Dim upstream As New IPEndPoint(master, port)

        If TypeOf result Is Message Then
            Return R.globalEnvir.postResult(
                result:=result,
                master:=upstream,
                request_id:=request_id,
                retryTimes:=retryTimes,
                timeoutMS:=timeout
            )
        ElseIf Not entry.StringEmpty Then
            result = R.Invoke(entry, parameters)
        End If

        ' post result data back to the master node
        Return R.globalEnvir.postResult(
            result:=result,
            master:=upstream,
            request_id:=request_id,
            retryTimes:=retryTimes,
            timeoutMS:=timeout
        )
    End Function

    <Extension>
    Private Function postResult(env As Environment,
                                result As Object,
                                master As IPEndPoint,
                                request_id As String,
                                retryTimes As Integer,
                                timeoutMS As Double) As Integer

        Dim buffer As New Buffer

        If result Is Nothing Then
            buffer.data = rawBuffer.getEmptyBuffer
        ElseIf TypeOf result Is dataframe Then
            Throw New NotImplementedException(result.GetType.FullName)
        ElseIf TypeOf result Is vector Then
            buffer.data = vectorBuffer.CreateBuffer(DirectCast(result, vector), env)
        ElseIf TypeOf result Is list Then
            Throw New NotImplementedException(result.GetType.FullName)
        ElseIf TypeOf result Is Message Then
            buffer.data = New messageBuffer(DirectCast(result, Message))
        ElseIf TypeOf result Is BufferObject Then
            buffer.data = DirectCast(result, BufferObject)
        Else
            Throw New NotImplementedException(result.GetType.FullName)
        End If

        Dim packageData As Byte() = New IPCBuffer(request_id, buffer).Serialize
        Dim request As New RequestStream(0, 0, packageData)
        Dim timeout As Boolean = False

        For i As Integer = 0 To retryTimes
            Call $"push callback data '{buffer.code.Description}' to [{master}] [{packageData.Length} bytes]".__INFO_ECHO
            Call New Tcp.TcpRequest(master).SendMessage(request, timeout:=timeoutMS, Sub() timeout = True)

            If Not timeout Then
                Exit For
            Else
                Call "operation timeout, retry...".__DEBUG_ECHO
            End If
        Next

        If Not result Is Nothing AndAlso result.GetType Is GetType(Message) Then
            Return DirectCast(result, Message).level
        Else
            Return 0
        End If
    End Function
End Module
