// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "DocumentsLegacy". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class DetailDocsModel
    {
        public DetailDocsModel()
        {
            _docs = new List<ProjectDocBO>();
            Clients = Array.Empty<IdNameBO>();
        }
        private List<ProjectDocBO> _docs;
        public List<ProjectDocBO> Docs
        {
            get
            {
                return _docs;
            }
            set
            {
                _docs = value;
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
        private int _clientaccountid;
        public int ClientAccountId
        {
            get
            {
                return _clientaccountid;
            }
            set
            {
                _clientaccountid = value;
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
        private string _clientname;
        public string ClientName
        {
            get
            {
                return _clientname;
            }
            set
            {
                _clientname = value;
            }
        }
        private ProjectDocBO _uploaddoc;
        public ProjectDocBO UploadDoc
        {
            get
            {
                return _uploaddoc;
            }
            set
            {
                _uploaddoc = value;
            }
        }
        public IReadOnlyList<IdNameBO> Clients { get; set; }
    }
}
