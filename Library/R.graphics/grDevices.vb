﻿#Region "Microsoft.VisualBasic::9328dfdbd924de0100c2fd79380ebc94, Library\R.graphics\grDevices.vb"

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

    ' Module grDevices
    ' 
    '     Function: devCur, devOff, rgb, saveImage
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Drawing
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal

''' <summary>
''' The R# Graphics Devices and Support for Colours and Fonts
''' </summary>
<Package("grDevices", Category:=APICategories.UtilityTools)>
Public Module grDevices

    Dim devlist As New Dictionary(Of Integer, IGraphics)
    Dim curDev As IGraphics

    <ExportAPI("save.graphics")>
    Public Function saveImage(graphics As Object, file$, envir As Environment) As Object
        If graphics Is Nothing Then
            Return Internal.debug.stop("Graphics data is NULL!", envir)
        ElseIf graphics.GetType Is GetType(Image) Then
            Return DirectCast(graphics, Image).SaveAs(file)
        ElseIf graphics.GetType Is GetType(Bitmap) Then
            Return DirectCast(graphics, Bitmap).SaveAs(file)
        ElseIf graphics.GetType.IsInheritsFrom(GetType(GraphicsData)) Then
            Return DirectCast(graphics, GraphicsData).Save(file)
        Else
            Return Internal.debug.stop(New InvalidProgramException($"'{graphics.GetType.Name}' is not a graphics data object!"), envir)
        End If
    End Function

    ''' <summary>
    ''' returns the number and name of the new active device 
    ''' (after the specified device has been shut down).
    ''' </summary>
    ''' <param name="which">An integer specifying a device number.</param>
    ''' <returns></returns>
    <ExportAPI("dev.off")>
    Public Function devOff(Optional which% = -1) As Integer

    End Function

    ''' <summary>
    ''' returns a length-one named integer vector giving the number and name of the 
    ''' active device, or 1, the null device, if none is active.
    ''' </summary>
    ''' <returns></returns>
    <ExportAPI("dev.cur")>
    Public Function devCur() As Integer
        If curDev Is Nothing Then
            Return -1
        Else
            Return curDev.GetHashCode
        End If
    End Function

    ''' <summary>
    ''' # RGB Color Specification
    ''' 
    ''' This function creates colors corresponding to the given intensities (between 0 and max) of the red, 
    ''' green and blue primaries. The colour specification refers to the standard sRGB colorspace 
    ''' (IEC standard 61966).
    ''' 
    ''' An alpha transparency value can also be specified (As an opacity, so 0 means fully transparent And 
    ''' max means opaque). If alpha Is Not specified, an opaque colour Is generated.
    ''' 
    ''' The names argument may be used To provide names For the colors.
    ''' 
    ''' The values returned by these functions can be used With a col= specification In graphics functions 
    ''' Or In par.
    ''' </summary>
    ''' <param name="red">numeric vectors with values in [0, M] where M is maxColorValue. When this is 255, 
    ''' the red, blue, green, and alpha values are coerced to integers in 0:255 and the result is computed 
    ''' most efficiently.</param>
    ''' <param name="green"></param>
    ''' <param name="blue"></param>
    ''' <param name="alpha"></param>
    ''' <param name="names">character vector. The names for the resulting vector.</param>
    ''' <param name="maxColorValue">number giving the maximum of the color values range, see above.</param>
    ''' <returns>
    ''' A character vector with elements of 7 or 9 characters, "#" followed by the red, blue, green and 
    ''' optionally alpha values in hexadecimal (after rescaling to 0 ... 255). The optional alpha values 
    ''' range from 0 (fully transparent) to 255 (opaque).
    '''
    ''' R does Not use 'premultiplied alpha’.
    ''' </returns>
    ''' <remarks>
    ''' The colors may be specified by passing a matrix or data frame as argument red, and leaving blue and 
    ''' green missing. In this case the first three columns of red are taken to be the red, green and blue 
    ''' values.
    ''' 
    ''' Semi-transparent colors (0 &lt; alpha &lt; 1) are supported only on some devices: at the time Of 
    ''' writing On the pdf, windows, quartz And X11(type = "cairo") devices And associated bitmap devices 
    ''' (jpeg, png, bmp, tiff And bitmap). They are supported by several third-party devices such As those 
    ''' In packages Cairo, cairoDevice And JavaGD. Only some Of these devices support semi-transparent 
    ''' backgrounds.
    ''' 
    ''' Most other graphics devices plot semi-transparent colors As fully transparent, usually With a 
    ''' warning When first encountered.
    ''' 
    ''' NA values are Not allowed For any Of red, blue, green Or alpha.
    ''' </remarks>
    <ExportAPI("rgb")>
    Public Function rgb(red As Integer(),
                        green As Integer(),
                        blue As Integer(),
                        Optional alpha As Integer() = Nothing,
                        Optional names As String() = Nothing,
                        Optional maxColorValue As Integer = 1,
                        Optional envir As Environment = Nothing) As vector

        If alpha.IsNullOrEmpty Then
            alpha = {255}
        ElseIf {red, green, blue}.Any(Function(bytes) bytes.IsNullOrEmpty) Then
            Return Nothing
        End If

        Dim populate As Func(Of IEnumerable(Of Color)) =
            Iterator Function() As IEnumerable(Of Color)
                Dim counts As i32 = Scan0

                For Each r As Integer In red
                    For Each g As Integer In green
                        For Each b As Integer In blue
                            For Each a As Integer In alpha
                                If maxColorValue > 0 AndAlso ++counts = maxColorValue Then
                                    GoTo break
                                Else
                                    Yield Color.FromArgb(a, r, g, b)
                                End If
                            Next
                        Next
                    Next
                Next
break:
                ' exit iterator loops
            End Function
        Dim colors As New vector With {
            .data = populate().ToArray
        }
        Dim result As Object = Nothing

        If Not names.IsNullOrEmpty Then
            result = colors.setNames(names, envir)
        End If

        If Not result Is Nothing AndAlso result.GetType Is GetType(Message) Then
            Return result
        Else
            Return colors
        End If
    End Function
End Module