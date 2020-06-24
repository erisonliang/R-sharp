﻿#Region "Microsoft.VisualBasic::dd122b38a714ae3dd2c1fff68805db50, R#\Runtime\Environment\RContentOutput.vb"

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

    '     Enum OutputEnvironments
    ' 
    '         Console, Html, None
    ' 
    '  
    ' 
    ' 
    ' 
    '     Class RContentOutput
    ' 
    '         Properties: env, recommendType, stream
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Sub: Flush, (+4 Overloads) Write, WriteLine, WriteStream
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Internal.Object

Namespace Runtime

    ''' <summary>
    ''' R# I/O redirect and interface for Rserve http server
    ''' </summary>
    Public Class RContentOutput

        Public ReadOnly Property env As OutputEnvironments = OutputEnvironments.Console
        Public ReadOnly Property recommendType As String
        Public ReadOnly Property stream As Stream
            Get
                Return stdout.BaseStream
            End Get
        End Property

        Friend stdout As StreamWriter

        Sub New(stdout As StreamWriter, env As OutputEnvironments)
            Me.env = env
            Me.stdout = stdout
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Sub Flush()
            Call stdout.Flush()
        End Sub

        ''' <summary>
        ''' Writes a string followed by a line terminator to the text string or stream.
        ''' </summary>
        ''' <param name="message">
        ''' The string to write. If value is null, only the line terminator is written.
        ''' </param>
        Public Sub WriteLine(Optional message As String = Nothing)
            If message Is Nothing Then
                Call stdout.WriteLine()
            Else
                Call stdout.WriteLine(message)
            End If

            Call stdout.Flush()

            If _recommendType Is Nothing Then
                _recommendType = "text/html;charset=UTF-8"
            End If
        End Sub

        Public Sub Write(message As String, Optional content_type$ = "text/html;charset=UTF-8")
            Call stdout.Write(message)
            Call stdout.Flush()

            If _recommendType Is Nothing Then
                _recommendType = content_type
            End If
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Sub Write(data As dataframe, globalEnv As GlobalEnvironment, Optional content_type$ = "text/csv")
            Call data _
                .GetTable(globalEnv, False, True) _
                .Select(Function(row)
                            Return row.Select(Function(cell) $"""{cell}""").JoinBy(",")
                        End Function) _
                .JoinBy(vbCrLf) _
                .DoCall(Sub(text)
                            Call Write(text, content_type)
                        End Sub)
        End Sub

        Public Sub Write(data As IEnumerable(Of Byte))
            Call stdout.Write(data.ToArray)

            If _recommendType Is Nothing Then
                _recommendType = "application/octet-stream"
            End If
        End Sub

        Public Sub Write(image As Image)
            _recommendType = "image/png"

            Using buffer As New MemoryStream
                Call image.Save(buffer, ImageFormat.Png)
                Call stdout.Write(buffer.ToArray)
            End Using
        End Sub

        Public Sub WriteStream(writeTo As Action(Of Stream), content_type As String)
            _recommendType = content_type
            Call writeTo(stream)
        End Sub
    End Class
End Namespace
