﻿#Region "Microsoft.VisualBasic::183677a70d1a3e7139954fe5f1c74fd9, Library\R.base\utils\buffer.vb"

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

    ' Module buffer
    ' 
    '     Function: float, numberFramework, toInteger
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop
Imports Rsharp = SMRUCC.Rsharp

<Package("buffer", Category:=APICategories.UtilityTools)>
Module buffer

    <ExportAPI("float")>
    <RApiReturn(GetType(Double))>
    Public Function float(<RRawVectorArgument> stream As Object, Optional networkOrder As Boolean = False, Optional sizeOf As Integer = 32, Optional env As Environment = Nothing) As Object
        If sizeOf = 32 Then
            Return env.numberFramework(stream, networkOrder, 4, AddressOf BitConverter.ToSingle)
        ElseIf sizeOf = 64 Then
            Return env.numberFramework(stream, networkOrder, 8, AddressOf BitConverter.ToDouble)
        Else
            Return Internal.debug.stop($"the given size value '{sizeOf}' is invalid!", env)
        End If
    End Function

    <Extension>
    Private Function numberFramework(Of T)(env As Environment, <RRawVectorArgument> stream As Object, networkOrder As Boolean, width As Integer, fromBlock As Func(Of Byte(), Integer, T)) As Object
        Dim buffer As [Variant](Of Byte(), Message) = Rsharp.Buffer(stream, env)

        If buffer Is Nothing Then
            Return Nothing
        ElseIf buffer Like GetType(Message) Then
            Return buffer.TryCast(Of Message)
        End If

        Dim bytes As Byte() = buffer.TryCast(Of Byte())

        If networkOrder AndAlso BitConverter.IsLittleEndian Then
            Return bytes _
                .Split(width) _
                .Select(Function(block)
                            Array.Reverse(block)
                            Return fromBlock(block, Scan0)
                        End Function) _
                .ToArray
        Else
            Return bytes _
               .Split(width) _
               .Select(Function(block)
                           Return fromBlock(block, Scan0)
                       End Function) _
               .ToArray
        End If
    End Function

    <ExportAPI("integer")>
    <RApiReturn(GetType(Integer))>
    Public Function toInteger(<RRawVectorArgument> stream As Object, Optional networkOrder As Boolean = False, Optional sizeOf As Integer = 32, Optional env As Environment = Nothing) As Object
        If sizeOf = 32 Then
            Return env.numberFramework(stream, networkOrder, 4, AddressOf BitConverter.ToInt32)
        ElseIf sizeOf = 64 Then
            Return env.numberFramework(stream, networkOrder, 8, AddressOf BitConverter.ToInt64)
        Else
            Return Internal.debug.stop($"the given size value '{sizeOf}' is invalid!", env)
        End If
    End Function
End Module
