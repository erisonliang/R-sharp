﻿#Region "Microsoft.VisualBasic::fb3b34c34596905a9e5fe33de05d0dd3, R#\Runtime\Internal\internalInvokes\math.vb"

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

    '     Module math
    ' 
    '         Function: log, round, sum
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Interop
Imports stdNum = System.Math

Namespace Runtime.Internal.Invokes

    ''' <summary>
    ''' 
    ''' </summary>
    Module math

        ''' <summary>
        ''' rounds the values in its first argument to the specified number of decimal places (default 0). 
        ''' See *'Details'* about "round to even" when rounding off a 5.
        ''' </summary>
        ''' <param name="x">a numeric vector. Or, for ``round`` and ``signif``, a complex vector.</param>
        ''' <param name="decimals">
        ''' integer indicating the number of decimal places (``round``) or significant digits (``signif``) to be used. 
        ''' Negative values are allowed (see *'Details'*).
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("round")>
        Public Function round(x As Double(), Optional decimals% = 0) As Object
            If x.IsNullOrEmpty Then
                Return Nothing
            Else
                Return (From element As Double In x Select stdNum.Round(element, decimals)).ToArray
            End If
        End Function

        ''' <summary>
        ''' computes logarithms, by default natural logarithms, log10 computes common (i.e., base 10) logarithms, 
        ''' and log2 computes binary (i.e., base 2) logarithms. 
        ''' The general form log(x, base) computes logarithms with base base.
        ''' </summary>
        ''' <param name="x">a numeric or complex vector.</param>
        ''' <param name="newBase">
        ''' a positive or complex number: the base with respect to which logarithms are computed. 
        ''' Defaults to ``e=exp(1)``.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("log")>
        Public Function log(x As Double(), Optional newBase As Double = stdNum.E) As Object
            Return Runtime.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(Function(d) stdNum.Log(d, newBase)) _
                .ToArray
        End Function

        <ExportAPI("sum")>
        Public Function sum(<RRawVectorArgument> x As Object) As Double
            If x Is Nothing Then
                Return 0
            End If

            Dim array = Runtime.asVector(Of Object)(x)
            Dim elementType As Type = Runtime.MeasureArrayElementType(array)

            Select Case elementType
                Case GetType(Boolean)
                    Return Runtime.asLogical(array).Select(Function(b) If(b, 1, 0)).Sum
                Case GetType(Integer), GetType(Long), GetType(Short), GetType(Byte)
                    Return Runtime.asVector(Of Long)(x).AsObjectEnumerator(Of Long).Sum
                Case Else
                    Return Runtime.asVector(Of Double)(x).AsObjectEnumerator(Of Double).Sum
            End Select
        End Function
    End Module
End Namespace