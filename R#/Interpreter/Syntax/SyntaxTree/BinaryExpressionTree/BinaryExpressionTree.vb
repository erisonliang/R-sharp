﻿#Region "Microsoft.VisualBasic::84ebf078952d1d669191eaa68d4dd749, R#\Interpreter\Syntax\SyntaxTree\BinaryExpressionTree\BinaryExpressionTree.vb"

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
    '         Function: joinNegatives, joinRemaining, ParseBinaryExpression, processOperators
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Operators
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser

    Module BinaryExpressionTree

        ReadOnly operatorPriority As String() = {"^", "*/%", "+-"}
        ReadOnly comparisonOperators As String() = {"<", ">", "<=", ">=", "==", "!=", "in", "like", "between"}
        ReadOnly logicalOperators As String() = {"&&", "||", "!"}

        <Extension>
        Private Function joinNegatives(tokenBlocks As List(Of Token()),
                                       ByRef buf As List(Of [Variant](Of SyntaxResult, String)),
                                       ByRef oplist As List(Of String),
                                       opts As SyntaxBuilderOptions) As SyntaxResult

            Dim syntaxResult As SyntaxResult
            Dim index As i32 = Scan0

            If tokenBlocks(Scan0).Length = 1 AndAlso tokenBlocks(Scan0)(Scan0) = (TokenType.operator, {"-", "+"}) Then
                ' insert a ZERO before
                tokenBlocks.Insert(Scan0, {New Token With {.name = TokenType.numberLiteral, .text = 0}})
            End If

            For i As Integer = Scan0 To tokenBlocks.Count - 1
                If ++index Mod 2 = 0 Then
                    If tokenBlocks(i).isOperator("+", "-") Then
                        syntaxResult = Expression.CreateExpression(tokenBlocks(i + 1), opts)

                        If syntaxResult.isException Then
                            Return syntaxResult
                        Else
                            syntaxResult = New BinaryExpression(
                                left:=New Literal(0),
                                right:=syntaxResult.expression,
                                op:=tokenBlocks(i)(Scan0).text
                            )
                            i += 1

                            Call buf.Add(syntaxResult)
                        End If
                    ElseIf tokenBlocks(i).isOperator("!") Then
                        ' not ...
                        syntaxResult = Expression.CreateExpression(tokenBlocks(i + 1), opts)

                        If syntaxResult.isException Then
                            Return syntaxResult
                        Else
                            syntaxResult = New UnaryNot(syntaxResult.expression)
                            i += 1

                            Call buf.Add(syntaxResult)
                        End If

                    Else
                        syntaxResult = Expression.CreateExpression(tokenBlocks(i), opts)

                        If syntaxResult.isException Then
                            Return syntaxResult
                        Else
                            Call buf.Add(syntaxResult)
                        End If
                    End If
                Else
                    Call buf.Add(tokenBlocks(i)(Scan0).text)
                    Call oplist.Add(buf.Last.VB)
                End If
            Next

            Return Nothing
        End Function

        <Extension>
        Public Function ParseBinaryExpression(tokenBlocks As List(Of Token()), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim buf As New List(Of [Variant](Of SyntaxResult, String))
            Dim lineNum As Integer = tokenBlocks(Scan0)(Scan0).span.line
            Dim oplist As New List(Of String)
            Dim syntaxResult As New Value(Of SyntaxResult)
            Dim processors As GenericSymbolOperatorProcessor() = {
                New NameMemberReferenceProcessor(),
                New NamespaceReferenceProcessor(),
                New PipelineProcessor(),     ' pipeline操作符是优先度最高的
                New VectorAppendProcessor()  ' append操作符
            }

            Call tokenBlocks.joinNegatives(buf, oplist, opts)

            Dim queue As New SyntaxQueue With {.buf = buf}

            For Each process As GenericSymbolOperatorProcessor In processors
                If Not (syntaxResult = process.JoinBinaryExpression(queue, oplist, opts)) Is Nothing Then
                    Return syntaxResult
                End If
            Next

            ' 算数操作符以及字符串操作符按照操作符的优先度进行构建
            If Not (syntaxResult = buf.processOperators(oplist, operatorPriority, test:=Function(op, o) op.IndexOf(o) > -1, opts)) Is Nothing Then
                Return syntaxResult
            End If

            ' 然后处理字符串操作符
            If Not (syntaxResult = buf.processOperators(oplist, {"&"}, test:=Function(op, o) op = o, opts)) Is Nothing Then
                Return syntaxResult
            End If

            ' 之后处理比较操作符
            If Not (syntaxResult = buf.processOperators(oplist, comparisonOperators, test:=Function(op, o) op = o, opts)) Is Nothing Then
                Return syntaxResult
            End If

            ' 最后处理逻辑操作符
            If Not (syntaxResult = buf.processOperators(oplist, logicalOperators, test:=Function(op, o) op = o, opts)) Is Nothing Then
                Return syntaxResult
            End If

            If buf > 1 Then
                Return buf.joinRemaining(lineNum, opts)
            Else
                Return buf(Scan0)
            End If
        End Function

        <Extension>
        Private Function joinRemaining(buf As List(Of [Variant](Of SyntaxResult, String)), lineNum%, opts As SyntaxBuilderOptions) As SyntaxResult
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
                Dim sourceMap As New StackFrame With {
                    .File = opts.source.fileName,
                    .Line = lineNum,
                    .Method = New Method With {
                        .Method = tokens(Scan0).TryCast(Of Expression).ToString,
                        .[Module] = "n/a",
                        .[Namespace] = SyntaxBuilderOptions.R_runtime
                    }
                }

                Return New ByRefFunctionCall(tokens(Scan0), tokens(2), sourceMap)
            ElseIf tokens.isNamespaceReferenceCall Then
                Dim calls As FunctionInvoke = buf(2).TryCast(Of Expression)
                Dim [namespace] As Expression = buf(Scan0).TryCast(Of Expression)

                Return New SyntaxResult(New NotImplementedException, opts.debug)
            ElseIf buf = 3 AndAlso tokens(1) Like GetType(String) AndAlso tokens(1).TryCast(Of String) Like ExpressionSignature.valueAssignOperatorSymbols Then
                ' set value by name
                Return New MemberValueAssign(tokens(Scan0), tokens(2))
            ElseIf tokens.isLambdaFunction Then
                Return SyntaxImplements.DeclareLambdaFunction(tokens(Scan0).VA, tokens(2).VA, lineNum, opts)
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
        Private Function processOperators(buf As List(Of [Variant](Of SyntaxResult, String)), oplist As List(Of String), operators$(), test As Func(Of String, String, Boolean), opts As SyntaxBuilderOptions) As SyntaxResult
            If buf = 1 Then
                Return Nothing
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
                            Dim a As SyntaxResult = If(j - 1 < 0, New SyntaxResult(New SyntaxErrorException, opts.debug), buf(j - 1).TryCast(Of SyntaxResult))
                            Dim b As SyntaxResult = If(j + 1 >= buf.Count, New SyntaxResult(New SyntaxErrorException, opts.debug), buf(j + 1).TryCast(Of SyntaxResult))

                            If a.isException Then
                                Return a
                            ElseIf b.isException Then
                                Return b
                            End If

                            Dim be As Expression
                            Dim opToken As String = buf(j).VB

                            If opToken = "in" Then
                                be = New BinaryInExpression(a.expression, b.expression)
                            ElseIf opToken = "between" Then
                                be = New BinaryBetweenExpression(a.expression, b.expression)
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

            Return Nothing
        End Function
    End Module
End Namespace
