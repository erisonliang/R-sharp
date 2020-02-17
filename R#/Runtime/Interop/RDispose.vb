﻿Namespace Runtime.Interop

    ''' <summary>
    ''' Helper for implements using in R#
    ''' </summary>
    Public Class RDispose : Implements IDisposable

        Friend ReadOnly x As Object
        Friend ReadOnly final As Action(Of Object)

        Public ReadOnly Property getObject As Object
            Get
                Return x
            End Get
        End Property

        Sub New(x As Object, final As Action(Of Object))
            Me.x = x
            Me.final = final
        End Sub

        Public Overrides Function ToString() As String
            Return x.ToString
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    Call final(x)
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class
End Namespace