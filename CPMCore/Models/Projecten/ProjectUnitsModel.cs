// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Units". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class DetailUnitsModel
    {
        public DetailUnitsModel()
        {
            _projectunits = new List<UnitBO>();
            _unitsgrouped = new List<GroupUnitsBO>();
            _addunit = new UnitBO();
            _constructionvalues = new List<UnitConstructionValueBO>();
            _grouptypes = new List<UnitGroupTypeBO>();
            _types = new List<UnitTypeBO>();

            _units = new List<IdNameBO>();
            _attachableunits = new List<IdNameBO>();
            _paymentgroups = new List<IdNameBO>();
        }
        private List<GroupUnitsBO> _unitsgrouped;
        public List<GroupUnitsBO> UnitsGrouped
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
        private List<UnitBO> _projectunits;
        public List<UnitBO> ProjectUnits
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
        private int _projectlandshare;
        public int ProjectLandShare
        {
            get
            {
                return _projectlandshare;
            }
            set
            {
                _projectlandshare = value;
            }
        }

        private UnitBO _addunit;
        public UnitBO AddUnit
        {
            get
            {
                return _addunit;
            }
            set
            {
                _addunit = value;
            }
        }
        private List<UnitConstructionValueBO> _constructionvalues;
        public List<UnitConstructionValueBO> ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }
        private List<UnitGroupTypeBO> _grouptypes;
        public List<UnitGroupTypeBO> GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO> _types;
        public List<UnitTypeBO> Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
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
        private List<IdNameBO> _attachableunits;
        public List<IdNameBO> AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<int> _selectedUnits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
            }
        }
        private List<IdNameBO> _paymentgroups;
        public List<IdNameBO> PaymentGroups
        {
            get
            {
                return _paymentgroups;
            }
            set
            {
                _paymentgroups = value;
            }
        }


        /// <summary>gl-v2 (Projecten/DetailUnitsV2, design-handoff punt 16a/16d) — de volledige,
        /// voorgekauwde eenhedenboom + KPI-cijfers voor de gl-v2-variant van deze pagina. Enkel gevuld
        /// door ProjectenController.DetailUnits; de legacy view en de AddUnit/EditUnit-paden die
        /// FillDetailUnitModel delen raken dit niet aan.</summary>
        public DetailUnitsV2Vm? GlV2 { get; set; }

        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
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
    }

    // UNITS
    public class AddUnitModel
    {
        public AddUnitModel()
        {
            _addunit = new UnitBO();
            _constructionvalues = new List<UnitConstructionValueBO>();
            _grouptypes = new List<UnitGroupTypeBO>();
            _types = new List<UnitTypeBO>();
            _attachableunits = new List<IdNameBO>();
            _paymentgroups = new List<IdNameBO>();
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
        private int _projectlandshare;
        public int ProjectLandShare
        {
            get
            {
                return _projectlandshare;
            }
            set
            {
                _projectlandshare = value;
            }
        }

        private UnitBO _addunit;
        public UnitBO AddUnit
        {
            get
            {
                return _addunit;
            }
            set
            {
                _addunit = value;
            }
        }
        private List<UnitConstructionValueBO> _constructionvalues;
        public List<UnitConstructionValueBO> ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }
        private List<UnitGroupTypeBO> _grouptypes;
        public List<UnitGroupTypeBO> GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO> _types;
        public List<UnitTypeBO> Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
            }
        }

        private List<IdNameBO> _attachableunits;
        public List<IdNameBO> AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<int> _selectedUnits;
        public List<int> SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
            }
        }
        private List<IdNameBO> _paymentgroups;
        public List<IdNameBO> PaymentGroups
        {
            get
            {
                return _paymentgroups;
            }
            set
            {
                _paymentgroups = value;
            }
        }

        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
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
    }

    public class EditUnitModel
    {
        public EditUnitModel()
        {
            _units = new List<IdNameBO>();
            _selectedUnits = new List<int>();
            _attachableunits = new List<IdNameBO>();
            _rooms = new List<RoomBO>();
            _executionPlans = new List<UnitExecutionPlanVm>();
        }
        private UnitBO _unit;
        public UnitBO Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                _unit = value;
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

        private List<UnitGroupTypeBO>? _grouptypes;
        public List<UnitGroupTypeBO>? GroupTypes
        {
            get
            {
                return _grouptypes;
            }
            set
            {
                _grouptypes = value;
            }
        }
        private List<UnitTypeBO>? _types;
        public List<UnitTypeBO>? Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }

        private int _selectedGroupType;
        public int SelectedGroupType
        {
            get
            {
                return _selectedGroupType;
            }
            set
            {
                _selectedGroupType = value;
            }
        }
        private int _selectedType;
        public int SelectedType
        {
            get
            {
                return _selectedType;
            }
            set
            {
                _selectedType = value;
            }
        }

        private List<IdNameBO>? _units;
        public List<IdNameBO>? Units
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
        private List<int>? _selectedUnits;
        public List<int>? SelectedUnits
        {
            get
            {
                return _selectedUnits;
            }
            set
            {
                _selectedUnits = value;
            }
        }
        public enum EnumType : int
        {
            Eenheid = 1,
            Koppeling = 2
        }
        private EnumType _type;
        public EnumType Type
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
        private List<IdNameBO>? _attachableunits;
        public List<IdNameBO>? AttachableUnits
        {
            get
            {
                return _attachableunits;
            }
            set
            {
                _attachableunits = value;
            }
        }
        private List<RoomBO>? _rooms;
        public List<RoomBO>? Rooms
        {
            get
            {
                return _rooms;
            }
            set
            {
                _rooms = value;
            }
        }
        private List<UnitExecutionPlanVm> _executionPlans;
        public List<UnitExecutionPlanVm> ExecutionPlans
        {
            get { return _executionPlans; }
            set { _executionPlans = value; }
        }
        // PaymentGroup
        private int? _selectedpaymentgroup;

        public int? SelectedPaymentGroup
        {
            get
            {
                return _selectedpaymentgroup;
            }
            set
            {
                _selectedpaymentgroup = value;
            }
        }
        private List<IdNameBO>? _PaymentGroups;
        public List<IdNameBO>? PaymentGroups
        {
            get
            {
                return _PaymentGroups;
            }
            set
            {
                _PaymentGroups = value;
            }
        }
        private List<UnitConstructionValueBO>? _constructionvalues;
        public List<UnitConstructionValueBO>? ConstructionValues
        {
            get
            {
                return _constructionvalues;
            }
            set
            {
                _constructionvalues = value;
            }
        }

        private List<UnitFinishingOptionBO>? _finishingoptions;
        public List<UnitFinishingOptionBO>? FinishingOptions
        {
            get { return _finishingoptions; }
            set { _finishingoptions = value; }
        }
    }

    public class UnitExecutionPlanVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    public class AddUnitLinkModel
    {
        public AddUnitLinkModel()
        {
            _units = new List<IdNameBO>();
            _selectedunits = new List<int>();
        }
        private UnitBO _selectedUnit;
        public UnitBO SelectedUnit
        {
            get
            {
                return _selectedUnit;
            }
            set
            {
                _selectedUnit = value;
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
        private UnitBO _unit;
        public UnitBO Unit
        {
            get
            {
                return _unit;
            }
            set
            {
                _unit = value;
            }
        }
    }

    public class AddUnitConstructionValueModel
    {
        public AddUnitConstructionValueModel()
        {
            _constructionvalue = new UnitConstructionValueBO();
            _paymentgroups = new List<IdNameBO>();
        }
        private UnitConstructionValueBO _constructionvalue;
        public UnitConstructionValueBO ConstructionValue
        {
            get
            {
                return _constructionvalue;
            }
            set
            {
                _constructionvalue = value;
            }
        }
        private List<IdNameBO> _paymentgroups;
        public List<IdNameBO> Paymentgroups
        {
            get
            {
                return _paymentgroups;
            }
            set
            {
                _paymentgroups = value;
            }
        }
    }
}
