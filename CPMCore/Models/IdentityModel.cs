using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.AspNetCore.Identity.Owin;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models
{
    public class ApplicationUser : IdentityUser
    {
        //public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        //{
        //    // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
        //    var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
        //    // Add custom user claims here
        //    return userIdentity;
        //}

        private string _Cellphone = "";
        [Display(Name = "Mobiele telefoon")]
        public string Cellphone
        {
            get
            {
                return _Cellphone;
            }
            set
            {
                _Cellphone = value;
            }
        }

        private string _Name = "";
        [Display(Name = "Naam")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        } 
        private string _Forename = "";
        [Display(Name = "Voornaam")]
        public string Forename
        {
            get
            {
                return _Forename;
            }
            set
            {
                _Forename = value;
            }
        }
        private string _JobFunction ="";
        [Display(Name = "Functie")]
        public string JobFunction
        {
            get
            {
                return _JobFunction;
            }
            set
            {
                _JobFunction = value;
            }
        }

        [Display(Name = "Weergavenaam")]
        public string Displayname
        {
            get
            {
                return _Name + " " + _Forename;
            }
        }

    }
}
