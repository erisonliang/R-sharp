﻿#Region "Microsoft.VisualBasic::03ef089f78e2a842c7ee838b7d599d22, R#\Interpreter\Syntax\SyntaxTree\BinaryExpressionTree\BinaryExpressionTree.vb"

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

    '     Module BinaryExpressionTree
    ' 
    '         Function: ParseBinaryExpression
    ' 
    '         Sub: processOperators
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser

    Module BinaryExpressionTree

        ReadOnly operatorPriority As String() = {"^", "*/", "+-"}
        ReadOnly comparisonOperators As String() = {"<", ">", "<=", ">=", "==", "!=", "in", "like"}
        ReadOnly logicalOperators As String() = {"&&", "||", "!"}

        <Extension>
        Public Function ParseBinaryExpression(tokenBlocks As List(Of Token()), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim buf As New List(Of [Variant](Of SyntaxResult, String))
            Dim oplist As New List(Of String)
            Dim syntaxResult As SyntaxResult

            If tokenBlocks(Scan0).Length = 1 AndAlso tokenBlocks(Scan0)(Scan0) = (TokenType.operator, {"-", "+"}) Then
                ' insert a ZERO before
                tokenBlocks.Insert(Scan0, {New Token With {.name = TokenType.numberLiteral, .text = 0}})
            End If

            For i As Integer = Scan0 To tokenBlocks.Count - 1
                If i Mod 2 = 0 Then
                    syntaxResult = Expression.CreateExpression(tokenBlocks(i), opts)

                    If syntaxResult.isException Then
                        Return syntaxResult
                    Else
                        Call buf.Add(syntaxResult)
                    End If
                Else
                    Call buf.Add(tokenBlocks(i)(Scan0).text)
                    Call oplist.Add(buf.Last.VB)
                End If
            Next

            Dim processors As GenericSymbolOperatorProcessor() = {
                New NameMemberReferenceProcessor(),
                New NamespaceReferenceProcessor(),
                New PipelineProcessor(),     ' pipeline操作符是优先度最高的
                New VectorAppendProcessor()  ' append操作符
            }
            Dim queue As New SyntaxQueue With {.buf = buf}

            For Each process As GenericSymbolOperatorProcessor In processors
                Call process.JoinBinaryExpression(queue, oplist, opts)
            Next

            ' 算数操作符以及字符串操作符按照操作符的优先度进行构建
            Call buf.processOperators(oplist, operatorPriority, test:=Function(op, o) op.IndexOf(o) > -1)

            ' 然后处理字符串操作符
            Call buf.processOperators(oplist, {"&"}, test:=Function(op, o) op = o)

            ' 之后处理比较操作符
            Call buf.processOperators(oplist, comparisonOperators, test:=Function(op, o) op = o)

            ' 最后处理逻辑操作符
            Call buf.processOperators(oplist, logicalOperators, test:=Function(op, o) op = o)

            If buf > 1 Then
                Return buf.joinRemaining(opts)
            Else
                Return buf(Scan0)
            End If
        End Function

        <Extension>
        Private Function joinRemaining(buf As List(Of [Variant](Of SyntaxResult, String)), opts As SyntaxBuilderOptions) As SyntaxResult
            For Each a As [Variant](Of SyntaxResult, String) In buf
                If a.VA IsNot Nothing AndAlso a.VA.isException Then
                    Return a.VA
                End If
            Next

            Dim tokens As [Variant](Of Expression, String)() = buf _
                .Select(Function(a)
                            If a.VA Is Nothing Then
                                Return New [Variant](Of Expression, String)(a.VB)
                            Else
                                Return New [Variant](Of Expression, String)(a.VA.expression)
                            End If
                        End Function) _
                .ToArray

            If tokens.isByRefCall Then
                Return New ByRefFunctionCall(tokens(Scan0), tokens(2))
            ElseIf tokens.isNamespaceReferenceCall Then
                Dim calls As FunctionInvoke = buf(2).TryCast(Of Expression)
                Dim [namespace] As Expression = buf(Scan0).TryCast(Of Expression)

                Return New SyntaxResult(New NotImplementedException, opts.debug)
            ElseIf buf = 3 AndAlso tokens(1) Like GetType(String) AndAlso tokens(1).TryCast(Of String) Like ExpressionSignature.valueAssignOperatorSymbols Then
                ' set value by name
                Return New MemberValueAssign(tokens(Scan0), tokens(2))
            End If

            Return New SyntaxResult(New SyntaxErrorException, opts.debug)
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="buf"></param>
        ''' <param name="oplist"></param>
        ''' <param name="operators$"></param>
        ''' <param name="test">test(op, o)</param>
        <Extension>
        Private Sub processOperators(buf As List(Of [Variant](Of SyntaxResult, String)), oplist As List(Of String), operators$(), test As Func(Of String, String, Boolean))
            If buf = 1 Then
                Return
            End If

            For Each op As String In operators
                Dim nop As Integer = oplist _
                    .AsEnumerable _
                    .Count(Function(o) test(op, o))

                ' 从左往右计算
                For i As Integer = 0 To nop - 1
                    For j As Integer = 0 To buf.Count - 1
                        If buf(j) Like GetType(String) AndAlso test(op, buf(j).VB) Then
                            ' j-1 and j+1
                            Dim a As SyntaxResult = buf(j - 1)
                            Dim b As SyntaxResult = buf(j + 1)
                            Dim be As Expression
                            Dim opToken As String = buf(j).VB

                            If opToken = "in" Then
                                be = New FunctionInvoke("any", New BinaryExpression(a.expression, b.expression, "=="))
                            ElseIf opToken = "||" Then
                                be = New BinaryOrExpression(a.expression, b.expression)
                            Else
                                be = New BinaryExpression(a.expression, b.expression, buf(j).VB)
                            End If

                            Call buf.RemoveRange(j - 1, 3)
                            Call buf.Insert(j - 1, New SyntaxResult(be))

                            Exit For
                        End If
                    Next
                Next
            Next
        End Sub
    End Module
End Namespace
