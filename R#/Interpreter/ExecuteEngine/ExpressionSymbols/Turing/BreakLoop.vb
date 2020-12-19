﻿#Region "Microsoft.VisualBasic::aad3a40a19accde2f39fd6ab9d5647f5, R#\Interpreter\ExecuteEngine\ExpressionSymbols\Turing\BreakLoop.vb"

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

    '     Class BreakLoop
    ' 
    '         Properties: expressionName, type
    ' 
    '         Function: Evaluate, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.System.Package.File

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.Blocks

    Public Class BreakLoop : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Public Overrides ReadOnly Property expressionName As ExpressionTypes
            Get
                Return ExpressionTypes.Break
            End Get
        End Property

        Public Overrides Function Evaluate(envir As Environment) As Object
            Return envir("$").value
        End Function

        Public Overrides Function ToString() As String
            Return "break"
        End Function
    End Class
End Namespace
