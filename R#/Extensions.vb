﻿Imports System.Runtime.CompilerServices
Imports SMRUCC.Rsharp.Runtime

''' <summary>
''' Internal runtime extensions
''' </summary>
Module Extensions

    'Public Function ValueTuple(value As Object, type As TypeCodes) As (Value As Object, RType As TypeCodes)

    'End Function

    ''' <summary>
    ''' Get R type code from the type constraint expression value.
    ''' </summary>
    ''' <param name="type$"></param>
    ''' <returns></returns>
    <Extension> Public Function GetRTypeCode(type$) As TypeCodes
        If type.StringEmpty Then
            Return TypeCodes.generic
        End If

        Return [Enum].Parse(GetType(TypeCodes), type.ToLower)
    End Function

    ''' <summary>
    ''' DotNET type to R type code
    ''' </summary>
    ''' <param name="type"></param>
    ''' <returns></returns>
    <Extension> Public Function GetRTypeCode(type As Type) As TypeCodes
        Select Case type
            Case GetType(String), GetType(String())
                Return TypeCodes.string
            Case GetType(Integer), GetType(Integer())
                Return TypeCodes.integer
            Case GetType(Double), GetType(Double())
                Return TypeCodes.double
            Case GetType(ULong), GetType(ULong())
                Return TypeCodes.uinteger
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

    Public Function ClosureStackName(func$, script$, line%) As String
        Return $"<{script.FileName}#{line}::{func}()>"
    End Function
End Module
