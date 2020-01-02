﻿#Region "Microsoft.VisualBasic::a69511755691d3c2e376e56081b4a99b, Library\R.graph\Comparison.vb"

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

    ' Module Comparison
    ' 
    '     Function: (+2 Overloads) similarity
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.Data.visualize.Network
Imports Microsoft.VisualBasic.Scripting.MetaData

''' <summary>
''' Network graph comparison tools
''' </summary>
<Package("igraph.comparison")>
Module Comparison

    ''' <summary>
    ''' calculate node similarity cos score.
    ''' </summary>
    ''' <param name="a"></param>
    ''' <param name="b"></param>
    ''' <returns></returns>
    <ExportAPI("node.cos")>
    Public Function similarity(a As Node, b As Node) As Double
        Return Analysis.NodeSimilarity(a, b)
    End Function

    ''' <summary>
    ''' calculate graph jaccard similarity based on the nodes' cos score.
    ''' </summary>
    ''' <param name="a"></param>
    ''' <param name="b"></param>
    ''' <param name="cutoff#"></param>
    ''' <returns></returns>
    <ExportAPI("graph.jaccard")>
    Public Function similarity(a As NetworkGraph, b As NetworkGraph, Optional cutoff# = 0.85) As Double
        Return Analysis.GraphSimilarity(a, b, cutoff)
    End Function
End Module

