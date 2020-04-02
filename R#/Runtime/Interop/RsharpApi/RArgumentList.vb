﻿#Region "Microsoft.VisualBasic::e962f50a45420d04d8f4a3b31e8d37c0, R#\Runtime\Interop\RArgumentList.vb"

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

'     Class RArgumentList
' 
'         Function: CreateObjectListArguments
' 
' 
' /********************************************************************************/

#End Region

Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Runtime.Interop

    Public NotInheritable Class RArgumentList

        Private Sub New()
        End Sub

        ''' <summary>
        ''' get index of list argument
        ''' </summary>
        ''' <param name="[declare]"></param>
        ''' <returns></returns>
        Private Shared Function objectListArgumentIndex([declare] As RMethodInfo) As Integer
            Dim args As RMethodArgument() = [declare].parameters

            For i As Integer = 0 To args.Length - 1
                If args(i).isObjectList Then
                    Return i
                End If
            Next

            Return -1
        End Function

        Public Shared Function objectListArgumentMargin([declare] As RMethodInfo) As ListObjectArgumentMargin
            Dim index As Integer = objectListArgumentIndex([declare])

            If index = -1 Then
                Return ListObjectArgumentMargin.none
            ElseIf index = Scan0 Then
                Return ListObjectArgumentMargin.left
            ElseIf index = [declare].parameters.Length - 1 Then
                Return ListObjectArgumentMargin.right
            Else
                If [declare].parameters.Last.type.isEnvironment AndAlso [declare].parameters.Length > 2 Then
                    If index = [declare].parameters.Length - 2 Then
                        Return ListObjectArgumentMargin.right
                    End If
                End If
            End If

            Return ListObjectArgumentMargin.invalid
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="[declare]">
        ''' the first argument in api parameters is the list object argument
        ''' all of the argument 
        ''' </param>
        ''' <param name="env"></param>
        ''' <param name="params">
        ''' parameter declares example as: ``(..., file, env)``
        ''' 
        ''' valids syntax:
        ''' 
        ''' 1. ``(a,b,c,d,e,f,g, file = "...")``
        ''' 2. ``(a= 1, b=2, c=a, d= 55, bb, ee, file = "...")``
        ''' </param>
        ''' <returns></returns>
        Private Shared Function CreateLeftMarginArguments([declare] As RMethodInfo, params As InvokeParameter(), env As Environment) As IEnumerable(Of Object)
            Dim parameterVals As Object() = New Object([declare].parameters.Length - 1) {}
            ' the parameter name of the list object argument has been removed
            ' so all of the index value should be move forward 1 unit.
            Dim parameterNames As Index(Of String) = [declare].parameters.Skip(1).Keys
            Dim declareArguments = [declare].parameters.ToDictionary(Function(a) a.name)
            Dim listObject As New List(Of InvokeParameter)
            Dim skipListObject As Boolean = False
            Dim normalNames As New List(Of String)
            Dim sequenceIndex As Integer = Scan0

            For Each arg As InvokeParameter In params
                If arg.isSymbolAssign AndAlso arg.name Like parameterNames Then
                    skipListObject = True
                    parameterVals(parameterNames(arg.name) + 1) = RMethodInfo.getValue(
                        arg:=declareArguments(arg.name),
                        value:=arg.Evaluate(env),
                        trace:=[declare].name,
                        envir:=env,
                        trygetListParam:=False
                    )
                    normalNames.Add(arg.name)
                    sequenceIndex += 1
                ElseIf skipListObject Then
                    ' normal parameters
                    If arg.isSymbolAssign Then
                        ' but arg.name is not one of the parameterNames
                        ' syntax error?
                        Dim syntaxError = Internal.debug.stop({
                            "syntax error!",
                            "the argument is not expected at here!",
                            "argument name: " & arg.name
                        }, env)

                        Return New Object() {syntaxError}
                    Else
                        If Not parameterVals(sequenceIndex) Is Nothing Then
                            ' is already have value at here
                            ' syntax error
                            Dim syntaxError = Internal.debug.stop({
                                "syntax error!",
                                "the argument is not expected at here!",
                                "argument expression: " & arg.value.ToString
                            }, env)

                            Return New Object() {syntaxError}
                        Else
                            parameterVals(sequenceIndex) = RMethodInfo.getValue(
                                arg:=[declare].parameters(sequenceIndex),
                                value:=arg.Evaluate(env),
                                trace:=[declare].name,
                                envir:=env,
                                trygetListParam:=False
                            )
                            normalNames.Add([declare].parameters(sequenceIndex).name)
                            sequenceIndex += 1
                        End If
                    End If
                Else
                    ' still a list object argument
                    ' 当前的参数为list object
                    listObject.Add(arg)
                End If
            Next

            For Each name As String In normalNames
                Call declareArguments.Remove(name)
            Next

            For Each arg As RMethodArgument In declareArguments.Values
                If arg.isOptional Then
                    If arg.type.isEnvironment Then
                        parameterVals(parameterNames(arg.name) + 1) = env
                    Else
                        parameterVals(parameterNames(arg.name) + 1) = arg.default
                    End If
                ElseIf arg.type.isEnvironment Then
                    parameterVals(parameterNames(arg.name) + 1) = env
                Else
                    Return New Object() {
                        RMethodInfo.missingParameter(arg, env, [declare].name)
                    }
                End If
            Next

            Return parameterVals
        End Function

        Private Shared Function CreateRightMarginArguments([declare] As RMethodInfo, params As InvokeParameter(), env As Environment) As IEnumerable(Of Object)
            Throw New NotImplementedException
        End Function

        ''' <summary>
        ''' Create argument value for <see cref="MethodInfo.Invoke(Object, Object())"/>
        ''' </summary>
        ''' <param name="params">
        ''' required of replace dot(.) to underline(_)?
        ''' </param>
        ''' <returns></returns>
        Friend Shared Function CreateObjectListArguments([declare] As RMethodInfo, env As Environment, params As InvokeParameter()) As IEnumerable(Of Object)
            If [declare].listObjectMargin = ListObjectArgumentMargin.left Then
                Return CreateLeftMarginArguments([declare], params, env)
            Else
                Return CreateRightMarginArguments([declare], params, env)
            End If

            Dim parameterVals As Object() = New Object([declare].parameters.Length - 1) {}
            Dim declareArguments = [declare].parameters.ToDictionary(Function(a) a.name)
            Dim declareNameIndex As Index(Of String) = [declare].parameters.Keys.Indexing
            Dim listObject As New List(Of InvokeParameter)
            Dim i As Integer = Scan0
            Dim sequenceIndex As Integer = Scan0
            Dim paramVal As Object
            Dim index As Integer = RArgumentList.objectListArgumentIndex([declare])

            For Each arg As InvokeParameter In params
                If sequenceIndex = index Then
                    If declareArguments.ContainsKey(arg.name) Then
                        ' move next
                        sequenceIndex += 1

                        GoTo SET_VALUE
                    Else
                        ' 当前的参数为list object
                        listObject.Add(arg)
                    End If
                Else
SET_VALUE:
                    paramVal = RMethodInfo.getValue(
                        arg:=declareArguments(arg.name),
                        value:=arg.Evaluate(env),
                        trace:=[declare].name,
                        envir:=env,
                        trygetListParam:=False
                    )

                    If Not paramVal Is Nothing AndAlso paramVal.GetType Is GetType(Message) Then
                        Return {paramVal}
                    End If

                    parameterVals(sequenceIndex) = paramVal
                    sequenceIndex = sequenceIndex + 1
                End If
            Next

            parameterVals(index) = listObject.ToArray

            If sequenceIndex = index Then
                sequenceIndex += 1
            End If

            If sequenceIndex < parameterVals.Length Then
                Dim envirArgument As RMethodArgument = declareArguments _
                    .Values _
                    .Where(Function(a)
                               Return a.type.raw Is GetType(Environment)
                           End Function) _
                    .FirstOrDefault

                If Not envirArgument Is Nothing Then
                    i = declareNameIndex(envirArgument.name)
                    parameterVals(i) = env
                    declareArguments.Remove(envirArgument.name)
                End If
            End If

            If declareArguments.Count > 0 Then
                Return {
                    declareArguments.Values _
                        .First _
                        .DoCall(Function(a)
                                    Return RMethodInfo.missingParameter(a, env, [declare].name)
                                End Function)
                }
            Else
                Return parameterVals
            End If
        End Function
    End Class
End Namespace
