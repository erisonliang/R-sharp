﻿#Region "Microsoft.VisualBasic::dbae2d6c71a16397d3bc1f31d5b5daf0, R#\Runtime\Internal\internalInvokes\Math\math.vb"

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

    '     Module math
    ' 
    '         Function: abs, cluster1D, exp, log, log10
    '                   log2, max, mean, min, pearson
    '                   pow, rnorm, round, rsd, runif
    '                   sample, sample_int, sd, sqrt, sum
    '                   var
    ' 
    '         Sub: set_seed
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.Correlations
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports randf = Microsoft.VisualBasic.Math.RandomExtensions
Imports REnv = SMRUCC.Rsharp.Runtime
Imports stdNum = System.Math

Namespace Runtime.Internal.Invokes

    ''' <summary>
    ''' R# math module
    ''' </summary>
    Module math

        ''' <summary>
        ''' rounds the values in its first argument to the specified number of decimal places (default 0). 
        ''' See *'Details'* about "round to even" when rounding off a 5.
        ''' </summary>
        ''' <param name="x">a numeric vector. Or, for ``round`` and ``signif``, a complex vector.</param>
        ''' <param name="decimals">
        ''' integer indicating the number of decimal places (``round``) or significant digits (``signif``) to be used. 
        ''' Negative values are allowed (see *'Details'*).
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("round")>
        Public Function round(x As Array, Optional decimals% = 0) As Double()
            If x Is Nothing OrElse x.Length = 0 Then
                Return Nothing
            Else
                Dim rounds = From element As Double
                             In REnv.asVector(Of Double)(x)
                             Select stdNum.Round(element, decimals)

                Return rounds.ToArray
            End If
        End Function

        ''' <summary>
        ''' computes logarithms, by default natural logarithms, log10 computes common (i.e., base 10) logarithms, 
        ''' and log2 computes binary (i.e., base 2) logarithms. 
        ''' The general form log(x, base) computes logarithms with base base.
        ''' </summary>
        ''' <param name="x">a numeric or complex vector.</param>
        ''' <param name="newBase">
        ''' a positive or complex number: the base with respect to which logarithms are computed. 
        ''' Defaults to ``e=exp(1)``.
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("log")>
        Public Function log(x As Array, Optional newBase As Double = stdNum.E) As Double()
            Return REnv.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(Function(d) stdNum.Log(d, newBase)) _
                .ToArray
        End Function

        ''' <summary>
        ''' ### Logarithms and Exponentials
        ''' 
        ''' log2 computes binary (i.e., base 2) logarithms. 
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        <ExportAPI("log2")>
        Public Function log2(x As Array) As Double()
            Return REnv.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(Function(d) stdNum.Log(d, 2)) _
                .ToArray
        End Function

        ''' <summary>
        ''' ### Logarithms and Exponentials
        ''' 
        ''' log10 computes common (i.e., base 10) logarithms
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns></returns>
        <ExportAPI("log10")>
        Public Function log10(x As Array) As Double()
            Return REnv.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(Function(d) stdNum.Log(d, 10)) _
                .ToArray
        End Function

        ''' <summary>
        ''' #### Sum of Vector Elements
        ''' 
        ''' sum returns the sum of all the values present in its arguments.
        ''' </summary>
        ''' <param name="x">numeric or complex or logical vectors.</param>
        ''' <returns></returns>
        <ExportAPI("sum")>
        Public Function sum(<RRawVectorArgument> x As Object, Optional narm As Boolean = False) As Double
            If x Is Nothing Then
                Return 0
            End If

            Dim array = REnv.asVector(Of Object)(x)
            Dim elementType As Type = Runtime.MeasureArrayElementType(array)

            Select Case elementType
                Case GetType(Boolean)
                    Return Runtime.asLogical(array).Select(Function(b) If(b, 1, 0)).Sum
                Case GetType(Integer), GetType(Long), GetType(Short), GetType(Byte)
                    Return Runtime.asVector(Of Long)(x).AsObjectEnumerator(Of Long).Sum
                Case Else
                    Return Runtime.asVector(Of Double)(x).AsObjectEnumerator(Of Double).Sum
            End Select
        End Function

        <ExportAPI("pow")>
        Public Function pow(x As Array, y As Array) As Object
            x = Runtime.asVector(Of Double)(x)
            y = Runtime.asVector(Of Double)(y)

            Return Runtime.Core.Power(Of Double, Double, Double)(x, y).ToArray
        End Function

        <ExportAPI("sqrt")>
        Public Function sqrt(x As Array) As Double()
            Return Runtime.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(AddressOf stdNum.Sqrt) _
                .ToArray
        End Function

        ''' <summary>
        ''' #### Logarithms and Exponentials
        ''' 
        ''' computes the exponential function.
        ''' </summary>
        ''' <param name="x">a numeric or complex vector.</param>
        ''' <returns></returns>
        <ExportAPI("exp")>
        Public Function exp(x As Array) As Double()
            Return Runtime.asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(AddressOf stdNum.Exp) _
                .ToArray
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="na_rm">a logical indicating whether missing values should be removed.</param>
        ''' <returns></returns>
        <ExportAPI("max")>
        Public Function max(x As Array, Optional na_rm As Boolean = False, Optional env As Environment = Nothing) As Double
            Dim dbl = REnv.asVector(Of Double)(x).AsObjectEnumerator(Of Double).ToArray

            If dbl.Length = 0 Then
                Call env.AddMessage({"no non-missing arguments to max; returning -Inf"}, MSG_TYPES.WRN)
                Return Double.NegativeInfinity
            Else
                Return dbl.Max
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="x"></param>
        ''' <param name="na_rm">a logical indicating whether missing values should be removed.</param>
        ''' <returns></returns>
        <ExportAPI("min")>
        Public Function min(x As Array, Optional na_rm As Boolean = False, Optional env As Environment = Nothing) As Double
            Dim dbl = REnv.asVector(Of Double)(x).AsObjectEnumerator(Of Double).ToArray

            If dbl.Length = 0 Then
                Call env.AddMessage({"no non-missing arguments to min; returning Inf"}, MSG_TYPES.WRN)
                Return Double.PositiveInfinity
            Else
                Return dbl.Min
            End If
        End Function

        ''' <summary>
        ''' Arithmetic Mean
        ''' </summary>
        ''' <param name="x">An R object. Currently there are methods for numeric/logical 
        ''' vectors and date, date-time and time interval objects. Complex vectors are 
        ''' allowed for trim = 0, only.</param>
        ''' <returns></returns>
        <ExportAPI("mean")>
        Public Function mean(x As Array, Optional na_rm As Boolean = False) As Double
            If x Is Nothing OrElse x.Length = 0 Then
                Return 0
            Else
                Dim array As Double() = REnv.asVector(Of Double)(x).AsObjectEnumerator(Of Double).ToArray

                If na_rm Then
                    Return array.Where(Function(a) Not a.IsNaNImaginary).Average
                Else
                    Return array.Average
                End If
            End If
        End Function

        ''' <summary>
        ''' abs(x) computes the absolute value of x
        ''' </summary>
        ''' <param name="x">a numeric Or complex vector Or array.</param>
        ''' <returns></returns>
        <ExportAPI("abs")>
        Public Function abs(x As Array) As Double()
            Return asVector(Of Double)(x) _
                .AsObjectEnumerator(Of Double) _
                .Select(AddressOf stdNum.Abs) _
                .ToArray
        End Function

        <ExportAPI("RSD")>
        Public Function rsd(x As Array) As Double
            Return Runtime.asVector(Of Double)(x).AsObjectEnumerator(Of Double).RSD
        End Function

        ''' <summary>
        ''' ### Standard Deviation
        ''' 
        ''' This function computes the standard deviation of the values in x. 
        ''' If na.rm is TRUE then missing values are removed before computation 
        ''' proceeds.
        ''' </summary>
        ''' <param name="x">
        ''' a numeric vector or an R object but not a factor coercible to numeric by as.double(x)
        ''' </param>
        ''' <param name="sample">
        ''' sample or population
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("sd")>
        Public Function sd(x As Array, Optional sample As Boolean = False) As Double
            If x Is Nothing OrElse x.Length = 0 Then
                Return 0
            Else
                Return DirectCast(asVector(Of Double)(x), Double()).SD(isSample:=sample)
            End If
        End Function

        <ExportAPI("pearson")>
        Public Function pearson(x As Array, y As Array, Optional MAXIT As Integer = 5000) As list
            Dim data1 As Double() = REnv.asVector(Of Double)(x)
            Dim data2 As Double() = REnv.asVector(Of Double)(y)
            Dim p1#
            Dim p2#
            Dim z#
            Dim cor#

            Beta.MAXIT = MAXIT

            cor = GetPearson(data1, data2, p1, p2, z, throwMaxIterError:=False)

            Return New list With {
                .slots = New Dictionary(Of String, Object) From {
                    {"cor", cor},
                    {"p-value", p1},
                    {"prob2", p2},
                    {"z", z}
                }
            }
        End Function

        ''' <summary>
        ''' set.seed is the recommended way to specify seeds.
        ''' </summary>
        ''' <param name="seed">a single value, interpreted as an integer, or NULL (see ‘Details’).</param>
        ''' <remarks>
        ''' set.seed returns NULL, invisibly.
        ''' </remarks>
        <ExportAPI("set.seed")>
        Public Sub set_seed(seed As Integer)
            randf.SetSeed(seed)
        End Sub

        ''' <summary>
        ''' runif generates random deviates.
        ''' </summary>
        ''' <param name="n">
        ''' number of observations. If length(n) > 1, the length is taken to be the number required.
        ''' </param>
        ''' <param name="min">lower And upper limits of the distribution. Must be finite.</param>
        ''' <param name="max">lower And upper limits of the distribution. Must be finite.</param>
        ''' <returns></returns>
        <ExportAPI("runif")>
        Public Function runif(n$, Optional min# = 0, Optional max# = 1) As Double()
            Dim rnd As Random = randf.seeds
            Dim [if] As New List(Of Double)

            For i As Integer = 0 To n - 1
                [if].Add(rnd.NextDouble(min, max))
            Next

            Return [if].ToArray
        End Function

        <ExportAPI("rnorm")>
        Public Function rnorm(n%, Optional mean# = 0, Optional sd# = 1) As Double()
            Dim rnd As Random = randf.seeds
            Dim gauss As New List(Of Double)

            For i As Integer = 0 To n - 1
                gauss.Add(rnd.NextGaussian(mean, sd))
            Next

            Return gauss.ToArray
        End Function

        ''' <summary>
        ''' ### Random Samples and Permutations
        ''' 
        ''' ``sample`` takes a sample of the specified size from the elements 
        ''' of x using either with or without replacement.
        ''' </summary>
        ''' <param name="x">
        ''' either a vector Of one Or more elements from which To choose, Or a positive Integer. See 'Details.’
        ''' </param>
        ''' <param name="size">a non-negative integer giving the number of items to choose.</param>
        ''' <param name="replace">should sampling be with replacement?</param>
        ''' <param name="prob">
        ''' a vector Of probability weights For obtaining the elements Of the vector being sampled.
        ''' </param>
        ''' <remarks>
        ''' If x has length 1, is numeric (in the sense of is.numeric) and ``x >= 1``, sampling 
        ''' via sample takes place from ``1:x``. Note that this convenience feature may lead to 
        ''' undesired behaviour when x is of varying length in calls such as sample(x). 
        ''' See the examples.
        '''
        ''' Otherwise x can be any R Object For which length And subsetting by integers make sense: 
        ''' S3 Or S4 methods for these operations will be dispatched as appropriate.
        '''
        ''' For sample the default for size Is the number of items inferred from the first argument, 
        ''' so that sample(x) generates a random permutation of the elements of x (Or 1:x).
        '''
        ''' It Is allowed to ask for size = 0 samples with n = 0 Or a length-zero x, but otherwise 
        ''' ``n > 0`` Or positive length(x) Is required.
        '''
        ''' Non-integer positive numerical values of n Or x will be truncated to the next smallest 
        ''' integer, which has to be no larger than ``.Machine$integer.max``.
        '''
        ''' The optional prob argument can be used to give a vector of weights for obtaining the elements 
        ''' of the vector being sampled. They need Not sum to one, but they should be non-negative And 
        ''' Not all zero. If replace Is true, Walker's alias method (Ripley, 1987) is used when there 
        ''' are more than 200 reasonably probable values: this gives results incompatible with those 
        ''' from ``R &lt; 2.2.0``.
        '''
        ''' If replace Is False, these probabilities are applied sequentially, that Is the probability 
        ''' Of choosing the Next item Is proportional To the weights amongst the remaining items. The 
        ''' number Of nonzero weights must be at least size In this Case.
        ''' </remarks>
        ''' <returns>
        ''' For sample a vector of length size with elements drawn from either ``x`` or from the 
        ''' integers ``1:x``.
        ''' </returns>
        <ExportAPI("sample")>
        Public Function sample(<RRawVectorArgument>
                               x As Object,
                               size As Integer,
                               Optional replace As Boolean = False,
                               Optional prob As Object = Nothing) As Object

            Dim data As Object() = asVector(Of Object)(x)
            Dim index As Integer() = sample_int(size, size, replace, prob)
            Dim takeSamples As New List(Of Object)

            For Each i As Integer In index
                Call takeSamples.Add(data(i))
            Next

            Return takeSamples.ToArray
        End Function

        <ExportAPI("sample.int")>
        <RApiReturn(GetType(Integer))>
        Public Function sample_int(n As Integer, Optional size As Object = "n", Optional replace As Boolean = False, Optional prob As Object = Nothing) As Object
            Dim i As New List(Of Integer)(n.Sequence(offset:=1))
            Dim list As New List(Of Integer)
            Dim seeds As Random = randf.seeds

            If size.ToString <> "n" Then
                n = size
            End If

            If replace Then
                ' 有重复的采样
                For j As Integer = 0 To n - 1
                    list.Add(i(seeds.Next(0, i.Count)))
                Next
            Else
                Dim index As Integer

                For j As Integer = 0 To n - 1
                    index = seeds.Next(0, i.Count)
                    list.Add(i(index))
                    i.RemoveAt(index)
                Next
            End If

            Return list.ToArray
        End Function

        ''' <summary>
        ''' grouping data input by given numeric tolerance
        ''' </summary>
        ''' <param name="sequence"></param>
        ''' <param name="eval"></param>
        ''' <param name="offset"></param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("cluster_1D")>
        Public Function cluster1D(<RRawVectorArgument> sequence As Object, eval As Object, Optional offset As Double = 0, Optional env As Environment = Nothing) As Object
            Dim data As pipeline = pipeline.TryCreatePipeline(Of Object)(sequence, env)
            Dim evalFUNC As Evaluate(Of Object)

            If data.isError Then
                Return data.getError
            End If

            If eval Is Nothing Then
                Return Internal.debug.stop("the evaluation delegate function can not be nothing, i'm unsure about how to evaluate the data object as numeric value...", env)
            ElseIf TypeOf eval Is Func(Of Object, Double) Then
                evalFUNC = New Evaluate(Of Object)(AddressOf DirectCast(eval, Func(Of Object, Double)).Invoke)
            End If

            Throw New NotImplementedException
        End Function

        ''' <summary>
        ''' ### Correlation, Variance and Covariance (Matrices)
        ''' 
        ''' var, cov and cor compute the variance of x and the covariance or 
        ''' correlation of x and y if these are vectors. If x and y are 
        ''' matrices then the covariances (or correlations) between the columns 
        ''' of x and the columns of y are computed.
        ''' </summary>
        ''' <param name="x">a numeric vector, matrix or data frame.</param>
        ''' <param name="y">
        ''' NULL (default) or a vector, matrix or data frame with compatible dimensions to x. 
        ''' The default is equivalent to y = x (but more efficient).
        ''' </param>
        ''' <param name="na_rm">logical. Should missing values be removed?</param>
        ''' <param name="use">
        ''' an optional character string giving a method for computing covariances 
        ''' in the presence of missing values. This must be (an abbreviation of) 
        ''' one of the strings "everything", "all.obs", "complete.obs", "na.or.complete", 
        ''' or "pairwise.complete.obs".</param>
        ''' <returns></returns>
        <ExportAPI("var")>
        Public Function var(<RRawVectorArgument> x As Object,
                            <RRawVectorArgument>
                            Optional y As Object = Nothing,
                            Optional na_rm As Boolean = False,
                            Optional use As varUseMethods = varUseMethods.everything) As Object

            Dim vx As Double() = REnv.asVector(Of Double)(x)
            Dim vy As Double()

            If y Is Nothing Then
                vy = vx
            Else
                vy = REnv.asVector(Of Double)(y)
            End If

            If na_rm Then
                vx = vx.Where(Function(xi) Not xi.IsNaNImaginary).ToArray
                vy = vy.Where(Function(yi) Not yi.IsNaNImaginary).ToArray
            End If

            Throw New NotImplementedException
        End Function
    End Module
End Namespace
