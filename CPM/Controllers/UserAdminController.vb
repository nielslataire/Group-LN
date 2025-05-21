Imports System.Web.Mvc
Imports Microsoft.AspNet.Identity
Imports Microsoft.AspNet.Identity.Owin
Imports Microsoft.AspNet.Identity.EntityFramework
Imports System.Collections.Generic
Imports System.Data.Entity
Imports System.Linq
Imports System.Net
Imports System.Threading.Tasks
Imports System.Web

Namespace Controllers
    <Authorize(Roles:="Admin")> _
    Public Class UserAdminController
        Inherits Controllers.ApplicationBaseController
        Public Sub New()
        End Sub

        Public Sub New(userManager__1 As ApplicationUserManager, roleManager__2 As ApplicationRoleManager)
            UserManager = userManager__1
            RoleManager = roleManager__2
        End Sub

        Private _userManager As ApplicationUserManager
        Public Property UserManager() As ApplicationUserManager
            Get
                Return If(_userManager, HttpContext.GetOwinContext().GetUserManager(Of ApplicationUserManager)())
            End Get
            Private Set(value As ApplicationUserManager)
                _userManager = value
            End Set
        End Property

        Private _roleManager As ApplicationRoleManager
        Public Property RoleManager() As ApplicationRoleManager
            Get
                Return If(_roleManager, HttpContext.GetOwinContext().[Get](Of ApplicationRoleManager)())
            End Get
            Private Set(value As ApplicationRoleManager)
                _roleManager = value
            End Set
        End Property

        '
        ' GET: /Users/
        Public Async Function Index() As Task(Of ActionResult)
            Return View(Await UserManager.Users.ToListAsync())
        End Function

        '
        ' GET: /Users/Details/5
        Public Async Function Details(id As String) As Task(Of ActionResult)
            If id Is Nothing Then
                Return New HttpStatusCodeResult(HttpStatusCode.BadRequest)
            End If
            Dim user = Await UserManager.FindByIdAsync(id)

            ViewBag.RoleNames = Await UserManager.GetRolesAsync(user.Id)

            Return View(user)
        End Function

        '
        ' GET: /Users/Create
        Public Async Function Create() As Task(Of ActionResult)
            'Get the list of Roles
            ViewBag.RoleId = New SelectList(Await RoleManager.Roles.ToListAsync(), "Name", "Name")
            Return View()
        End Function

        '
        ' POST: /Users/Create
        <HttpPost> _
        Public Async Function Create(userViewModel As RegisterViewModel, ParamArray selectedRoles As String()) As Task(Of ActionResult)
            If ModelState.IsValid Then
                Dim user = New ApplicationUser() With {
                    .UserName = userViewModel.Email,
                    .Email = userViewModel.Email,
                    .Forename = userViewModel.Forename,
                    .Name = userViewModel.Name,
                    .JobFunction = userViewModel.JobFunction,
                    .Cellphone = userViewModel.Cellphone,
                    .EmailConfirmed = True
                }
                Dim adminresult = Await UserManager.CreateAsync(user)

                'Add User to the selected Roles 
                If adminresult.Succeeded Then
                    If selectedRoles IsNot Nothing Then
                        Dim result = Await UserManager.AddToRolesAsync(User.Id, selectedRoles)
                        If Not result.Succeeded Then
                            ModelState.AddModelError("", result.Errors.First())
                            ViewBag.RoleId = New SelectList(Await RoleManager.Roles.ToListAsync(), "Name", "Name")
                            Return View()
                        End If
                    End If
                Else
                    ModelState.AddModelError("", adminresult.Errors.First())
                    ViewBag.RoleId = New SelectList(RoleManager.Roles, "Name", "Name")

                    Return View()
                End If
                Await SendActivationMail(user)
                Return RedirectToAction("Index")
                'Return RedirectToAction("Index")
            End If
            ViewBag.RoleId = New SelectList(RoleManager.Roles, "Name", "Name")
            Return View()
        End Function

        '
        ' GET: /Users/Edit/1
        Public Async Function Edit(id As String) As Task(Of ActionResult)
            If id Is Nothing Then
                Return New HttpStatusCodeResult(HttpStatusCode.BadRequest)
            End If
            Dim user = Await UserManager.FindByIdAsync(id)
            If user Is Nothing Then
                Return HttpNotFound()
            End If

            Dim userRoles = Await UserManager.GetRolesAsync(user.Id)

            Return View(New EditUserViewModel() With { _
                .Id = user.Id, _
                .Username = user.UserName, _
                .Name = user.Name, _
                .Forename = user.Forename, _
                .JobFunction = user.JobFunction, _
                .Cellphone = user.Cellphone, _
                .Email = user.Email, _
                .RolesList = RoleManager.Roles.ToList().[Select](Function(x) New SelectListItem() With { _
                    .Selected = userRoles.Contains(x.Name), _
                    .Text = x.Name, _
                    .Value = x.Name _
                }) _
            })
        End Function

        '
        ' POST: /Users/Edit/5
        <HttpPost> _
        <ValidateAntiForgeryToken> _
        Public Async Function Edit(<Bind(Include:="Email,Id,Username,Name,Forename,Cellphone,Jobfunction")> editUser As EditUserViewModel, ParamArray selectedRole As String()) As Task(Of ActionResult)
            If ModelState.IsValid Then
                Dim user = Await UserManager.FindByIdAsync(editUser.Id)
                If user Is Nothing Then
                    Return HttpNotFound()
                End If

                user.UserName = editUser.Username
                user.Email = editUser.Email
                user.Name = editUser.Name
                user.Forename = editUser.Forename
                user.Cellphone = editUser.Forename
                user.JobFunction = editUser.JobFunction

                Dim userRoles = Await UserManager.GetRolesAsync(user.Id)

                selectedRole = If(selectedRole, New String() {})

                Dim result = Await UserManager.AddToRolesAsync(user.Id, selectedRole.Except(userRoles).ToArray())

                If Not result.Succeeded Then
                    ModelState.AddModelError("", result.Errors.First())
                    Return View()
                End If
                result = Await UserManager.RemoveFromRolesAsync(user.Id, userRoles.Except(selectedRole).ToArray())

                If Not result.Succeeded Then
                    ModelState.AddModelError("", result.Errors.First())
                    Return View()
                End If
                Return RedirectToAction("Index")
            End If
            ModelState.AddModelError("", "Something failed.")
            Return View()
        End Function

        '
        ' GET: /Users/Delete/5
        Public Async Function Delete(id As String) As Task(Of ActionResult)
            If id Is Nothing Then
                Return New HttpStatusCodeResult(HttpStatusCode.BadRequest)
            End If
            Dim user = Await UserManager.FindByIdAsync(id)
            If user Is Nothing Then
                Return HttpNotFound()
            End If
            Return View(user)
        End Function

        '
        ' POST: /Users/Delete/5
        <HttpPost, ActionName("Delete")>
        <ValidateAntiForgeryToken>
        Public Async Function DeleteConfirmed(id As String) As Task(Of ActionResult)
            If ModelState.IsValid Then
                If id Is Nothing Then
                    Return New HttpStatusCodeResult(HttpStatusCode.BadRequest)
                End If

                Dim user = Await UserManager.FindByIdAsync(id)
                If user Is Nothing Then
                    Return HttpNotFound()
                End If
                Dim result = Await UserManager.DeleteAsync(user)
                If Not result.Succeeded Then
                    ModelState.AddModelError("", result.Errors.First())
                    Return View()
                End If
                Return RedirectToAction("Index")
            End If
            Return View()
        End Function
        Private Async Function SendActivationMail(ByVal user As ApplicationUser) As Task
            Dim code As String = Await UserManager.GeneratePasswordResetTokenAsync(user.Id)
            Dim callbackUrl = Url.Action("ResetPassword", "Account", New With {Key .userId = user.Id, Key .code = code}, protocol:=Request.Url.Scheme)
            Dim body As String = "<h4>Welcome to my system!</h4><p>To get started, please <a href=""" & callbackUrl & """>activate</a> your account.</p><p>The account must be activated within 24 hours from receving this mail.</p>"
            Await UserManager.SendEmailAsync(user.Id, "Welcome to my system!", body)
        End Function
    End Class
End Namespace



