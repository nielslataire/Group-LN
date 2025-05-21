Imports System.Linq
Imports System.Web.Mvc
Imports CPM.Controllers.Results

Namespace Controllers.Extensions
    ''' <summary> 
    ''' Excel controller extensions class. 
    ''' </summary> 
    Public Module ExcelControllerExtensions
        Sub New()
        End Sub
        ''' <summary> 
        ''' Controller Extension: Returns an Excel
        ''' result constructor for returning values from rows. 
        ''' </summary> 
        ''' <param name="controller">This controller.</param> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="excelWorkSheetName">Excel worksheet name: 
        ''' default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        ''' <returns>Action result.</returns> 
        <System.Runtime.CompilerServices.Extension> _
        Public Function Excel(controller As Controller, fileName As String, excelWorkSheetName As String, rows As IQueryable) As ActionResult
            Return New ExcelResults(fileName, excelWorkSheetName, rows)
        End Function

        ''' <summary> 
        ''' Controller Extension: Excel result constructor
        ''' for returning values from rows and headers. 
        ''' </summary> 
        ''' <param name="controller">This controller.</param> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="excelWorkSheetName">
        ''' Excel worksheet name: default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        ''' <param name="headers">Excel header values.</param> 
        ''' <returns>Action result.</returns> 
        <System.Runtime.CompilerServices.Extension> _
        Public Function Excel(controller As Controller, fileName As String, excelWorkSheetName As String, rows As IQueryable, headers As String()) As ActionResult
            Return New ExcelResults(fileName, excelWorkSheetName, rows, headers)
        End Function

        ''' <summary> 
        ''' Controller Extension: Excel result constructor for returning
        ''' values from rows and headers and row keys. 
        ''' </summary> 
        ''' <param name="controller">This controller.</param> 
        ''' <param name="fileName">Excel file name.</param> 
        ''' <param name="excelWorkSheetName">
        ''' Excel worksheet name: default: sheet1.</param> 
        ''' <param name="rows">Excel row values.</param> 
        ''' <param name="headers">Excel header values.</param> 
        ''' <param name="rowKeys">Key values for the rows collection.</param> 
        ''' <returns>Action result.</returns> 
        <System.Runtime.CompilerServices.Extension> _
        Public Function Excel(controller As Controller, fileName As String, excelWorkSheetName As String, rows As IQueryable, headers As String(), rowKeys As String()) As ActionResult
            Return New ExcelResults(fileName, excelWorkSheetName, rows, headers, rowKeys)
        End Function
    End Module
End Namespace

