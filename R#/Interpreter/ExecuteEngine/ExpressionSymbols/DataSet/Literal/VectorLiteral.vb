﻿#Region "Microsoft.VisualBasic::4a17ee44851609e4a33e758bdc97616e, R#\Interpreter\ExecuteEngine\ExpressionSymbols\DataSet\Literal\VectorLiteral.vb"

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

    '     Class VectorLiteral
    ' 
    '         Properties: expressionName, length, type
    ' 
    '         Constructor: (+2 Overloads) Sub New
    '         Function: Evaluate, GetEnumerator, IEnumerable_GetEnumerator, ToArray, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.SyntaxParser
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Development.Package.File

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.DataSets

    ''' <summary>
    ''' ```
    ''' [x,y,z,...]
    ''' ```
    ''' </summary>
    Public Class VectorLiteral : Inherits Expression
        Implements IEnumerable(Of Expression)

        Public Overrides ReadOnly Property type As TypeCodes

        Public Overrides ReadOnly Property expressionName As ExpressionTypes
            Get
                Return ExpressionTypes.VectorLiteral
            End Get
        End Property

        Public ReadOnly Property length As Integer
            Get
                Return values.Length
            End Get
        End Property

        Friend ReadOnly values As Expression()

        Sub New(values As Expression(), type As TypeCodes)
            Me.values = values
            Me.type = type
        End Sub

        Sub New(values As IEnumerable(Of Expression))
            Me.values = values.ToArray
            Me.type = Me.values.DoCall(AddressOf SyntaxImplements.TypeCodeOf)
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Dim vector As Array = values _
                .Select(Function(exp)
                            Return Runtime.[single](exp.Evaluate(envir))
                        End Function) _
                .ToArray
            Dim type As Type = MeasureRealElementType(vector)

            If Not type Is GetType(Void) AndAlso Not type Is GetType(Object) Then
                Return New vector(type, vector, envir)
            Else
                Return Environment.asRVector(Me.type, vector)
            End If
        End Function

        Public Function ToArray() As Expression()
            Return values.ToArray
        End Function

        Public Overrides Function ToString() As String
            Return $"[{values.JoinBy(", ")}]"
        End Function

        Public Iterator Function GetEnumerator() As IEnumerator(Of Expression) Implements IEnumerable(Of Expression).GetEnumerator
            For Each value As Expression In Me.values
                Yield value
            Next
        End Function

        Private Iterator Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Yield GetEnumerator()
        End Function
    End Class
End Namespace
