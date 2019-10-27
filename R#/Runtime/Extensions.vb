﻿Imports System.Runtime.CompilerServices

Namespace Runtime

    <HideModuleName> Module Extensions

        Friend Function getFirst(value As Object) As Object
            Dim valueType As Type = value.GetType

            If valueType.IsInheritsFrom(GetType(Array)) Then
                Return DirectCast(value, Array).GetValue(Scan0)
            Else
                Return value
            End If
        End Function

        Friend Function asVector(Of T)(value As Object) As Object
            Dim valueType As Type = value.GetType

            If valueType Is GetType(T) Then
                value = {DirectCast(value, T)}
            ElseIf valueType Is GetType(Object()) Then
                If DirectCast(value, Object()) _
                    .All(Function(i)
                             If Not i.GetType.IsInheritsFrom(GetType(Array)) Then
                                 Return True
                             Else
                                 Return DirectCast(i, Array).Length = 1
                             End If
                         End Function) Then

                    value = DirectCast(value, Object()) _
                        .Select(Function(o)
                                    If Not o.GetType Is GetType(T) Then
                                        o = DirectCast(o, Array).GetValue(Scan0)
                                    End If

                                    Return DirectCast(o, T)
                                End Function) _
                        .ToArray
                End If
            Else
                value = DirectCast(value, IEnumerable(Of T)).ToArray
            End If

            Return value
        End Function

        ''' <summary>
        ''' Get R type code from the type constraint expression value.
        ''' </summary>
        ''' <param name="type$"></param>
        ''' <returns></returns>
        <Extension>
        Public Function GetRTypeCode(type As String) As TypeCodes
            If type.StringEmpty Then
                Return TypeCodes.generic
            Else
                Return [Enum].Parse(GetType(TypeCodes), type.ToLower)
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension>
        Public Function IsPrimitive(type As TypeCodes) As Boolean
            Return type = TypeCodes.boolean OrElse
                   type = TypeCodes.char OrElse
                   type = TypeCodes.double OrElse
                   type = TypeCodes.integer OrElse
                   type = TypeCodes.list OrElse
                   type = TypeCodes.string
        End Function

        ''' <summary>
        ''' DotNET type to R type code
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        <Extension>
        Public Function GetRTypeCode(type As Type) As TypeCodes
            Select Case type
                Case GetType(String), GetType(String())
                    Return TypeCodes.string
                Case GetType(Integer), GetType(Integer()), GetType(Long()), GetType(Long)
                    Return TypeCodes.integer
                Case GetType(Double), GetType(Double())
                    Return TypeCodes.double
                Case GetType(Char), GetType(Char())
                    Return TypeCodes.char
                Case GetType(Boolean), GetType(Boolean())
                    Return TypeCodes.boolean
                Case GetType(Dictionary(Of String, Object)), GetType(Dictionary(Of String, Object)())
                    Return TypeCodes.list
                Case Else
                    Return TypeCodes.generic
            End Select
        End Function

        Public Function [GetType](type As TypeCodes) As Type
            Select Case type
                Case TypeCodes.boolean : Return GetType(Boolean())
                Case TypeCodes.char : Return GetType(String())
                Case TypeCodes.double : Return GetType(Double())
                Case TypeCodes.integer : Return GetType(Long())
                Case TypeCodes.list : Return GetType(Dictionary(Of String, Object))
                Case TypeCodes.string : Return GetType(String())
                Case Else
                    Throw New InvalidCastException(type.Description)
            End Select
        End Function

        Public Function ClosureStackName(func$, script$, line%) As String
            Return $"<{script.FileName}#{line}::{func}()>"
        End Function
    End Module
End Namespace