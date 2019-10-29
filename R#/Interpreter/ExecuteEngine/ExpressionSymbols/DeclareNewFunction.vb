﻿Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine

    Public Class DeclareNewFunction : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return TypeCodes.closure
            End Get
        End Property

        Friend funcName$
        Friend params As DeclareNewVariable()
        Friend body As Program

        Sub New()
        End Sub

        Sub New(code As List(Of Token()))
            Dim [declare] = code(4)
            Dim parts = [declare].SplitByTopLevelDelimiter(TokenType.close)
            Dim paramPart = parts(Scan0).Skip(1).ToArray
            Dim bodyPart = parts(2).Skip(1).ToArray

            funcName = code(1)(Scan0).text

            Call getParameters(paramPart)
            Call getExecBody(bodyPart)
        End Sub

        Private Sub getParameters(tokens As Token())
            Dim parts = tokens.SplitByTopLevelDelimiter(TokenType.comma) _
                .Where(Function(t) Not t.isComma) _
                .ToArray

            params = parts _
                .Select(Function(t)
                            Dim [let] As New List(Of Token) From {
                                New Token With {.name = TokenType.keyword, .text = "let"}
                            }
                            Return New DeclareNewVariable([let] + t)
                        End Function) _
                .ToArray
        End Sub

        Private Sub getExecBody(tokens As Token())
            body = New Program With {
               .execQueue = tokens.GetExpressions.ToArray
            }
        End Sub

        Public Function Invoke(envir As Environment, params As Object()) As Object
            Dim var As DeclareNewVariable
            Dim value As Object

            envir = New Environment(envir, funcName)

            ' initialize environment
            For i As Integer = 0 To Me.params.Length - 1
                var = Me.params(i)

                If i >= params.Length Then
                    ' missing, use default value
                    If var.hasInitializeExpression Then
                        value = var.value.Evaluate(envir)
                    Else
                        Throw New MissingFieldException(var.names.GetJson)
                    End If
                Else
                    value = params(i)
                End If

                Call DeclareNewVariable.PushNames(var.names, value, var.type, envir)
            Next

            Return body.Execute(envir)
        End Function

        Public Overrides Function Evaluate(envir As Environment) As Object
            Return envir.Push(funcName, Me, TypeCodes.closure)
        End Function

        Public Overrides Function ToString() As String
            Return $"declare function '${funcName}'"
        End Function
    End Class
End Namespace