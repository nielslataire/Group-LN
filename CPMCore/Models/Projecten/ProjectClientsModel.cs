// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Clients". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
using BOCore;
using CPMCore.Models.Leveranciers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System;

namespace CPMCore.Models.Projecten
{
    public class DetailContactsModel
    {
        public DetailContactsModel()
        {
            _contacts = new List<ContactRequestBO>();
            _contactGroups = new List<ContactGroupModel>();
            _stats = new ContactStatsModel();
        }

        private List<ContactRequestBO> _contacts;
        public List<ContactRequestBO> Contacts
        {
            get { return _contacts; }
            set { _contacts = value; }
        }
        private List<ContactGroupModel> _contactGroups;
        public List<ContactGroupModel> ContactGroups
        {
            get { return _contactGroups; }
            set { _contactGroups = value; }
        }

        private ContactStatsModel _stats;
        public ContactStatsModel Stats
        {
            get { return _stats; }
            set { _stats = value; }
        }

        private int _projectid;
        public int ProjectId
        {
            get { return _projectid; }
            set { _projectid = value; }
        }

        private string _projectname;
        public string ProjectName
        {
            get { return _projectname; }
            set { _projectname = value; }
        }
    }

    public class ContactGroupModel
    {
        public string GroupKey { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime LatestContactAt { get; set; }
        public string LatestRequestType { get; set; }
        public string LatestSourceSite { get; set; }
        public string LatestOrigin { get; set; }
        public int TotalRequests { get; set; }
        public string LatestStatus { get; set; }
        public string LatestStatusComment { get; set; }
        public DateTime? LatestStatusAt { get; set; }
        public DateTime? LastEmailSentAt { get; set; }
        public string LastEmailSentBy { get; set; }
    }

    public class SendContactEmailModalVM
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public List<SelectListItem> Templates { get; set; } = new();
    }

    public class SendContactEmailInputModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public int TemplateId { get; set; }
        public bool IncludeSignature { get; set; }
    }

    public class ContactStatsModel
    {
        public int TotalContacts { get; set; }
        public int ActiveContacts { get; set; }
        public int NewContactsWeek { get; set; }
        public int NewContactsMonth { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ResponseRate { get; set; }
    }

    public class ContactDetailsModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public ContactGroupModel Contact { get; set; }
        public List<ContactRequestBO> Requests { get; set; } = new List<ContactRequestBO>();
        public ContactActionInputModel NewAction { get; set; } = new ContactActionInputModel();
        public ContactStatusInputModel NewStatus { get; set; } = new ContactStatusInputModel();
    }

    public class ContactActionInputModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string ActionType { get; set; }
        public DateTime ActionDate { get; set; }
        public TimeSpan ActionTime { get; set; }
        public string Comment { get; set; }
    }

    public class ContactStatusInputModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime StatusDate { get; set; }
        public TimeSpan StatusTime { get; set; }
        public string Comment { get; set; }
    }

    public class ContactEditModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string NewEmail { get; set; }
        public string NewPhone { get; set; }
        public DateTime? ContactDate { get; set; }
        public string ContactMethod { get; set; }

    }

    public class ContactDeleteModel
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Phone { get; set; }
    }

    public class ContactAddModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Comment { get; set; }
        public DateTime? ContactDate { get; set; }
        public string ContactMethod { get; set; }
    }
}
