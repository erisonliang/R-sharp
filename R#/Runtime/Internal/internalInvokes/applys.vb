﻿#Region "Microsoft.VisualBasic::7ed97daf1b8756b24c59fc8791fb6ed4, R#\Runtime\Internal\internalInvokes\applys.vb"

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

    '     Module applys
    ' 
    '         Function: apply, checkInternal, (+2 Overloads) keyNameAuto, lapply, parSapply
    '                   sapply
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel.Repository
Imports Microsoft.VisualBasic.Emit.Delegates
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Internal.Object.Converts
Imports SMRUCC.Rsharp.Runtime.Interop
Imports REnv = SMRUCC.Rsharp.Runtime
Imports RObj = SMRUCC.Rsharp.Runtime.Internal.Object

Namespace Runtime.Internal.Invokes

    Module applys

        ''' <summary>
        ''' ### Apply Functions Over Array Margins
        ''' 
        ''' Returns a vector or array or list of values obtained by applying 
        ''' a function to margins of an array or matrix.
        ''' </summary>
        ''' <param name="x">an array, including a matrix.</param>
        ''' <param name="margin">a vector giving the subscripts which the 
        ''' function will be applied over. E.g., for a matrix 1 indicates rows, 
        ''' 2 indicates columns, c(1, 2) indicates rows and columns. Where X has 
        ''' named dimnames, it can be a character vector selecting dimension 
        ''' names.</param>
        ''' <param name="FUN">
        ''' the function to be applied: see ‘Details’. In the case of functions 
        ''' like +, %*%, etc., the function name must be backquoted or quoted.
        ''' </param>
        ''' <param name="env"></param>
        ''' <returns>If each call to FUN returns a vector of length n, then apply 
        ''' returns an array of dimension c(n, dim(X)[MARGIN]) if ``n &gt; 1``. 
        ''' If n equals 1, apply returns a vector if MARGIN has length 1 and an array 
        ''' of dimension dim(X)[MARGIN] otherwise. If n is 0, the result has length 
        ''' 0 but not necessarily the ‘correct’ dimension.
        '''
        ''' If the calls To FUN Return vectors Of different lengths, apply returns 
        ''' a list Of length prod(Dim(X)[MARGIN]) With Dim Set To MARGIN If this has 
        ''' length greater than one.
        '''
        ''' In all cases the result Is coerced by as.vector to one of the basic 
        ''' vector types before the dimensions are set, so that (for example) factor 
        ''' results will be coerced to a character array.</returns>
        ''' <remarks>
        ''' If X is not an array but an object of a class with a non-null dim value 
        ''' (such as a data frame), apply attempts to coerce it to an array via 
        ''' ``as.matrix`` if it is two-dimensional (e.g., a data frame) or via 
        ''' ``as.array``.
        '''
        ''' FUN Is found by a call to match.fun And typically Is either a function 
        ''' Or a symbol (e.g., a backquoted name) Or a character string specifying 
        ''' a function to be searched for from the environment of the call to apply.
        '''
        ''' Arguments in ``...`` cannot have the same name as any of the other 
        ''' arguments, And care may be needed to avoid partial matching to MARGIN Or 
        ''' FUN. In general-purpose code it Is good practice to name the first three 
        ''' arguments if ... Is passed through: this both avoids Partial matching To 
        ''' MARGIN Or FUN And ensures that a sensible Error message Is given If 
        ''' arguments named X, MARGIN Or FUN are passed through ``...``.
        ''' </remarks>
        <ExportAPI("apply")>
        Public Function apply(x As Object, margin As margins, FUN As Object, Optional env As Environment = Nothing) As Object
            If x Is Nothing Then
                Return New Object() {}
            ElseIf TypeOf x Is dataframe Then
                Return doApply.apply(DirectCast(x, dataframe), margin, FUN, env)
            Else
                Return debug.stop(New InvalidProgramException, env)
            End If
        End Function

        ''' <summary>
        ''' parallel sapply
        ''' </summary>
        ''' <returns></returns>
        <ExportAPI("parSapply")>
        Public Function parSapply(<RRawVectorArgument> X As Object, FUN As Object, envir As Environment) As Object
            If X Is Nothing Then
                Return New Object() {}
            End If

            Dim check = checkInternal(X, FUN, envir)

            If Not TypeOf check Is Boolean Then
                Return check
            End If

            Dim seq As New List(Of Object)
            Dim names As New List(Of String)
            Dim apply As RFunction = FUN

            If X.GetType Is GetType(list) Then
                X = DirectCast(X, list).slots
            End If

            If X.GetType.ImplementInterface(GetType(IDictionary)) Then
                Dim list = DirectCast(X, IDictionary)
                Dim values = DirectCast(list.Keys, IEnumerable) _
                    .Cast(Of Object) _
                    .Select(Function(a) (key:=a, value:=list(a))) _
                    .AsParallel _
                    .Select(Function(a)
                                Return (key:=Scripting.ToString(a.key), value:=apply.Invoke(envir, invokeArgument(a.value)))
                            End Function)

                For Each tuple As (key As String, value As Object) In values
                    If Program.isException(tuple.value) Then
                        Return tuple.value
                    End If

                    seq.Add(REnv.single(tuple.value))
                    names.Add(tuple.key)
                Next
            Else
                Dim values As IEnumerable(Of Object) = REnv.asVector(Of Object)(X) _
                    .AsObjectEnumerator _
                    .AsParallel _
                    .Select(Function(d)
                                Return apply.Invoke(envir, invokeArgument(d))
                            End Function)

                For Each value As Object In values
                    If Program.isException(value) Then
                        Return value
                    Else
                        seq.Add(REnv.single(value))
                    End If
                Next
            End If

            Return New RObj.vector(names, seq.ToArray, envir)
        End Function

        Private Function checkInternal(X As Object, FUN As Object, env As Environment) As Object
            If FUN Is Nothing Then
                Return Internal.debug.stop({"Missing apply function!"}, env)
            ElseIf Not FUN.GetType.ImplementInterface(GetType(RFunction)) Then
                Return Internal.debug.stop({"Target is not a function!"}, env)
            End If

            If Program.isException(X) Then
                Return X
            ElseIf Program.isException(FUN) Then
                Return FUN
            ElseIf X Is Nothing Then
                Return Nothing
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' # Apply a Function over a List or Vector
        ''' 
        ''' sapply is a user-friendly version and wrapper of lapply by default 
        ''' returning a vector, matrix or, if simplify = "array", an array 
        ''' if appropriate, by applying simplify2array(). sapply(x, f, simplify 
        ''' = FALSE, USE.NAMES = FALSE) is the same as lapply(x, f).
        ''' </summary>
        ''' <param name="X">
        ''' a vector (atomic or list) or an expression object. Other objects 
        ''' (including classed objects) will be coerced by ``base::as.list``.
        ''' </param>
        ''' <param name="FUN">
        ''' the Function to be applied To Each element Of X: see 'Details’. 
        ''' In the case of functions like +, %*%, the function name must be 
        ''' backquoted or quoted.
        ''' </param>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        <ExportAPI("sapply")>
        <RApiReturn(GetType(vector))>
        Public Function sapply(<RRawVectorArgument> X As Object, FUN As Object, envir As Environment) As Object
            If X Is Nothing Then
                Return New Object() {}
            End If

            Dim check = checkInternal(X, FUN, envir)

            If Not TypeOf check Is Boolean Then
                Return check
            End If

            Dim apply As RFunction = FUN

            If X.GetType Is GetType(list) Then
                X = DirectCast(X, list).slots
            End If

            If X.GetType.ImplementInterface(GetType(IDictionary)) Then
                Dim list = DirectCast(X, IDictionary)
                Dim seq As New List(Of Object)
                Dim names As New List(Of String)
                Dim value As Object

                For Each key As Object In list.Keys
                    value = apply.Invoke(envir, invokeArgument(list(key)))

                    If Program.isException(value) Then
                        Return value
                    End If

                    seq.Add(REnv.single(value))
                    names.Add(Scripting.ToString(key))
                Next

                Dim a As Object = TryCastGenericArray(seq.ToArray, envir)
                Dim type As Type

                If TypeOf a Is Message Then
                    type = GetType(Object)
                Else
                    type = a.GetType.GetElementType
                End If

                Return New RObj.vector(names, DirectCast(a, Array), RType.GetRSharpType(type), envir)
            Else
                Dim seq As New List(Of Object)
                Dim value As Object
                Dim argsPreviews As InvokeParameter()

                For Each d In Runtime.asVector(Of Object)(X) _
                    .AsObjectEnumerator

                    argsPreviews = invokeArgument(d)
                    value = apply.Invoke(envir, argsPreviews)

                    If Program.isException(value) Then
                        Return value
                    Else
                        seq.Add(REnv.single(value))
                    End If
                Next

                Dim a As Object = TryCastGenericArray(seq.ToArray, envir)
                Dim type As Type

                If TypeOf a Is Message Then
                    type = GetType(Object)
                Else
                    type = a.GetType.GetElementType
                End If

                Return New RObj.vector(DirectCast(a, Array), RType.GetRSharpType(type))
            End If
        End Function

        ''' <summary>
        ''' # Apply a Function over a List or Vector
        ''' 
        ''' lapply returns a list of the same length as X, each element of 
        ''' which is the result of applying FUN to the corresponding 
        ''' element of X.
        ''' </summary>
        ''' <param name="X">
        ''' a vector (atomic or list) or an expression object. Other objects 
        ''' (including classed objects) will be coerced by ``base::as.list``.
        ''' </param>
        ''' <param name="FUN">
        ''' the Function to be applied To Each element Of X: see 'Details’. 
        ''' In the case of functions like +, %*%, the function name must be 
        ''' backquoted or quoted.
        ''' </param>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        <ExportAPI("lapply")>
        Public Function lapply(<RRawVectorArgument> X As Object, FUN As Object,
                               Optional names As RFunction = Nothing,
                               Optional envir As Environment = Nothing) As Object

            If X Is Nothing Then
                Return New list With {.slots = New Dictionary(Of String, Object)}
            End If

            Dim check = checkInternal(X, FUN, envir)

            If Not TypeOf check Is Boolean Then
                Return check
            ElseIf X.GetType Is GetType(list) Then
                X = DirectCast(X, list).slots
            End If

            Dim apply As RFunction = FUN
            Dim list As New Dictionary(Of String, Object)

            If X.GetType Is GetType(Dictionary(Of String, Object)) Then
                For Each d In DirectCast(X, Dictionary(Of String, Object))
                    list(d.Key) = apply.Invoke(envir, invokeArgument(d.Value))

                    If Program.isException(list(d.Key)) Then
                        Return list(d.Key)
                    End If
                Next
            Else
                Dim getName As Func(Of SeqValue(Of Object), String) = keyNameAuto(names, envir)
                Dim keyName$
                Dim value As Object

                For Each d As SeqValue(Of Object) In REnv.asVector(Of Object)(X) _
                    .AsObjectEnumerator _
                    .SeqIterator

                    keyName = getName(d)
                    value = apply.Invoke(envir, invokeArgument(d.value))

                    If Program.isException(value) Then
                        Return value
                    Else
                        list(keyName) = value
                    End If
                Next
            End If

            Return New list With {.slots = list}
        End Function

        Private Function keyNameAuto(type As Type) As Func(Of Object, String)
            Static cache As New Dictionary(Of Type, Func(Of Object, String))

            Return cache.ComputeIfAbsent(
                key:=type,
                lazyValue:=Function(key As Type)
                               If key.ImplementInterface(GetType(INamedValue)) Then
                                   Return Function(a) DirectCast(a, INamedValue).Key
                               ElseIf key.ImplementInterface(GetType(IReadOnlyId)) Then
                                   Return Function(a) DirectCast(a, IReadOnlyId).Identity
                               ElseIf key.ImplementInterface(GetType(IKeyedEntity(Of String))) Then
                                   Return Function(a) DirectCast(a, IKeyedEntity(Of String)).Key
                               Else
                                   Return Function() Nothing
                               End If
                           End Function)
        End Function

        Public Function keyNameAuto(names As RFunction, env As Environment) As Func(Of SeqValue(Of Object), String)
            If names Is Nothing Then
                Return Function(i)
                           Dim name As String = Nothing

                           If Not i.value Is Nothing Then
                               name = keyNameAuto(i.value.GetType)(i.value)
                           End If

                           If name Is Nothing Then
                               name = $"[[{i.i + 1}]]"
                           End If

                           Return name
                       End Function
            Else
                Return Function(i)
                           Dim nameVals = names.Invoke(env, invokeArgument(i.value))
                           Dim namesVec = RConversion.asCharacters(nameVals)

                           Return getFirst(namesVec)
                       End Function
            End If
        End Function
    End Module
End Namespace
