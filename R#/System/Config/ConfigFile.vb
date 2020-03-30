﻿#Region "Microsoft.VisualBasic::5ce1c54ec1660ab542c42e9fe8dfa31f, R#\System\Config\ConfigFile.vb"

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

    '     Class StartupConfigs
    ' 
    '         Properties: loadingPackages
    ' 
    '         Function: DefaultLoadingPackages
    ' 
    '     Class ConfigFile
    ' 
    '         Properties: config, localConfigs, size, startups, system
    ' 
    '         Function: EmptyConfigs, GenericEnumerator, GetEnumerator, GetStartupLoadingPackages, Load
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Xml.Serialization
Imports Microsoft.VisualBasic.ApplicationServices.Development
Imports Microsoft.VisualBasic.ComponentModel
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Text.Xml.Models

Namespace System.Configuration

    Public Class StartupConfigs

        Public Property loadingPackages As String()

        Public Shared Function DefaultLoadingPackages() As String()
            Return {"base", "utils", "grDevices", "stats"}
        End Function

    End Class

    Public Class ConfigFile : Inherits XmlDataModel
        Implements IList(Of NamedValue)

        <XmlAttribute>
        Public Property size As Integer Implements IList(Of NamedValue).size
            Get
                Return config.Length
            End Get
            Set(value As Integer)
                ' readonly
                ' do nothing
            End Set
        End Property

        <XmlElement> Public Property system As AssemblyInfo
        <XmlElement> Public Property config As NamedValue()
        <XmlElement> Public Property startups As StartupConfigs

        Public Shared ReadOnly Property localConfigs As String = App.LocalData & "/R#.configs.xml"

        Public Function GetStartupLoadingPackages() As String()
            If startups Is Nothing Then
                Return StartupConfigs.DefaultLoadingPackages
            ElseIf startups.loadingPackages.IsNullOrEmpty Then
                Return StartupConfigs.DefaultLoadingPackages
            Else
                Return startups.loadingPackages
            End If
        End Function

        Public Shared Function EmptyConfigs() As ConfigFile
            Return New ConfigFile With {
                .config = {},
                .system = GetType(ConfigFile).Assembly.FromAssembly
            }
        End Function

        Public Shared Function Load(configs As String) As ConfigFile
            If configs.FileLength < 100 Then
                Return EmptyConfigs()
            Else
                Return configs.LoadXml(Of ConfigFile)
            End If
        End Function

        Public Iterator Function GenericEnumerator() As IEnumerator(Of NamedValue) Implements Enumeration(Of NamedValue).GenericEnumerator
            For Each item As NamedValue In config
                Yield item
            Next
        End Function

        Public Iterator Function GetEnumerator() As IEnumerator Implements Enumeration(Of NamedValue).GetEnumerator
            Yield GenericEnumerator()
        End Function
    End Class
End Namespace
