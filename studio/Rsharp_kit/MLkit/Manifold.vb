﻿#Region "Microsoft.VisualBasic::885fc2b32d3f3f3d4dd97096f01dd046, studio\Rsharp_kit\MLkit\Manifold.vb"

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

    ' Module Manifold
    ' 
    '     Function: asGraph, umapProjection
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.visualize
Imports Microsoft.VisualBasic.Data.visualize.Network.FileStream.Generic
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.DataMining.UMAP
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Invokes
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports Rdataframe = SMRUCC.Rsharp.Runtime.Internal.Object.dataframe
Imports REnv = SMRUCC.Rsharp.Runtime

''' <summary>
''' UMAP: Uniform Manifold Approximation and Projection for Dimension Reduction
''' </summary>
<Package("umap")>
Module Manifold

    Sub New()
        Call Internal.Object.Converts.makeDataframe.addHandler(GetType(Umap), AddressOf exportUmapTable)
    End Sub

    Private Function exportUmapTable(umap As Umap, args As list, env As Environment) As Rdataframe
        Dim labels = args.getByName("labels")
        Dim colNames = args.getByName("dimension")
        Dim table As New Rdataframe With {.columns = New Dictionary(Of String, Array)}

        Return table
    End Function

    ''' <summary>
    ''' UMAP: Uniform Manifold Approximation and Projection for Dimension Reduction
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="dimension%"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("umap")>
    <RApiReturn(GetType(list))>
    Public Function umapProjection(<RRawVectorArgument> data As Object,
                                   Optional dimension% = 2,
                                   Optional numberOfNeighbors As Integer = 15,
                                   Optional localConnectivity As Double = 1,
                                   Optional KnnIter As Integer = 64,
                                   Optional bandwidth As Double = 1,
                                   Optional env As Environment = Nothing) As Object
        Dim labels As String()
        Dim matrix As Double()()

        If TypeOf data Is Rdataframe Then
            labels = DirectCast(data, Rdataframe).getRowNames
            matrix = DirectCast(data, Rdataframe) _
                .forEachRow _
                .Select(Function(r)
                            Return r.Select(Function(n) CDbl(n)).ToArray
                        End Function) _
                .ToArray
        Else
            Dim raw As pipeline = pipeline.TryCreatePipeline(Of DataSet)(data, env)

            If raw.isError Then
                Return raw.getError
            End If

            Dim rawMatrix As DataSet() = raw.populates(Of DataSet)(env).ToArray
            Dim cols As String() = rawMatrix.PropertyNames

            labels = rawMatrix.Keys(distinct:=False)
            matrix = rawMatrix.Select(Function(r) r(cols)).ToArray
        End If

        Dim umap As New Umap(
            distance:=AddressOf DistanceFunctions.CosineForNormalizedVectors,
            dimensions:=dimension,
            numberOfNeighbors:=numberOfNeighbors,
            localConnectivity:=localConnectivity,
            KnnIter:=KnnIter,
            bandwidth:=bandwidth
        )
        Dim nEpochs As Integer

        Call Console.WriteLine("Initialize fit..")

        nEpochs = umap.InitializeFit(matrix)

        Console.WriteLine("- Done")
        Console.WriteLine()
        Console.WriteLine("Calculating..")

        For i As Integer = 0 To nEpochs - 1
            Call umap.Step()

            If i Mod 10 = 0 Then
                Console.WriteLine($"- Completed {i + 1} of {nEpochs}")
            End If
        Next

        Return New list With {
            .slots = New Dictionary(Of String, Object) From {
                {"labels", labels},
                {"umap", umap}
            }
        }
    End Function

    <ExportAPI("as.graph")>
    <RApiReturn(GetType(NetworkGraph))>
    Public Function asGraph(umap As Umap, <RRawVectorArgument> labels As Object,
                            <RRawVectorArgument>
                            Optional groups As Object = Nothing,
                            Optional env As Environment = Nothing) As Object

        Dim labelList As String() = REnv.asVector(Of String)(labels)
        Dim uniqueLabels As String() = labelList.makeNames(unique:=True)
        Dim g As NetworkGraph = umap.CreateGraph(uniqueLabels, labelList)

        If Not groups Is Nothing Then
            labelList = REnv.asVector(Of String)(groups)

            Call base.print("cluster groups that you defined for the nodes:", env)
            Call base.print(labelList.Distinct.OrderBy(Function(str) str).ToArray, env)

            For i As Integer = 0 To uniqueLabels.Length - 1
                g.GetElementByID(uniqueLabels(i)).data(NamesOf.REFLECTION_ID_MAPPING_NODETYPE) = labelList(i)
            Next
        End If

        Return g
    End Function
End Module
