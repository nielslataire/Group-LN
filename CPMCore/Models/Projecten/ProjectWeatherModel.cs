// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Weather". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class ProjectVacationDaysModel
    {
        public ProjectVacationDaysModel()
        {
        }
        private int _projectId;
        public int ProjectID
        {
            get
            {
                return _projectId;
            }
            set
            {
                _projectId = value;
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

    public class BWDModel
    {
        public BWDModel()
        {
            _weatherstations = new List<IdNameBO>();
        }
        private List<IdNameBO> _weatherstations;
        public List<IdNameBO> WeatherStations
        {
            get
            {
                return _weatherstations;
            }
            set
            {
                _weatherstations = value;
            }
        }
        private int _selectedweatherstation;
        public int SelectedWeatherStation
        {
            get
            {
                return _selectedweatherstation;
            }
            set
            {
                _selectedweatherstation = value;
            }
        }
    }
}
