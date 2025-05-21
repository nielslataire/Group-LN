Imports System.Web.Mvc
Imports System.IO
Imports BO
Imports Facade
Imports DAL
Imports DocumentFormat.OpenXml.Vml.Office
Imports DocumentFormat.OpenXml.Packaging
Imports System.Net.WebRequestMethods




Namespace Controllers
    <Authorize>
    Public Class FacturatieController
        Inherits Controllers.ApplicationBaseController

        ' GET: Facturatie
        Function Index(company As Integer, Optional folder As String = "") As ActionResult
            ViewData("company") = company
            ViewData("currentfolder") = folder
            Dim model As New FacturatieModel
            Dim url As String = ""
            If company = 1 Then
                ViewData("Title") = "Facturatie - BCO"
                url = My.Settings.InvoiceURL
            ElseIf company = 2 Then
                ViewData("Title") = "Facturatie - Group LN"
                url = My.Settings.InvoiceURLGroupLN
            End If
            '----NEW FTP CODE----
            Dim ftp As Chilkat.Ftp2 = New Chilkat.Ftp2
            ftp.Hostname = My.Settings.FTPHost
            ftp.Username = My.Settings.FTPUser
            ftp.Password = My.Settings.FTPPassword
            ftp.Port = My.Settings.FTPPort
            Dim success As Boolean = ftp.Connect()
            Dim dirExists As Boolean
            dirExists = ftp.ChangeRemoteDir(url)
            If (dirExists = True) Then
                ftp.ChangeRemoteDir(url)
                Dim i As Integer
                Dim n As Integer
                n = ftp.GetDirCount()
                If (n > 0) Then
                    For i = 0 To n - 1
                        ' Is this a sub-directory?
                        If (ftp.GetIsDirectory(i) = True) Then
                            Dim l As Integer
                            If Integer.TryParse(ftp.GetFilename(i), l) AndAlso ftp.GetFilename(i).Length = 4 Then
                                model.Folders.Add(ftp.GetFilename(i))
                            End If

                        End If
                    Next
                End If

            End If
            '----OLD CODE----

            'If Directory.Exists(url) Then
            '    For Each Dir As String In Directory.GetDirectories(url)
            '        Dim dirInfo As New DirectoryInfo(Dir)
            '        Dim n As Integer
            '        If Integer.TryParse(dirInfo.Name, n) AndAlso dirInfo.Name.Length = 4 Then
            '            model.Folders.Add(dirInfo.Name)
            '        End If
            '    Next
            'End If
            model.Folders = model.Folders.OrderByDescending(Function(m) m).ToList
            Dim filefolder As String = ""
            If Not model.Folders(0) Is Nothing And folder = "" Then
                filefolder = model.Folders(0)
                ViewData("currentfolder") = filefolder
            ElseIf Not folder = "" Then
                filefolder = folder
            End If
            If Not filefolder = "" Then
                '----NEW FTP CODE----
                ftp.ChangeRemoteDir(".")
                ftp.ChangeRemoteDir(url & filefolder)
                Dim i As Integer
                Dim n As Integer
                n = ftp.GetDirCount()
                If (n > 0) Then
                    For i = 0 To n - 1
                        Dim filename As String = ftp.GetFilename(i)
                        If Not filename.Contains("~") Then
                            Dim f As New BO.InvoiceFileBO
                            Dim service = ServiceFactory.GetInvoicingService
                            Dim response = service.GetInvoiceFileByFilename(filename)
                            If (response.Success) Then
                                f = response.Value
                                f.InvoiceName = filename
                                f.InvoiceName = filename.Substring(filename.IndexOf(" "), filename.LastIndexOf(".") - filename.IndexOf(" "))
                                f.InvoiceNumber = filename.Substring(0, filename.IndexOf("."))
                                f.InvoiceNumberLong = filename.Substring(0, filename.IndexOf(" "))
                                f.InvoiceDateChanged = ftp.GetLastModDt(i).GetAsDateTime(True)
                                f.FullPath = url & filefolder & "/"
                            Else
                                f.Filename = filename
                                f.InvoiceName = filename
                                f.InvoiceName = filename.Substring(filename.IndexOf(" "), filename.LastIndexOf(".") - filename.IndexOf(" "))
                                f.InvoiceNumber = filename.Substring(0, filename.Replace("-", ".").IndexOf("."))
                                f.InvoiceNumberLong = filename.Substring(0, filename.IndexOf(" "))
                                f.InvoiceDate = ftp.GetCreateDt(i).GetAsDateTime(True)
                                f.InvoiceDateChanged = ftp.GetLastModDt(i).GetAsDateTime(True)
                                f.FullPath = url & filefolder & "/"

                            End If
                            model.Files.Add(f)
                        End If
                        'Dim fileExtPos As Integer = filename.LastIndexOf(".")
                        'If fileExtPos >= 0 Then
                        '    filename = filename.Substring(0, fileExtPos)
                        'End If


                    Next
                End If
            End If

            '----OLD CODE----
            'For Each file As String In Directory.GetFiles(url & filefolder).Where(Function(f) (New FileInfo(f).Attributes And FileAttributes.Hidden) = 0)

            '    Dim f As New BO.InvoiceFileBO
            '    Dim info As New FileInfo(file)
            '    Dim service = ServiceFactory.GetInvoicingService
            '    Dim response = service.GetInvoiceFileByFilename(info.Name)
            '    If (response.Success) Then
            '        f = response.Value
            '        f.InvoiceName = info.Name
            '        f.InvoiceName = info.Name.Substring(info.Name.IndexOf(" "), info.Name.LastIndexOf(".") - info.Name.IndexOf(" "))
            '        f.InvoiceNumber = info.Name.Substring(0, info.Name.IndexOf("."))
            '        f.InvoiceNumberLong = info.Name.Substring(0, info.Name.IndexOf(" "))
            '        f.InvoiceDateChanged = info.LastWriteTime.Date
            '        f.FullPath = file
            '    Else
            '        f.Filename = info.Name
            '        f.InvoiceName = info.Name
            '        f.InvoiceName = info.Name.Substring(info.Name.IndexOf(" "), info.Name.LastIndexOf(".") - info.Name.IndexOf(" "))
            '        f.InvoiceNumber = info.Name.Substring(0, info.Name.Replace("-", ".").IndexOf("."))
            '        f.InvoiceNumberLong = info.Name.Substring(0, info.Name.IndexOf(" "))
            '        f.InvoiceDate = info.CreationTime.Date
            '        f.InvoiceDateChanged = info.LastWriteTime.Date
            '        f.FullPath = file
            '        Try
            '            Dim doc As New Spire.Doc.Document
            '            doc.LoadFromFile(file)
            '            Dim text As Spire.Doc.Documents.TextSelection = doc.FindString("datum", False, True)
            '            Try
            '                Dim tr As Spire.Doc.Fields.TextRange = text.GetAsOneRange
            '                'Dim tr2 As String = tr.OwnerParagraph.Text.Substring(tr.OwnerParagraph.Text.LastIndexOf("&"), (tr.OwnerParagraph.Text.Length - tr.OwnerParagraph.Text.LastIndexOf("&")))
            '                If tr.OwnerParagraph.ChildObjects.Count > 3 Then
            '                    Dim st As String = ""
            '                    For i As Integer = 2 To tr.OwnerParagraph.ChildObjects.Count - 1
            '                        Dim tr2 As Spire.Doc.Fields.TextRange = tr.OwnerParagraph.ChildObjects(i)
            '                        st = st & tr2.Text
            '                    Next
            '                    f.InvoiceDate = Date.Parse(st)
            '                Else
            '                    f.InvoiceDate = Date.Parse(tr.OwnerParagraph.Text(2))
            '                End If
            '                'f.InvoiceDate = Date.Parse(tr2)
            '            Catch ex As Exception

            '            End Try
            '            doc.Dispose()
            '        Catch ex As Exception

            '        End Try
            '    End If
            '    'model.Files.Add(f)
            '    Next

            'End If
            model.Files = model.Files.OrderByDescending(Function(m) m.InvoiceNumber).ToList
            model.Files.First().Deletable = True
            ftp.Disconnect()
            Return View(model)
        End Function
        <HttpGet>
        Public Sub GetInvoicePdf(fullpath As String, filename As String)

            Dim ftp As Chilkat.Ftp2 = New Chilkat.Ftp2
            ftp.Hostname = My.Settings.FTPHost
            ftp.Username = My.Settings.FTPUser
            ftp.Password = My.Settings.FTPPassword
            Dim success As Boolean = ftp.Connect()
            ftp.ChangeRemoteDir(fullpath)
            success = ftp.GetFile(filename, My.Settings.localTempPath & filename)
            ftp.Disconnect()
            If IO.File.Exists(My.Settings.localTempPath & filename) Then
                Dim extension As String = System.IO.Path.GetExtension(My.Settings.localTempPath & filename)
                If (extension = ".doc") Or (extension = ".docx") Then
                    Dim doc As New Spire.Doc.Document
                    doc.LoadFromFile(My.Settings.localTempPath & filename)
                    doc.SaveToFile(My.Settings.DocLocalURL & Path.GetFileNameWithoutExtension(filename) & ".pdf", Spire.Doc.FileFormat.PDF)
                    Dim newfilepath As String = My.Settings.DocWebURL & Path.GetFileNameWithoutExtension(filename) & ".pdf"

                    Response.ContentType = "APPLICATION/OCTET-STREAM"
                    Dim Header As [String] = "Attachment; Filename=" & Path.GetFileName(newfilepath)
                    Response.AppendHeader("Content-Disposition", Header)
                    Dim Dfile As New System.IO.FileInfo(My.Settings.DocLocalURL & Path.GetFileNameWithoutExtension(filename) & ".pdf")
                    Response.WriteFile(Dfile.FullName)
                    Response.[End]()
                    IO.File.Delete(My.Settings.DocLocalURL & Path.GetFileNameWithoutExtension(filename) & ".pdf")

                End If
                IO.File.Delete(My.Settings.localTempPath & filename)
            End If
        End Sub
    End Class
End Namespace