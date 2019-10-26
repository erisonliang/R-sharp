﻿Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine

    Public Class FunctionInvoke : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Dim funcName As String
        Dim parameters As Expression()

        Sub New(tokens As Token())
            funcName = tokens(Scan0).text
            parameters = tokens _
                .Skip(2) _
                .Take(tokens.Length - 3) _
                .ToArray _
                .SplitByTopLevelDelimiter(TokenType.comma) _
                .Select(Function(param) Expression.CreateExpression(param)) _
                .ToArray
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Dim paramVals As Object() = parameters _
                .Select(Function(a) a.Evaluate(envir)) _
                .ToArray
            ' 当前环境中的函数符号的优先度要高于
            ' 系统环境下的函数符号
            Dim funcVar As Variable = envir.FindSymbol(funcName)

            If funcVar Is Nothing Then
                ' 可能是一个系统的内置函数
                Throw New NotImplementedException
            Else
                Return DirectCast(funcVar.value, RMethodInfo).Invoke(envir, paramVals)
            End If
        End Function
    End Class
End Namespace