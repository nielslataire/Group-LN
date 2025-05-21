Imports System.Linq.Expressions

Public Module HtmlExtensions
    Private Const idsToReuseKey As String = "__htmlPrefixScopeExtensions_IdsToReuse_"

    <System.Runtime.CompilerServices.Extension>
    Public Function BeginCollectionItem(html As HtmlHelper, collectionName As String) As IDisposable
        Dim idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName)
        Dim itemIndex As String = If(idsToReuse.Count > 0, idsToReuse.Dequeue(), Guid.NewGuid().ToString())

        ' autocomplete="off" is needed to work around a very annoying Chrome behaviour whereby it reuses old values after the user clicks "Back", which causes the xyz.index and xyz[...] values to get out of sync.
        html.ViewContext.Writer.WriteLine(String.Format("<input type=""hidden"" name=""{0}.index"" autocomplete=""off"" value=""{1}"" />", collectionName, html.Encode(itemIndex)))

        Return BeginHtmlFieldPrefixScope(html, String.Format("{0}[{1}]", collectionName, itemIndex))
    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function BeginCollectionItem(ByVal html As HtmlHelper, ByVal collectionName As String, ByRef identifier As String) As IDisposable
        Dim idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName, String.Empty)
        Dim itemIndex As String = If(idsToReuse.Count > 0, idsToReuse.Dequeue(), Guid.NewGuid().ToString())
        html.ViewContext.Writer.WriteLine("<input type=""hidden"" name=""{0}.index"" autocomplete=""off"" value=""{1}"" />", collectionName, html.Encode(itemIndex))
        identifier = String.Format("{0}[{1}]", collectionName, itemIndex)
        Return BeginHtmlFieldPrefixScope(html, identifier)
    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function BeginChildCollectionItem(ByVal html As HtmlHelper, ByVal collectionName As String, ByVal parentIdentifier As String, ByRef identifier As String) As IDisposable
        Dim idsToReuse = GetIdsToReuse(html.ViewContext.HttpContext, collectionName, parentIdentifier)
        Dim itemIndex As String = If(idsToReuse.Count > 0, idsToReuse.Dequeue(), Guid.NewGuid().ToString())
        html.ViewContext.Writer.WriteLine("<input type=""hidden"" name=""{0}.{1}.index"" autocomplete=""off"" value=""{2}"" />", parentIdentifier, collectionName, html.Encode(itemIndex))
        identifier = String.Format("{0}.{1}[{2}]", parentIdentifier, collectionName, itemIndex)
        Return BeginHtmlFieldPrefixScope(html, identifier)
    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function GetHtmlId(ByVal html As HtmlHelper, ByVal identifier As String, ByVal propertyName As String) As String
        Dim idValue = identifier
        idValue = idValue.Replace("[", "_")
        idValue = idValue.Replace("]", "__" & propertyName)
        Return idValue
    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function GetHtmlName(ByVal html As HtmlHelper, ByVal identifier As String, ByVal propertyName As String) As String
        Return identifier & "." & propertyName
    End Function

    <System.Runtime.CompilerServices.Extension>
    Public Function BeginHtmlFieldPrefixScope(html As HtmlHelper, htmlFieldPrefix As String) As IDisposable
        Return New HtmlFieldPrefixScope(html.ViewData.TemplateInfo, htmlFieldPrefix)
    End Function

    Private Function GetIdsToReuse(httpContext As HttpContextBase, collectionName As String) As Queue(Of String)
        ' We need to use the same sequence of IDs following a server-side validation failure,  
        ' otherwise the framework won't render the validation error messages next to each item.
        Dim key As String = idsToReuseKey & collectionName
        Dim queue = DirectCast(httpContext.Items(key), Queue(Of String))
        If queue Is Nothing Then
            httpContext.Items(key) = InlineAssignHelper(queue, New Queue(Of String)())
            Dim previouslyUsedIds = httpContext.Request(collectionName & Convert.ToString(".index"))
            If Not String.IsNullOrEmpty(previouslyUsedIds) Then
                For Each previouslyUsedId As String In previouslyUsedIds.Split(","c)
                    queue.Enqueue(previouslyUsedId)
                Next
            End If
        End If
        Return queue
    End Function

    Private Function GetIdsToReuse(ByVal httpContext As HttpContextBase, ByVal collectionName As String, ByVal parentIdentifier As String) As Queue(Of String)
        Dim key As String

        If Not String.IsNullOrEmpty(parentIdentifier) Then
            key = idsToReuseKey & parentIdentifier & "." & collectionName
        Else
            key = idsToReuseKey & collectionName
        End If

        Dim queue = CType(httpContext.Items(key), Queue(Of String))

        If queue Is Nothing Then
            httpContext.Items(key) = CSharpImpl.__Assign(queue, New Queue(Of String)())
            Dim previouslyUsedIds As String

            If Not String.IsNullOrEmpty(parentIdentifier) Then
                previouslyUsedIds = httpContext.Request(parentIdentifier & "." & collectionName & ".index")
            Else
                previouslyUsedIds = httpContext.Request(collectionName & ".index")
            End If

            If Not String.IsNullOrEmpty(previouslyUsedIds) Then

                For Each previouslyUsedId As String In previouslyUsedIds.Split(","c)
                    queue.Enqueue(previouslyUsedId)
                Next
            End If
        End If

        Return queue
    End Function

    Private Class CSharpImpl
        <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
        Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class

    Private Class HtmlFieldPrefixScope
        Implements IDisposable
        Private ReadOnly templateInfo As TemplateInfo
        Private ReadOnly previousHtmlFieldPrefix As String

        Public Sub New(templateInfo As TemplateInfo, htmlFieldPrefix As String)
            Me.templateInfo = templateInfo

            previousHtmlFieldPrefix = templateInfo.HtmlFieldPrefix
            templateInfo.HtmlFieldPrefix = htmlFieldPrefix
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            templateInfo.HtmlFieldPrefix = previousHtmlFieldPrefix
        End Sub
    End Class
    Private Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
    'Disable controls when .....
    <System.Runtime.CompilerServices.Extension>
    Public Function DisableIf(htmlString As MvcHtmlString, expression As Func(Of Boolean)) As MvcHtmlString
        If expression.Invoke() Then
            Dim html = htmlString.ToString()
            Const disabled As String = "''"
            Dim index = html.IndexOf("/>")
            If index = -1 Then
                index = html.IndexOf(">")
            End If
            html = html.Insert(index, Convert.ToString(" disabled= ") & disabled)
            Return New MvcHtmlString(html)
        End If
        Return htmlString
    End Function
    'extending html.actionlink with html tag inside
    <System.Runtime.CompilerServices.Extension>
    Public Function HtmlActionLink(htmlHelper As HtmlHelper, linkText As String, actionName As String, controllerName As String, routeValues As Object, htmlAttributes As Object) As MvcHtmlString
        Dim repID = Guid.NewGuid().ToString
        Dim lnk = htmlHelper.ActionLink(repID, actionName, controllerName, routeValues, htmlAttributes)
        Return MvcHtmlString.Create(lnk.ToString().Replace(repID, linkText))

    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function GenerateSlug(phrase As String) As String
        Dim str As String = RemoveAccent(phrase).ToLower()
        str = Regex.Replace(str, "[^a-z0-9\s-]", "")
        str = Regex.Replace(str, "\s+", " ").Trim()
        str.Substring(0, If(str.Length <= 45, str.Length, 45)).Trim()
        str = Regex.Replace(str, "\s", "-")
        Return str
    End Function
    Public Function RemoveAccent(txt As String) As String
        Dim bytes As Byte() = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt)
        Return System.Text.Encoding.ASCII.GetString(bytes)

    End Function
    <System.Runtime.CompilerServices.Extension>
    Public Function BasicCheckBoxFor(Of T)(html As HtmlHelper(Of T), expression As Expression(Of Func(Of T, Boolean)), Optional htmlAttributes As Object = Nothing) As MvcHtmlString
        Dim tag = New TagBuilder("input")

        tag.Attributes("type") = "checkbox"
        tag.Attributes("id") = html.IdFor(expression).ToString()
        tag.Attributes("name") = html.NameFor(expression).ToString()
        tag.Attributes("value") = "true"

        ' set the "checked" attribute if true
        Dim metadata As ModelMetadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData)
        If metadata.Model IsNot Nothing Then
            Dim modelChecked As Boolean
            If [Boolean].TryParse(metadata.Model.ToString(), modelChecked) Then
                If modelChecked Then
                    tag.Attributes("checked") = "checked"
                End If
            End If
        End If

        ' merge custom attributes
        tag.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes))

        Dim tagString = tag.ToString(TagRenderMode.SelfClosing)
        Return MvcHtmlString.Create(tagString)
    End Function


End Module
