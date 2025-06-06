﻿Imports System.Threading.Tasks
Imports System.Security.Claims
Imports Microsoft.AspNet.Identity
Imports Microsoft.AspNet.Identity.EntityFramework
Imports Microsoft.AspNet.Identity.Owin
Imports Microsoft.Owin
Imports Microsoft.Owin.Security
Imports System.Net
Imports System.Net.Mail

Public Class EmailService
    Implements IIdentityMessageService

    Public Async Function SendAsync(message As IdentityMessage) As Task Implements IIdentityMessageService.SendAsync
        ' Plug in your email service here to send an email.

        Using client = New SmtpClient()
            client.UseDefaultCredentials = False
            client.Port = 587
            client.Host = "smtp.office365.com"
            client.DeliveryMethod = SmtpDeliveryMethod.Network
            client.EnableSsl = True
            client.Credentials = New NetworkCredential("niels.lataire@groupln.be", "840683Pas")
            Using mailMessage = New MailMessage()
                mailMessage.Body = message.Body
                mailMessage.[To].Add(message.Destination)
                mailMessage.Subject = message.Subject
                mailMessage.IsBodyHtml = True
                Await client.SendMailAsync(mailMessage)
            End Using
        End Using

    End Function
End Class

Public Class SmsService
    Implements IIdentityMessageService

    Public Function SendAsync(message As IdentityMessage) As Task Implements IIdentityMessageService.SendAsync
        ' Plug in your SMS service here to send a text message.
        Return Task.FromResult(0)
    End Function
End Class


' Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
Public Class ApplicationUserManager
    Inherits UserManager(Of ApplicationUser)

    Public Sub New(store As IUserStore(Of ApplicationUser))
        MyBase.New(store)
    End Sub

    Public Shared Function Create(options As IdentityFactoryOptions(Of ApplicationUserManager), context As IOwinContext)
        Dim manager = New ApplicationUserManager(New UserStore(Of ApplicationUser)(context.Get(Of ApplicationDbContext)()))

        ' Configure validation logic for usernames
        manager.UserValidator = New UserValidator(Of ApplicationUser)(manager) With {
            .AllowOnlyAlphanumericUserNames = False,
            .RequireUniqueEmail = True
        }

        ' Configure validation logic for passwords
        manager.PasswordValidator = New PasswordValidator With {
            .RequiredLength = 6,
            .RequireNonLetterOrDigit = False,
            .RequireDigit = False,
            .RequireLowercase = False,
            .RequireUppercase = False
        }

        ' Configure user lockout defaults
        manager.UserLockoutEnabledByDefault = True
        manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5)
        manager.MaxFailedAccessAttemptsBeforeLockout = 5

        ' Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
        ' You can write your own provider and plug it in here.
        manager.RegisterTwoFactorProvider("Phone Code", New PhoneNumberTokenProvider(Of ApplicationUser) With {
                                          .MessageFormat = "Your security code is {0}"
                                      })
        manager.RegisterTwoFactorProvider("Email Code", New EmailTokenProvider(Of ApplicationUser) With {
                                          .Subject = "Security Code",
                                          .BodyFormat = "Your security code is {0}"
                                          })
        manager.EmailService = New EmailService()
        manager.SmsService = New SmsService()
        Dim dataProtectionProvider = options.DataProtectionProvider
        If (dataProtectionProvider IsNot Nothing) Then
            manager.UserTokenProvider = New DataProtectorTokenProvider(Of ApplicationUser)(dataProtectionProvider.Create("ASP.NET Identity"))
        End If

        Return manager
    End Function

End Class

'Configure the RoleManager used in the application. RoleManager is defined in the ASP.NET Identity core assembly
Public Class ApplicationRoleManager
    Inherits RoleManager(Of IdentityRole)
    Public Sub New(roleStore As IRoleStore(Of IdentityRole, String))
        MyBase.New(roleStore)
    End Sub

    Public Shared Function Create(options As IdentityFactoryOptions(Of ApplicationRoleManager), context As IOwinContext) As ApplicationRoleManager
        Return New ApplicationRoleManager(New RoleStore(Of IdentityRole)(context.[Get](Of ApplicationDbContext)()))
    End Function
End Class

' Configure the application sign-in manager which is used in this application.
Public Class ApplicationSignInManager
    Inherits SignInManager(Of ApplicationUser, String)
    Public Sub New(userManager As ApplicationUserManager, authenticationManager As IAuthenticationManager)
        MyBase.New(userManager, authenticationManager)
    End Sub

    Public Overrides Function CreateUserIdentityAsync(user As ApplicationUser) As Task(Of ClaimsIdentity)
        Return user.GenerateUserIdentityAsync(DirectCast(UserManager, ApplicationUserManager))
    End Function

    Public Shared Function Create(options As IdentityFactoryOptions(Of ApplicationSignInManager), context As IOwinContext) As ApplicationSignInManager
        Return New ApplicationSignInManager(context.GetUserManager(Of ApplicationUserManager)(), context.Authentication)
    End Function
End Class


