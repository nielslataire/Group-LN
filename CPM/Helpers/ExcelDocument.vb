Imports System.IO
Imports System.Linq
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports DocumentFormat.OpenXml
Imports DocumentFormat.OpenXml.Extensions
Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Spreadsheet
Imports ClosedXML.Excel
Namespace Controllers.ControllerExtensions
    Public NotInheritable Class ExcelDocument
        'Excel Document

        Private Sub New()

        End Sub
        'Default werkblad naam
        Private Const DefaultSheetName As String = "Sheet1"
        'Excel document aanmaken om te streamen
        Public Shared Function Create(documentName As String, excelWorkSheetName As String, rowData As IQueryable, headerData As String(), rowPointers As String()) As MemoryStream
            'Return CreateSpreadSheet(documentName, excelWorkSheetName, rowData, headerData, rowPointers, Nothing)
            Dim fs As Stream = New MemoryStream()
            CreateWorkbook(documentName, excelWorkSheetName, rowData, headerData, rowPointers, Nothing).SaveAs(fs)
            fs.Position = 0
            Return fs
        End Function
        Private Shared Function CreateWorkbook(documentName As String, excelWorkSheetName As String, rowData As IQueryable, headerData As String(), rowPointers As String(), styleSheet As Stylesheet) As XLWorkbook
            Dim wb As New XLWorkbook
            excelWorkSheetName = If(excelWorkSheetName, DefaultSheetName)
            Dim ws = wb.Worksheets.Add(excelWorkSheetName)
            ws.Style.Font.FontName = "Avenir Light"
            ws.Style.Font.FontSize = 10

            Dim rowNum As Integer = 0
            Dim colNum As Integer = 0
            Dim maxWidth As Integer = 0
            Dim minCol As Integer = 1
            Dim maxCol As Integer = If(rowPointers Is Nothing, minCol, rowPointers.Length)
            maxCol = If(maxCol = 1 AndAlso headerData Is Nothing, 1, headerData.Length)
            WriteWorkbookHeaders(headerData, rowNum, colNum, maxWidth, ws)
            If rowPointers Is Nothing OrElse rowPointers.Length = 0 Then
                WriteWorkbookRowsFromHeaders(rowData, headerData, rowNum, ws)
            Else
                WriteWorkbookRowsFromKeys(rowData, rowPointers, rowNum, ws)
            End If

            ws.Columns().AdjustToContents()

            Return wb
        End Function
        Private Shared Sub WriteWorkbookHeaders(headerData As String(), ByRef rowNum As Integer, ByRef colNum As Integer, ByRef maxWidth As Integer, ws As IXLWorksheet)
            rowNum = 1
            colNum = 0
            ws.Row(rowNum).Height = 22
            ws.Rows(rowNum).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            Dim celllocation As String = ""
            For Each header As String In headerData
                Dim strValue As String = ReplaceSpecialCharacters(header)
                celllocation = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
                If colNum = 0 Then
                    ws.Cell(cellLocation).Style.Border.LeftBorder = XLBorderStyleValues.Thin
                    ws.Cell(cellLocation).Style.Border.LeftBorderColor = XLColor.Black
                End If
                WriteWorkbookValues(cellLocation, strValue, ws)
                SetHeaderStyle(cellLocation, ws)
                colNum += 1
            Next
            ws.Cell(celllocation).Style.Border.RightBorder = XLBorderStyleValues.Thin
            ws.Cell(celllocation).Style.Border.RightBorderColor = XLColor.Black
            rowNum += 1
        End Sub
        Private Shared Sub WriteWorkbookValues(cellLocation As String, strValue As String, ws As IXLWorksheet)

            Dim intValue As Integer = 0
            If strValue.Contains("€") Then
                strValue = strValue.Replace("€", "")
                ws.Cell(cellLocation).Value = strValue
                ws.Cell(cellLocation).Style.NumberFormat.Format = "#.##0,00 €"
                ws.Cell(cellLocation).DataType = XLCellValues.Text
            ElseIf Integer.TryParse(strValue, intValue) Then
                ws.Cell(cellLocation).Value = strValue 
            ElseIf String.IsNullOrEmpty(strValue) Then
                ws.Cell(cellLocation).Value = strValue
            Else
                ws.Cell(cellLocation).Value = strValue
            End If
        End Sub
        Private Shared Sub SetHeaderStyle(cellLocation As String, ws As IXLWorksheet)
            ws.Cell(cellLocation).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 125, 49)
            ws.Cell(cellLocation).Style.Font.FontColor = XLColor.White
            ws.Cell(cellLocation).Style.Font.FontName = "Avenir Heavy"
            ws.Cell(cellLocation).Style.Font.FontSize = 10
            ws.Cell(cellLocation).Style.Border.BottomBorder = XLBorderStyleValues.Thin
            ws.Cell(cellLocation).Style.Border.BottomBorderColor = XLColor.Black
            ws.Cell(cellLocation).Style.Border.TopBorder = XLBorderStyleValues.Thin
            ws.Cell(cellLocation).Style.Border.TopBorderColor = XLColor.Black
        End Sub
        Private Shared Sub WriteWorkbookRowsFromHeaders(rowData As IQueryable, headerData As String(), rowNum As Integer, ws As IXLWorksheet)

            For Each row As Object In rowData
                Dim colNum As Integer = 0
                For Each header As String In headerData
                    Dim strValue As String = row.[GetType]().GetProperty(header).GetValue(row, Nothing).ToString()
                    strValue = ReplaceSpecialCharacters(strValue)
                    Dim cellLocation As String = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
                    WriteWorkbookValues(cellLocation, strValue, ws)
                    colNum += 1
                Next
                rowNum += 1
            Next
        End Sub
        Private Shared Sub WriteWorkbookRowsFromKeys(rowData As IQueryable, rowDataKeys As String(), rowNum As Integer, ws As IXLWorksheet)

            For Each row As Object In rowData
                Dim colNum As Integer = 0
                For Each rowKey As String In rowDataKeys
                    Dim strValue As String = row.[GetType]().GetProperty(rowKey).GetValue(row, Nothing).ToString()
                    strValue = ReplaceSpecialCharacters(strValue)
                    Dim cellLocation As String = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
                    WriteWorkbookValues(cellLocation, strValue, ws)
                    colNum += 1
                Next
                rowNum += 1
            Next
        End Sub







        ''Create excel spreadsheet
        'Private Shared Function CreateSpreadSheet(documentName As String, excelWorkSheetName As String, rowData As IQueryable, headerData As String(), rowPointers As String(), styleSheet As Stylesheet) As MemoryStream
        '    Dim rowNum As Integer = 0
        '    Dim colNum As Integer = 0
        '    Dim maxWidth As Integer = 0
        '    Dim minCol As Integer = 1
        '    Dim maxCol As Integer = If(rowPointers Is Nothing, minCol, rowPointers.Length)
        '    maxCol = If(maxCol = 1 AndAlso headerData Is Nothing, 1, headerData.Length)
        '    Dim xmlStream As MemoryStream = SpreadsheetReader.Create()
        '    Dim spreadSheet As SpreadsheetDocument = SpreadsheetDocument.Open(xmlStream, True)
        '    SetSheetName(excelWorkSheetName, spreadSheet)
        '    If styleSheet Is Nothing Then
        '        SetStyleSheet(spreadSheet)
        '    Else
        '        spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet = styleSheet
        '        spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet.Save()
        '    End If
        '    Dim worksheetPart As WorksheetPart = SpreadsheetReader.GetWorksheetPartByName(spreadSheet, excelWorkSheetName)
        '    WriteHeaders(headerData, rowNum, colNum, maxWidth, spreadSheet, worksheetPart)
        '    AddCellWidthStyles(Convert.ToUInt32(minCol), Convert.ToUInt32(maxCol), maxWidth, spreadSheet, worksheetPart)
        '    If rowPointers Is Nothing OrElse rowPointers.Length = 0 Then
        '        WriteRowsFromHeaders(rowData, headerData, rowNum, maxWidth, spreadSheet, worksheetPart)
        '    Else
        '        WriteRowsFromKeys(rowData, rowPointers, rowNum, maxWidth, spreadSheet, worksheetPart)
        '    End If

        '    'Save to memory stream
        '    SpreadsheetWriter.Save(spreadSheet)
        '    spreadSheet.Close()
        '    spreadSheet.Dispose()
        '    Return xmlStream
        'End Function

        ' ''' <summary> 
        ' ''' Set the name of the spreadsheet. 
        ' ''' </summary> 
        ' ''' <param name="excelSpreadSheetName">Spread sheet name.</param> 
        ' ''' <param name="spreadSheet">Spread sheet.</param> 
        'Private Shared Sub SetSheetName(excelSpreadSheetName As String, spreadSheet As SpreadsheetDocument)
        '    excelSpreadSheetName = If(excelSpreadSheetName, DefaultSheetName)
        '    Dim ss As Sheet = spreadSheet.WorkbookPart.Workbook.Descendants(Of Sheet)().Where(Function(s) s.Name = DefaultSheetName).SingleOrDefault()
        '    ss.Name = excelSpreadSheetName
        'End Sub

        'Add cell width styles
        'Private Shared Sub AddCellWidthStyles(minCol As UInteger, maxCol As UInteger, maxWidth As Integer, spreadSheet As SpreadsheetDocument, workSheetPart As WorksheetPart)
        '    Dim cols As New Columns(New Column() With { _
        '                            .CustomWidth = True, _
        '                            .Min = minCol, _
        '                            .Max = maxCol, _
        '                            .Width = maxWidth, _
        '                            .BestFit = True _
        '                        })
        '    workSheetPart.Worksheet.InsertBefore(Of Columns)(cols, workSheetPart.Worksheet.GetFirstChild(Of SheetData)())


        'End Sub
        ''Set the style Sheet
        'Private Shared Sub SetStyleSheet(spreadSheet As SpreadsheetDocument)
        '    Dim styleSheet As Stylesheet = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet
        '    styleSheet.Fonts.AppendChild(New Font(New FontSize() With { _
        '                                          .Val = 11 _
        '                                      }, New Color() With { _
        '                                      .Rgb = "FFFFFF" _
        '                                  }, New FontName() With { _
        '                                  .Val = "Verdana" _
        '                              }))
        '    styleSheet.Fills.AppendChild(New Fill() With { _
        '                                 .PatternFill = New PatternFill() With { _
        '                                     .PatternType = PatternValues.Solid, _
        '                                     .BackgroundColor = New BackgroundColor() With { _
        '                                         .Rgb = New HexBinaryValue() With {.Value = "007D31"} _
        '                                     } _
        '                                 } _
        '                             })
        '    spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet.Save()
        'End Sub

        ''Save the style for worksheet header
        'Private Shared Sub SeatHeaderStyle(cellLocation As String, spreadSheet As SpreadsheetDocument, workSheetPart As WorksheetPart)
        '    Dim styleSheet As Stylesheet = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet
        '    Dim cell As Cell = workSheetPart.Worksheet.Descendants(Of Cell)().Where(Function(c) c.CellReference = cellLocation).FirstOrDefault()
        '    If cell Is Nothing Then
        '        Throw New ArgumentNullException("Cell not found")
        '    End If
        '    cell.SetAttribute(New OpenXmlAttribute("", "s", "", "1"))
        '    Dim cellStyleAttribute As OpenXmlAttribute = cell.GetAttribute("s", "")
        '    Dim cellFormats As CellFormats = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet.CellFormats
        '    'pick first cell format
        '    Dim cellFormat As CellFormat = DirectCast(cellFormats.ElementAt(0), CellFormat)
        '    Dim cf As New CellFormat(cellFormat.OuterXml)
        '    cf.FontId = styleSheet.Fonts.Count
        '    cf.FillId = styleSheet.Fills.Count
        '    cellFormats.AppendChild(cf)
        '    Dim a As Integer = CInt(styleSheet.CellFormats.Count.Value)
        '    cell.SetAttribute(cellStyleAttribute)
        '    cell.StyleIndex = styleSheet.CellFormats.Count
        '    workSheetPart.Worksheet.Save()

        'End Sub

        'Replace special characters
        Private Shared Function ReplaceSpecialCharacters(value As String) As String
            value = value.Replace("’", "'")
            'value = value.Replace("“", """)
            'value = value.Replace("”", """")
            value = value.Replace("–", "-")
            value = value.Replace("…", "...")
            Return value

        End Function

        ''Write values to spreadsheet
        'Private Shared Sub WriteValues(cellLocation As String, strValue As String, spreadSheet As SpreadsheetDocument, workSheet As WorksheetPart)
        '    Dim workSheetWriter As New WorksheetWriter(spreadSheet, workSheet)
        '    Dim intValue As Integer = 0
        '    If strValue.Contains("$") Then
        '        strValue = strValue.Replace("$", "")
        '        strValue = strValue.Replace(",", "")
        '        workSheetWriter.PasteValue(cellLocation, strValue, CellValues.Number)
        '    ElseIf Integer.TryParse(strValue, intValue) Then
        '        workSheetWriter.PasteValue(cellLocation, strValue, CellValues.Number)
        '    ElseIf String.IsNullOrEmpty(strValue) Then
        '        workSheetWriter.PasteText(cellLocation, strValue)
        '    Else
        '        workSheetWriter.PasteText(cellLocation, strValue)
        '    End If
        'End Sub

        '' Write the excel rows for the spreadsheet. 
        'Private Shared Sub WriteRowsFromKeys(rowData As IQueryable, rowDataKeys As String(), rowNum As Integer, ByRef maxWidth As Integer, spreadSheet As SpreadsheetDocument, workSheet As WorksheetPart)
        '    maxWidth = 0
        '    For Each row As Object In rowData
        '        Dim colNum As Integer = 0
        '        For Each rowKey As String In rowDataKeys
        '            Dim strValue As String = row.[GetType]().GetProperty(rowKey).GetValue(row, Nothing).ToString()
        '            strValue = ReplaceSpecialCharacters(strValue)
        '            maxWidth = If(strValue.Length > maxWidth, strValue.Length, maxWidth)
        '            Dim cellLocation As String = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
        '            WriteValues(cellLocation, strValue, spreadSheet, workSheet)
        '            colNum += 1
        '        Next
        '        rowNum += 1
        '    Next
        'End Sub

        'Convert column number to alpha numeric value. 
        Private Shared Function GetColumnLetter(colNumber As String) As String
            If String.IsNullOrEmpty(colNumber) Then
                Throw New ArgumentNullException(colNumber)
            End If
            Dim colName As String = Nothing
            Try
                For i As Integer = 0 To colNumber.Length - 1
                    Dim colValue As String = colNumber.Substring(i, 1)
                    Dim asc As Integer = Convert.ToInt16(colValue) + 65
                    colName += Convert.ToChar(asc)
                Next
            Finally
                colName = If(colName, "A")
            End Try
            Return colName
        End Function

        ' ''' <summary> 
        ' ''' Write the values for the rows from headers. 
        ' ''' </summary> 
        ' ''' <param name="rowData">Excel row values.</param> 
        ' ''' <param name="headerData">Excel header values.</param> 
        ' ''' <param name="rowNum">Row number.</param> 
        ' ''' <param name="maxWidth">Max width.</param> 
        ' ''' <param name="spreadSheet">Spreadsheet to write to. </param> 
        ' ''' <param name="workSheet">Worksheet to write to. </param> 
        'Private Shared Sub WriteRowsFromHeaders(rowData As IQueryable, headerData As String(), rowNum As Integer, ByRef maxWidth As Integer, spreadSheet As SpreadsheetDocument, workSheet As WorksheetPart)
        '    Dim workSheetWriter As New WorksheetWriter(spreadSheet, workSheet)
        '    maxWidth = 0
        '    For Each row As Object In rowData
        '        Dim colNum As Integer = 0
        '        For Each header As String In headerData
        '            Dim strValue As String = row.[GetType]().GetProperty(header).GetValue(row, Nothing).ToString()
        '            strValue = ReplaceSpecialCharacters(strValue)
        '            maxWidth = If(strValue.Length > maxWidth, strValue.Length, maxWidth)
        '            Dim cellLocation As String = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
        '            WriteValues(cellLocation, strValue, spreadSheet, workSheet)
        '            colNum += 1
        '        Next
        '        rowNum += 1
        '    Next
        'End Sub

        ''' <summary> 
        ''' Write the excel headers for the spreadsheet. 
        ''' </summary> 
        ''' <param name="headerData">Excel header values.</param> 
        ''' <param name="rowNum">Row number.</param> 
        ''' <param name="colNum">Column Number.</param> 
        ''' <param name="maxWidth">Max column width</param> 
        ''' <param name="spreadSheet">Maximum Column Width to write to. </param> 
        ''' <param name="workSheet">Worksheet to write to. </param> 
        'Private Shared Sub WriteHeaders(headerData As String(), ByRef rowNum As Integer, ByRef colNum As Integer, ByRef maxWidth As Integer, spreadSheet As SpreadsheetDocument, workSheet As WorksheetPart)
        '    rowNum = 1
        '    colNum = 0
        '    maxWidth = 0
        '    For Each header As String In headerData
        '        Dim strValue As String = ReplaceSpecialCharacters(header)
        '        Dim cellLocation As String = String.Format("{0}{1}", GetColumnLetter(colNum.ToString()), rowNum)
        '        maxWidth = If(strValue.Length > maxWidth, strValue.Length, maxWidth)
        '        WriteValues(cellLocation, strValue, spreadSheet, workSheet)
        '        SeatHeaderStyle(cellLocation, spreadSheet, workSheet)
        '        colNum += 1
        '    Next
        '    rowNum += 1
        'End Sub
    End Class
End Namespace
