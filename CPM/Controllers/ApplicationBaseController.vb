Imports System.Web.Mvc
Imports System.Web
Imports System.Linq
Imports System.Collections.Generic

Namespace Controllers
    Public Class ApplicationBaseController
        Inherits Controller

        Protected Overrides Sub OnActionExecuted(filterContext As ActionExecutedContext)
            If User IsNot Nothing Then
                Dim context = New ApplicationDbContext()
                Dim username = User.Identity.Name
                If Not String.IsNullOrEmpty(username) Then
                    Dim user1 = context.Users.SingleOrDefault(Function(m) m.UserName = username)
                    ViewData.Add("Jobfunction", user1.JobFunction.ToString())
                    ViewData.Add("Fullname", user1.Displayname.ToString())
                    ViewData.Add("UserEmail", user1.Email.ToString())
                    ViewData.Add("UserId", user1.Id.ToString())
                End If
            End If
            MyBase.OnActionExecuted(filterContext)
        End Sub
        Public Sub New()
        End Sub
    End Class
End Namespace