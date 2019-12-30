﻿#Region "Microsoft.VisualBasic::decf2d0ebe27bca6d130a207ebbc76bc, R#\Interpreter\Extensions.vb"

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

    '     Module Extensions
    ' 
    '         Function: GetExpressions, getMessageColor, getMessagePrefix, isTerminator, printMessageInternal
    '                   RunProgram
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter

    <HideModuleName> Module Extensions

        <Extension>
        Public Function RunProgram(code As Token(), envir As Environment) As Object
            Return Program.CreateProgram(code).Execute(envir)
        End Function

        ReadOnly ignores As Index(Of TokenType) = {
            TokenType.comment,
            TokenType.terminator
        }

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension>
        <DebuggerStepThrough>
        Private Function isTerminator(block As Token()) As Boolean
            Return block.Length = 1 AndAlso block(Scan0).name Like ignores
        End Function

        <Extension>
        Public Iterator Function GetExpressions(code As Token()) As IEnumerable(Of Expression)
            For Each block In code.SplitByTopLevelDelimiter(TokenType.terminator)
                If block.Length = 0 OrElse block.isTerminator Then
                    ' skip code comments
                    ' do nothing
                Else
                    ' have some bugs about
                    ' handles closure
                    Dim parts() = block _
                        .Where(Function(t) Not t.name = TokenType.comment) _
                        .SplitByTopLevelDelimiter(TokenType.close,, "}") _
                        .Split(2)
                    Dim expr As Expression

                    For Each joinBlock In parts
                        block = joinBlock(Scan0).JoinIterates(joinBlock.ElementAtOrDefault(1)).ToArray
                        expr = Expression.CreateExpression(block)

                        Yield expr
                    Next
                End If
            Next
        End Function
    End Module
End Namespace
