﻿#Region "Microsoft.VisualBasic::57ac18de4a7bf15b435ef45f46cc1b7f, R#\Runtime\Interop\RsharpOperator\BinaryOperatorEngine.vb"

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

    '     Module BinaryOperatorEngine
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: getOperator
    ' 
    '         Sub: addBinary, addEtcTypeCompres, addFloatOperators, addIntegerOperators, addMixedOperators
    '              arithmeticOperators
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Runtime.Interop

    Public Module BinaryOperatorEngine

        ReadOnly index As New Dictionary(Of String, BinaryIndex)

        Sub New()
            Call arithmeticOperators()
            Call addEtcTypeCompres()
        End Sub

        Private Sub arithmeticOperators()
            Dim numerics As RType() = New Type() {
                GetType(Long),
                GetType(Double)
            }.Select(AddressOf RType.GetRSharpType) _
             .ToArray

            Call addIntegerOperators()
            Call addFloatOperators()

            For Each left As RType In numerics
                For Each right As RType In numerics
                    Call addMixedOperators(left, right)
                Next
            Next
        End Sub

        Private Sub addEtcTypeCompres()
            Dim dateType As RType = RType.GetRSharpType(GetType(Date))

            Call addBinary(dateType, dateType, "==", Function(a, b, env)
                                                         Return BinaryCoreInternal(Of Date, Date, Boolean)(
                                                             x:=asVector(Of Date)(a),
                                                             y:=asVector(Of Date)(b),
                                                             [do]:=Function(x, y)
                                                                       Dim dx = DirectCast(x, Date)
                                                                       Dim dy = DirectCast(y, Date)

                                                                       If dx.Year <> dy.Year Then
                                                                           Return False
                                                                       ElseIf dx.Month <> dy.Month Then
                                                                           Return False
                                                                       ElseIf dx.Day <> dy.Day Then
                                                                           Return False
                                                                       Else
                                                                           Return True
                                                                       End If
                                                                   End Function) _
                                                             .ToArray
                                                     End Function, Nothing)
        End Sub

        Private Sub addFloatOperators()
            Dim left As RType = RType.GetRSharpType(GetType(Double))
            Dim right As RType = RType.GetRSharpType(GetType(Double))

            Call addBinary(left, right, "+", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) + DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "-", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) - DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "*", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) * DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "/", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) / DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "%", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) Mod DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "^", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) ^ DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "<", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) < DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, ">", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) > DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "<=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) <= DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, ">=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) >= DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "==", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) = DirectCast(y, Double)).ToArray, Nothing)
            Call addBinary(left, right, "!=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) <> DirectCast(y, Double)).ToArray, Nothing)
        End Sub

        Private Sub addIntegerOperators()
            Dim left As RType = RType.GetRSharpType(GetType(Long))
            Dim right As RType = RType.GetRSharpType(GetType(Long))

            Call addBinary(left, right, "+", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Long)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) + DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "-", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Long)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) - DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "*", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Long)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) * DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "/", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Double)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) / DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "%", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Long)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) Mod DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "^", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Double)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) ^ DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "<", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) < DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, ">", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) > DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "<=", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) <= DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, ">=", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) >= DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "==", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) = DirectCast(y, Long)).ToArray, Nothing)
            Call addBinary(left, right, "!=", Function(a, b, env) BinaryCoreInternal(Of Long, Long, Boolean)(asVector(Of Long)(a), asVector(Of Long)(b), Function(x, y) DirectCast(x, Long) <> DirectCast(y, Long)).ToArray, Nothing)
        End Sub

        Private Sub addMixedOperators(left As RType, right As RType)
            Call addBinary(left, right, "+", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) + DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "-", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) - DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "*", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) * DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "/", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) / DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "%", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) Mod DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "^", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Double)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) ^ DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "<", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) < DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, ">", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) > DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "<=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) <= DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, ">=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) >= DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "==", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) = DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
            Call addBinary(left, right, "!=", Function(a, b, env) BinaryCoreInternal(Of Double, Double, Boolean)(asVector(Of Double)(a), asVector(Of Double)(b), Function(x, y) DirectCast(x, Double) <> DirectCast(y, Double)).ToArray, Nothing, [overrides]:=False)
        End Sub

        Public Function getOperator(symbol As String, env As Environment) As [Variant](Of BinaryIndex, Message)
            If index.ContainsKey(symbol) Then
                Return index(symbol)
            Else
                Return Internal.debug.stop({$"missing operator '{symbol}'", $"symbol: {symbol}"}, env)
            End If
        End Function

        Public Sub addBinary(left As RType, right As RType, symbol As String, op As IBinaryOperator, env As Environment, Optional [overrides] As Boolean = True)
            If Not index.ContainsKey(symbol) Then
                index.Add(symbol, New BinaryIndex(symbol))
            End If

            If Not [overrides] AndAlso index(symbol).hasOperator(left, right) Then
                Return
            Else
                Call index(symbol).addOperator(left, right, op, env)
            End If
        End Sub

    End Module
End Namespace
