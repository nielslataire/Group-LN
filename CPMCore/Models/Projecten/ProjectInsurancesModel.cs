// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Insurances". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class DetailInsurancesModel
    {
        public DetailInsurancesModel()
        {
            _insurances = new List<InsuranceBO>();
        }
        private List<InsuranceBO> _insurances;
        public List<InsuranceBO> Insurances
        {
            get
            {
                return _insurances;
            }
            set
            {
                _insurances = value;
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
    }

    public class ProjectAddInsurancesModel
    {
        public ProjectAddInsurancesModel()
        {
            _insurance = new InsuranceBO();
            _brokers = new List<IdNameBO>();
            _companies = new List<IdNameBO>();
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
        private InsuranceBO _insurance;
        public InsuranceBO Insurance
        {
            get
            {
                return _insurance;
            }
            set
            {
                _insurance = value;
            }
        }
        private string? _projectname;
        public string? ProjectName
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
        private List<IdNameBO> _companies;
        [Display(Name = "Maatschappij")]
        public List<IdNameBO> Companies
        {
            get
            {
                return _companies;
            }
            set
            {
                _companies = value;
            }
        }
        private List<IdNameBO> _brokers;
        [Display(Name = "Makelaar")]
        public List<IdNameBO> Brokers
        {
            get
            {
                return _brokers;
            }
            set
            {
                _brokers = value;
            }
        }

        /// <summary>Gekozen makelaar bij het toevoegen van een nieuwe verzekering (Id=0).</summary>
        [Display(Name = "Makelaar")]
        public int SelectedBrokerId { get; set; }
    }
}
