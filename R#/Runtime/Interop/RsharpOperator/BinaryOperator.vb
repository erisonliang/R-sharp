﻿#Region "Microsoft.VisualBasic::4c9a858903dbc495ecc5e64a82c893e9, R#\Runtime\Interop\RsharpOperator\BinaryOperator.vb"

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

    '     Delegate Function
    ' 
    ' 
    '     Class BinaryOperator
    ' 
    '         Properties: left, operatorSymbol, right
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: Execute, ToString
    ' 
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object

Namespace Runtime.Interop

    Public Delegate Function IBinaryOperator(left As Object, right As Object, env As Environment) As Object

    Public Class BinaryOperator

        Public Property operatorSymbol As String
        Public Property left As RType
        Public Property right As RType

        ReadOnly operation As IBinaryOperator

        Sub New(op As IBinaryOperator)
            operation = op
        End Sub

        ''' <summary>
        ''' 主要是为了兼容R#语言的<see cref="unit"/>特性
        ''' </summary>
        ''' <param name="left"></param>
        ''' <param name="right"></param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        Public Function Execute(left As Object, right As Object, env As Environment) As Object
            Dim result As Object = operation(left, right, env)
            Dim unitL As unit = Nothing
            Dim unitR As unit = Nothing
            Dim unit As unit

            If TypeOf left Is vector Then
                unitL = DirectCast(left, vector).unit
            End If
            If TypeOf right Is vector Then
                unitR = DirectCast(right, vector).unit
            End If

            If (Not result Is Nothing) AndAlso (Not (unitL Is Nothing AndAlso unitR Is Nothing)) Then
                If unitL Is Nothing Then
                    unit = unitR
                ElseIf unitR Is Nothing Then
                    If operatorSymbol = "^" Then
                        Dim type = right.GetType.GetRTypeCode

                        ' 在这里主要是为了自动化生成单位转换，例如
                        ' s^-1或者m^2
                        If type = TypeCodes.double OrElse type = TypeCodes.integer Then
                            right = asVector(Of Object)(right)

                            If DirectCast(right, Array).Length = 1 Then
                                unit = New unit With {
                                    .name = $"{unitL.name}^{DirectCast(right, Array).GetValue(Scan0)}"
                                }
                            Else
                                unit = unitL
                            End If
                        Else
                            unit = unitL
                        End If
                    Else
                        unit = unitL
                    End If
                Else
                    unit = New unit With {
                        .name = $"{unitL.name}{operatorSymbol}{unitR.name}"
                    }
                End If

                If TypeOf result Is vector Then
                    DirectCast(result, vector).unit = unit
                ElseIf result.GetType.IsArray Then
                    result = New vector(MeasureRealElementType(result), result, env)
                    DirectCast(result, vector).unit = unit
                Else
                    env.AddMessage({
                         $"the result value is not supported by R# unit, unit ({unit}) will be ignored...",
                         $"unit: " & unit.name,
                         $"value: " & result.GetType.FullName
                    }, MSG_TYPES.WRN)
                End If
            End If

            Return result
        End Function

        Public Overrides Function ToString() As String
            Return $"({left} {operatorSymbol} {right})"
        End Function

    End Class
End Namespace
