﻿#Region "Microsoft.VisualBasic::fb1f6b9755fc739a8fe704277ae32b3e, R#\Interpreter\Syntax\SyntaxImplements\RequireSyntax.vb"

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

    '     Module RequireSyntax
    ' 
    '         Function: Require
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Language
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module RequireSyntax

        Public Function Require(names As Token()) As SyntaxResult
            Dim packages As New List(Of Expression)

            For Each name As SyntaxResult In names _
                .SplitByTopLevelDelimiter(TokenType.comma) _
                .Where(Function(b) Not b.isComma) _
                .Select(AddressOf Expression.CreateExpression)

                If name.isException Then
                    Return name
                Else
                    packages += name.expression
                End If
            Next

            Return New SyntaxResult(New Require(packages))
        End Function
    End Module
End Namespace
