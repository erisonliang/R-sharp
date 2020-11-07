﻿#Region "Microsoft.VisualBasic::5d7dffe8ce034582f7062d059b70c385, R#\Interpreter\ExecuteEngine\ExpressionSymbols\DataSet\Literal.vb"

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

    '     Class Literal
    ' 
    '         Properties: [FALSE], [TRUE], NULL, type
    ' 
    '         Constructor: (+5 Overloads) Sub New
    '         Function: Evaluate, ToString
    '         Operators: <>, =
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.DataSets

    Public Class Literal : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return m_type
            End Get
        End Property

        Friend value As Object
        Friend m_type As TypeCodes

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

        Public ReadOnly Property ValueStr As String
            Get
                If value Is Nothing Then
                    Return ""
                Else
                    Return Scripting.ToString(value)
                End If
            End Get
        End Property

        Friend Sub New()
        End Sub

        Sub New(value As Double)
            Me.m_type = TypeCodes.double
            Me.value = value
        End Sub

        ''' <summary>
        ''' create a string literal
        ''' </summary>
        ''' <param name="value"></param>
        Sub New(value As String)
            Me.m_type = TypeCodes.string
            Me.value = value
        End Sub

        Sub New(value As Boolean)
            Me.m_type = TypeCodes.boolean
            Me.value = value
        End Sub

        Sub New(value As Integer)
            Me.m_type = TypeCodes.integer
            Me.value = CLng(value)
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <DebuggerStepThrough>
        Public Overrides Function Evaluate(envir As Environment) As Object
            Return value
        End Function

        ''' <summary>
        ''' Get string representation of the literal object value
        ''' </summary>
        ''' <returns></returns>
        ''' 
        <DebuggerStepThrough>
        Public Overrides Function ToString() As String
            If value Is Nothing Then
                Return "NULL"
            ElseIf TypeOf value Is String Then
                Return $"""{value}"""
            ElseIf TypeOf value Is Date Then
                Return $"#{value}#"
            Else
                Return value.ToString
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overloads Shared Operator =(exp As Literal, literal As String) As Boolean
            Return DirectCast(exp.value, String) = literal
        End Operator

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overloads Shared Operator <>(exp As Literal, literal As String) As Boolean
            Return Not exp = literal
        End Operator
    End Class
End Namespace