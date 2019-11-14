﻿Imports System.Runtime.CompilerServices
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Interpreter.ExecuteEngine

    Public Class Literal : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Friend value As Object

        Public Shared ReadOnly Property NULL As Literal
            Get
                Return New Literal With {.value = Nothing}
            End Get
        End Property

        Public Shared ReadOnly Property [TRUE] As Literal
            Get
                Return New Literal(True)
            End Get
        End Property

        Public Shared ReadOnly Property [FALSE] As Literal
            Get
                Return New Literal(False)
            End Get
        End Property

        Sub New()
        End Sub

        Sub New(token As Token)
            Select Case token.name
                Case TokenType.booleanLiteral
                    type = TypeCodes.boolean
                    value = token.text.ParseBoolean
                Case TokenType.integerLiteral
                    type = TypeCodes.integer
                    value = CLng(token.text.ParseInteger)
                Case TokenType.numberLiteral
                    type = TypeCodes.double
                    value = token.text.ParseDouble
                Case TokenType.stringLiteral, TokenType.cliShellInvoke
                    type = TypeCodes.string
                    value = token.text
                Case TokenType.missingLiteral
                    type = TypeCodes.generic

                    Select Case token.text
                        Case "NULL" : value = Nothing
                        Case "NA" : value = GetType(Void)
                        Case "Inf" : value = Double.PositiveInfinity
                        Case Else
                            Throw New SyntaxErrorException
                    End Select

                Case Else
                    Throw New InvalidExpressionException(token.ToString)
            End Select
        End Sub

        Sub New(value As String)
            Me.type = TypeCodes.string
            Me.value = value
        End Sub

        Sub New(value As Boolean)
            Me.type = TypeCodes.boolean
            Me.value = value
        End Sub

        Sub New(value As Integer)
            Me.type = TypeCodes.integer
            Me.value = CLng(value)
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overrides Function Evaluate(envir As Environment) As Object
            ' Return Environment.asRVector(TypeCodes.generic, value)
            Return value
        End Function

        Public Overrides Function ToString() As String
            If value Is Nothing Then
                Return "NULL"
            Else
                Return value.ToString
            End If
        End Function
    End Class
End Namespace