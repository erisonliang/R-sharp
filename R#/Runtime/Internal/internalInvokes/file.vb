﻿#Region "Microsoft.VisualBasic::5682a55ffc10fa520e6ac554cbf737cd, R#\Runtime\Internal\internalInvokes\file.vb"

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

'     Module file
' 
'         Function: basename, buffer, close, dir_exists, dirCreate
'                   dirname, exists, file, filecopy, fileinfo
'                   getwd, listDirs, listFiles, loadListInternal, normalizeFileName
'                   normalizePath, openGzip, openZip, readLines, readList
'                   readText, Rhome, saveList, setwd, writeLines
' 
' 
' /********************************************************************************/

#End Region

Imports System.IO
Imports System.IO.Compression
Imports System.Reflection
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Emit.Delegates
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Language.UnixBash
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.My.UNIX
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Serialize
Imports SMRUCC.Rsharp.System.Components
Imports BASICString = Microsoft.VisualBasic.Strings
Imports fsOptions = Microsoft.VisualBasic.FileIO.SearchOption
Imports randf = Microsoft.VisualBasic.Math.RandomExtensions
Imports REnv = SMRUCC.Rsharp.Runtime

Namespace Runtime.Internal.Invokes

    ''' <summary>
    ''' #### File Manipulation
    ''' 
    ''' These functions provide a low-level interface to the computer's file system.
    ''' </summary>
    Public Module file

        ''' <summary>
        ''' Extract File Information
        ''' 
        ''' Utility function to extract information about files on the user's file systems.
        ''' </summary>
        ''' <param name="files"></param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("file.info")>
        Public Function fileinfo(files As String(), Optional env As Environment = Nothing) As Object
            If files.IsNullOrEmpty Then
                Return Nothing
            ElseIf files.Length = 1 Then
                Dim file As String = files(Scan0)
                Dim fileInfoObj As New FileInfo(file)
                Dim data As New Dictionary(Of String, Object)

                For Each [property] As PropertyInfo In fileInfoObj _
                    .GetType _
                    .GetProperties(PublicProperty) _
                    .Where(Function(p)
                               Return p.GetIndexParameters.IsNullOrEmpty
                           End Function)

                    Call data.Add([property].Name, [property].GetValue(fileInfoObj))
                Next

                Return New list With {.slots = data}
            Else
                Return Internal.debug.stop(New NotImplementedException, env)
            End If
        End Function

        ''' <summary>
        ''' ``file.copy`` works in a similar way to ``file.append`` but with the arguments 
        ''' in the natural order for copying. Copying to existing destination files is 
        ''' skipped unless overwrite = TRUE. The to argument can specify a single existing 
        ''' directory. If copy.mode = TRUE file read/write/execute permissions are copied 
        ''' where possible, restricted by ‘umask’. (On Windows this applies only to files.
        ''' ) Other security attributes such as ACLs are not copied. On a POSIX filesystem 
        ''' the targets of symbolic links will be copied rather than the links themselves, 
        ''' and hard links are copied separately. Using copy.date = TRUE may or may not 
        ''' copy the timestamp exactly (for example, fractional seconds may be omitted), 
        ''' but is more likely to do so as from R 3.4.0.
        ''' </summary>
        ''' <param name="from"></param>
        ''' <param name="to"></param>
        ''' <returns>
        ''' These functions return a logical vector indicating which operation succeeded 
        ''' for each of the files attempted. Using a missing value for a file or path 
        ''' name will always be regarded as a failure.
        ''' </returns>
        ''' 
        <ExportAPI("file.copy")>
        <RApiReturn(GetType(Boolean()))>
        Public Function filecopy(from$(), to$(), Optional env As Environment = Nothing) As Object
            Dim result As New List(Of Object)
            Dim isDir As Boolean = from.Length > 1 AndAlso [to].Length = 1

            If from.Length = 0 Then
                Return {}
            End If

            If isDir Then
                Dim dirName$ = [to](Scan0) & "/"

                For Each file As String In from
                    If file.FileCopy(dirName) Then
                        result.Add(True)
                    Else
                        result.Add(file)
                    End If
                Next
            ElseIf from.Length <> [to].Length Then
                Return Internal.debug.stop("number of from files is not equals to the number of target file locations!", env)
            Else
                For i As Integer = 0 To from.Length - 1
                    If from(i).FileCopy([to](i)) Then
                        result.Add(True)
                    Else
                        result.Add(from(i))
                    End If
                Next
            End If

            Return result.ToArray
        End Function

        ''' <summary>
        ''' Express File Paths in Canonical Form
        ''' 
        ''' Convert file paths to canonical form for the platform, to display them in a 
        ''' user-understandable form and so that relative and absolute paths can be 
        ''' compared.
        ''' </summary>
        ''' <param name="fileNames">character vector of file paths.</param>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        <ExportAPI("normalizePath")>
        <RApiReturn(GetType(String()))>
        Public Function normalizePath(fileNames$(), envir As Environment) As Object
            If fileNames.IsNullOrEmpty Then
                Return Internal.debug.stop("no file names provided!", envir)
            Else
                Return fileNames _
                    .Select(Function(path)
                                If path.DirectoryExists Then
                                    Return path.GetDirectoryFullPath
                                Else
                                    Return path.GetFullPath
                                End If
                            End Function) _
                    .ToArray
            End If
        End Function

        ''' <summary>
        ''' Return the R Home Directory
        ''' 
        ''' Return the R home directory, or the full path to a 
        ''' component of the R installation.
        ''' 
        ''' The R home directory is the top-level directory of the R installation being run.
        '''
        ''' The R home directory Is often referred To As R_HOME, And Is the value Of an 
        ''' environment variable Of that name In an R session. It can be found outside 
        ''' an R session by R RHOME.
        ''' </summary>
        ''' <returns></returns>
        <ExportAPI("R.home")>
        Public Function Rhome() As String
            Return GetType(file).Assembly.Location.ParentPath
        End Function

        ''' <summary>
        ''' ``dirname`` returns the part of the ``path`` up to but excluding the last path separator, 
        ''' or "." if there is no path separator.
        ''' </summary>
        ''' <param name="fileNames">character vector, containing path names.</param>
        ''' <returns></returns>
        <ExportAPI("dirname")>
        Public Function dirname(fileNames As String()) As String()
            Return fileNames.Select(AddressOf ParentPath).ToArray
        End Function

        ''' <summary>
        ''' List the Files in a Directory/Folder
        ''' </summary>
        ''' <param name="dir">
        ''' a character vector of full path names; the default corresponds to the working directory, ``getwd()``. 
        ''' Tilde expansion (see path.expand) is performed. Missing values will be ignored.
        ''' </param>
        ''' <param name="pattern">
        ''' an optional regular expression. Only file names which match the regular expression will be returned.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("list.files")>
        <RApiReturn(GetType(String()))>
        Public Function listFiles(Optional dir$ = "./",
                                  Optional pattern$() = Nothing,
                                  Optional recursive As Boolean = False) As Object

            If pattern.IsNullOrEmpty Then
                pattern = {"*.*"}
            End If

            If dir.ExtensionSuffix("zip") AndAlso dir.FileLength > 0 Then
                Using zip As New ZipFolder(dir)
                    Return Search.DoFileNameGreps(ls - l - r - pattern, zip.ls).ToArray
                End Using
            Else
                If recursive Then
                    Return (ls - l - r - pattern <= dir).ToArray
                Else
                    Return (ls - l - pattern <= dir).ToArray
                End If
            End If
        End Function

        ''' <summary>
        ''' List the Files in a Directory/Folder
        ''' </summary>
        ''' <param name="dir">
        ''' a character vector of full path names; the default corresponds to the working directory, ``getwd()``. 
        ''' Tilde expansion (see path.expand) is performed. Missing values will be ignored.
        ''' </param>
        ''' <param name="fullNames"></param>
        ''' <param name="recursive"></param>
        ''' <returns></returns>
        <ExportAPI("list.dirs")>
        <RApiReturn(GetType(String()))>
        Public Function listDirs(Optional dir$ = "./",
                                 Optional fullNames As Boolean = True,
                                 Optional recursive As Boolean = True) As Object

            If Not dir.DirectoryExists Then
                Return {}
            Else
                Dim level As fsOptions = If(recursive, fsOptions.SearchAllSubDirectories, fsOptions.SearchTopLevelOnly)
                Dim dirs$() = dir _
                    .ListDirectory(level, fullNames) _
                    .ToArray

                Return dirs
            End If
        End Function

        ''' <summary>
        ''' removes all of the path up to and including the last path separator (if any).
        ''' </summary>
        ''' <param name="fileNames">character vector, containing path names.</param>
        ''' <param name="withExtensionName"></param>
        ''' <returns></returns>
        <ExportAPI("basename")>
        Public Function basename(fileNames$(), Optional withExtensionName As Boolean = False) As String()
            If withExtensionName Then
                ' get fileName
                Return fileNames.Select(AddressOf FileName).ToArray
            Else
                Return fileNames _
                    .Select(Function(file)
                                If file.DirectoryExists Then
                                    Return file.DirectoryName
                                Else
                                    Return file.BaseName
                                End If
                            End Function) _
                    .ToArray
            End If
        End Function

        <ExportAPI("normalize.filename")>
        Public Function normalizeFileName(strings$()) As String()
            Return strings _
                .Select(Function(file)
                            Return file.NormalizePathString(False)
                        End Function) _
                .ToArray
        End Function

        ''' <summary>
        ''' ``file.exists`` returns a logical vector indicating whether the files named by its 
        ''' argument exist. (Here ‘exists’ is in the sense of the system's stat call: a file 
        ''' will be reported as existing only if you have the permissions needed by stat. 
        ''' Existence can also be checked by file.access, which might use different permissions 
        ''' and so obtain a different result. Note that the existence of a file does not 
        ''' imply that it is readable: for that use file.access.) What constitutes a ‘file’ 
        ''' is system-dependent, but should include directories. (However, directory names 
        ''' must not include a trailing backslash or slash on Windows.) Note that if the file 
        ''' is a symbolic link on a Unix-alike, the result indicates if the link points to 
        ''' an actual file, not just if the link exists. Lastly, note the different function 
        ''' exists which checks for existence of R objects.
        ''' </summary>
        ''' <param name="files">
        ''' character vectors, containing file names or paths.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("file.exists")>
        Public Function exists(files$()) As Boolean()
            Return files.Select(AddressOf FileExists).ToArray
        End Function

        <ExportAPI("dir.create")>
        Public Function dirCreate(path$, Optional showWarnings As Boolean = True, Optional recursive As Boolean = False, Optional mode$ = "0777") As Boolean
            If showWarnings AndAlso path.DirectoryExists Then

            End If

            Call path.MkDIR

            Return True
        End Function

        ''' <summary>
        ''' dir.exists returns a logical vector of TRUE or FALSE values (without names).
        ''' </summary>
        ''' <param name="paths">
        ''' character vectors containing file or directory paths. 
        ''' Tilde expansion (see path.expand) is done.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("dir.exists")>
        Public Function dir_exists(paths As String()) As Boolean()
            Return paths.Select(AddressOf DirectoryExists).ToArray
        End Function

        ''' <summary>
        ''' Read Text Lines from a Connection
        ''' 
        ''' Read some or all text lines from a connection.
        ''' </summary>
        ''' <param name="con">a connection object or a character string.</param>
        ''' <returns></returns>
        <ExportAPI("readLines")>
        Public Function readLines(con As String, Optional encoding As Encodings = Encodings.UTF8) As String()
            Return con.ReadAllLines(encoding.CodePage)
        End Function

        <ExportAPI("readText")>
        Public Function readText(con As String, Optional encoding As Encodings = Encodings.UTF8) As String
            Return con.ReadAllText(encoding.CodePage)
        End Function

        ' writeLines(text, con = stdout(), sep = "\n", useBytes = FALSE)

        ''' <summary>
        ''' ### Write Lines to a Connection
        ''' 
        ''' Write text lines to a connection.
        ''' </summary>
        ''' <param name="text">A character vector</param>
        ''' <param name="con">A connection Object Or a character String.</param>
        ''' <param name="sep">
        ''' character string. A string to be written to the connection after each line of text.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks>
        ''' If the con is a character string, the function calls file to obtain a file connection
        ''' which is opened for the duration of the function call.
        '''
        ''' If the connection Is open it Is written from its current position. If it Is Not open, 
        ''' it Is opened For the duration Of the Call In "wt" mode And Then closed again.
        '''
        ''' Normally writeLines Is used With a text-mode connection, And the Default separator Is 
        ''' converted To the normal separator For that platform (LF On Unix/Linux, CRLF On Windows). 
        ''' For more control, open a binary connection And specify the precise value you want 
        ''' written To the file In sep. For even more control, use writeChar On a binary connection.
        '''
        ''' useBytes Is for expert use. Normally (when false) character strings with marked 
        ''' encodings are converted to the current encoding before being passed to the connection 
        ''' (which might do further re-encoding). useBytes = TRUE suppresses the re-encoding of 
        ''' marked strings so they are passed byte-by-byte to the connection: this can be useful 
        ''' When strings have already been re-encoded by e.g. iconv. (It Is invoked automatically 
        ''' For strings With marked encoding "bytes".)
        ''' </remarks>
        <ExportAPI("writeLines")>
        Public Function writeLines(text$(), Optional con As Object = Nothing, Optional sep$ = vbCrLf, Optional env As Environment = Nothing) As Object
            If con Is Nothing OrElse (TypeOf con Is String AndAlso DirectCast(con, String).StringEmpty) Then
                Dim stdOut As Action(Of String)

                If env.globalEnvironment.stdout Is Nothing Then
                    stdOut = AddressOf Console.WriteLine
                Else
                    stdOut = AddressOf env.globalEnvironment.stdout.WriteLine
                End If

                Call text.AsObjectEnumerator _
                    .JoinBy(sep) _
                    .DoCall(stdOut)
            ElseIf TypeOf con Is String Then
                Call text.AsObjectEnumerator _
                    .JoinBy(sep) _
                    .SaveTo(con, Encodings.UTF8WithoutBOM.CodePage)
            ElseIf TypeOf con Is textBuffer Then
                DirectCast(con, textBuffer).text = text.AsObjectEnumerator.JoinBy(sep)
                Return con
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' getwd returns an absolute filepath representing the current working directory of the R process;
        ''' </summary>
        ''' <returns></returns>
        <ExportAPI("getwd")>
        Public Function getwd() As String
            Return App.CurrentDirectory
        End Function

        ''' <summary>
        ''' setwd(dir) is used to set the working directory to dir.
        ''' </summary>
        ''' <param name="dir">A character String: tilde expansion will be done.</param>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        <ExportAPI("setwd")>
        <RApiReturn(GetType(String))>
        Public Function setwd(dir$(), envir As Environment) As Object
            If dir.Length = 0 Then
                Return invoke.missingParameter(NameOf(setwd), "dir", envir)
            ElseIf dir(Scan0).StringEmpty Then
                Return invoke.invalidParameter("cannot change working directory due to the reason of NULL value provided!", NameOf(setwd), "dir", envir)
            Else
                App.CurrentDirectory = PathMapper.GetMapPath(dir(Scan0))
            End If

            Return App.CurrentDirectory
        End Function

        ''' <summary>
        ''' Save a R# object list in json file format
        ''' </summary>
        ''' <param name="list"></param>
        ''' <param name="file$"></param>
        ''' <returns></returns>
        <ExportAPI("save.list")>
        Public Function saveList(list As Object, file$, Optional encodings As Encodings = Encodings.UTF8, Optional envir As Environment = Nothing) As Object
            If list Is Nothing Then
                Return False
            End If

            Dim json$
            Dim listType As Type = list.GetType

            If listType Is GetType(list) Then
                json = DirectCast(list, list).slots.GetJson(knownTypes:=listKnownTypes)
            ElseIf listType.ImplementInterface(GetType(IDictionary)) AndAlso
                listType.GenericTypeArguments.Length > 0 AndAlso
                listType.GenericTypeArguments(Scan0) Is GetType(String) Then

                json = JsonContract.GetObjectJson(listType, list, True, True, listKnownTypes)
            Else
                Return Internal.debug.stop(New NotSupportedException(listType.FullName), envir)
            End If

            Return json.SaveTo(file, encodings.CodePage)
        End Function

        ReadOnly listKnownTypes As Type() = {
            GetType(String), GetType(Boolean), GetType(Double), GetType(Long), GetType(Integer),
            GetType(String()), GetType(Boolean()), GetType(Double()), GetType(Long()), GetType(Integer())
        }

        ''' <summary>
        ''' read list from a given json file
        ''' </summary>
        ''' <param name="file">A json file path</param>
        ''' <param name="mode">The value mode of the loaded list object in ``R#``</param>
        ''' <param name="ofVector">
        ''' Is a list of vector?
        ''' </param>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        <ExportAPI("read.list")>
        Public Function readList(file$,
                                 Optional mode$ = "character",
                                 Optional ofVector As Boolean = False,
                                 Optional encoding As Encodings = Encodings.UTF8,
                                 Optional envir As Environment = Nothing) As Object

            Select Case BASICString.LCase(mode)
                Case "character" : Return loadListInternal(Of String)(file, ofVector, encoding.CodePage)
                Case "numeric" : Return loadListInternal(Of Double)(file, ofVector, encoding.CodePage)
                Case "integer" : Return loadListInternal(Of Long)(file, ofVector, encoding.CodePage)
                Case "logical" : Return loadListInternal(Of Boolean)(file, ofVector, encoding.CodePage)
                Case "any"
                    Return file.LoadJsonFile(Of Dictionary(Of String, Object))(knownTypes:=listKnownTypes)
                Case Else
                    Return Internal.debug.stop($"Invalid data mode: '{mode}'!", envir)
            End Select
        End Function

        Private Function loadListInternal(Of T)(file As String, ofVector As Boolean, encoding As Encoding) As Object
            If ofVector Then
                Return file.LoadJsonFile(Of Dictionary(Of String, T()))(encoding)
            Else
                Return file.LoadJsonFile(Of Dictionary(Of String, T))(encoding)
            End If
        End Function

        ''' <summary>
        ''' Functions to create, open and close connections, i.e., 
        ''' "generalized files", such as possibly compressed files, 
        ''' URLs, pipes, etc.
        ''' </summary>
        ''' <param name="description">character string. A description of the connection: see ‘Details’.</param>
        ''' <param name="open">
        ''' character string. A description of how to open the connection (if it should be opened initially). 
        ''' See section ‘Modes’ for possible values.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("file")>
        Public Function file(description$,
                             Optional open As FileMode = FileMode.OpenOrCreate,
                             Optional truncate As Boolean = False) As FileStream

            If open = FileMode.Truncate OrElse open = FileMode.CreateNew Then
                Return description.Open(open, doClear:=truncate)
            Else
                Return description.Open(open, doClear:=False)
            End If
        End Function

        ''' <summary>
        ''' close connections, i.e., “generalized files”, such as possibly compressed files, URLs, pipes, etc.
        ''' </summary>
        ''' <param name="con">a connection.</param>
        ''' <returns></returns>
        ''' 
        <ExportAPI("close")>
        <RApiReturn(GetType(Boolean))>
        Public Function close(con As Object, Optional env As Environment = Nothing) As Object
            If con Is Nothing Then
                Return Internal.debug.stop("the required connection can not be nothing!", env)
            ElseIf TypeOf con Is Stream Then
                With DirectCast(con, Stream)
                    Call .Flush()
                    Call .Close()
                    Call .Dispose()
                End With

                Return True
            ElseIf TypeOf con Is StreamWriter Then
                With DirectCast(con, StreamWriter)
                    Call .Flush()
                    Call .Close()
                    Call .Dispose()
                End With

                Return True
            ElseIf con.GetType.ImplementInterface(GetType(IDisposable)) Then
                Call DirectCast(con, IDisposable).Dispose()
                Return True
            Else
                Return Internal.debug.stop(Message.InCompatibleType(GetType(Stream), con.GetType, env), env)
            End If
        End Function

        ''' <summary>
        ''' open a zip file
        ''' </summary>
        ''' <param name="file"></param>
        ''' <param name="env"></param>
        ''' <returns>
        ''' a folder liked list object
        ''' </returns>
        <ExportAPI("open.zip")>
        <RApiReturn(GetType(ZipFolder))>
        Public Function openZip(file As String, Optional env As Environment = Nothing) As Object
            If Not file.FileExists Then
                Return debug.stop({"target file is not exists on your file system!", "file: " & file}, env)
            Else
                Return New ZipFolder(file)
            End If
        End Function

        ''' <summary>
        ''' decompression of a gzip file and get the deflate file data stream.
        ''' </summary>
        ''' <param name="file">
        ''' the file path or file stream data.
        ''' </param>
        ''' <param name="tmpfileWorker">
        ''' using tempfile for process the large data file which its file length 
        ''' is greater then the memorystream its upbound capacity.
        ''' </param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("open.gzip")>
        <RApiReturn(GetType(Stream))>
        Public Function openGzip(file As Object,
                                 Optional tmpfileWorker$ = Nothing,
                                 Optional env As Environment = Nothing) As Object

            If file Is Nothing Then
                Return Nothing
            End If

            Dim originalFileStream As Stream

            If TypeOf file Is String Then
                originalFileStream = DirectCast(file, String).Open(FileMode.Open, doClear:=False)
            ElseIf TypeOf file Is Stream Then
                originalFileStream = DirectCast(file, Stream)
            Else
                Return Internal.debug.stop(Message.InCompatibleType(GetType(Stream), file.GetType, env), env)
            End If

            Using originalFileStream
                Dim deflate As Stream

                If Not tmpfileWorker.StringEmpty Then
                    deflate = tmpfileWorker.Open(FileMode.OpenOrCreate, doClear:=True)
                Else
                    deflate = New MemoryStream
                End If

                Using decompressionStream As New GZipStream(originalFileStream, CompressionMode.Decompress)
                    decompressionStream.CopyTo(deflate)
                    deflate.Position = Scan0
                End Using

                Return deflate
            End Using
        End Function

        ''' <summary>
        ''' create a new buffer object
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        <ExportAPI("buffer")>
        Public Function buffer(Optional type As BufferObjects = BufferObjects.raw, Optional env As Environment = Nothing) As Object
            Select Case type
                Case BufferObjects.raw : Return New rawBuffer
                Case BufferObjects.text : Return New textBuffer
                Case BufferObjects.bitmap : Return New bitmapBuffer
                Case BufferObjects.vector : Return New vectorBuffer
                Case Else
                    Return Internal.debug.stop(New NotImplementedException(type.Description), env)
            End Select
        End Function

        ''' <summary>
        ''' ### Create Names for Temporary Files
        ''' 
        ''' ``tempfile`` returns a vector of character strings 
        ''' which can be used as names for temporary files.
        ''' </summary>
        ''' <param name="pattern">a non-empty character vector giving the initial part of the name.</param>
        ''' <param name="tmpdir">a non-empty character vector giving the directory name</param>
        ''' <param name="fileext">a non-empty character vector giving the file extension</param>
        ''' <returns>
        ''' a character vector giving the names of possible (temporary) files. 
        ''' Note that no files are generated by tempfile.
        ''' </returns>
        ''' <remarks>
        ''' The length of the result is the maximum of the lengths of the three arguments; 
        ''' values of shorter arguments are recycled.
        '''
        ''' The names are very likely To be unique among calls To tempfile In an R session 
        ''' And across simultaneous R sessions (unless tmpdir Is specified). The filenames 
        ''' are guaranteed Not To be currently In use.
        '''
        ''' The file name Is made by concatenating the path given by tmpdir, the pattern 
        ''' String, a random String In hex And a suffix Of fileext.
        '''
        ''' By Default, tmpdir will be the directory given by tempdir(). This will be a 
        ''' subdirectory of the per-session temporary directory found by the following 
        ''' rule when the R session Is started. The environment variables TMPDIR, TMP And TEMP 
        ''' are checked in turn And the first found which points to a writable directory Is 
        ''' used: If none succeeds the value Of R_USER (see Rconsole) Is used. If the path 
        ''' To the directory contains a space In any Of the components, the path returned will 
        ''' use the shortnames version Of the path. Note that setting any Of these environment 
        ''' variables In the R session has no effect On tempdir(): the per-session temporary 
        ''' directory Is created before the interpreter Is started.
        ''' </remarks>
        <ExportAPI("tempfile")>
        <RApiReturn(GetType(String))>
        Public Function tempfile(<RRawVectorArgument>
                                 Optional pattern As Object = "file",
                                 <RDefaultExpression>
                                 Optional tmpdir$ = "~tempdir()",
                                 <RRawVectorArgument>
                                 Optional fileext As Object = ".tmp",
                                 Optional env As Environment = Nothing) As Object

            Dim patterns As String() = REnv.asVector(Of String)(pattern)
            Dim exts As String() = REnv.asVector(Of String)(fileext)
            Dim files As New List(Of String)

            If patterns.Length = 1 Then
                For Each ext As String In exts
                    files += $"{tmpdir}/{patterns(Scan0)}{NextTempToken()}{ext}".GetFullPath
                Next
            ElseIf exts.Length = 1 Then
                For Each patternStr As String In patterns
                    files += $"{tmpdir}/{patternStr}{NextTempToken()}{exts(Scan0)}".GetFullPath
                Next
            ElseIf patterns.Length <> exts.Length Then
                Return Internal.debug.stop({
                    $"the size of filename patterns should be equals to the file extension names!",
                    $"sizeof pattern: {patterns.Length}",
                    $"sizeof fileext: {exts.Length}"
                }, env)
            Else
                For i As Integer = 0 To exts.Length - 1
                    files += $"{tmpdir}/{patterns(i)}{NextTempToken()}{exts(i)}".GetFullPath
                Next
            End If

            Return files.ToArray
        End Function

        Private Function NextTempToken() As String
            Return (randf.NextInteger(10000).ToString & Now.ToString).MD5.Substring(3, 9)
        End Function

        ''' <summary>
        ''' ### Create Names For Temporary Files
        ''' </summary>
        ''' <param name="check">
        ''' logical indicating if tmpdir() should be checked and recreated if no longer valid.
        ''' </param>
        ''' <returns>the path of the per-session temporary directory.</returns>
        ''' <remarks>
        ''' + On Windows, both will use a backslash as the path separator.
        ''' + On a Unix-alike, the value will be an absolute path (unless tmpdir Is set to a relative path), 
        '''   but it need Not be canonical (see normalizePath) And on macOS it often Is Not.
        ''' </remarks>
        <ExportAPI("tempdir")>
        Public Function tempdir(Optional check As Boolean = False) As String
            Static dir As String = (App.AppSystemTemp & $"/Rtmp{App.PID.ToString.MD5.Substring(3, 6).ToUpper}").GetDirectoryFullPath

            If check Then
                Call dir.MkDIR
            End If

            Return dir
        End Function

        ''' <summary>
        ''' File renames
        ''' </summary>
        ''' <param name="from">character vectors, containing file names Or paths.</param>
        ''' <param name="to">character vectors, containing file names Or paths.</param>
        ''' <param name="env"></param>
        <ExportAPI("file.rename")>
        Public Sub fileRename(from$, to$, Optional env As Environment = Nothing)
            If Not from.FileExists Then
                Call env.AddMessage({$"the given file is not exists...", $"source file: {from}"}, MSG_TYPES.WRN)
            Else
                Call [to].ParentPath.MkDIR
                Call from.FileMove(to$)
            End If
        End Sub

        ''' <summary>
        ''' Delete files or directories
        ''' </summary>
        <ExportAPI("file.remove")>
        Public Sub fileRemove(x As String())
            For Each file As String In x.SafeQuery
                Call file.DeleteFile
            Next
        End Sub
    End Module
End Namespace
