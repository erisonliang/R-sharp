﻿#Region "Microsoft.VisualBasic::1f87d593bccec6b6e9c7498e851080c5, Library\R.base\utils\xlsx.vb"

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

    ' Module xlsx
    ' 
    '     Function: createSheet, createWorkbook, getSheetNames, openXlsx, readXlsx
    '               writeXlsx
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.csv
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.MIME.Office.Excel.MsHtml
Imports Microsoft.VisualBasic.MIME.Office.Excel.XML.xl.worksheets
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports csv = Microsoft.VisualBasic.Data.csv.IO.File
Imports msXlsx = Microsoft.VisualBasic.MIME.Office.Excel.File
Imports Rdataframe = SMRUCC.Rsharp.Runtime.Internal.Object.dataframe
Imports REnv = SMRUCC.Rsharp.Runtime
Imports Rsharp = SMRUCC.Rsharp

''' <summary>
''' Xlsx file toolkit
''' </summary>
<Package("xlsx", Category:=APICategories.UtilityTools)>
Module xlsx

    ''' <summary>
    ''' read a sheet table in xlsx file as a ``dataframe`` object.
    ''' </summary>
    ''' <param name="sheetIndex">
    ''' the data sheet index or name for read data
    ''' </param>
    ''' <returns></returns>
    ''' 
    <ExportAPI("read.xlsx")>
    <RApiReturn(GetType(Rdataframe), GetType(csv))>
    Public Function readXlsx(file As Object,
                             Optional sheetIndex$ = "Sheet1",
                             <RRawVectorArgument>
                             Optional row_names As Object = Nothing,
                             Optional raw As Boolean = False,
                             Optional check_names As Boolean = True,
                             Optional check_modes As Boolean = True,
                             Optional env As Environment = Nothing) As Object

        Dim xlsx As msXlsx

        If TypeOf file Is String Then
            xlsx = msXlsx.Open(DirectCast(file, String))
        ElseIf TypeOf file Is msXlsx Then
            xlsx = DirectCast(file, msXlsx)
        Else
            Return Internal.debug.stop(Message.InCompatibleType(GetType(String), file.GetType, env), env)
        End If

        Dim table As csv = xlsx.GetTable(sheetName:=sheetIndex)

        If raw Then
            Return table
        Else
            Return table.rawToDataFrame(row_names, check_names, check_modes, env)
        End If
    End Function

    <ExportAPI("open.xlsx")>
    Public Function openXlsx(file As String) As msXlsx
        Return msXlsx.Open(file)
    End Function

    <ExportAPI("sheetNames")>
    Public Function getSheetNames(file As msXlsx) As vector
        Return file.SheetNames.DoCall(AddressOf Internal.[Object].vector.asVector)
    End Function

    <ExportAPI("write.xlsx")>
    <RApiReturn(GetType(Boolean))>
    Public Function writeXlsx(<RRawVectorArgument> x As Object, file$,
                              Optional sheetName$ = "Sheet1",
                              Optional row_names As Boolean = True,
                              Optional fileEncoding As Object = "",
                              Optional env As Environment = Nothing) As Object
        If x Is Nothing Then
            Return Internal.debug.stop("Empty dataframe object!", env)
        ElseIf Not file.StringEmpty Then
            ' test if the target table file is not locked by excel
            Dim err As Object = REnv.TryCatch(Function() "".SaveTo(file), debug:=env.globalEnvironment.debugMode)

            If Not TypeOf err Is Boolean Then
                Return Internal.debug.stop(err, env)
            End If
        End If

        Dim type As Type = x.GetType
        Dim encoding As Encodings = TextEncodings.GetEncodings(Rsharp.GetEncoding(fileEncoding))
        Dim table As File

        If type Is GetType(Rdataframe) Then
            table = DirectCast(x, Rdataframe).DataFrameRows(row_names, env)
        ElseIf type Is GetType(File) Then
            table = DirectCast(x, File)
        ElseIf type Is GetType(IO.DataFrame) Then
            table = DirectCast(x, IO.DataFrame)
        ElseIf REnv.isVector(Of EntityObject)(x) Then
            table = Reflector.GetsRowData(
                source:=DirectCast(REnv.asVector(Of EntityObject)(x), EntityObject()).Select(Function(d) CObj(d)),
                type:=GetType(EntityObject),
                strict:=False,
                maps:=Nothing,
                parallel:=False,
                metaBlank:="",
                reorderKeys:=0,
                layout:=Nothing
            ).DoCall(Function(rows) New File(rows))
        ElseIf REnv.isVector(Of DataSet)(x) Then
            table = Reflector.GetsRowData(
                source:=DirectCast(REnv.asVector(Of DataSet)(x), DataSet()).Select(Function(d) CObj(d)),
                type:=GetType(DataSet),
                strict:=False,
                maps:=Nothing,
                parallel:=False,
                metaBlank:="",
                reorderKeys:=0,
                layout:=Nothing
            ).DoCall(Function(rows) New File(rows))
        ElseIf type.IsArray OrElse type Is GetType(vector) Then
            table = Reflector.doSave(objSource:=utils.MeasureGenericType(x, type),
                typeDef:=type,
                strict:=False,
                schemaOut:=Nothing
            ).DoCall(Function(rows) New File(rows))
        Else
            Return Message.InCompatibleType(GetType(File), type, env)
        End If

        Return table.ToExcel(sheetName).SaveTo(file, encoding.CodePage, append:=False)
    End Function

    <ExportAPI("createWorkbook")>
    Public Function createWorkbook() As msXlsx

    End Function

    ''' <summary>
    ''' create a new worksheet for a given xlsx file 
    ''' </summary>
    ''' <param name="wb"></param>
    ''' <param name="sheetName$"></param>
    ''' <returns></returns>
    <ExportAPI("createSheet")>
    Public Function createSheet(wb As msXlsx, Optional sheetName$ = "Sheet1") As worksheet
        Return wb.AddSheetTable(sheetName)
    End Function

End Module
