﻿#Region "Microsoft.VisualBasic::3e25e20c810fce0b066f751391ccf320, R#\Runtime\Internal\printer\printer.vb"

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
    '     Module printer
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: f64_InternalToString, getStrings, ToString, ValueToString
    ' 
    '         Sub: AttachConsoleFormatter, AttachInternalConsoleFormatter, printArray, printContentArray, printInternal
    '              printList
    ' 
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal
Imports Microsoft.VisualBasic.Emit.Delegates
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Serialization
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.System.Configuration

Namespace Runtime.Internal.ConsolePrinter

    Public Delegate Function InternalToString(env As GlobalEnvironment) As IStringBuilder

    ''' <summary>
    ''' R# console nice print supports.
    ''' </summary>
    Public Module printer

        Friend ReadOnly RtoString As New Dictionary(Of Type, IStringBuilder)
        Friend ReadOnly RInternalToString As New Dictionary(Of Type, InternalToString)

        Sub New()
            RtoString(GetType(Color)) = Function(c) DirectCast(c, Color).ToHtmlColor.ToLower
            RtoString(GetType(vbObject)) = Function(o) DirectCast(o, vbObject).ToString

            RInternalToString(GetType(Double)) = AddressOf printer.f64_InternalToString
        End Sub

        Private Function f64_InternalToString(env As GlobalEnvironment) As IStringBuilder
            Dim opts As Options = env.globalEnvironment.options
            Dim format As String = $"{opts.f64Format}{opts.digits}"

            Return Function(d)
                       Dim val As Double = DirectCast(d, Double)
                       Dim str As String = val.ToString(format)

                       If val > 0 Then
                           str = " " & str
                       End If

                       Return str
                   End Function
        End Function

        ''' <summary>
        ''' <see cref="Object"/> -> <see cref="String"/>
        ''' </summary>
        ''' <typeparam name="T"></typeparam>
        ''' <param name="formatter"></param>
        Public Sub AttachConsoleFormatter(Of T)(formatter As IStringBuilder)
            RtoString(GetType(T)) = formatter
        End Sub

        ''' <summary>
        ''' <see cref="Object"/> -> <see cref="String"/>
        ''' </summary>
        ''' <typeparam name="T"></typeparam>
        ''' <param name="formatter"></param>
        Friend Sub AttachInternalConsoleFormatter(Of T)(formatter As InternalToString)
            RInternalToString(GetType(T)) = formatter
        End Sub

        Friend Sub printInternal(x As Object, listPrefix$, maxPrint%, env As GlobalEnvironment)
            Dim valueType As Type = x.GetType

            If RtoString.ContainsKey(valueType) Then
                Call Console.WriteLine(RtoString(valueType)(x))
            ElseIf valueType.IsInheritsFrom(GetType(Array)) Then
                With DirectCast(x, Array)
                    If .Length > 1 Then
                        Call .printArray(maxPrint, env)
                    ElseIf .Length = 0 Then
                        Call Console.WriteLine("NULL")
                    Else
                        x = .GetValue(Scan0)
                        ' get the first value and then print its
                        ' text value onto console
                        GoTo printSingleElement
                    End If
                End With
            ElseIf valueType Is GetType(vector) Then
                Call DirectCast(x, vector).data.printArray(maxPrint, env)
            ElseIf valueType.ImplementInterface(GetType(IDictionary)) Then
                Call DirectCast(x, IDictionary).printList(listPrefix, maxPrint, env)
            ElseIf valueType Is GetType(list) Then
                Call DirectCast(x, list) _
                    .slots _
                    .DoCall(Sub(list)
                                list.printList(listPrefix, maxPrint, env)
                            End Sub)
            ElseIf valueType Is GetType(dataframe) Then
                Call DirectCast(x, dataframe) _
                    .GetTable(env) _
                    .Print(addBorder:=False) _
                    .DoCall(AddressOf Console.WriteLine)
            ElseIf valueType Is GetType(vbObject) Then
                Call DirectCast(x, vbObject).ToString.DoCall(AddressOf Console.WriteLine)
            Else
printSingleElement:
                Call Console.WriteLine("[1] " & printer.ValueToString(x, env))
            End If
        End Sub

        <Extension>
        Private Sub printList(list As IDictionary, listPrefix$, maxPrint%, env As GlobalEnvironment)
            For Each objKey As Object In list.Keys
                Dim slotValue As Object = list(objKey)
                Dim key$ = objKey.ToString

                If key.IsPattern("\d+") Then
                    key = $"{listPrefix}[[{key}]]"
                Else
                    key = $"{listPrefix}${key}"
                End If

                Call Console.WriteLine(key)
                Call printer.printInternal(slotValue, key, maxPrint, env)
                Call Console.WriteLine()
            Next
        End Sub

        ''' <summary>
        ''' Debugger test api of <see cref="ToString"/>
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        <Extension>
        Public Function ValueToString(x As Object, env As GlobalEnvironment) As String
            Return printer.ToString(x.GetType, env)(x)
        End Function

        ''' <summary>
        ''' The external string formatter will overrides the internal formatter
        ''' </summary>
        ''' <param name="elementType"></param>
        ''' <returns></returns>
        <Extension>
        Friend Function ToString(elementType As Type, env As GlobalEnvironment) As IStringBuilder
            If RtoString.ContainsKey(elementType) Then
                Return RtoString(elementType)
            ElseIf RInternalToString.ContainsKey(elementType) Then
                Return RInternalToString(elementType)(env)
            ElseIf elementType Is GetType(String) Then
                Return Function(o) As String
                           If o Is Nothing Then
                               Return "NULL"
                           Else
                               Return $"""{o}"""
                           End If
                       End Function
            ElseIf Not (elementType.Namespace.StartsWith("System.") OrElse elementType.Namespace = "System") Then
                Return AddressOf classPrinter.printClass
            ElseIf elementType = GetType(Boolean) Then
                Return Function(b) b.ToString.ToUpper
            Else
                Return Function(obj) Scripting.ToString(obj, "NULL", True)
            End If
        End Function

        Friend Function getStrings(xVec As Array, env As GlobalEnvironment) As IEnumerable(Of String)
            Dim elementType As Type = Runtime.MeasureArrayElementType(xVec)
            Dim toString As IStringBuilder = printer.ToString(elementType, env)

            Return From element As Object
                   In xVec.AsQueryable
                   Let str As String = toString(element)
                   Select str
        End Function

        ''' <summary>
        ''' Print vector elements
        ''' </summary>
        ''' <param name="xvec"></param>
        <Extension>
        Friend Sub printArray(xvec As Array, maxPrint%, env As GlobalEnvironment)
            Dim stringVec As IEnumerable(Of String) = getStrings(xvec, env)
            Dim contents As String() = stringVec.Take(maxPrint).ToArray

            Call contents.printContentArray(Nothing, Nothing)

            If xvec.Length > maxPrint Then
                Call Console.WriteLine($"[ reached getOption(""max.print"") -- omitted {xvec.Length - contents.Length} entries ]")
            End If
        End Sub

        <Extension>
        Friend Sub printContentArray(contents$(), deli$, indentPrefix$)
            Dim maxColumns As Integer = Console.WindowWidth - 1
            ' maxsize / average size
            Dim unitWidth As Integer = contents.Max(Function(c) c.Length) + 1
            Dim divSize As Integer = maxColumns \ unitWidth - 3
            Dim i As i32 = 1 - divSize

            If divSize <= 0 Then
                divSize = 1
            End If

            For Each row As String() In contents.Split(partitionSize:=divSize)
                If indentPrefix Is Nothing Then
                    Call Console.Write($"[{i = i + divSize}]{vbTab}")
                Else
                    Call Console.Write(indentPrefix)
                End If

                For Each c As String In row
                    Call Console.Write(c)

                    If deli Is Nothing Then
                        Call Console.Write(New String(" "c, unitWidth - c.Length))
                    Else
                        Call Console.Write(deli)
                    End If
                Next

                Call Console.WriteLine()
            Next
        End Sub
    End Module
End Namespace
