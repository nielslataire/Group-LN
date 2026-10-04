// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Media". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class ProjectMediaSectionVM
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string Description { get; set; }
        public int    SortOrder   { get; set; }
        public bool   IsPublic    { get; set; }
        public int    MediaCount  { get; set; }
        public int    PhotoCount  { get; set; }
        public int    VideoCount  { get; set; }
    }

    public class DetailPhotosModel
    {
        public DetailPhotosModel()
        {
            Photos   = new List<ProjectPictureBO>();
            Sections = new List<ProjectMediaSectionVM>();
        }

        public List<ProjectPictureBO>    Photos   { get; set; }
        public List<ProjectMediaSectionVM> Sections { get; set; }
        public List<IdNameBO> Units { get; set; } = new List<IdNameBO>();
        public int    ProjectId   { get; set; }
        public string ProjectName { get; set; }

        // gl-v2: enkel voor GlV2ProjectMenuVm.IsCoordinationProject (zelfde vlag/reden als
        // DetailClientsModel/ClientModel.IsCoordinationProject) — _ProjectInnerMenuV2 verbergt
        // Media/Nieuws/Contacten al op basis hiervan, maar de andere items (Nieuws/Contacten)
        // moeten dezelfde vlag krijgen zodra ze via dit menu meegerenderd worden.
        public bool IsCoordinationProject { get; set; }
    }

    public class DetailNewsModel
    {
        public DetailNewsModel()
        {
            _news = new List<ProjectNewsBO>();
        }
        private List<ProjectNewsBO> _news;
        public List<ProjectNewsBO> News
        {
            get
            {
                return _news;
            }
            set
            {
                _news = value;
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
}
