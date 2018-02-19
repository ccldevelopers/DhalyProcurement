using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using DhaliProcurement.Helper;
using DhaliProcurement.Models;
using DhaliProcurement.ReportDataset;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace DhaliProcurement.Controllers
{
    public class ReportController : Controller
    {

        private readonly DCPSContext db;
        public ReportController()
        {
            db = new DCPSContext();
        }


        // GET: Report
        public ActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public ActionResult ProjectInfoCosting()
        {

            var procprojects = (from procProject in db.ProcProject
                                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                join project in db.Project on site.ProjectId equals project.Id
                                where site.Id == procProject.ProjectSiteId
                                select new { site, project }).Distinct().ToList();

            List<ProjectSite> sites = new List<ProjectSite>();
            foreach (var i in procprojects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.site.Id);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();
            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }

            ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");

            ViewBag.SiteId = new SelectList(sites, "Id", "Name");




            return View();
        }



        [HttpPost]
        public ActionResult ProjectInfoCosting(int? ProjectId, int? SiteId)
        {


            var companyInfo = db.CompanyInformation.SingleOrDefault();
            ProjectInformationDetail dsProjInfoCost = new ProjectInformationDetail();


            dsProjInfoCost.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);


            var PrManMobile = (from projRes in db.ProjectResource
                               join compRes in db.CompanyResource on projRes.CompanyResourceId equals compRes.Id
                               where projRes.ProjectId == ProjectId && projRes.CompanyResourceId == compRes.Id
                               select compRes.Mobile).SingleOrDefault();

            var StEngrMobile = (from projSiteRes in db.ProjectSiteResource
                                join compResSite in db.CompanyResource on projSiteRes.CompanyResourceId equals compResSite.Id
                                where projSiteRes.ProjectSiteId == SiteId && projSiteRes.CompanyResourceId == compResSite.Id
                                select compResSite.Mobile).SingleOrDefault();


            var projCostingMasters = (from proj in db.Project
                                      join projSite in db.ProjectSite on proj.Id equals projSite.ProjectId
                                      join procProj in db.ProcProject on projSite.Id equals procProj.ProjectSiteId
                                      join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                      join projRes in db.ProjectResource on proj.Id equals projRes.ProjectId
                                      join siteRes in db.ProjectSiteResource on projSite.Id equals siteRes.ProjectSiteId
                                      join compResProject in db.CompanyResource on projRes.CompanyResourceId equals compResProject.Id
                                      join compResSite in db.CompanyResource on siteRes.CompanyResourceId equals compResSite.Id
                                      where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id
                                      select new
                                      {
                                          ProjectId = proj.Id,
                                          ProjectName = proj.Name,
                                          ProjectStartDate = proj.StartDate,
                                          ProjectEndDate = proj.EndDate,
                                          ProjectResId = projRes.ProjectId,
                                          ProjectResName = compResProject.Name,
                                          ProjectSiteResId = siteRes.ProjectSiteId,
                                          ProjectSiteResName = compResSite.Name,
                                          ProjectPhone = compResProject.Mobile,
                                          SiteId = projSite.Id,
                                          SiteName = projSite.Name,
                                          SitePhone = compResSite.Mobile,
                                          ProcProjId = procProj.Id,
                                          BOQNo = procProj.BOQNo,
                                          BOQDate = procProj.BOQDate,
                                          NOANo = procProj.NOANo,
                                          NOADate = procProj.NOADate,
                                          ProjCompResId = projRes.CompanyResourceId,
                                          SiteCompResId = siteRes.CompanyResourceId

                                      }).Distinct().ToList();

            if (ProjectId != null && SiteId != null)
            {
                projCostingMasters = projCostingMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            }
            else if (ProjectId != null)
            {
                projCostingMasters = projCostingMasters.Where(x => x.ProjectId == ProjectId).ToList();
            }


            foreach (var projCostingMaster in projCostingMasters)
            {

                //details
                var projCosting = (from proj in db.Project
                                   join projSite in db.ProjectSite on proj.Id equals projSite.ProjectId
                                   join procProj in db.ProcProject on projSite.Id equals procProj.ProjectSiteId
                                   join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                   join projRes in db.ProjectResource on proj.Id equals projRes.ProjectId
                                   join siteRes in db.ProjectSiteResource on projSite.Id equals siteRes.ProjectSiteId
                                   join compResProject in db.CompanyResource on projRes.CompanyResourceId equals compResProject.Id
                                   join compResSite in db.CompanyResource on siteRes.CompanyResourceId equals compResSite.Id
                                   join items in db.Item on procItem.ItemId equals items.Id
                                   join units in db.Unit on procItem.UnitId equals units.Id
                                   where procItem.ProcProjectId == projCostingMaster.ProcProjId
                                   select new
                                   {

                                       ProcProjId = procProj.Id,
                                       ItemId = procItem.ItemId,
                                       ItemName = items.Name,
                                       UnitId = procItem.UnitId,
                                       UnitName = units.Name,
                                       POQuantity = procItem.PQuantity,
                                       PCost = procItem.PCost,
                                       Remarks = procItem.Remarks,
                                       ProcItemProcProjId = procItem.ProcProjectId,

                                   }).Distinct().ToList();




                dsProjInfoCost.ProcProjMaster.AddProcProjMasterRow(projCostingMaster.ProcProjId,
                                                      projCostingMaster.BOQNo,
                                                      NullHelper.DateToString(projCostingMaster.BOQDate),
                                                      projCostingMaster.NOANo,
                                                      NullHelper.DateToString(projCostingMaster.NOADate),
                                                      projCostingMaster.ProjectId,
                                                      projCostingMaster.ProjectName,
                                                      projCostingMaster.ProjectResId,
                                                      projCostingMaster.SiteId,
                                                      projCostingMaster.SiteName,
                                                      projCostingMaster.ProjectSiteResId,
                                                      NullHelper.DateToString(projCostingMaster.ProjectStartDate),
                                                      NullHelper.DateToString(projCostingMaster.ProjectEndDate),
                                                      projCostingMaster.ProjectResName,
                                                      projCostingMaster.ProjectSiteResName,
                                                      PrManMobile,
                                                      StEngrMobile);

                foreach (var item in projCosting)
                {
                    dsProjInfoCost.ProcProjDetail.AddProcProjDetailRow(item.ProcProjId,
                                                                       item.ItemId,
                                                                       item.ItemName,
                                                                       item.UnitId,
                                                                       item.UnitName,
                                                                       item.PCost,
                                                                       item.POQuantity,
                                                                       item.Remarks);

                }

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "ProjectInformationCosting.rpt"));

            rd.SetDataSource(dsProjInfoCost);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;
        }






        //Req No 
        [HttpPost]
        public JsonResult GetReqNo(int SiteId, int ProjectId)
        {
            List<SelectListItem> ReqList = new List<SelectListItem>();

            var requisitions = (from requisitionDet in db.Proc_RequisitionDet
                                join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                join project in db.Project on site.ProjectId equals project.Id
                                where project.Id == ProjectId && site.Id == SiteId && requisitionMas.Status == "A"
                                select requisitionMas).ToList().Distinct();

            foreach (var x in requisitions)
            {
                ReqList.Add(new SelectListItem { Text = x.Rcode, Value = x.Id.ToString() });
            }

            var result = new
            {
                Items = ReqList
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult MaterialRequisition()
        {

            ViewBag.RCode = new SelectList(db.Proc_RequisitionMas, "Id", "Rcode");
            var requisitionProjects = (from requisitionMas in db.Proc_RequisitionMas
                                       join procproject in db.ProcProject on requisitionMas.ProcProjectId equals procproject.Id
                                       where requisitionMas.ProcProjectId == procproject.Id && requisitionMas.Status == "A"
                                       select procproject).ToList();

            List<ProjectSite> sites = new List<ProjectSite>();

            foreach (var i in requisitionProjects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }
            ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");


            return View();
        }



        [HttpPost]
        public ActionResult MaterialRequisition(int? ProjectId, int? SiteId, int? RCode)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            MaterialRequisitionDetail dsMaterialRequisition = new MaterialRequisitionDetail();


            dsMaterialRequisition.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);

            //var matchingExistingReqNo = db.Proc_RequisitionMas.Where(x => x.Id == ReqNo).SingleOrDefault();
            //var REQNo = matchingExistingReqNo.Rcode;



            var projRequisitionMasters = (from reqDet in db.Proc_RequisitionDet
                                          join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                          join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                          join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                          join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                          join proj in db.Project on projSite.ProjectId equals proj.Id
                                          join projRes in db.ProjectResource on proj.Id equals projRes.ProjectId
                                          join siteRes in db.ProjectSiteResource on projSite.Id equals siteRes.ProjectSiteId
                                          join compResProj in db.CompanyResource on projRes.CompanyResourceId equals compResProj.Id
                                          join compResSite in db.CompanyResource on siteRes.CompanyResourceId equals compResSite.Id
                                          join items in db.Item on reqDet.ItemId equals items.Id
                                          where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id && reqMas.Status == "A" && reqDet.ItemId == procItem.ItemId
                                          select new
                                          {
                                              ProjectId = proj.Id,
                                              ProjectName = proj.Name,
                                              ProjectResId = projRes.ProjectId,
                                              ProjectResName = compResProj.Name,
                                              ProjectSiteResId = siteRes.ProjectSiteId,
                                              ProjectSiteResName = compResSite.Name,
                                              SiteId = projSite.Id,
                                              SiteName = projSite.Name,
                                              ProcProjId = procProj.Id,
                                              ProjCompResId = projRes.CompanyResourceId,
                                              SiteCompResId = siteRes.CompanyResourceId,
                                              ReqMasId = reqMas.Id,
                                              ReqMasProcProjId = reqMas.ProcProjectId,
                                              ReqMasRCode = reqMas.Rcode,
                                              ReqMasReqDate = reqMas.ReqDate,
                                              ReqMasRemarks = reqMas.Remarks,
                                              ReqMasStatus = reqMas.Status
                                          }).Distinct().ToList();


            if (ProjectId != null && SiteId != null && RCode != null)
            {
                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.ReqMasId == RCode).ToList();
            }
            else if (ProjectId != null && SiteId != null)
            {
                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).Distinct().ToList();
            }
            else if (ProjectId != null)
            {

                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            }







            foreach (var reqMaster in projRequisitionMasters)
            {


                dsMaterialRequisition.RequisitionMaster.AddRequisitionMasterRow(reqMaster.ReqMasId,
                                           reqMaster.ReqMasRCode,
                                           NullHelper.DateToString(reqMaster.ReqMasReqDate),
                                           reqMaster.ReqMasRemarks,
                                           reqMaster.ReqMasStatus,
                                           reqMaster.ProcProjId,
                                           reqMaster.ProjectId,
                                           reqMaster.SiteId,
                                           reqMaster.ProjectName,
                                           reqMaster.SiteName,
                                           reqMaster.ProjectResId,
                                           reqMaster.ProjectSiteResId,
                                           reqMaster.ProjectResName,
                                           reqMaster.ProjectSiteResName
                                          );


                var projReqDet = (from reqDet in db.Proc_RequisitionDet
                                  join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                  join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                  join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                  join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                  join proj in db.Project on projSite.ProjectId equals proj.Id
                                  join items in db.Item on procItem.ItemId equals items.Id
                                  join units in db.Unit on procItem.UnitId equals units.Id
                                  where reqDet.Proc_RequisitionMasId == reqMaster.ReqMasId && reqDet.ItemId == items.Id
                                  select new
                                  {
                                      ReqDetId = reqDet.Id,
                                      ReqDetMasId = reqMaster.ReqMasId,
                                      ItemId = reqDet.ItemId,
                                      ItemName = items.Name,
                                      ReqQty = reqDet.ReqQty,
                                      CStockQty = reqDet.CStockQty,
                                      RequiredDate = reqDet.RequiredDate,
                                      Remarks = reqDet.Remarks

                                  }).Distinct().ToList();





                foreach (var item in projReqDet)
                {
                    dsMaterialRequisition.RequisitionDetail.AddRequisitionDetailRow(item.ReqDetMasId,
                                                                       item.ItemId,
                                                                       item.ReqQty,
                                                                       item.CStockQty,
                                                                       NullHelper.DateToString(item.RequiredDate),
                                                                       item.Remarks,
                                                                       item.ItemName
                                                                        );

                }

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "MaterialRequisition.rpt"));

            rd.SetDataSource(dsMaterialRequisition);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;


        }


     
	 
	 	 
	 
        [HttpGet]
        public ActionResult PurchaseOrderReportGet(int? ProjectId, int? SiteId, int? PONo, int? VendorId)
        {
            return PurchaseOrderReport(ProjectId, SiteId, PONo, VendorId);
        }

        [HttpGet]
        public ActionResult PurchaseOrderReport()
        {
            //var requisitionProjects = (from requisitionMas in db.Proc_RequisitionMas
            //                           join procproject in db.ProcProject on requisitionMas.ProcProjectId equals procproject.Id
            //                           where requisitionMas.ProcProjectId == procproject.Id && requisitionMas.Status == "A"
            //                           select procproject).ToList();

            //List<ProjectSite> sites = new List<ProjectSite>();

            //foreach (var i in requisitionProjects)
            //{
            //    var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
            //    sites.Add(site);
            //}

            //List<Project> projects = new List<Project>();

            //foreach (var i in sites)
            //{
            //    var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
            //    projects.Add(proj);
            //}
            //ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            //ViewBag.SiteId = new SelectList(sites, "Id", "Name");


            var tenderProjects = (from tendarMas in db.Proc_TenderMas
                                  join tenderDet in db.Proc_TenderDet on tendarMas.Id equals tenderDet.Proc_TenderMasId
                                  join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                  join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                  join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                  join Site in db.ProjectSite on procProj.ProjectSiteId equals Site.Id
                                  join proj in db.Project on Site.ProjectId equals proj.Id
                                  where procProj.ProjectSiteId == Site.Id && tendarMas.isApproved == "A"
                                  select Site).ToList();

            List<ProjectSite> sites = new List<ProjectSite>();
            foreach (var i in tenderProjects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.Id);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();
            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }



            ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");



            ViewBag.PONo = new SelectList(db.Proc_RequisitionMas, "Id", "PONo");


            ViewBag.VendorId = new SelectList(db.Vendor, "Id", "Name");

            return View();
        }

        [HttpPost]
        public ActionResult PurchaseOrderReport(int? ProjectId, int? SiteId, int? PONo, int? VendorId)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            PurchaseOrderDetail dsPurchaseOrder = new PurchaseOrderDetail();


            dsPurchaseOrder.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);


            var purchaseOrderMasters = (from purDet in db.Proc_PurchaseOrderDet
                                        join purMas in db.Proc_PurchaseOrderMas on purDet.Proc_PurchaseOrderMasId equals purMas.Id
                                        join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
                                        join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                        join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                        join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                        join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                        join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                        join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                        join proj in db.Project on projSite.ProjectId equals proj.Id
                                        join vendors in db.Vendor on purMas.VendorId equals vendors.Id
                                        join items in db.Item on purDet.ItemId equals items.Id
                                        join units in db.Unit on procItem.UnitId equals units.Id
                                        where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id
                                        select new
                                        {
                                            PurchaseMasId = purMas.Id,
                                            PONo = purMas.PONo,
                                            PODate = purMas.PODate,
                                            VendorId = purMas.VendorId,
                                            VendorName = purMas.Vendor.Name,
                                            Proc_TenderMasId = tenderMas.Id,
                                            LeadTime = purMas.LeadTime,
                                            OrderTo = purMas.OrderTo,
                                            Attention = purMas.Attention,
                                            AttnCell = purMas.AttnCell,
                                            AttnEmail = purMas.AttnEmail,
                                            Subject = purMas.Subject,
                                            Content = purMas.Content,
                                            RecvConcernPerson = purMas.RecvConcernPerson,
                                            RecvContactPersonCell = purMas.RecvConcernPersonCell,
                                            POTotalAmt = purMas.POTotalAmt,
                                            ProjectId = proj.Id,
                                            SiteId = procItem.ProcProject.ProjectSiteId,
                                            ProjectName = procItem.ProcProject.ProjectSite.Project.Name,
                                            SiteName = procItem.ProcProject.ProjectSite.Name,
                                            TenderNo = tenderMas.TNo,
                                            //  VendorAddress = purMas.Vendor.Address,
                                            VendorAddress = (from purMas in db.Proc_PurchaseOrderMas
                                                             join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
                                                             join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                                             where purMas.VendorId == VendorId && purMas.Id == PONo
                                                             select purMas.Vendor.Address).Distinct().FirstOrDefault(),
                                            SiteAddress = reqMas.ProcProject.ProjectSite.Location
                                        }).Distinct().ToList();


            //var getVendorAddress = (from purMas in db.Proc_PurchaseOrderMas
            //                        join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
            //                        join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
            //                        where purMas.VendorId== VendorId && purMas.Id== PONo
            //                        select purMas.Vendor.Address).Distinct().SingleOrDefault();



            if (ProjectId != null && SiteId != null && PONo != null && VendorId != null)
            {
                purchaseOrderMasters = purchaseOrderMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.PurchaseMasId == PONo && x.VendorId == VendorId).Distinct().ToList();
            }
            else if (ProjectId != null && SiteId != null && VendorId != null)
            {
                purchaseOrderMasters = purchaseOrderMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.PurchaseMasId == PONo).Distinct().ToList();
            }
            else if (ProjectId != null && SiteId != null)
            {
                purchaseOrderMasters = purchaseOrderMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).Distinct().ToList();
            }
            else if (ProjectId != null)
            {

                purchaseOrderMasters = purchaseOrderMasters.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            }






            foreach (var purchaseMaster in purchaseOrderMasters)
            {
                dsPurchaseOrder.PurchaseOrderMaster.AddPurchaseOrderMasterRow(purchaseMaster.PurchaseMasId,
                                           purchaseMaster.PONo,
                                           NullHelper.DateToString(purchaseMaster.PODate),
                                           purchaseMaster.VendorId,
                                           purchaseMaster.VendorName,
                                           purchaseMaster.Proc_TenderMasId,
                                           purchaseMaster.LeadTime,
                                           purchaseMaster.OrderTo,
                                           purchaseMaster.Attention,
                                           purchaseMaster.AttnCell,
                                           purchaseMaster.AttnEmail,
                                           purchaseMaster.Subject,
                                           purchaseMaster.Content,
                                           purchaseMaster.RecvConcernPerson,
                                           purchaseMaster.RecvContactPersonCell,
                                           purchaseMaster.POTotalAmt,
                                           purchaseMaster.ProjectId,
                                           purchaseMaster.SiteId,
                                           purchaseMaster.ProjectName,
                                           purchaseMaster.SiteName,
                                           purchaseMaster.TenderNo,
                                           purchaseMaster.VendorAddress,
                                           // getVendorAddress,
                                           purchaseMaster.SiteAddress
                                          );


                var purchaseDet = (from purDet in db.Proc_PurchaseOrderDet
                                   join purMas in db.Proc_PurchaseOrderMas on purDet.Proc_PurchaseOrderMasId equals purMas.Id
                                   join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
                                   join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                   join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                   join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                   join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                   join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                   join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                   join proj in db.Project on projSite.ProjectId equals proj.Id
                                   join items in db.Item on procItem.ItemId equals items.Id
                                   join units in db.Unit on procItem.UnitId equals units.Id
                                   where purDet.Proc_PurchaseOrderMasId == purchaseMaster.PurchaseMasId && purDet.ItemId == items.Id /*&& tenderDet.Proc_RequisitionDetId == reqDet.Id*/ && purDet.ItemId == reqDet.ItemId
                                   select new
                                   {
                                       PurchaseDetId = purDet.Id,
                                       Proc_PurchaseOrderMasId = purMas.Id,
                                       ItemId = purDet.ItemId,
                                       ItemName = purDet.Item.Name,
                                       POQty = purDet.POQty,
                                       POAmt = purDet.POAmt,
                                       PORcv = purDet.PORcv,
                                       Size = reqDet.Size,
                                       UnitPrice = tenderDet.TQPrice,
                                       UnitId = procItem.UnitId,
                                       UnitName = procItem.Unit.Name
                                   }).Distinct().ToList();


                //var findUnitPrice = (from purDet in db.Proc_PurchaseOrderDet
                //                     join purMas in db.Proc_PurchaseOrderMas on purDet.Proc_PurchaseOrderMasId equals purMas.Id
                //                     join tenderMas in db.Proc_TenderDet on purMas.Proc_TenderMasId equals tenderMas.Id
                //                     join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                //                     join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                //                     where tenderDet.Proc_TenderMasId == tenderMas.Id && purMas.Id == purchaseMaster.PurchaseMasId && purDet.ItemId == reqDet.ItemId
                //                     select tenderDet.TQPrice).Distinct().FirstOrDefault();

                foreach (var item in purchaseDet.Distinct())
                {
                    dsPurchaseOrder.PurchaseOrderDetails.AddPurchaseOrderDetailsRow(item.PurchaseDetId,
                                                                       item.Proc_PurchaseOrderMasId,
                                                                       item.ItemId,
                                                                       item.ItemName,
                                                                       item.POQty,
                                                                       item.POAmt,
                                                                       item.PORcv,
                                                                       item.Size,
                                                                       item.UnitName,
                                                                       item.UnitPrice,
                                                                       item.UnitId
                                                                        );

                }

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "PurchaseOrderReport.rpt"));

            rd.SetDataSource(dsPurchaseOrder);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;

        }




	 










        [HttpGet]
        public ActionResult MaterialRequisitionSummary()
        {

            ViewBag.RCode = new SelectList(db.Proc_RequisitionMas, "Id", "Rcode");
            var requisitionProjects = (from requisitionMas in db.Proc_RequisitionMas
                                       join procproject in db.ProcProject on requisitionMas.ProcProjectId equals procproject.Id
                                       where requisitionMas.ProcProjectId == procproject.Id && requisitionMas.Status == "A"
                                       select procproject).ToList();

            List<ProjectSite> sites = new List<ProjectSite>();

            foreach (var i in requisitionProjects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }

            ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");

            return View();
        }


        [HttpPost]
        public ActionResult MaterialRequisitionSummary(int? ProjectId, int? SiteId, int? RCode)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            MaterialRequisitionSummary dsMaterialRequisition = new MaterialRequisitionSummary();


            dsMaterialRequisition.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);

            //var matchingExistingReqNo = db.Proc_RequisitionMas.Where(x => x.Id == ReqNo).SingleOrDefault();
            //var REQNo = matchingExistingReqNo.Rcode;



            var projRequisitionMasters = (from reqDet in db.Proc_RequisitionDet
                                          join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                          join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                          join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                          join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                          join proj in db.Project on projSite.ProjectId equals proj.Id
                                          join projRes in db.ProjectResource on proj.Id equals projRes.ProjectId
                                          join siteRes in db.ProjectSiteResource on projSite.Id equals siteRes.ProjectSiteId
                                          join compResProj in db.CompanyResource on projRes.CompanyResourceId equals compResProj.Id
                                          join compResSite in db.CompanyResource on siteRes.CompanyResourceId equals compResSite.Id
                                          where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id
                                          select new
                                          {
                                              ProjectId = proj.Id,
                                              ProjectName = proj.Name,
                                              ProjectResId = projRes.ProjectId,
                                              ProjectResName = compResProj.Name,
                                              ProjectSiteResId = siteRes.ProjectSiteId,
                                              ProjectSiteResName = compResSite.Name,
                                              SiteId = projSite.Id,
                                              SiteName = projSite.Name,
                                              ProcProjId = procProj.Id,
                                              ProjCompResId = projRes.CompanyResourceId,
                                              SiteCompResId = siteRes.CompanyResourceId,
                                              ReqMasId = reqMas.Id,
                                              ReqMasProcProjId = reqMas.ProcProjectId,
                                              ReqMasRCode = reqMas.Rcode,
                                              ReqMasReqDate = reqMas.ReqDate,
                                              ReqMasRemarks = reqMas.Remarks,
                                              ReqMasStatus = reqMas.Status
                                          }).Distinct().ToList();


            if (ProjectId != null && SiteId != null && RCode != null)
            {
                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.ReqMasId == RCode).ToList();
            }
            else if (ProjectId != null && SiteId != null)
            {
                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            }
            else if (ProjectId != null)
            {

                projRequisitionMasters = projRequisitionMasters.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            }


            Dictionary<string, int> shoppingDictionary = new Dictionary<string, int>();

            foreach (var reqMaster in projRequisitionMasters)
            {


                dsMaterialRequisition.RequisitionMaster.AddRequisitionMasterRow(reqMaster.ReqMasId,
                                           reqMaster.ReqMasRCode,
                                           NullHelper.DateToString(reqMaster.ReqMasReqDate),
                                           reqMaster.ReqMasRemarks,
                                           reqMaster.ReqMasStatus,
                                           reqMaster.ProcProjId,
                                           reqMaster.ProjectId,
                                           reqMaster.SiteId,
                                           reqMaster.ProjectName,
                                           reqMaster.SiteName,
                                           reqMaster.ProjectResId,
                                           reqMaster.ProjectSiteResId,
                                           reqMaster.ProjectResName,
                                           reqMaster.ProjectSiteResName
                                          );




                var projReqDet = (from reqDet in db.Proc_RequisitionDet
                                  join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                  join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                  join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                  join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                  join proj in db.Project on projSite.ProjectId equals proj.Id
                                  join items in db.Item on procItem.ItemId equals items.Id
                                  join units in db.Unit on procItem.UnitId equals units.Id
                                  where reqDet.Proc_RequisitionMasId == reqMaster.ReqMasId && reqDet.ItemId == items.Id && projSite.Id == reqMaster.SiteId
                                  select new
                                  {
                                      ReqDetId = reqDet.Id,
                                      ReqDetMasId = reqMaster.ReqMasId,
                                      ItemId = reqDet.ItemId,
                                      ItemName = items.Name,
                                      ReqQty = reqDet.ReqQty,
                                      CStockQty = reqDet.CStockQty,
                                      RequiredDate = reqDet.RequiredDate,
                                      Remarks = reqDet.Remarks,
                                      TotalMaterialsCost = procItem.PCost,
                                      TotalMaterialsRequired = procItem.PQuantity,
                                      UnitId = units.Id,
                                      UnitName = units.Name
                                  }).Distinct().ToList();




                foreach (var item in projReqDet)
                {

                    var totalReceived = (from entryDet in db.Proc_MaterialEntryDet
                                         join purchaseDet in db.Proc_PurchaseOrderDet on entryDet.Proc_PurchaseOrderDetId equals purchaseDet.Id
                                         join purchaseMas in db.Proc_PurchaseOrderMas on purchaseDet.Proc_PurchaseOrderMasId equals purchaseMas.Id
                                         join tenderDet in db.Proc_TenderDet on purchaseMas.Proc_TenderMasId equals tenderDet.Proc_TenderMasId
                                         join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                                         join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                         join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                         join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                         where purchaseDet.ItemId == item.ItemId && site.Id == reqMaster.SiteId
                                         select entryDet).Distinct().ToList();

                    decimal receivedQuantity = 0;
                    decimal remainingQty = 0;
                    decimal total = 0;
                    if (totalReceived == null)
                    {
                        receivedQuantity = 0;
                    }
                    else
                    {
                        foreach (var i in totalReceived)
                        {
                            total = total + i.EntryQty;
                        }

                        receivedQuantity = total;

                    }

                    remainingQty = item.TotalMaterialsRequired - total;

                    var totalRequisitionQty = (from reqDet in db.Proc_RequisitionDet
                                               join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                               join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                               join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                               join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                               join proj in db.Project on projSite.ProjectId equals proj.Id
                                               join items in db.Item on procItem.ItemId equals items.Id
                                               join units in db.Unit on procItem.UnitId equals units.Id
                                               where reqDet.ItemId == item.ItemId && projSite.Id == reqMaster.SiteId
                                               select reqDet).Distinct().ToList();

                    decimal totalReqQty = 0;
                    if (totalRequisitionQty == null)
                    {
                        totalReqQty = 0;
                    }
                    else
                    {
                        foreach (var i in totalRequisitionQty)
                        {
                            totalReqQty = totalReqQty + i.ReqQty;
                        }

                    }

                    decimal currentReq = totalReqQty - receivedQuantity;

                    //if (!shoppingDictionary.ContainsKey(item.ItemName))
                    //{
                    //    shoppingDictionary.Add(item.ItemName, reqMaster.SiteId);


                    dsMaterialRequisition.RequisitionDetail.AddRequisitionDetailRow(item.ReqDetMasId,
                           item.ItemId,
                           item.ReqQty,
                           item.CStockQty,
                           NullHelper.DateToString(item.RequiredDate),
                           item.Remarks,
                           item.ItemName,
                           item.UnitId,
                           item.UnitName,
                           item.TotalMaterialsCost,
                           item.TotalMaterialsRequired,
                           total,
                           remainingQty,
                           currentReq,
                           totalReqQty
                            );

                    //}
                    //else
                    //{
                    //    int count = 0;
                    //    shoppingDictionary.TryGetValue(item.ItemName, out count);
                    //    shoppingDictionary.Remove(item.ItemName);
                    //    shoppingDictionary.Add(item.ItemName, count + 1);
                    //}






                }

            }


            //foreach (var item in dsMaterialRequisition.RequisitionDetail.Distinct())
            //{
            //    dsMaterialRequisition.RequisitionDetailDistinct.AddRequisitionDetailDistinctRow(
            //                                                          item.Proc_RequisitionMasId,
            //                                                          item.ItemId,
            //                                                          item.ReqQty,
            //                                                          item.CStockQty,
            //                                                          item.ItemName,
            //                                                          item.UnitId,
            //                                                          item.UnitName,
            //                                                          item.TotalMaterialsCost,
            //                                                          item.TotalRequiredMaterialSQty,
            //                                                          item.ReceivedQty,
            //                                                          item.RemainingQty,
            //                                                          item.CurrentRequisition,
            //                                                          item.TotalRequisitionQty
            //                                                           );
            //}


            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "MaterialRequisitionSummary.rpt"));

            rd.SetDataSource(dsMaterialRequisition);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;


        }






        [HttpGet]
        public ActionResult MaterialsEntry()
        {
            //var projects = (from entryMas in db.Proc_MaterialEntryMas
            //                join procProject in db.ProcProject on entryMas.ProcProjectId equals procProject.Id
            //                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
            //                join project in db.Project on site.ProjectId equals project.Id
            //                select project).Distinct().ToList();

            //List<SelectListItem> projectList = new List<SelectListItem>();

            //foreach (var x in projects)
            //{
            //    projectList.Add(new SelectListItem { Text = x.Name, Value = x.Id.ToString() });
            //}

            //ViewBag.ProjectId = projectList;
            //ViewBag.SiteId = new SelectList(db.ProjectSite, "Id", "Name");

            var EntryProject = (from purchaseMas in db.Proc_PurchaseOrderMas
                                join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                                join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                join project in db.Project on site.ProjectId equals project.Id
                                where purchaseMas.Proc_TenderMasId == tenderMas.Id && purchaseMas.VendorId == tenderDet.VendorId
                                select project).Distinct().ToList();

            var procprojects = db.ProcProject.ToList();
            List<ProjectSite> sites = new List<ProjectSite>();
            foreach (var i in procprojects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }

          
            ViewBag.ProjectId = new SelectList(EntryProject.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");




            return View();
        }



        [HttpPost]
        public ActionResult MaterialsEntry(int? ProjectId, int? SiteId)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            MaterialsEntryDS dsMaterialsEntry = new MaterialsEntryDS();


            dsMaterialsEntry.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);

            //var matchingExistingReqNo = db.Proc_RequisitionMas.Where(x => x.Id == ReqNo).SingleOrDefault();
            //var REQNo = matchingExistingReqNo.Rcode;



            var projEntryMasters = (from entryDet in db.Proc_MaterialEntryDet
                                    join entryMas in db.Proc_MaterialEntryMas on entryDet.Proc_MaterialEntryMasId equals entryMas.Id
                                    join purchaseDet in db.Proc_PurchaseOrderDet on entryDet.Proc_PurchaseOrderDetId equals purchaseDet.Id
                                    join purchaseMas in db.Proc_PurchaseOrderMas on purchaseDet.Proc_PurchaseOrderMasId equals purchaseMas.Id
                                    join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                    join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                    join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                    join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                    join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                    join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                    join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                    join proj in db.Project on projSite.ProjectId equals proj.Id
                                    where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id
                                    select new
                                    {
                                        ProjectId = proj.Id,
                                        ProjectName = proj.Name,
                                        SiteId = projSite.Id,
                                        SiteName = projSite.Name,
                                        EntryMasId = entryMas.Id,
                                        EntryDate = entryMas.EDate
                                    }).Distinct().ToList();


            if (ProjectId != null && SiteId != null)
            {
                projEntryMasters = projEntryMasters.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            }
            else if (ProjectId != null)
            {

                projEntryMasters = projEntryMasters.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            }


            //Dictionary<string, int> shoppingDictionary = new Dictionary<string, int>();

            foreach (var entryMaster in projEntryMasters)
            {
                var edate = "";
                if (entryMaster.EntryDate == null)
                {
                    edate = "";
                }
                else
                {
                    edate = entryMaster.EntryDate.ToString();
                }

                dsMaterialsEntry.MaterialsEntryMaster.AddMaterialsEntryMasterRow(
                                       entryMaster.ProjectId,
                                       entryMaster.ProjectName,
                                       entryMaster.SiteId,
                                       entryMaster.SiteName,
                                       entryMaster.EntryMasId,
                                       edate
                                      );



                var projReqDet = (from entryDet in db.Proc_MaterialEntryDet
                                  join entryMas in db.Proc_MaterialEntryMas on entryDet.Proc_MaterialEntryMasId equals entryMas.Id
                                  join purchaseDet in db.Proc_PurchaseOrderDet on entryDet.Proc_PurchaseOrderDetId equals purchaseDet.Id
                                  join purchaseMas in db.Proc_PurchaseOrderMas on purchaseDet.Proc_PurchaseOrderMasId equals purchaseMas.Id
                                  join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                  join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                  join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                  join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                  join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                  join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                  join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                  join items in db.Item on procItem.ItemId equals items.Id
                                  join units in db.Unit on procItem.UnitId equals units.Id
                                  where entryDet.Proc_PurchaseOrderDetId == purchaseDet.Id && projSite.Id == entryMaster.SiteId && entryDet.Proc_PurchaseOrderDet.ItemId == items.Id
                                  //where entryDet.Proc_MaterialEntryMasId == entryMaster.EntryMasId && reqDet.ItemId == items.Id && projSite.Id == entryMaster.SiteId
                                  select new
                                  {
                                      EntryDetId = entryDet.Id,
                                      //EntryMasId = entryMaster.EntryMasId,
                                      ItemId = items.Id,
                                      ItemName = items.Name,
                                      ReqQty = reqDet.ReqQty,
                                      EntryDate = entryMas.EDate,
                                      TotalMaterialsRequired = procItem.PQuantity,
                                      UnitId = units.Id,
                                      UnitName = units.Name,
                                      ReceivedQty = entryDet.EntryQty
                                  }).AsEnumerable().Distinct().ToList();


                foreach (var item in projReqDet.Distinct())
                {
                    decimal remainingQty = 0;

                    remainingQty = item.TotalMaterialsRequired - item.ReceivedQty;

                    var EDate = "";
                    if (item.EntryDate != null)
                    {
                        EDate = NullHelper.DateToString(item.EntryDate);
                    }


                    dsMaterialsEntry.MaterialEntryDetailDistinct.AddMaterialEntryDetailDistinctRow(
                           entryMaster.SiteId,
                           item.EntryDetId,
                           item.ItemId,
                           item.ItemName,
                           item.UnitId,
                           item.UnitName,
                           item.TotalMaterialsRequired,
                           EDate,
                           item.ReceivedQty,
                           remainingQty
                          );


                }

            }




            foreach (var c in (dsMaterialsEntry.MaterialEntryDetailDistinct).AsEnumerable().Distinct())
            {

                dsMaterialsEntry.MaterialEntryDetail.AddMaterialEntryDetailRow(
                           c.SiteId,
                           c.EntryDetailId,
                           c.ItemId,
                           c.ItemName,
                           c.UnitId,
                           c.UnitName,
                           c.Total_Required_Materials_Qty,
                           c.ReceivedDate,
                           c.ReceivedQty,
                           c.RemainingQty
                    );
            }


            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "MaterialsEntry.rpt"));

            rd.SetDataSource(dsMaterialsEntry);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;


        }


        [HttpGet]
        public ActionResult MaterialRequisitionList()
        {

            ViewBag.RCode = new SelectList(db.Proc_RequisitionMas, "Id", "Rcode");

            var requisitionProjects = (from requisitionMas in db.Proc_RequisitionMas
                                       join procproject in db.ProcProject on requisitionMas.ProcProjectId equals procproject.Id
                                       where requisitionMas.ProcProjectId == procproject.Id && requisitionMas.Status == "A"
                                       select procproject).ToList();

            List<ProjectSite> sites = new List<ProjectSite>();

            foreach (var i in requisitionProjects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }

            ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");


            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "Approved", Value = "0" });
            items.Add(new SelectListItem { Text = "Pending", Value = "1" });
            //items.Add(new SelectListItem { Text = "Comedy", Value = "2", Selected = true });
            ViewBag.Status = items;

            return View();
        }

        [HttpPost]
        public ActionResult MaterialRequisitionList(int? ProjectId, int? SiteId, int? RCode, int? Status)
        {

            var companyInfo = db.CompanyInformation.SingleOrDefault();
            MaterialRequisitionListDS dsMaterialsRequisition = new MaterialRequisitionListDS();


            dsMaterialsRequisition.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);


            //var requisitionPending = (from reqMas in db.Proc_RequisitionMas
            //                          join reqDet in db.Proc_RequisitionDet on reqMas.Id equals reqDet.Proc_RequisitionMasId
            //                          join procProject in db.ProcProject on reqMas.ProcProjectId equals procProject.Id
            //                          join procProjectItem in db.ProcProjectItem on procProject.Id equals procProjectItem.ProcProjectId
            //                          join projSite in db.ProjectSite on procProject.ProjectSiteId equals projSite.Id
            //                          join proj in db.Project on projSite.ProjectId equals proj.Id
            //                          join items in db.Item on procProjectItem.ItemId equals items.Id
            //                          join units in db.Unit on procProjectItem.UnitId equals units.Id
            //                          where reqDet.ItemId == items.Id && reqMas.Status != "A"
            //                          select new
            //                          {
            //                              Status = reqMas.Status,
            //                              RequisitionMasId = reqMas.Id,
            //                              ProjectId = proj.Id,
            //                              ProjectName = proj.Name,
            //                              SiteId = projSite.Id,
            //                              SiteName = projSite.Name,
            //                              RequisitionDate = reqMas.ReqDate,
            //                              RequiredDate = reqDet.RequiredDate,
            //                              ReqNo = reqMas.Rcode,
            //                              ItemId = items.Id,
            //                              ItemName = items.Name,
            //                              Qty = reqDet.ReqQty,
            //                              unitId = units.Id,
            //                              unitName = units.Name
            //                          }).Distinct().ToList();


            //if (ProjectId != null && SiteId != null && RCode != null)
            //{
            //    requisitionPending = requisitionPending.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode).ToList();
            //}
            //else if (ProjectId != null && SiteId != null)
            //{
            //    requisitionPending = requisitionPending.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            //}
            //else if (ProjectId != null)
            //{

            //    requisitionPending = requisitionPending.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            //}


            //foreach (var item in requisitionPending)
            //{
            //    dsMaterialsRequisition.RequisitionPendingList.AddRequisitionPendingListRow(
            //        item.RequisitionMasId,
            //        item.ProjectId,
            //        item.ProjectName,
            //        item.SiteId,
            //        item.SiteName,
            //        NullHelper.DateToString(item.RequisitionDate),
            //        NullHelper.DateToString(item.RequiredDate),
            //        item.ReqNo,
            //        item.ItemId,
            //        item.ItemName,
            //        item.Qty,
            //        item.unitId,
            //        item.unitName,
            //        item.Status
            //        );
            //}




            //var requisitionApproved = (from reqMas in db.Proc_RequisitionMas
            //                           join reqDet in db.Proc_RequisitionDet on reqMas.Id equals reqDet.Proc_RequisitionMasId
            //                           join procProject in db.ProcProject on reqMas.ProcProjectId equals procProject.Id
            //                           join procProjectItem in db.ProcProjectItem on procProject.Id equals procProjectItem.ProcProjectId
            //                           join projSite in db.ProjectSite on procProject.ProjectSiteId equals projSite.Id
            //                           join proj in db.Project on projSite.ProjectId equals proj.Id
            //                           join items in db.Item on procProjectItem.ItemId equals items.Id
            //                           join units in db.Unit on procProjectItem.UnitId equals units.Id
            //                           where reqDet.ItemId == items.Id && reqMas.Status == "A"
            //                           select new
            //                           {
            //                               Status = reqMas.Status,
            //                               RequisitionMasId = reqMas.Id,
            //                               ProjectId = proj.Id,
            //                               ProjectName = proj.Name,
            //                               SiteId = projSite.Id,
            //                               SiteName = projSite.Name,
            //                               RequisitionDate = reqMas.ReqDate,
            //                               RequiredDate = reqDet.RequiredDate,
            //                               ReqNo = reqMas.Rcode,
            //                               ItemId = items.Id,
            //                               ItemName = items.Name,
            //                               Qty = reqDet.ReqQty,
            //                               unitId = units.Id,
            //                               unitName = units.Name
            //                           }).Distinct().OrderBy(x=>x.Status).ToList();


            //if (ProjectId != null && SiteId != null && RCode != null)
            //{
            //    requisitionApproved = requisitionApproved.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode).ToList();
            //}
            //else if (ProjectId != null && SiteId != null)
            //{
            //    requisitionApproved = requisitionApproved.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            //}
            //else if (ProjectId != null)
            //{

            //    requisitionApproved = requisitionApproved.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            //}



            //foreach (var item in requisitionApproved)
            //{
            //    dsMaterialsRequisition.RequisitionApprovedList.AddRequisitionApprovedListRow(
            //        item.RequisitionMasId,
            //        item.ProjectId,
            //        item.ProjectName,
            //        item.SiteId,
            //        item.SiteName,
            //        NullHelper.DateToString(item.RequisitionDate),
            //        NullHelper.DateToString(item.RequiredDate),
            //        item.ReqNo,
            //        item.ItemId,
            //        item.ItemName,
            //        item.Qty,
            //        item.unitId,
            //        item.unitName,
            //        item.Status
            //        );
            //}



            var requisitionList = (from reqMas in db.Proc_RequisitionMas
                                   join reqDet in db.Proc_RequisitionDet on reqMas.Id equals reqDet.Proc_RequisitionMasId
                                   join procProject in db.ProcProject on reqMas.ProcProjectId equals procProject.Id
                                   join procProjectItem in db.ProcProjectItem on procProject.Id equals procProjectItem.ProcProjectId
                                   join projSite in db.ProjectSite on procProject.ProjectSiteId equals projSite.Id
                                   join proj in db.Project on projSite.ProjectId equals proj.Id
                                   join items in db.Item on procProjectItem.ItemId equals items.Id
                                   join units in db.Unit on procProjectItem.UnitId equals units.Id
                                   where reqDet.ItemId == items.Id
                                   select new
                                   {
                                       Status = reqMas.Status,
                                       RequisitionMasId = reqMas.Id,
                                       ProjectId = proj.Id,
                                       ProjectName = proj.Name,
                                       SiteId = projSite.Id,
                                       SiteName = projSite.Name,
                                       RequisitionDate = reqMas.ReqDate,
                                       RequiredDate = reqDet.RequiredDate,
                                       ReqNo = reqMas.Rcode,
                                       ItemId = items.Id,
                                       ItemName = items.Name,
                                       Qty = reqDet.ReqQty,
                                       unitId = units.Id,
                                       unitName = units.Name
                                   }).Distinct().OrderBy(x => x.Status).ToList();

            var ReqStatus = "";
            if (Status == 0)
            {
                ReqStatus = "A";
            }
            else
            {
                ReqStatus = "N";
            }


            if (ProjectId != null && SiteId != null && RCode != null && Status != null)
            {

                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode && x.Status == ReqStatus).ToList();
            }
            else if (ProjectId != null && SiteId != null && RCode != null)
            {
                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode).ToList();
            }
            else if (ProjectId != null && SiteId != null && Status != null)
            {
                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.Status == ReqStatus).ToList();
            }
            else if (ProjectId != null && SiteId != null)
            {
                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            }
            else if (ProjectId != null && Status != null)
            {

                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.Status == ReqStatus).Distinct().ToList();
            }
            else if (ProjectId != null)
            {
                requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            }
            else
            {
                requisitionList = requisitionList.OrderBy(x => x.SiteId).OrderBy(y => y.Status).ToList();
            }



            foreach (var item in requisitionList)
            {
                dsMaterialsRequisition.RequisitionApprovedList.AddRequisitionApprovedListRow(
                    item.RequisitionMasId,
                    item.ProjectId,
                    item.ProjectName,
                    item.SiteId,
                    item.SiteName,
                    NullHelper.DateToString(item.RequisitionDate),
                    NullHelper.DateToString(item.RequiredDate),
                    item.ReqNo,
                    item.ItemId,
                    item.ItemName,
                    item.Qty,
                    item.unitId,
                    item.unitName,
                    item.Status
                    );
            }



            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "MaterialRequisitionList.rpt"));

            rd.SetDataSource(dsMaterialsRequisition);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;
        }

        [HttpGet]
        public ActionResult VendorPayment()
        {
            //var tenderProjects = (from tendarMas in db.Proc_TenderMas
            //                      join tenderDet in db.Proc_TenderDet on tendarMas.Id equals tenderDet.Proc_TenderMasId
            //                      join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
            //                      join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
            //                      join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
            //                      join Site in db.ProjectSite on procProj.ProjectSiteId equals Site.Id
            //                      join proj in db.Project on Site.ProjectId equals proj.Id
            //                      where procProj.ProjectSiteId == Site.Id && tendarMas.isApproved == "A"
            //                      select Site).ToList();

            //List<ProjectSite> sites = new List<ProjectSite>();
            //foreach (var i in tenderProjects)
            //{
            //    var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.Id);
            //    sites.Add(site);
            //}

            //List<Project> projects = new List<Project>();
            //foreach (var i in sites)
            //{
            //    var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
            //    projects.Add(proj);
            //}



            //ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            //ViewBag.SiteId = new SelectList(sites, "Id", "Name");



            var EntryProject = (from purchaseMas in db.Proc_PurchaseOrderMas
                                    //   join purchaseDet in db.Proc_PurchaseOrderDet on purchaseMas.Id equals purchaseDet.Proc_PurchaseOrderMasId
                                join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                                join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                join project in db.Project on site.ProjectId equals project.Id
                                where purchaseMas.Proc_TenderMasId == tenderMas.Id && purchaseMas.VendorId == tenderDet.VendorId
                                select project).Distinct().ToList();

            var procprojects = db.ProcProject.ToList();
            List<ProjectSite> sites = new List<ProjectSite>();
            foreach (var i in procprojects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }


            ViewBag.ProjectId = new SelectList(EntryProject.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");


            var vendors = (from vendorMas in db.Proc_VendorPaymentMas
                           join vendor in db.Vendor on vendorMas.VendorId equals vendor.Id
                           select vendor
                           ).Distinct().ToList();



            ViewBag.VendorId = new SelectList(vendors, "Id", "Name");
            //List<SelectListItem> items = new List<SelectListItem>();
            //items.Add(new SelectListItem { Text = "Approved", Value = "0" });
            //items.Add(new SelectListItem { Text = "Pending", Value = "1" });
            //ViewBag.Status = items;

            return View();
        }


        [HttpPost]
        public ActionResult VendorPayment(int? vendorId, int? ProjectId, int? SiteId)
        {

            var companyInfo = db.CompanyInformation.SingleOrDefault();
            VendorPaymentDS dsVendorPaymentDS = new VendorPaymentDS();


            dsVendorPaymentDS.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);




            var vendorMasterList = (from vendorMaster in db.Proc_VendorPaymentMas
                                    join vendorDet in db.Proc_VendorPaymentDet on vendorMaster.Id equals vendorDet.Proc_VendorPaymentMasId
                                    join vendor in db.Vendor on vendorMaster.VendorId equals vendor.Id
                                    where vendorMaster.VendorId == vendor.Id
                                    select new
                                    {
                                        VendorId = vendorMaster.VendorId,
                                        VendorName = vendor.Name,
                                        VendorMasterId = vendorMaster.Id
                                    }).Distinct().ToList();



            if (vendorId != null)
            {
                vendorMasterList = vendorMasterList.Where(x => x.VendorId == vendorId).ToList();
            }

            foreach (var vendorMaster in vendorMasterList)
            {

                dsVendorPaymentDS.VendorMaster.AddVendorMasterRow(
                    vendorMaster.VendorMasterId,
                    vendorMaster.VendorId,
                    vendorMaster.VendorName
                    );

                //var detList = (from vendorMas in db.Proc_VendorPaymentMas
                //               join vendorDet in db.Proc_VendorPaymentDet on vendorMas.Id equals vendorDet.Proc_VendorPaymentMasId
                //               join entryDet in db.Proc_MaterialEntryDet on vendorDet.Proc_MaterialEntryDetId equals entryDet.Id
                //               join entryMas in db.Proc_MaterialEntryMas on entryDet.Proc_MaterialEntryMasId equals entryMas.Id
                //               join purchaseDet in db.Proc_PurchaseOrderDet on entryDet.Proc_PurchaseOrderDetId equals purchaseDet.Id
                //               join purchaseMas in db.Proc_PurchaseOrderMas on purchaseDet.Proc_PurchaseOrderMasId equals purchaseMas.Id
                //               where vendorDet.Proc_VendorPaymentMasId == vendorMaster.VendorMasterId
                //               select new
                //               {
                //                   VendorMasId = vendorMaster.VendorMasterId,
                //                   ProjectId = vendorMaster.VendorMasterId,
                //                   ProjectName = vendorMaster.VendorMasterId,
                //                   SiteId = vendorMaster.VendorMasterId,
                //                   SiteName = vendorMaster.VendorMasterId,
                //                   ItemId = vendorMaster.VendorMasterId,
                //                   ItemName = vendorMaster.VendorMasterId,
                //                   PaymentDate = vendorMas.VPDate,
                //                   PONo = vendorMaster.VendorMasterId,
                //                   Payment = vendorDet.PayAmt,
                //                   EntryQty = entryDet.EntryQty,
                //                   UnitPrice = vendorDet.PayAmt
                //               }).Distinct().ToList();

                var vendorDetList = (from vendorMas in db.Proc_VendorPaymentMas
                                     join vendorDet in db.Proc_VendorPaymentDet on vendorMas.Id equals vendorDet.Proc_VendorPaymentMasId
                                     join entryDet in db.Proc_MaterialEntryDet on vendorDet.Proc_MaterialEntryDetId equals entryDet.Id
                                     //join entryMas in db.Proc_MaterialEntryMas on entryDet.Proc_MaterialEntryMasId equals entryMas.Id
                                     join purchaseDet in db.Proc_PurchaseOrderDet on entryDet.Proc_PurchaseOrderDetId equals purchaseDet.Id
                                     join purchaseMas in db.Proc_PurchaseOrderMas on purchaseDet.Proc_PurchaseOrderMasId equals purchaseMas.Id
                                     join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                     join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                     join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                                     join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                     join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                     join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                     join project in db.Project on site.ProjectId equals project.Id
                                     join items in db.Item on purchaseDet.ItemId equals items.Id
                                     where vendorDet.Proc_VendorPaymentMasId == vendorMaster.VendorMasterId
                                     && tenderDet.VendorId == vendorMaster.VendorId
                                     && tenderDet.Proc_RequisitionDet.ItemId == items.Id 
                                     select new
                                     {
                                         VendorMasId = vendorMaster.VendorMasterId,
                                         ProjectId = project.Id,
                                         ProjectName = project.Name,
                                         SiteId = site.Id,
                                         SiteName = site.Name,
                                         ItemId = purchaseDet.Id,
                                         ItemName = items.Name,
                                         PaymentDate = vendorMas.VPDate,
                                         PONo = purchaseMas.PONo,
                                         Payment = vendorDet.PayAmt,
                                         EntryQty = entryDet.EntryQty,
                                         EntryDetailId = entryDet.Id,
                                         UnitPrice = tenderDet.TQPrice
                                     }).Distinct().ToList();


                if (ProjectId != null && SiteId != null)
                {
                    vendorDetList = vendorDetList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
                }
                else if (ProjectId != null)
                {
                    vendorDetList = vendorDetList.Where(x => x.ProjectId == ProjectId).ToList();
                }

                foreach (var i in vendorDetList)
                {
                    //var UnitPrice = db.Proc_TenderDet.SingleOrDefault(x => x.Id == i.TenderDetId).TQPrice;

                    var TotalPayable = i.EntryQty * i.UnitPrice;
                    //var TotalPayable = i.EntryQty * UnitPrice;

                    dsVendorPaymentDS.VendorDetails.AddVendorDetailsRow(
                        i.VendorMasId,
                        i.ProjectId,
                        i.ProjectName.ToString(),
                        i.SiteId,
                        i.SiteName.ToString(),
                        i.ItemId,
                        i.ItemName.ToString(),
                        NullHelper.DateToString(i.PaymentDate),
                        i.PONo,
                        TotalPayable,
                        i.Payment,
                        TotalPayable - i.Payment,
                        TotalPayable
                        );
                }


            }
            //if (ProjectId != null && SiteId != null && RCode != null && Status != null)
            //{

            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode && x.Status == ReqStatus).ToList();
            //}
            //else if (ProjectId != null && SiteId != null && RCode != null)
            //{
            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.RequisitionMasId == RCode).ToList();
            //}
            //else if (ProjectId != null && SiteId != null && Status != null)
            //{
            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.Status == ReqStatus).ToList();
            //}
            //else if (ProjectId != null && SiteId != null)
            //{
            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            //}
            //else if (ProjectId != null && Status != null)
            //{

            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId && x.Status == ReqStatus).Distinct().ToList();
            //}
            //else if (ProjectId != null)
            //{
            //    requisitionList = requisitionList.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
            //}
            //else
            //{
            //    requisitionList = requisitionList.OrderBy(x => x.SiteId).OrderBy(y => y.Status).ToList();
            //}



            //foreach (var item in requisitionList)
            //{
            //    dsMaterialsRequisition.RequisitionApprovedList.AddRequisitionApprovedListRow(
            //        item.RequisitionMasId,
            //        item.ProjectId,
            //        item.ProjectName,
            //        item.SiteId,
            //        item.SiteName,
            //        NullHelper.DateToString(item.RequisitionDate),
            //        NullHelper.DateToString(item.RequiredDate),
            //        item.ReqNo,
            //        item.ItemId,
            //        item.ItemName,
            //        item.Qty,
            //        item.unitId,
            //        item.unitName,
            //        item.Status
            //        );
            //}



            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "VendorPayment.rpt"));

            rd.SetDataSource(dsVendorPaymentDS);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;
        }

        /*start------*/

        [HttpGet]
        public ActionResult TenderInformation()
        {
            var tenderProjects = (from tendarMas in db.Proc_TenderMas
                                  join tenderDet in db.Proc_TenderDet on tendarMas.Id equals tenderDet.Proc_TenderMasId
                                  join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                  join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                  join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                  join Site in db.ProjectSite on procProj.ProjectSiteId equals Site.Id
                                  where Site.Id == reqMas.ProcProject.ProjectSiteId && tendarMas.isApproved == "A"
                                  select Site).Distinct().ToList();

            List<ProjectSite> sites = new List<ProjectSite>();

            foreach (var i in tenderProjects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.ProjectId == i.ProjectId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }

            ViewBag.TenderId = new SelectList(db.Proc_TenderMas, "Id", "TNo");
            //ViewBag.ProjectId = new SelectList(db.Project,"Id","Name");
            //ViewBag.SiteId = new SelectList(db.ProjectSite, "Id", "Name");

            return View();
        }



        [HttpPost]
        public ActionResult TenderInformation(int? TenderId, DateTime? DateFrom, DateTime? DateTo, String Status)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            TenderDetails dsTender = new TenderDetails();


            dsTender.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);



            var TenderMasters = (from TenderDet in db.Proc_TenderDet
                                 join TenderMas in db.Proc_TenderMas on TenderDet.Proc_TenderMasId equals TenderMas.Id
                                 select new
                                 {
                                     TenderId = TenderMas.Id,
                                     TenderNo = TenderMas.TNo,
                                     TenderDate = TenderMas.TDate,
                                     TenderSpecifi = TenderMas.Specs,
                                     TenderRemarks = TenderMas.Remarks,
                                     TenderApporved = TenderMas.isApproved
                                 }).Distinct().ToList();


            if (TenderId != null && DateFrom != null && DateTo != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId && x.TenderDate >= DateFrom && x.TenderDate <= DateTo && x.TenderApporved == Status).ToList();
            }

            else if (DateFrom != null && DateTo != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderDate >= DateFrom && x.TenderDate <= DateTo && x.TenderApporved == Status).ToList();
            }
            else if (DateFrom != null && DateTo != null && String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderDate >= DateFrom && x.TenderDate <= DateTo).ToList();
            }

            else if (TenderId != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId && x.TenderApporved == Status).ToList();
            }
            else if (TenderId != null)
            {

                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId).Distinct().ToList();
            }

            else if (!String.IsNullOrEmpty(Status))
            {

                TenderMasters = TenderMasters.Where(x => x.TenderApporved == Status).Distinct().ToList();
            }



            foreach (var TenderMaster in TenderMasters)
            {
                dsTender.TenderMaster.AddTenderMasterRow(TenderMaster.TenderId,
                                           TenderMaster.TenderNo,
                                           NullHelper.DateToString(TenderMaster.TenderDate),
                                           TenderMaster.TenderSpecifi,
                                           TenderMaster.TenderRemarks,
                                           TenderMaster.TenderApporved
                                          );


                var TenderDets = (from TenderDet in db.Proc_TenderDet
                                  join TenderMas in db.Proc_TenderMas on TenderDet.Proc_TenderMasId equals TenderMas.Id
                                  join ReqDet in db.Proc_RequisitionDet on TenderDet.Proc_RequisitionDetId equals ReqDet.Id
                                  join ReqMas in db.Proc_RequisitionMas on ReqDet.Proc_RequisitionMasId equals ReqMas.Id
                                  join procProj in db.ProcProject on ReqMas.ProcProjectId equals procProj.Id
                                  join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                  join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                  join proj in db.Project on projSite.ProjectId equals proj.Id
                                  join items in db.Item on procItem.ItemId equals items.Id
                                  join units in db.Unit on procItem.UnitId equals units.Id
                                  join vendor in db.Vendor on TenderDet.VendorId equals vendor.Id
                                  where TenderMas.Id == TenderMaster.TenderId && items.Id == ReqDet.ItemId
                                  select new
                                  {
                                      TenderMasId = TenderMas.Id,
                                      ProjectId = proj.Id,
                                      ProjectName = proj.Name,
                                      SiteId = projSite.Id,
                                      SiteName = projSite.Name,
                                      ItemId = items.Id,
                                      ItemName = items.Name,
                                      ReqId = ReqMas.Id,
                                      ReqNo = ReqMas.Rcode,
                                      ReqQty = ReqDet.ReqQty,
                                      UnitId = units.Id,
                                      UnitName = units.Name,
                                      VendorId = TenderDet.VendorId,
                                      VendorName = vendor.Name,
                                      QuotationNo = TenderDet.TQNo,
                                      QuotationDate = TenderDet.TQDate,
                                      UnitPrice = TenderDet.TQPrice,
                                      TotalPrice = ReqDet.ReqQty * TenderDet.TQPrice,
                                      Status = TenderDet.Status

                                  }).Distinct().ToList();

                //if (ProjectId != null && SiteId != null && TenderId !=null)
                //{
                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.TenderMasId == TenderId).ToList();
                //}
                //else if (ProjectId != null && SiteId !=null)
                //{

                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).Distinct().ToList();
                //}

                //else if (ProjectId != null)
                //{

                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
                //}



                foreach (var tender in TenderDets)
                {
                    dsTender.TenderDetail.AddTenderDetailRow(tender.TenderMasId,
                                                                       tender.ProjectId,
                                                                       tender.ProjectName,
                                                                       tender.SiteId,
                                                                       tender.SiteName,
                                                                       tender.ItemId,
                                                                       tender.ItemName,
                                                                       tender.ReqId,
                                                                       tender.ReqNo,
                                                                       tender.ReqQty,
                                                                       tender.UnitId,
                                                                       tender.UnitName,
                                                                       tender.VendorId,
                                                                       tender.VendorName,
                                                                       tender.QuotationNo,
                                                                       NullHelper.DateToString(tender.QuotationDate),
                                                                       tender.UnitPrice,
                                                                       tender.TotalPrice,
                                                                       tender.Status
                                                                        );

                }

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "Tender_Information.rpt"));

            rd.SetDataSource(dsTender);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;


        }



        [HttpGet]
        public ActionResult TenderInformationAccepted()
        {
            //var tenderProjects = (from tendarMas in db.Proc_TenderMas
            //                      join tenderDet in db.Proc_TenderDet on tendarMas.Id equals tenderDet.Proc_TenderMasId
            //                      join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
            //                      join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
            //                      join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
            //                      join Site in db.ProjectSite on procProj.ProjectSiteId equals Site.Id
            //                      where Site.Id == reqMas.ProcProject.ProjectSiteId && tendarMas.isApproved == "A"
            //                      select Site).Distinct().ToList();

            //List<ProjectSite> sites = new List<ProjectSite>();

            //foreach (var i in tenderProjects)
            //{
            //    var site = db.ProjectSite.FirstOrDefault(x => x.ProjectId == i.ProjectId);
            //    sites.Add(site);
            //}

            //List<Project> projects = new List<Project>();

            //foreach (var i in sites)
            //{
            //    var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
            //    projects.Add(proj);
            //}

            List<Proc_TenderMas> tenderNo = new List<Proc_TenderMas>();


            var tenders = (from tenderMas in db.Proc_TenderMas
                           join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                           join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                           join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                           join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                           join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                           where tenderDet.Proc_RequisitionDetId == requisitionDet.Id && tenderMas.isApproved == "A"
                           select tenderMas).Distinct().ToList();

       
            foreach (var i in tenders)
            {
                var tenderNoDropDown = db.Proc_TenderMas.FirstOrDefault(x => x.Id == i.Id);
                tenderNo.Add(tenderNoDropDown);
                // tenderList.Add(new SelectListItem { Text = x.TNo, Value = x.Id.ToString() });
            }

            ViewBag.TenderId = new SelectList(tenderNo, "Id", "TNo");

            //ViewBag.TenderId = new SelectList(db.Proc_TenderMas, "Id", "TNo");
            //ViewBag.ProjectId = new SelectList(db.Project,"Id","Name");
            //ViewBag.SiteId = new SelectList(db.ProjectSite, "Id", "Name");

            return View();
        }



        [HttpPost]
        public ActionResult TenderInformationAccepted(int? TenderId, DateTime? DateFrom, DateTime? DateTo, String Status)
        {
            var companyInfo = db.CompanyInformation.SingleOrDefault();
            TenderDetailsAccepted dsTender = new TenderDetailsAccepted();


            dsTender.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);



            var TenderMasters = (from TenderDet in db.Proc_TenderDet
                                 join TenderMas in db.Proc_TenderMas on TenderDet.Proc_TenderMasId equals TenderMas.Id
                                 select new
                                 {
                                     TenderId = TenderMas.Id,
                                     TenderNo = TenderMas.TNo,
                                     TenderDate = TenderMas.TDate,
                                     TenderSpecifi = TenderMas.Specs,
                                     TenderRemarks = TenderMas.Remarks,
                                     TenderApporved = TenderMas.isApproved
                                 }).Distinct().ToList();


            if (TenderId != null && DateFrom != null && DateTo != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId && x.TenderDate >= DateFrom && x.TenderDate <= DateTo && x.TenderApporved == Status).ToList();
            }

            else if (DateFrom != null && DateTo != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderDate >= DateFrom && x.TenderDate <= DateTo && x.TenderApporved == Status).ToList();
            }
            else if (DateFrom != null && DateTo != null && String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderDate >= DateFrom && x.TenderDate <= DateTo).ToList();
            }

            else if (TenderId != null && !String.IsNullOrEmpty(Status))
            {
                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId && x.TenderApporved == Status).ToList();
            }
            else if (TenderId != null)
            {

                TenderMasters = TenderMasters.Where(x => x.TenderId == TenderId).Distinct().ToList();
            }

            else if (!String.IsNullOrEmpty(Status))
            {

                TenderMasters = TenderMasters.Where(x => x.TenderApporved == Status).Distinct().ToList();
            }



            foreach (var TenderMaster in TenderMasters)
            {
                dsTender.TenderMaster.AddTenderMasterRow(TenderMaster.TenderId,
                                           TenderMaster.TenderNo,
                                           NullHelper.DateToString(TenderMaster.TenderDate),
                                           TenderMaster.TenderSpecifi,
                                           TenderMaster.TenderRemarks,
                                           TenderMaster.TenderApporved
                                          );


                var TenderDets = (from TenderDet in db.Proc_TenderDet
                                  join TenderMas in db.Proc_TenderMas on TenderDet.Proc_TenderMasId equals TenderMas.Id
                                  join ReqDet in db.Proc_RequisitionDet on TenderDet.Proc_RequisitionDetId equals ReqDet.Id
                                  join ReqMas in db.Proc_RequisitionMas on ReqDet.Proc_RequisitionMasId equals ReqMas.Id
                                  join procProj in db.ProcProject on ReqMas.ProcProjectId equals procProj.Id
                                  join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                  join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                  join proj in db.Project on projSite.ProjectId equals proj.Id
                                  join items in db.Item on procItem.ItemId equals items.Id
                                  join units in db.Unit on procItem.UnitId equals units.Id
                                  join vendor in db.Vendor on TenderDet.VendorId equals vendor.Id
                                  where TenderMas.Id == TenderMaster.TenderId && items.Id == ReqDet.ItemId
                                  && TenderDet.Status == "A"
                                  select new
                                  {
                                      TenderMasId = TenderMas.Id,
                                      ProjectId = proj.Id,
                                      ProjectName = proj.Name,
                                      SiteId = projSite.Id,
                                      SiteName = projSite.Name,
                                      ItemId = items.Id,
                                      ItemName = items.Name,
                                      ReqId = ReqMas.Id,
                                      ReqNo = ReqMas.Rcode,
                                      ReqQty = ReqDet.ReqQty,
                                      UnitId = units.Id,
                                      UnitName = units.Name,
                                      VendorId = TenderDet.VendorId,
                                      VendorName = vendor.Name,
                                      QuotationNo = TenderDet.TQNo,
                                      QuotationDate = TenderDet.TQDate,
                                      UnitPrice = TenderDet.TQPrice,
                                      TotalPrice = ReqDet.ReqQty * TenderDet.TQPrice,
                                      Status = TenderDet.Status

                                  }).Distinct().ToList();

                //if (ProjectId != null && SiteId != null && TenderId !=null)
                //{
                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.TenderMasId == TenderId).ToList();
                //}
                //else if (ProjectId != null && SiteId !=null)
                //{

                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).Distinct().ToList();
                //}

                //else if (ProjectId != null)
                //{

                //    TenderDets = TenderDets.Where(x => x.ProjectId == ProjectId).Distinct().ToList();
                //}



                foreach (var tender in TenderDets)
                {
                    dsTender.TenderDetail.AddTenderDetailRow(tender.TenderMasId,
                                                                       tender.ProjectId,
                                                                       tender.ProjectName,
                                                                       tender.SiteId,
                                                                       tender.SiteName,
                                                                       tender.ItemId,
                                                                       tender.ItemName,
                                                                       tender.ReqId,
                                                                       tender.ReqNo,
                                                                       tender.ReqQty,
                                                                       tender.UnitId,
                                                                       tender.UnitName,
                                                                       tender.VendorId,
                                                                       tender.VendorName,
                                                                       tender.QuotationNo,
                                                                       NullHelper.DateToString(tender.QuotationDate),
                                                                       tender.UnitPrice,
                                                                       tender.TotalPrice,
                                                                       tender.Status
                                                                        );

                }

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "Tender_Information_Accepted.rpt"));

            rd.SetDataSource(dsTender);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;


        }



        //Vendoy payment item wise
        [HttpGet]
        public ActionResult VendorPaymentItemWise()
        {
            //var requisitionProjects = (from requisitionMas in db.Proc_RequisitionMas
            //                           join procproject in db.ProcProject on requisitionMas.ProcProjectId equals procproject.Id
            //                           where requisitionMas.ProcProjectId == procproject.Id && requisitionMas.Status == "A"
            //                           select procproject).ToList();

            //List<ProjectSite> sites = new List<ProjectSite>();

            //foreach (var i in requisitionProjects)
            //{
            //    var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
            //    sites.Add(site);
            //}

            //List<Project> projects = new List<Project>();

            //foreach (var i in sites)
            //{
            //    var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
            //    projects.Add(proj);
            //}
            //ViewBag.ProjectId = new SelectList(projects.Distinct(), "Id", "Name");
            //ViewBag.SiteId = new SelectList(sites, "Id", "Name");



            var EntryProject = (from purchaseMas in db.Proc_PurchaseOrderMas
                                    //   join purchaseDet in db.Proc_PurchaseOrderDet on purchaseMas.Id equals purchaseDet.Proc_PurchaseOrderMasId
                                join tenderMas in db.Proc_TenderMas on purchaseMas.Proc_TenderMasId equals tenderMas.Id
                                join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                join requisitionDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals requisitionDet.Id
                                join requisitionMas in db.Proc_RequisitionMas on requisitionDet.Proc_RequisitionMasId equals requisitionMas.Id
                                join procProject in db.ProcProject on requisitionMas.ProcProjectId equals procProject.Id
                                join site in db.ProjectSite on procProject.ProjectSiteId equals site.Id
                                join project in db.Project on site.ProjectId equals project.Id
                                where purchaseMas.Proc_TenderMasId == tenderMas.Id && purchaseMas.VendorId == tenderDet.VendorId
                                select project).Distinct().ToList();

            var procprojects = db.ProcProject.ToList();
            List<ProjectSite> sites = new List<ProjectSite>();
            foreach (var i in procprojects)
            {
                var site = db.ProjectSite.FirstOrDefault(x => x.Id == i.ProjectSiteId);
                sites.Add(site);
            }

            List<Project> projects = new List<Project>();

            foreach (var i in sites)
            {
                var proj = db.Project.FirstOrDefault(x => x.Id == i.ProjectId);
                projects.Add(proj);
            }


            ViewBag.ProjectId = new SelectList(EntryProject.Distinct(), "Id", "Name");
            ViewBag.SiteId = new SelectList(sites, "Id", "Name");





            //old not needed
            //var purchaseMasId = purchaseOrderId;
            //var purDetId = db.Proc_PurchaseOrderDet.SingleOrDefault(x => x.Id == purchaseMasId);
            //ViewBag.PurchaseDetIdForVendorReport = purDetId.Id;
            //old ended


            ViewBag.ItemId = new SelectList(db.Item, "Id", "Name");


            ViewBag.VendorId = new SelectList(db.Vendor, "Id", "Name");
            return View();
        }



        [HttpPost]
        public ActionResult VendorPaymentItemWise(int? ProjectId, int? SiteId, int? ItemId, int? VendorId)
        {

            var companyInfo = db.CompanyInformation.SingleOrDefault();
            VendorPaymentItemWiseDS vendorPaymentItemWise = new VendorPaymentItemWiseDS();


            vendorPaymentItemWise.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);





            //var MasterTableData = (from purdet in db.Proc_PurchaseOrderDet
            //                       join purMas in db.Proc_PurchaseOrderMas on purdet.Proc_PurchaseOrderMasId equals purMas.Id
            //                       join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
            //                       join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
            //                       join vendors in db.Vendor on tenderDet.VendorId equals vendors.Id
            //                       join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
            //                       join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
            //                       join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
            //                       join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
            //                       join items in db.Item on procItem.ItemId equals items.Id
            //                       join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
            //                       join proj in db.Project on projSite.ProjectId equals proj.Id
            //                       where procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id && purdet.ItemId == items.Id && purMas.VendorId == vendors.Id

            //procProj.ProjectSite.ProjectId == proj.Id && procProj.ProjectSiteId == projSite.Id  &&

            var MasterTableData = (from purdet in db.Proc_PurchaseOrderDet
                                   join purMas in db.Proc_PurchaseOrderMas on purdet.Proc_PurchaseOrderMasId equals purMas.Id
                                   join tenderMas in db.Proc_TenderMas on purMas.Proc_TenderMasId equals tenderMas.Id
                                   join tenderDet in db.Proc_TenderDet on tenderMas.Id equals tenderDet.Proc_TenderMasId
                                   join vendors in db.Vendor on purMas.VendorId equals vendors.Id
                                   join reqDet in db.Proc_RequisitionDet on tenderDet.Proc_RequisitionDetId equals reqDet.Id
                                   join reqMas in db.Proc_RequisitionMas on reqDet.Proc_RequisitionMasId equals reqMas.Id
                                   join procProj in db.ProcProject on reqMas.ProcProjectId equals procProj.Id
                                   //join procItem in db.ProcProjectItem on procProj.Id equals procItem.ProcProjectId
                                   join items in db.Item on purdet.ItemId equals items.Id
                                   join projSite in db.ProjectSite on procProj.ProjectSiteId equals projSite.Id
                                   join proj in db.Project on projSite.ProjectId equals proj.Id
                                   where purdet.Proc_PurchaseOrderMasId == purMas.Id && purMas.Proc_TenderMasId == tenderMas.Id && purdet.ItemId == reqDet.ItemId && tenderDet.Proc_RequisitionDetId == reqDet.Id
                                   select new
                                   {
                                       ProjectId = proj.Id,
                                       ProjectName = proj.Name,
                                       SiteId = projSite.Id,
                                       SiteName = projSite.Name,
                                       VendorId = vendors.Id,
                                       VendorName = vendors.Name,
                                       ItemId = items.Id,
                                       ItemName = items.Name,
                                       //TenderDetId= tenderDet.Id,
                                       UnitPrice = tenderDet.TQPrice,
                                       PurchaseMasterId = purMas.Id,
                                       PONo = purMas.PONo,
                                       PODate = purMas.PODate,
                                       POQty = purdet.POQty,
                                       POAmt = purdet.POAmt
                                   }).Distinct().ToList();


            if (ProjectId != null && SiteId != null && ItemId != null && VendorId != null)
            {
                MasterTableData = MasterTableData.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.ItemId == ItemId && x.VendorId == VendorId).ToList();
            }

            else if (ProjectId != null && SiteId != null && ItemId != null)
            {
                MasterTableData = MasterTableData.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId && x.ItemId == ItemId).ToList();
            }
            else if (ProjectId != null && SiteId != null)
            {
                MasterTableData = MasterTableData.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();

            }

            else if (ProjectId != null)
            {
                MasterTableData = MasterTableData.Where(x => x.ProjectId == ProjectId).ToList();
            }



            foreach (var master in MasterTableData)
            {
                vendorPaymentItemWise.MasterTable.AddMasterTableRow(master.ProjectId,
                                           master.SiteId,
                                           master.ProjectName,
                                           master.SiteName,
                                           master.ItemId,
                                           master.ItemName,
                                           master.VendorId,
                                           master.VendorName,
                                           master.PurchaseMasterId,
                                           master.PONo,
                                           master.PODate,
                                           // master.TenderDetId,
                                           master.UnitPrice,
                                           master.POQty,
                                           master.POAmt
                                           );

            }

            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "VendorPaymentItemWiseReport.rpt"));

            rd.SetDataSource(vendorPaymentItemWise);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;
        }








        //ProjectList report february 19
        [HttpGet]
        public ActionResult ProjectList()
        {

            ViewBag.ProjectId = new SelectList(db.Project,"Id", "Name");

            ViewBag.SiteId = new SelectList(db.ProjectSite,"Id", "Name");




            return View();
        }

        [HttpPost]
        public ActionResult ProjectList(int? ProjectId, int? SiteId)
        {

            var companyInfo = db.CompanyInformation.SingleOrDefault();
            ProjectList projectListDS = new ProjectList();


            projectListDS.CompanyInfo.AddCompanyInfoRow(companyInfo.Name,
                                                         companyInfo.Address,
                                                         companyInfo.Phone,
                                                         companyInfo.Web,
                                                         companyInfo.Email,
                                                         companyInfo.Id);
      

            var projectLists = (from projSite in db.ProjectSite
                               join proj in db.Project on projSite.Id equals proj.Id
                               join projRes in db.ProjectResource on proj.Id equals projRes.ProjectId
                               join projSiteRes in db.ProjectSiteResource on projSite.Id equals projSiteRes.ProjectSiteId
                               join compRes in db.CompanyResource on projRes.CompanyResourceId equals compRes.Id 
                               join compResSite in db.CompanyResource on projSiteRes.CompanyResourceId equals compResSite.Id
                               where projSite.Id == proj.Id
                               select new
                                   {
                                   ProjectId= proj.Id,
                                   SiteId= projSite.Id,
                                   ProjectName= proj.Name,
                                   SiteName= projSite.Name,
                                   SiteLocation= projSite.Location,
                                   ProjectManager= projRes.CompanyResource.Name,
                                   SiteEngineer= projSiteRes.CompanyResource.Name                                  
                                   }).Distinct().ToList();



            if (ProjectId != null && SiteId != null)
            {

                projectLists = projectLists.Where(x => x.ProjectId == ProjectId && x.SiteId == SiteId).ToList();
            }
          
            else if (ProjectId != null)
            {
                projectLists = projectLists.Where(x => x.ProjectId == ProjectId).ToList();
            }          

            foreach (var item in projectLists)
            {
                projectListDS.ProjectListDS.AddProjectListDSRow(
                   item.ProjectId,
                   item.SiteId,
                   item.ProjectName,
                   item.SiteName,
                   item.SiteLocation,
                   item.ProjectManager,
                   item.SiteEngineer
                    );
            }



            ReportDocument rd = new ReportDocument();

            rd.Load(Path.Combine(Server.MapPath("~/Reports"), "ProjectList.rpt"));

            rd.SetDataSource(projectListDS);

            Stream stream = rd.ExportToStream(ExportFormatType.PortableDocFormat);
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            Byte[] fileBuffer = ms.ToArray();

            Response.Buffer = false;
            Response.ClearContent();
            Response.ClearHeaders();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-length", fileBuffer.Length.ToString());
            Response.BinaryWrite(fileBuffer);
            return null;
        }







    }
}