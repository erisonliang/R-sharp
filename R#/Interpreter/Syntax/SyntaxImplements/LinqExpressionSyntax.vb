﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Emit.Marshal
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module LinqExpressionSyntax

        Public Function LinqExpression(tokens As List(Of Token())) As SyntaxResult
            Dim variables As New List(Of String)
            Dim i As Integer = 0
            Dim sequence As SyntaxResult = Nothing
            Dim locals As New List(Of DeclareNewVariable)

            For i = 1 To tokens.Count - 1
                If tokens(i).isIdentifier Then
                    variables.Add(tokens(i)(Scan0).text)
                ElseIf tokens(i).isKeyword("in") Then
                    sequence = Expression.CreateExpression(tokens(i + 1))
                    Exit For
                End If
            Next

            If sequence Is Nothing Then
                Return New SyntaxResult(New SyntaxErrorException)
            ElseIf sequence.isException Then
                Return sequence
            Else
                i += 2
                locals = New DeclareNewVariable With {
                    .names = variables.ToArray,
                    .hasInitializeExpression = False,
                    .value = Nothing
                }
            End If

            tokens = tokens _
                .Skip(i) _
                .IteratesALL _
                .SplitByTopLevelDelimiter(TokenType.keyword)

            Dim projection As Expression = Nothing
            Dim output As ClosureExpression = Nothing
            Dim program As ClosureExpression = Nothing
            Dim [error] As SyntaxResult = New Pointer(Of Token())(tokens).doParseLINQProgram(
                locals:=locals,
                projection:=projection,
                output:=output,
                programClosure:=program
            )

            If [error].isException Then
                Return [error]
            Else
                Return New LinqExpression(locals, sequence.expression, program, projection, output)
            End If
        End Function

        ReadOnly linqKeywordDelimiters As String() = {"where", "distinct", "select", "order", "group", "let"}

        <Extension>
        Private Function doParseLINQProgram(p As Pointer(Of Token()), locals As List(Of DeclareNewVariable),
                                            ByRef projection As Expression,
                                            ByRef output As ClosureExpression,
                                            ByRef programClosure As ClosureExpression) As SyntaxResult

            Dim buffer As New List(Of Token())
            Dim token As Token()
            Dim program As New List(Of Expression)
            Dim outputs As New List(Of Expression)

            Do While Not p.EndRead
                buffer *= 0
                token = ++p

                If token.isKeyword Then
                    Select Case token(Scan0).text
                        Case "let"
                            buffer += token

                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            Dim declares = buffer _
                                .IteratesALL _
                                .SplitByTopLevelDelimiter(TokenType.operator, True) _
                                .DoCall(Function(blocks)
                                            Return SyntaxImplements.DeclareNewVariable(blocks)
                                        End Function)

                            If declares.isException Then
                                Return declares
                            End If

                            Dim [declare] As DeclareNewVariable = declares.expression

                            program += New ValueAssign([declare].names, [declare].value)
                            locals += [declare]
                            [declare].value = Nothing
                        Case "where"
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            Dim exprSyntax As SyntaxResult = buffer _
                                .IteratesALL _
                                .DoCall(AddressOf Expression.CreateExpression)

                            If exprSyntax.isException Then
                                Return exprSyntax
                            End If

                            ' 需要取反才可以正常执行中断语句
                            ' 例如 where 5 < 2
                            ' if test的结果为false
                            ' 则当前迭代循环需要跳过
                            ' 即执行trueclosure部分
                            ' 或者添加一个else closure
                            Dim booleanExp As New BinaryExpression(exprSyntax.expression, Literal.FALSE, "==")
                            program += New IfBranch(booleanExp, {New ReturnValue(Literal.NULL)})
                        Case "distinct"
                            outputs += New FunctionInvoke("unique", New SymbolReference("$"))
                        Case "order"
                            ' order by xxx asc
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            token = buffer.IteratesALL.ToArray

                            If Not token(Scan0).isKeyword("by") Then
                                Return New SyntaxResult(New SyntaxErrorException)
                            End If

                            ' skip first by keyword
                            Dim exprSyntax As SyntaxResult = token _
                                .Skip(1) _
                                .Take(token.Length - 2) _
                                .DoCall(AddressOf Expression.CreateExpression)

                            If exprSyntax.isException Then
                                Return exprSyntax
                            End If

                            outputs += New FunctionInvoke("sort", exprSyntax.expression, New Literal(token.Last.isKeyword("descending")))
                        Case "select"
                            If Not projection Is Nothing Then
                                Return New SyntaxResult(New SyntaxErrorException("Only allows one project function!"))
                            End If

                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            Dim projectSyntax = Expression.CreateExpression(buffer.IteratesALL)

                            If projectSyntax.isException Then
                                Return projectSyntax
                            Else
                                projection = projectSyntax.expression
                            End If

                            If TypeOf projection Is VectorLiteral Then
                                projection = New FunctionInvoke("list", DirectCast(projection, VectorLiteral).ToArray)
                            End If
                        Case "group"
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            Return New SyntaxResult(New NotImplementedException)
                        Case Else
                            Return New SyntaxResult(New SyntaxErrorException)
                    End Select
                End If
            Loop

            programClosure = New ClosureExpression(program.ToArray)
            output = New ClosureExpression(outputs.ToArray)

            Return Nothing
        End Function
    End Module
End Namespace