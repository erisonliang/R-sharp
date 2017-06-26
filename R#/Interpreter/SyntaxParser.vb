﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Scripting.TokenIcer

Public Module SyntaxParser

    Const SyntaxNotSupport$ = "The syntax is currently not support yet!"

    ''' <summary>
    ''' Convert the string token model as the language runtime model
    ''' </summary>
    ''' <param name="statement"></param>
    ''' <returns></returns>
    <Extension> Public Function Parse(statement As Statement(Of LanguageTokens)) As PrimitiveExpression
        Dim expression As PrimitiveExpression = Nothing

        If TryParseTypedObjectDeclare(statement, expression) Then
            Return expression
        ElseIf TryParseObjectDeclare(statement, expression) Then
            Return expression
        End If

        Dim source As New Exception(statement.GetXml)
        Throw New SyntaxErrorException(SyntaxNotSupport, source)
    End Function

    ''' <summary>
    ''' ```R
    ''' var x &lt;- "string";
    ''' ```
    ''' </summary>
    ''' <param name="statement"></param>
    ''' <returns></returns>
    Public Function TryParseObjectDeclare(statement As Statement(Of LanguageTokens), ByRef out As PrimitiveExpression) As Boolean
        Dim tokens = statement.tokens
        Dim var$ = tokens.ElementAtOrDefault(1)?.Text  ' 变量名

        If Not tokens.First.name = LanguageTokens.Variable Then
            Return False
        ElseIf Not tokens(2).name = LanguageTokens.LeftAssign Then
            ' var x
            ' 这种形式的申明默认为NULL

            If tokens.Length = 2 Then
                out = New VariableDeclareExpression(var, NameOf(TypeCodes.generic), LiteralExpression.NULL)
                Return True
            Else
                Return False
            End If
        End If

        ' 现在剩下的就是 var x <- ..... 的形式了
        ' 需要解析这个数学表达式
        Dim initExpression = tokens.Skip(3).ToArray


    End Function

    ''' <summary>
    ''' ```R
    ''' var x as string &lt;- "string"; 
    ''' ```
    ''' </summary>
    ''' <param name="statement"></param>
    ''' <returns></returns>
    Public Function TryParseTypedObjectDeclare(statement As Statement(Of LanguageTokens), ByRef out As PrimitiveExpression) As Boolean
        Dim tokens = statement.tokens
        Dim var$ = tokens.ElementAtOrDefault(1)?.Text

        If Not tokens.First.name = LanguageTokens.Variable Then
            Return False
        ElseIf Not tokens(2).Text = "as" Then
            ' 没有类型约束，则肯定不是这种类型的表达式
            Return False
        ElseIf tokens.Length = 4 Then
            ' var x as type
            ' 只是申明了变量和其类型，则默认是NULL值
            out = New VariableDeclareExpression(var, tokens(3).Text, LiteralExpression.NULL)
            Return True
        End If



    End Function
End Module
