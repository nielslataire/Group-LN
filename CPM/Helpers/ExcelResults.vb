Imports System.IO
Imports System.Linq
Imports System.Web
Imports System.Web.Mvc
Imports CPM.Controllers.ControllerExtensions
Namespace Controllers.Results
    Public Class ExcelResults
        Inherits ActionResult
        ''' <summary> 
        ''' File Name. 
        ''' </summary> 
        Private m_excelFileName As String
        ''' <summary> 
        ''' Sheet Name. 
        ''' </summary> 
        Private m_excelWorkSheetName As String
        ''' <summary> 
        ''' Excel Row data. 
        ''' </summary> 
        Private rowData As IQueryable
        ''' <summary> 
        ''' Excel Header Data. 
        ''' </summary> 
        Private headerData As String() = Nothing
        ''' <summary> 
        ''' Row Data Keys. 
        ''' </summary> 
        Private rowPointers As String() = Nothing
        ''' <summary> 
        ''' Action Result: Excel result constructor for returning values from rows. 
        ''' </summary> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="workSheetName">Excel worksheet name: default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        Public Sub New(fileName As String, workSheetName As String, rows As IQueryable)
            Me.New(fileName, workSheetName, rows, Nothing, Nothing)
        End Sub
        ''' <summary> 
        ''' Action Result: Excel result constructor
        ''' for returning values from rows and headers. 
        ''' </summary> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="workSheetName">Excel worksheet name: default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        ''' <param name="headers">Excel header values.</param> 
        Public Sub New(fileName As String, workSheetName As String, rows As IQueryable, headers As String())
            Me.New(fileName, workSheetName, rows, headers, Nothing)
        End Sub
        ''' <summary> 
        ''' Action Result: Excel result constructor for returning
        ''' values from rows and headers and row keys. 
        ''' </summary> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="workSheetName">Excel worksheet name: default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        ''' <param name="headers">Excel header values.</param> 
        ''' <param name="rowKeys">Key values for the rows collection.</param> 
        Public Sub New(fileName As String, workSheetName As String, rows As IQueryable, headers As String(), rowKeys As String())
            Me.rowData = rows
            Me.m_excelFileName = fileName
            Me.m_excelWorkSheetName = workSheetName
            Me.headerData = headers
            Me.rowPointers = rowKeys
        End Sub
        ''' <summary> 
        ''' Gets a value for file name. 
        ''' </summary> 
        Public ReadOnly Property ExcelFileName() As String
            Get
                Return Me.m_excelFileName
            End Get
        End Property
        ''' <summary> 
        ''' Gets a value for file name. 
        ''' </summary> 
        Public ReadOnly Property ExcelWorkSheetName() As String
            Get
                Return Me.m_excelWorkSheetName
            End Get
        End Property
        ''' <summary> 
        ''' Gets a value for rows. 
        ''' </summary> 
        Public ReadOnly Property ExcelRowData() As IQueryable
            Get
                Return Me.rowData
            End Get
        End Property
        ''' <summary> 
        ''' Execute the Excel Result. 
        ''' </summary> 
        ''' <param name="context">Controller context.</param> 
        Public Overrides Sub ExecuteResult(context As ControllerContext)
            Dim stream As MemoryStream = ExcelDocument.Create(Me.m_excelFileName, Me.m_excelWorkSheetName, Me.rowData, Me.headerData, Me.rowPointers)
            WriteStream(stream, Me.m_excelFileName)
        End Sub
        ''' <summary> 
        ''' Writes the memory stream to the browser. 
        ''' </summary> 
        ''' <param name="memoryStream">Memory stream.</param> 
        ''' <param name="excelFileName">Excel file name.</param> 
        Private Shared Sub WriteStream(memoryStream As MemoryStream, excelFileName As String)
            Dim context As HttpContext = HttpContext.Current
            context.Response.Clear()
            context.Response.AddHeader("content-disposition", [String].Format("attachment;filename={0}", excelFileName))
            memoryStream.WriteTo(context.Response.OutputStream)
            memoryStream.Close()
            context.Response.[End]()
        End Sub

    End Class

End Namespace

