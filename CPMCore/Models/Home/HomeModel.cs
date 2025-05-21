using BOCore;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace CPMCore.Models.Home
{
    public class HomeModel
    {
        public HomeModel()
        {
            _projects = new List<ProjectBO>();
            _statuses = new List<ProjectStatusBO>();    
            _oldprojects = new List<ProjectBO>();
            m_DeedofSaleWarnings = new List<ClientAccountBO>();
        }
        private List<ProjectBO> _projects;
        public List<ProjectBO> Projects
        {
            get
            {
                return _projects;
            }
            set
            {
                _projects = value;
            }
        }
        private List<ProjectBO> _oldprojects;
        public List<ProjectBO> OldProjects
        {
            get
            {
                return _oldprojects;
            }
            set
            {
                _oldprojects = value;
            }
        }
        private IdNameBO? m_selectedsearch;
        public IdNameBO? SelectedSearch
        {
            get
            {
                return m_selectedsearch;
            }
            set
            {
                m_selectedsearch = value;
            }
        }
        private List<ClientAccountBO> m_DeedofSaleWarnings;
        public List<ClientAccountBO> DeedofSaleWarnings
        {
            get
            {
                return m_DeedofSaleWarnings;
            }
            set
            {
                m_DeedofSaleWarnings = value;
            }
        }
        private List<WarningBO>? _insurancewarnings;
        public List<WarningBO>? InsuranceWarnings
        {
            get
            {
                return _insurancewarnings;
            }
            set
            {
                _insurancewarnings = value;
            }
        }
        private List<WarningBO>? _projectInfo;
        public List<WarningBO>? ProjectInfo
        {
            get
            {
                return _projectInfo;
            }
            set
            {
                _projectInfo = value;
            }
        }
        private List<ProjectStatusBO> _statuses;
        public List<ProjectStatusBO> Statuses
        {
            get
            {
                return _statuses;
            }
            set
            {
                _statuses = value;
            }
        }

    }
}
