﻿''' <summary>
''' Logical and arithmetic expression
''' </summary>
Public Class OperatorExpression : Inherits PrimitiveExpression

    Public Property [Operator] As String

End Class

''' <summary>
''' 单目运算符表达式
''' </summary>
Public Class UnaryOperator : Inherits OperatorExpression

    Public Property Expression As PrimitiveExpression

End Class

Public Class BinaryOperator : Inherits OperatorExpression

    Public Property a As PrimitiveExpression
    Public Property b As PrimitiveExpression

End Class

''' <summary>
''' ```R
''' a &lt;- b
''' ```
''' </summary>
Public Class ValueAssign : Inherits BinaryOperator

    Public Property IsByRef As Boolean = False

    Public Overrides Function ToString() As String
        If IsByRef Then
            Return $"{a} = {b}"
        Else
            Return $"{a} <- {b}"
        End If
    End Function
End Class