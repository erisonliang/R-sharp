﻿#Region "Microsoft.VisualBasic::245dbd82fb66fbd66a9ba24242f21c6b, R#\Runtime\Serialize\bufferObjects\vectorBuffer.vb"

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

    '     Class vectorBuffer
    ' 
    '         Properties: code, names, type, underlyingType, unit
    '                     vector
    ' 
    '         Function: (+2 Overloads) CreateBuffer, GetVector, Serialize
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Serialization
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports REnv = SMRUCC.Rsharp.Runtime

Namespace Runtime.Serialize

    Public Class vectorBuffer : Inherits BufferObject

        ''' <summary>
        ''' the full name of the <see cref="Global.System.Type"/> for create <see cref="RType"/>
        ''' </summary>
        ''' <returns></returns>
        Public Property type As String
        Public Property vector As Array
        Public Property names As String()
        Public Property unit As String

        Public ReadOnly Property underlyingType As Type
            Get
                Return Global.System.Type.GetType(type)
            End Get
        End Property

        Public Overrides ReadOnly Property code As BufferObjects
            Get
                Return BufferObjects.vector
            End Get
        End Property

        Public Function GetVector() As vector
            Dim rtype As RType = RType.GetRSharpType(underlyingType)
            Dim unit As New unit With {.name = Me.unit}
            Dim vec As New vector(names, vector, rtype, unit)

            Return vec
        End Function

        Public Shared Function CreateBuffer(vector As vector, env As Environment) As Object
            Dim buffer As New vectorBuffer With {
                .names = If(vector.getNames, {}),
                .type = vector.elementType.raw.FullName,
                .unit = If(vector.unit?.name, "")
            }
            Dim generic = REnv.asVector(vector.data, vector.elementType.raw, env)

            If TypeOf generic Is Message Then
                Return generic
            Else
                buffer.vector = generic
            End If

            Return buffer
        End Function

        Public Shared Function CreateBuffer(bytes As Stream) As vectorBuffer
            Dim raw As Byte() = New Byte(2 * Marshal.SizeOf(GetType(Integer)) - 1) {}
            Dim text As Encoding = Encodings.UTF8.CodePage

            bytes.Read(raw, Scan0, raw.Length)

            Dim name_size As Integer = BitConverter.ToInt32(raw, Scan0)
            Dim vector_size As Integer = BitConverter.ToInt32(raw, Marshal.SizeOf(GetType(Integer)))
            Dim int_size As Byte() = New Byte(3) {}
            Dim sizeof As Integer

            bytes.Read(int_size, Scan0, int_size.Length)
            sizeof = BitConverter.ToInt32(int_size, Scan0)
            raw = New Byte(sizeof - 1) {}
            bytes.Read(raw, Scan0, raw.Length)

            Dim type As Type = Type.GetType(text.GetString(raw))

            bytes.Read(int_size, Scan0, int_size.Length)
            sizeof = BitConverter.ToInt32(int_size, Scan0)
            raw = New Byte(sizeof - 1) {}
            bytes.Read(raw, Scan0, raw.Length)

            Dim unit As String = text.GetString(raw)

            bytes.Read(int_size, Scan0, int_size.Length)
            sizeof = BitConverter.ToInt32(int_size, Scan0)
            raw = New Byte(sizeof - 1) {}
            bytes.Read(raw, Scan0, raw.Length)

            Dim names As String()

            Using ms As New MemoryStream(raw)
                names = RawStream.GetData(ms, TypeCode.String)
            End Using

            raw = New Byte(3) {}
            bytes.Read(raw, Scan0, raw.Length)
            sizeof = BitConverter.ToInt32(raw, Scan0)
            raw = New Byte(sizeof - 1) {}
            bytes.Read(raw, Scan0, raw.Length)

            Using ms As New MemoryStream(raw)
                Dim vector As Array = RawStream.GetData(ms, type.PrimitiveTypeCode)

                Return New vectorBuffer With {
                    .type = type.FullName,
                    .names = names,
                    .unit = unit,
                    .vector = vector
                }
            End Using
        End Function

        Public Overrides Function Serialize() As Byte()
            Dim type As Type = Type.GetType(Me.type)
            Dim bytes As Byte()
            Dim text As Encoding = Encodings.UTF8.CodePage
            Dim raw As Byte()
            Dim sizeof As Byte()

            Using buffer As New MemoryStream
                buffer.Write(BitConverter.GetBytes(names.Length), Scan0, 4)
                buffer.Write(BitConverter.GetBytes(vector.Length), Scan0, 4)

                bytes = text.GetBytes(type.FullName)
                buffer.Write(BitConverter.GetBytes(bytes.Length), Scan0, 4)
                buffer.Write(bytes, Scan0, bytes.Length)

                bytes = text.GetBytes(unit)
                buffer.Write(BitConverter.GetBytes(bytes.Length), Scan0, 4)
                buffer.Write(bytes, Scan0, bytes.Length)

                raw = RawStream.GetBytes(names)
                sizeof = BitConverter.GetBytes(raw.Length)

                buffer.Write(sizeof, Scan0, 4)
                buffer.Write(raw, Scan0, raw.Length)

                raw = RawStream.GetBytes(vector)
                sizeof = BitConverter.GetBytes(raw.Length)

                buffer.Write(sizeof, Scan0, 4)
                buffer.Write(raw, Scan0, raw.Length)

                buffer.Flush()

                Return buffer.ToArray
            End Using
        End Function
    End Class
End Namespace
