﻿#Region "Microsoft.VisualBasic::3f1c062e33b7deab339370d541a1f93c, R#\Interpreter\ExecuteEngine\ExpressionSymbols\CLI\CommandLine.vb"

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

'     Class CommandLine
' 
'         Properties: type
' 
'         Constructor: (+1 Overloads) Sub New
'         Function: Evaluate, isInterpolation, possibleInterpolationFailure
' 
' 
' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Invokes
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports devtools = Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics

Namespace Interpreter.ExecuteEngine

    ''' <summary>
    ''' External commandline invoke
    ''' 
    ''' ```
    ''' @"cli"
    ''' ```
    ''' </summary>
    Public Class CommandLine : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Dim cli As Expression

        Sub New(shell As Expression)
            cli = shell
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Dim commandline$ = Runtime.getFirst(cli.Evaluate(envir))
            Dim process As New IORedirectFile(commandline, isShellCommand:=True)
            Dim std_out$
            Dim error_code%

            If commandline.DoCall(AddressOf SyntaxImplements.isInterpolation) Then
                Call commandline _
                    .DoCall(Function(cli)
                                Return possibleInterpolationFailure(cli, envir)
                            End Function) _
                    .DoCall(AddressOf envir.globalEnvironment.messages.Add)
            End If

            error_code = process.Run()
            std_out = process.StandardOutput

            Return New list With {
                .slots = New Dictionary(Of String, Object) From {
                    {"std_out", std_out},
                    {"error_code", error_code}
                }
            }
        End Function

        Private Shared Function possibleInterpolationFailure(commandline As String, envir As Environment) As Message
            Return New Message With {
                .message = {
                    $"The input commandline string contains string interpolation syntax tag...",
                    $"commandline: " & commandline
                },
                .level = MSG_TYPES.WRN,
                .environmentStack = envir.getEnvironmentStack,
                .trace = devtools.ExceptionData.GetCurrentStackTrace
            }
        End Function
    End Class
End Namespace
