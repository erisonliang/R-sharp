﻿#Region "Microsoft.VisualBasic::8e6dee2146f3210769c9eba272ba3852, R#\Runtime\Serialize\bufferObjects\bitmapBuffer.vb"

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

    '     Class bitmapBuffer
    ' 
    '         Properties: bitmap, code
    ' 
    '         Constructor: (+3 Overloads) Sub New
    ' 
    '         Function: Serialize
    ' 
    '         Sub: loadAuto
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Net.Http

Namespace Runtime.Serialize

    Public Class bitmapBuffer : Inherits BufferObject

        Public Property bitmap As Image

        Public Overrides ReadOnly Property code As BufferObjects
            Get
                Return BufferObjects.bitmap
            End Get
        End Property

        Sub New()
        End Sub

        Sub New(buffer As Stream)
            Call loadAuto(buffer)
        End Sub

        Sub New(bytes As Byte())
            Using ms As New MemoryStream(bytes)
                Call loadAuto(ms)
            End Using
        End Sub

        Private Sub loadAuto(buffer As Stream)
            If buffer.CheckGZipMagic Then
                bitmap = Image.FromStream(buffer.UnGzipStream)
            Else
                bitmap = Image.FromStream(buffer)
            End If
        End Sub

        Public Overrides Function Serialize() As Byte()
            Using buffer As New MemoryStream
                Call bitmap.Save(buffer, Imaging.ImageFormat.Png)
                Call buffer.Flush()

                Return buffer.GZipStream.ToArray
            End Using
        End Function
    End Class
End Namespace
