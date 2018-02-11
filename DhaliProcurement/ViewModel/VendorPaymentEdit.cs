using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DhaliProcurement.Models;

namespace DhaliProcurement.ViewModel
{
    public class VendorPaymentEdit
    {      
           
    }

    public class VendorPaymentMasterDetail
    {
        public Proc_VendorPaymentMas proc_vPaymentMas { get; set; }
        public ICollection<Proc_VendorPaymentDet> proc_vPaymentDet { get; set; }
    }


    public class VendorPaymentEditDetail
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public int PONo { get; set; }
        public string PONoName { get; set; }
        //public int ReqNoId { get; set; }
        public string ReqNoName { get; set; }
        public int ReqNo { get; set; }      
        public int Proc_MaterialEntryDetId { get; set; }
        public int Proc_VendorPaymentMasId { get; set; }
        public int Proc_VendorPaymentDetId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal ReqQty { get; set; }
        public int ChallanNo { get; set; }     
        public string ChallanNoName { get; set; }
        public string BillNo { get; set; }
        public decimal Qty { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Payment { get; set; }
        public string Remarks { get; set; }
        public decimal TotalAmt { get; set; }
    }
}