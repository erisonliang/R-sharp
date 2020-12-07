﻿#Region "Microsoft.VisualBasic::b45dd77ad88966de2dd6e5fc5880ffe4, R#\System\Package\PackageFile\DESCRIPTION.vb"

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

    '     Class DESCRIPTION
    ' 
    '         Properties: [Date], Author, Description, License, Maintainer
    '                     MetaData, Package, Title, Type, Version
    ' 
    '         Function: Parse
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace System.Package.File

    Public Class DESCRIPTION

        Public Property Package As String
        Public Property Type As String
        Public Property Title As String
        Public Property Version As String
        Public Property [Date] As String
        Public Property Author As String
        Public Property Maintainer As String
        Public Property Description As String
        Public Property License As String
        Public Property MetaData As Dictionary(Of String, String)

        Public Shared Function Parse(file As String) As DESCRIPTION

        End Function

    End Class
End Namespace