﻿#Region "Microsoft.VisualBasic::47a36896ecb8c2f5d049bd54b945d4ea, R#\Runtime\Internal\debug.vb"

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

    '     Class debug
    ' 
    '         Properties: verbose
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: [stop], createDotNetExceptionMessage, CreateMessageInternal, getEnvironmentStack, getMessageColor
    '                   getMessagePrefix, PrintMessageInternal, PrintRExceptionStackTrace, PrintRStackTrace, PrintWarningMessages
    ' 
    '         Sub: write, writeErrMessage
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.My
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Runtime.Components
Imports devtools = Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics

Namespace Runtime.Internal

    Public NotInheritable Class debug

        ''' <summary>
        ''' 啰嗦模式下会输出一些调试信息
        ''' </summary>
        ''' <returns></returns>
        Public Shared Property verbose As Boolean = False

        Private Sub New()
        End Sub

        ''' <summary>
        ''' 只在<see cref="verbose"/>啰嗦模式下才会工作
        ''' </summary>
        ''' <param name="message$"></param>
        ''' <param name="color"></param>
        Public Shared Sub write(message$, Optional color As ConsoleColor = ConsoleColor.White)
            If verbose Then
                Call VBDebugger.WaitOutput()
                Call Log4VB.Print(message & ASCII.LF, color)
            End If
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="message"></param>
        ''' <param name="envir"></param>
        ''' <param name="suppress">
        ''' this parameter indicated that the R environment should not 
        ''' throw the exception when running in debug mode. 
        ''' </param>
        ''' <returns></returns>
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function [stop](message As Object, envir As Environment, Optional suppress As Boolean = False) As Message
            Dim debugMode As Boolean = envir.globalEnvironment.debugMode AndAlso Not suppress

            If Not message Is Nothing AndAlso message.GetType.IsInheritsFrom(GetType(Exception), strict:=False) Then
                Call App.LogException(DirectCast(message, Exception), trace:=getEnvironmentStack(envir).JoinBy(vbCrLf))

                If debugMode Then
                    Throw DirectCast(message, Exception)
                Else
                    Return createDotNetExceptionMessage(DirectCast(message, Exception), envir)
                End If
            ElseIf message.GetType Is GetType(Message) Then
                If debugMode Then
                    Dim err As New Exception(DirectCast(message, Message).message.JoinBy("; "))
                    Call App.LogException(err)
                    Throw err
                Else
                    Return message
                End If
            Else
                If debugMode Then
                    Dim err As New Exception(Runtime.asVector(Of Object)(message) _
                       .AsObjectEnumerator _
                       .SafeQuery _
                       .Select(Function(o) Scripting.ToString(o, "NULL")) _
                       .JoinBy("; ")
                    )
                    Call App.LogException(err)
                    Throw err
                Else
                    Return debug.CreateMessageInternal(message, envir, level:=MSG_TYPES.ERR)
                End If
            End If
        End Function

        Friend Shared Function getEnvironmentStack(parent As Environment) As StackFrame()
            Dim frames As New List(Of StackFrame)

            Do While Not parent Is Nothing
                frames += parent.stackFrame
                parent = parent.parent
            Loop

            Return frames
        End Function

        ''' <summary>
        ''' Create R# internal message
        ''' </summary>
        ''' <param name="messages"></param>
        ''' <param name="envir"></param>
        ''' <param name="level">The message level</param>
        ''' <returns></returns>
        Friend Shared Function CreateMessageInternal(messages As Object, envir As Environment, level As MSG_TYPES) As Message
            Return New Message With {
                .message = Runtime.asVector(Of Object)(messages) _
                    .AsObjectEnumerator _
                    .SafeQuery _
                    .Select(Function(o) Scripting.ToString(o, "NULL")) _
                    .ToArray,
                .level = level,
                .environmentStack = envir.DoCall(AddressOf getEnvironmentStack),
                .trace = devtools.ExceptionData.GetCurrentStackTrace
            }
        End Function

        Private Shared Function createDotNetExceptionMessage(ex As Exception, envir As Environment) As Message
            Dim messages As New List(Of String)
            Dim exception As Exception = ex

            Do While Not ex Is Nothing
                messages += ex.GetType.Name & ": " & ex.Message
                ex = ex.InnerException
            Loop

            ' add stack info for display
            If exception.StackTrace.StringEmpty Then
                messages += "stackFrames: none"
            Else
                messages += "stackFrames: " & vbCrLf & exception.StackTrace
            End If

            Return New Message With {
                .message = messages,
                .environmentStack = envir.DoCall(AddressOf getEnvironmentStack),
                .level = MSG_TYPES.ERR,
                .trace = devtools.ExceptionData.GetCurrentStackTrace
            }
        End Function

        Public Shared Function PrintRExceptionStackTrace(err As ExceptionData) As String
            Return PrintRStackTrace(err.StackTrace)
        End Function

        Public Shared Function PrintRStackTrace(stacktrace As StackFrame()) As String
            Dim info As New StringBuilder

            For Each frame As StackFrame In stacktrace
                Call info.AppendLine(frame.ToString)
            Next

            Return info.ToString
        End Function

        Public Shared Function PrintWarningMessages(warnings As IEnumerable(Of Message), globalEnv As GlobalEnvironment, Optional all As Boolean = False) As Object
            Dim i As i32 = 1
            Dim backup As ConsoleColor
            Dim dev As StreamWriter = New StreamWriter(globalEnv.stdout.stream)
            Dim topn As Integer = globalEnv.options.nwarnings
            Dim warningList As Message() = warnings.ToArray

            If App.IsConsoleApp Then
                backup = Console.ForegroundColor
                Console.ForegroundColor = ConsoleColor.Yellow
            End If

            Call globalEnv.stdout.Flush()

            If Not all AndAlso warningList.Length >= topn Then
                Call dev.WriteLine($"There were {topn} or more warnings (use warnings(all = TRUE) to see all warning messages).")
            End If

            Call dev.WriteLine("Warning messages:")

            For Each msg As Message In If(all, warningList, warningList.Take(topn))
                dev.WriteLine($"  {++i}. {msg.message.JoinBy("; ")}")
            Next

            Call dev.Flush()

            If App.IsConsoleApp Then
                Console.ForegroundColor = backup
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="message"></param>
        ''' <param name="globalEnv">这个参数可以为空，空值的时候会默认将消息打印在标准输出</param>
        ''' <returns></returns>
        Public Shared Function PrintMessageInternal(message As Message, globalEnv As GlobalEnvironment) As Object
            Dim stdout As StreamWriter
            Dim redirectErr2stdout As Boolean

            If globalEnv Is Nothing Then
                stdout = App.StdOut
                redirectErr2stdout = False
            Else
                stdout = New StreamWriter(globalEnv.stdout.stream, Encodings.ASCII.CodePage)
                redirectErr2stdout = globalEnv.Rscript.redirectError2stdout
                globalEnv.stdout.Flush()
            End If

            Call writeErrMessage(message, stdout, redirectErr2stdout)

            Return 500
        End Function

        Public Shared Sub writeErrMessage(message As Message, Optional stdout As StreamWriter = Nothing, Optional redirectError2stdout As Boolean = False)
            Dim execRoutine$ = message.environmentStack _
                .SafeQuery _
                .Reverse _
                .Select(Function(frame) frame.Method.Method) _
                .JoinBy(" -> ")
            Dim i As i32 = 1
            Dim backup As ConsoleColor
            Dim dev As StreamWriter

            If App.IsConsoleApp Then
                backup = Console.ForegroundColor
            End If

            If message.level = MSG_TYPES.ERR AndAlso Not redirectError2stdout Then
                dev = App.StdErr
            Else
                dev = stdout
            End If

            If App.IsConsoleApp Then
                Console.ForegroundColor = message.DoCall(AddressOf getMessageColor)
            End If

            dev.WriteLine($" {message.DoCall(AddressOf getMessagePrefix)} in {execRoutine}")

            For Each msg As String In message
                dev.WriteLine($"  {++i}. {msg}")
            Next

            If Not message.source Is Nothing Then
                Call dev.WriteLine()
                Call dev.WriteLine($" R# source: {message.source.ToString}")
            End If

            If Not message.environmentStack.IsNullOrEmpty Then
                Call dev.WriteLine()
                Call dev.WriteLine(debug.PrintRStackTrace(message.environmentStack))
            End If

            Call dev.Flush()

            If App.IsConsoleApp Then
                Console.ForegroundColor = backup
            End If
        End Sub

        Private Shared Function getMessagePrefix(message As Message) As String
            Select Case message.level
                Case MSG_TYPES.ERR : Return "Error"
                Case MSG_TYPES.INF : Return "Information"
                Case MSG_TYPES.WRN : Return "Warning"
                Case MSG_TYPES.DEBUG : Return "Debug output"
                Case Else
                    Return "Message"
            End Select
        End Function

        Private Shared Function getMessageColor(message As Message) As ConsoleColor
            Select Case message.level
                Case MSG_TYPES.ERR : Return ConsoleColor.Red
                Case MSG_TYPES.INF : Return ConsoleColor.Blue
                Case MSG_TYPES.WRN : Return ConsoleColor.Yellow
                Case MSG_TYPES.DEBUG : Return ConsoleColor.Green
                Case Else
                    Return ConsoleColor.White
            End Select
        End Function
    End Class
End Namespace
