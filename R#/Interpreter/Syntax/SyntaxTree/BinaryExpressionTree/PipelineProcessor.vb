﻿#Region "Microsoft.VisualBasic::3339513a02e244777647f31609db3569, R#\Interpreter\Syntax\SyntaxTree\BinaryExpressionTree\PipelineProcessor.vb"

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

    '     Class PipelineProcessor
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: buildPipeline, expression, isFunctionTuple
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine

Namespace Interpreter.SyntaxParser

    Friend Class PipelineProcessor : Inherits GenericSymbolOperatorProcessor

        Public Sub New()
            MyBase.New(":>")
        End Sub

        Protected Overrides Function expression(a As [Variant](Of SyntaxResult, String), b As [Variant](Of SyntaxResult, String), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim pip As Expression

            If a.VA.isException Then
                Return a
            ElseIf b.VA.isException Then
                Return b
            Else
                pip = buildPipeline(a.VA.expression, b.VA.expression, opts)
            End If

            If pip Is Nothing Then
                If b.VA.expression.DoCall(AddressOf isFunctionTuple) Then
                    Dim invokes As VectorLiteral = b.VA.expression
                    Dim calls As New List(Of Expression)

                    For Each [call] As Expression In invokes
                        calls += buildPipeline(a.VA.expression, [call], opts)
                    Next

                    Return New SyntaxResult(New VectorLiteral(calls))
                Else
                    Return New SyntaxResult(New SyntaxErrorException, opts.debug)
                End If
            Else
                Return New SyntaxResult(pip)
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Private Function isFunctionTuple(b As Expression) As Boolean
            If Not TypeOf b Is VectorLiteral Then
                Return False
            ElseIf Not DirectCast(b, VectorLiteral) _
                .All(Function(e)
                         Return TypeOf e Is FunctionInvoke OrElse TypeOf e Is SymbolReference
                     End Function) Then

                Return False
            End If

            Return True
        End Function

        Private Function buildPipeline(a As Expression, b As Expression, opts As SyntaxBuilderOptions) As Expression
            Dim pip As FunctionInvoke

            If TypeOf a Is VectorLiteral Then
                With DirectCast(a, VectorLiteral)
                    If .length = 1 AndAlso TypeOf .First Is ValueAssign Then
                        a = .First
                    End If
                End With
            End If

            If TypeOf b Is FunctionInvoke Then
                pip = b
                pip.parameters.Insert(Scan0, a)
            ElseIf TypeOf b Is SymbolReference Then
                Dim name$ = DirectCast(b, SymbolReference).symbol
                Dim stacktrace As New StackFrame With {
                    .File = opts.source.fileName,
                    .Line = "n/a",
                    .Method = New Method With {
                        .Method = name,
                        .[Module] = "call_function",
                        .[Namespace] = "SMRUCC/R#"
                    }
                }

                pip = New FunctionInvoke(name, stacktrace, a)
            Else
                pip = Nothing
            End If

            Return pip
        End Function
    End Class
End Namespace