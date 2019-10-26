﻿Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine

    Public Class Literal : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Dim value As Object

        Sub New(token As Token)
            Select Case token.name
                Case TokenType.booleanLiteral
                    type = TypeCodes.boolean
                    value = token.text.ParseBoolean
                Case TokenType.integerLiteral
                    type = TypeCodes.integer
                    value = token.text.ParseInteger
                Case TokenType.numberLiteral
                    type = TypeCodes.double
                    value = token.text.ParseDouble
                Case TokenType.stringLiteral
                    type = TypeCodes.string
                    value = token.text
                Case Else
                    Throw New InvalidExpressionException(token.ToString)
            End Select
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Return value
        End Function
    End Class
End Namespace