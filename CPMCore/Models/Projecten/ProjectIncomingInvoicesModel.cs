// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "IncomingInvoices". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class ProjectIncommingInvoiceAddUpdateModel
    {
        public ProjectIncommingInvoiceAddUpdateModel()
        {
            _projectContracts = new List<IdNameBO>();
            _incomminginvoice = new IncommingInvoiceBO();
            _activities = new List<Select2DTO>();
            _listactivities = new List<IdNameBO>();
            _selectedActivities = new List<int>();
        }
        private int _type;
        public int Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        private string? _companyname;
        public string? CompanyName
        {
            get
            {
                return _companyname;
            }
            set
            {
                _companyname = value;
            }
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private IncommingInvoiceBO _incomminginvoice;
        public IncommingInvoiceBO IncommingInvoice
        {
            get
            {
                return _incomminginvoice;
            }
            set
            {
                _incomminginvoice = value;
            }
        }
        private List<IdNameBO> _projectContracts;
        [Display(Name = "Contracten")]
        public List<IdNameBO> ProjectContracts
        {
            get
            {
                return _projectContracts;
            }
            set
            {
                _projectContracts = value;
            }
        }
        private int _selectedcontract;
        public int SelectedContract
        {
            get
            {
                return _selectedcontract;
            }
            set
            {
                _selectedcontract = value;
            }
        }
        private List<Select2DTO> _activities;
        [Display(Name = "Activiteiten")]
        public List<Select2DTO> Activities
        {
            get
            {
                return _activities;
            }
            set
            {
                _activities = value;
            }
        }
        private List<IdNameBO> _listactivities;
        public List<IdNameBO> ListActivities
        {
            get
            {
                return _listactivities;
            }
            set
            {
                _listactivities = value;
            }
        }
        private List<int> _selectedActivities;
        public List<int> SelectedActivities
        {
            get
            {
                return _selectedActivities;
            }
            set
            {
                _selectedActivities = value;
            }
        }
        public decimal TotaalPrijsLijnen
        {
            get
            {
                return IncommingInvoice.Details.Sum(m => m.Price);
            }
        }
    }

    public class ProjectIncommingInvoiceModel
    {
        public ProjectIncommingInvoiceModel()
        {
            _incomminginvoice = new IncommingInvoiceBO();
            _company = new CompanyBO();
            _contract = new ContractBO();
        }
        private int _projectid;
        public int ProjectId
        {
            get
            {
                return _projectid;
            }
            set
            {
                _projectid = value;
            }
        }
        private string _projectname;
        public string ProjectName
        {
            get
            {
                return _projectname;
            }
            set
            {
                _projectname = value;
            }
        }
        private IncommingInvoiceBO _incomminginvoice;
        public IncommingInvoiceBO IncommingInvoice
        {
            get
            {
                return _incomminginvoice;
            }
            set
            {
                _incomminginvoice = value;
            }
        }
        private CompanyBO _company;
        public CompanyBO Company
        {
            get
            {
                return _company;
            }
            set
            {
                _company = value;
            }
        }
        private ContractBO _contract;
        public ContractBO Contract
        {
            get
            {
                return _contract;
            }
            set
            {
                _contract = value;
            }
        }

    }
}
