﻿#Region "Microsoft.VisualBasic::5865532df870dcb27b70657554e0aa74, R#\Runtime\Interop\RType.vb"

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

'     Class RType
' 
'         Properties: fullName, haveDynamicsProperty, isArray, isCollection, isEnvironment
'                     mode, raw
' 
'         Constructor: (+1 Overloads) Sub New
'         Function: getNames, GetRawElementType, GetRSharpType, populateNames, ToString
' 
' 
' /********************************************************************************/

#End Region

Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Emit.Delegates
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Internal.ConsolePrinter

Namespace Runtime.Interop

    ''' <summary>
    ''' The type wrapper for .NET type to R# language runtime
    ''' </summary>
    Public Class RType : Implements IReflector

        ''' <summary>
        ''' <see cref="Type.FullName"/>
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property fullName As String
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            <DebuggerStepThrough>
            Get
                Return raw.FullName
            End Get
        End Property

        ''' <summary>
        ''' The mapped R# data type
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property mode As TypeCodes
        Public ReadOnly Property isArray As Boolean
        Public ReadOnly Property isCollection As Boolean

        ''' <summary>
        ''' Is dictionary of string and value types?
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property isGenericListObject As Boolean
            Get
                If Not raw.ImplementInterface(GetType(IDictionary)) Then
                    Return False
                End If
                If raw.GenericTypeArguments.Length <> 2 Then
                    Return False
                End If
                If raw.GenericTypeArguments(Scan0) Is GetType(String) Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        ''' <summary>
        ''' .NET type
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property raw As Type
        ''' <summary>
        ''' implements interface of <see cref="IDynamicsObject"/>?
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property haveDynamicsProperty As Boolean
        ''' <summary>
        ''' is an <see cref="Environment"/> object?
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property isEnvironment As Boolean

        Dim names As String()

        Private Sub New(raw As Type)
            Me.raw = raw
            Me.names = populateNames _
                .Distinct _
                .ToArray
            Me.haveDynamicsProperty = raw.ImplementInterface(GetType(IDynamicsObject))
            Me.isArray = raw Is GetType(Array) _
                  OrElse raw.IsInheritsFrom(GetType(Array))
            Me.isCollection = raw.ImplementInterface(GetType(IEnumerable)) AndAlso Not raw Is GetType(String)
            Me.mode = raw.GetRTypeCode
            Me.isEnvironment = raw.IsInheritsFrom(GetType(Environment), strict:=False)
        End Sub

        Public Function GetRawElementType() As Type
            If raw Is GetType(Array) Then
                Return GetType(Object)
            Else
                Return raw.GetElementType
            End If
        End Function

        ''' <summary>
        ''' <see cref="getNames()"/>
        ''' </summary>
        ''' <returns></returns>
        Private Iterator Function populateNames() As IEnumerable(Of String)
            For Each m As MethodInfo In raw.getObjMethods
                Yield m.Name
            Next
            For Each p As PropertyInfo In raw.getObjProperties
                Yield p.Name
            Next
        End Function

        Public Overrides Function ToString() As String
            If mode.IsPrimitive Then
                Return mode.Description
            ElseIf isGenericListObject Then
                Return $"list[{GetRSharpType(raw.GenericTypeArguments(1)).ToString}]"
            ElseIf raw.IsEnum Then
                Return $"<integer> {raw.Name}"
            Else
                Return $"<{mode.Description}> {raw.Name}"
            End If
        End Function

        ''' <summary>
        ''' Get method names and property names of target type object instance
        ''' </summary>
        ''' <returns></returns>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <DebuggerStepThrough>
        Public Function getNames() As String() Implements IReflector.getNames
            Return names.Clone
        End Function

        ''' <summary>
        ''' Get VB.NET to R# type wrapper
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        Public Shared Function GetRSharpType(type As Type) As RType
            Static cache As New Dictionary(Of Type, RType)
            Return cache.ComputeIfAbsent(type, Function(t) New RType(t))
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <DebuggerStepThrough>
        Public Shared Narrowing Operator CType(type As RType) As Type
            Return type.raw
        End Operator
    End Class
End Namespace
