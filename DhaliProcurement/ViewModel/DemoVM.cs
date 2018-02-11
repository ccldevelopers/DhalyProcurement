using DhaliProcurement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DhaliProcurement.ViewModel
{
    public class DemoVM
    {
        public int ItemISLNO { get; set; }
        public int UnitUSLNO { get; set; }
        public decimal PQuantity { get; set; }
        public decimal PCost { get; set; }
        public string Remarks { get; set; }
        public int ProcProjectId { get; set; }
        //public int ProjectId { get; set; }
        public int ProjectSiteId { get; set; }

    }

    public class ProcItem
    {
        public int ItemISLNO { get; set; }
        public int UnitUSLNO { get; set; }
        public int PQuantity { get; set; }
        public int PCost { get; set; }
        public string Remarks { get; set; }
        //public int ProjectId { get; set; }
        public int ProjectSiteId { get; set; }

    }
}