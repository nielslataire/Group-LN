// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Sales". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    // SALES
    public class ProjectSalesModel
    {
        public ProjectSalesModel()
        {
            _unitsgrouped = new List<GroupUnitsWithAttachedUnitsBO>();
            _projectunits = new List<UnitWithAttachedUnitsBO>();
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

        private List<GroupUnitsWithAttachedUnitsBO> _unitsgrouped;
        public List<GroupUnitsWithAttachedUnitsBO> UnitsGrouped
        {
            get
            {
                return _unitsgrouped;
            }
            set
            {
                _unitsgrouped = value;
            }
        }
        private List<UnitWithAttachedUnitsBO> _projectunits;
        public List<UnitWithAttachedUnitsBO> ProjectUnits
        {
            get
            {
                return _projectunits;
            }
            set
            {
                _projectunits = value;
            }
        }
    }

    public class ProjectSalesSettingsModel
    {
        public ProjectSalesSettingsModel()
        {

        }

        public List<SelectListItem> BankAccounts { get; set; } = new();
        public string? NewBankAccountIban { get; set; }
        public bool MissingBuilder { get; set; }
        public string? BuilderWarning { get; set; }
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
        private ProjectSalesSettingsBO _settings;
        public ProjectSalesSettingsBO Settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
            }
        }
        private ProjectBO _project;
        public ProjectBO Project
        {
            get
            {
                return _project;
            }
            set
            {
                _project = value;
            }
        }


    }

    // Spiegel je BO-enum:
    public enum RegistrationType
    {
        Vat = 0,            // enkel BTW (op grond + bouw)
        Registration = 1,   // enkel registratierechten (op grond + bouw)
        Mixed = 2           // BTW op bouw + registratie op grond
    }

    public class ProjectSalesExportModel
    {
        public ProjectSalesExportModel()
        {
            _unitsgrouped = new List<GroupUnitsWithAttachedUnitsWithDetailsBO>();
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
        private List<GroupUnitsWithAttachedUnitsWithDetailsBO> _unitsgrouped;
        public List<GroupUnitsWithAttachedUnitsWithDetailsBO> UnitsGrouped
        {
            get
            {
                return _unitsgrouped;
            }
            set
            {
                _unitsgrouped = value;
            }
        }
        private List<RoomType> _surfacetypes;
        public List<RoomType> SurfaceTypes
        {
            get
            {
                return _surfacetypes;
            }
            set
            {
                _surfacetypes = value;
            }
        }
    }

    public class ProjectSalesSelectForPriceModel
    {
        public ProjectSalesSelectForPriceModel()
        {
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
        private List<IdNameBO> _units;
        public List<IdNameBO> Units
        {
            get
            {
                return _units;
            }
            set
            {
                _units = value;
            }
        }
        private List<int> _selectedunits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedunits;
            }
            set
            {
                _selectedunits = value;
            }
        }
    }

    public class ProjectSalesCalculatePrice
    {
        public ProjectSalesCalculatePrice()
        {
            _units = new List<UnitWithReductionBO>();
            _reductions = new List<ConstructionReductionBO>();
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
        private List<UnitWithReductionBO> _units;
        public List<UnitWithReductionBO> Units
        {
            get
            {
                return _units;
            }
            set
            {
                _units = value;
            }
        }
        private ProjectSalesSettingsBO _salessettings;
        public ProjectSalesSettingsBO SalesSettings
        {
            get
            {
                return _salessettings;
            }
            set
            {
                _salessettings = value;
            }
        }
        private List<ConstructionReductionBO> _reductions;
        public List<ConstructionReductionBO> Reductions
        {
            get
            {
                return _reductions;
            }
            set
            {
                _reductions = value;
            }
        }
        private bool _abatement;
        public bool Abatement
        {
            get
            {
                return _abatement;
            }
            set
            {
                _abatement = value;
            }
        }
        private bool _raisedabatement;
        public bool RaisedAbatement
        {
            get
            {
                return _raisedabatement;
            }
            set
            {
                _raisedabatement = value;
            }
        }
        private bool _oneandownhome;
        public bool OneAndOwnHome
        {
            get
            {
                return _oneandownhome;
            }
            set
            {
                _oneandownhome = value;
            }
        }
    }
}
