using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DhaliProcurement.Models;
using DhaliProcurement.ViewModel;
using DhaliProcurement.Helper;

namespace DhaliProcurement.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private DCPSContext db = new DCPSContext();

        // GET: Projects
        public ActionResult Index(int? ProjectId)
        {
            ViewBag.ProjectId = new SelectList(db.Project, "Id", "Name");

            var projectList = db.Project.ToList();


            if (ProjectId != null)
            {

                projectList = projectList.Where(x => x.Id == ProjectId).ToList();
                return View(projectList);

            }

            else
            {
                return View(projectList.ToList());
            }




            //var projectList = db.Project.ToList();

            // return View();

        }




        [HttpPost]
        public ActionResult Sites(int ProjectId)
        {

            List<SelectListItem> sitetNames = new List<SelectListItem>();

            var sites = db.ProjectSite.Where(x => x.ProjectId == ProjectId).ToList();

            foreach (var x in sites)
            {
                sitetNames.Add(new SelectListItem { Text = x.Name, Value = x.Id.ToString() });
            }
            return Json(sitetNames, JsonRequestBehavior.AllowGet);
        }




        // GET: Projects/Details/5
        public ActionResult Details(int? id)
        {
            var pId = db.Project.FirstOrDefault(x => x.Id == id);
            ViewBag.Proj = pId.Id;



            ViewBag.PId = id;
            var projectData = db.Project.Where(x => x.Id == id).FirstOrDefault();
            ViewBag.ProjectName = projectData.Name;


            var prMan = (from projRes in db.ProjectResource
                         join comRes in db.CompanyResource on projRes.CompanyResourceId equals comRes.Id
                         where projRes.ProjectId == id
                         select comRes).FirstOrDefault();

            ViewBag.ProjectManager = new SelectList(db.CompanyResource, "Id", "Name", prMan.Id);

            var startDate = NullHelper.DateToString(projectData.StartDate);
            ViewBag.StartDate = startDate;

            var endDate = NullHelper.DateToString(projectData.EndDate);
            ViewBag.EndDate = endDate;

            var remarks = projectData.Remarks;
            ViewBag.ProjectRemarks = NullHelper.ObjectToString(remarks);



            var status = new SelectList(new List<SelectListItem> { new SelectListItem { Text = "Active", Value = "A" }, new SelectListItem { Text = "Inactive", Value = "I" }, }, "Value", "Text");
            ViewBag.Statuses = status;
            ViewBag.RName = new SelectList(db.CompanyResource, "Id", "Name");

            return View();
        }


        // GET: Projects/Create
        public ActionResult Create()
        {


            ViewBag.RName = new SelectList(db.CompanyResource, "Id", "Name");

            var status = new SelectList(new List<SelectListItem> { new SelectListItem { Text = "Active", Value = "A" }, new SelectListItem { Text = "Inactive", Value = "I" }, }, "Value", "Text");
            ViewBag.Statuses = status;

            return View();
        }


        // POST: Projects/ProjectCreate           
        public JsonResult ProjectCreate(List<VMProjectSite> SiteResourceDetails, string ProjectName, DateTime? StartDate, DateTime? EndDate, string Remarks, int RName)
        {

            var result = new
            {
                flag = false,
                message = "Saving failed"
            };

            var check = db.Project.Where(x => x.Name.Trim().ToUpper() == ProjectName.Trim().ToUpper()).ToList();

            if (check.Count == 0)
            {
                if (ProjectName.Trim() != "")
                {
                    Project project = new Project();
                    ProjectResource projectResource = new ProjectResource();

                    using (var dbContextTransaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            project.Name = ProjectName;
                            project.StartDate = StartDate;
                            project.EndDate = EndDate;
                            project.Remarks = Remarks;
                            //projectResource.ProjectId = project.Id;
                            projectResource.CompanyResourceId = RName;

                            db.Project.Add(project);
                            db.ProjectResource.Add(projectResource);
                            db.SaveChanges();

                            var projectId = project.Id;
                            ViewBag.ProjectId = projectId;


                            if (SiteResourceDetails != null)
                            {
                                foreach (var item in SiteResourceDetails)
                                {
                                    ProjectSite projectSite = new ProjectSite();
                                    ProjectSiteResource projectSiteResource = new ProjectSiteResource();

                                    projectSite.ProjectId = projectId;
                                    projectSite.Name = item.SiteName;
                                    projectSite.Location = item.SiteLocation;
                                    projectSiteResource.CompanyResourceId = item.SiteEngineerId;

                                    db.ProjectSite.Add(projectSite);
                                    db.ProjectSiteResource.Add(projectSiteResource);
                                    db.SaveChanges();
                                }
                            }
                            dbContextTransaction.Commit();
                            //end
                            result = new
                            {
                                flag = true,
                                message = "Project saving successful!"
                            };
                        }
                        catch (Exception ex)
                        {
                            dbContextTransaction.Rollback();
                            result = new
                            {

                                flag = false,
                                message = "Saving failed! Error occurred."
                                //message = ex.Message
                            };
                        }
                    }
                }

                else
                {
                    result = new
                    {
                        flag = false,
                        message = "Saving failed!\nProject name required."
                    };
                }
            }
            else
            {
                result = new
                {
                    flag = false,
                    message = "Project already exists!"
                };
            }



            return Json(result, JsonRequestBehavior.AllowGet);
        }





        // POST: Projects/ProjectCreate           
        public JsonResult ProjectUpdate(List<VMProjectSite> SiteResourceDetails, int?[] DeleteItems, int ProjectId, string ProjectName, DateTime? StartDate, DateTime? EndDate, string Remarks, int RName)
        {
            var flag = false;
            var result = new
            {
                flag = false,
                message = "Saving failed"
            };

            if (DeleteItems != null)
            {
                foreach (var i in DeleteItems)
                {
                    var siteresource = db.ProjectSiteResource.SingleOrDefault(x => x.ProjectSiteId == i);
                    //foreach(var m in siteresource)
                    //{
                    //    var deleteResource = siteresource.Find(m.);
                    //}
                    /*    var deleteResource = db.ProjectSiteResource.Find(siteresource)*/
                    ;
                    db.ProjectSiteResource.Remove(siteresource);
                    db.SaveChanges();
                    //var delteItem = db.Proc_MaterialEntryDet.SingleOrDefault(x => x.ItemId == i && x.Proc_PurchaseOrderMasId == ProcPurchaseMasterId);
                    var entryDetId = db.ProjectSite.Find(i);
                    db.ProjectSite.Remove(entryDetId);
                    flag = db.SaveChanges() > 0;

                }
            }



            var existingProject = db.Project.Where(x => x.Id == ProjectId).ToList();

            if (existingProject.Count != 0)
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var checkdata = db.Project.Where(x => x.Id == ProjectId).SingleOrDefault();

                        if (checkdata != null)
                        {
                            ProjectResource pres = new ProjectResource();
                            checkdata.Name = ProjectName;
                            checkdata.StartDate = StartDate;
                            checkdata.EndDate = EndDate;
                            checkdata.Remarks = Remarks;
                            pres.ProjectId = ProjectId;
                            //pres.CompanyResourceId = RName;
                            try
                            {
                                db.Entry(checkdata).State = EntityState.Modified;
                                //var projectResouce = db.ProjectResource.SingleOrDefault(x => x.ProjectId == ProjectId && x.CompanyResourceId == );
                                var projectResouce = db.ProjectResource.SingleOrDefault(x => x.ProjectId == ProjectId);
                                //projectResouce.CompanyResourceId = RName;

                                db.Entry(projectResouce).State = EntityState.Modified;

                            }
                            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                            {
                                //dbContextTransaction.Rollback();
                                Exception raise = dbEx;
                                foreach (var validationErrors in dbEx.EntityValidationErrors)
                                {
                                    foreach (var validationError in validationErrors.ValidationErrors)
                                    {
                                        string message = string.Format("{0}:{1}",
                                           validationErrors.Entry.Entity.ToString(),
                                            validationError.ErrorMessage);
                                        // raise a new exception nesting
                                        // the current instance as InnerException
                                        raise = new InvalidOperationException(message, raise);
                                    }
                                }
                                throw raise;
                            }



                            flag = db.SaveChanges() > 0;
                            if (flag == true)
                            {
                                if (SiteResourceDetails != null)
                                {
                                    foreach (var item in SiteResourceDetails)
                                    {

                                        ProjectSite sites = new ProjectSite();
                                        ProjectSiteResource pSiteRes = new ProjectSiteResource();
                                        sites.ProjectId = ProjectId;
                                        sites.Name = item.SiteName;
                                        sites.Location = item.SiteLocation;
                                        pSiteRes.CompanyResourceId = item.SiteEngineerId;

                                        var checkingProjectSites = db.ProjectSite.FirstOrDefault(x => x.Id == item.ProjectSiteId);

                                        flag = false;
                                        if (checkingProjectSites == null)
                                        {
                                            db.Entry(sites).State = EntityState.Added;
                                            db.Entry(pSiteRes).State = EntityState.Added;
                                        }
                                        else
                                        {
                                            var ProjectSite = db.ProjectSite.Where(x => x.ProjectId == ProjectId && x.Id == item.ProjectSiteId).FirstOrDefault();

                                            var ProjectSiteEngineer = db.ProjectSiteResource.Where(x => x.ProjectSiteId == ProjectSite.Id && ProjectSite.ProjectId == ProjectId).FirstOrDefault();
                                            ProjectSite.ProjectId = ProjectId;
                                            ProjectSite.Name = item.SiteName;
                                            ProjectSite.Location = item.SiteLocation;
                                            pSiteRes.CompanyResourceId = item.SiteEngineerId;
                                            ProjectSiteEngineer.CompanyResource.Name = item.SiteEngineer;


                                            db.Entry(ProjectSite).State = EntityState.Modified;
                                            db.Entry(ProjectSiteEngineer.CompanyResource).State = EntityState.Modified;

                                        }

                                        db.SaveChanges();
                                    }

                                    dbContextTransaction.Commit();

                                    result = new
                                    {
                                        flag = true,
                                        message = "Edit saving successful !"
                                    };

                                    return Json(result, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        result = new
                        {

                            flag = false,
                            message = "Saving failed! Error occurred."
                            //message = ex.Message
                        };
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                result = new
                {
                    flag = false,
                    message = "Project and Project Sites already exist!"
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }



        }


        // GET: Projects/Edit/5
        public ActionResult Edit(int? id)
        {

            var pId = db.Project.FirstOrDefault(x => x.Id == id);
            ViewBag.Proj = pId.Id;



            ViewBag.PId = id;
            var projectData = db.Project.Where(x => x.Id == id).FirstOrDefault();
            ViewBag.ProjectName = projectData.Name;


            var prMan = (from projRes in db.ProjectResource
                         join comRes in db.CompanyResource on projRes.CompanyResourceId equals comRes.Id
                         where projRes.ProjectId == id
                         select comRes).FirstOrDefault();

            ViewBag.ProjectManager = new SelectList(db.CompanyResource, "Id", "Name", prMan.Id);

            var startDate = NullHelper.DateToString(projectData.StartDate);
            ViewBag.StartDate = startDate;

            var endDate = NullHelper.DateToString(projectData.EndDate);
            ViewBag.EndDate = endDate;

            var remarks = projectData.Remarks;
            ViewBag.ProjectRemarks = NullHelper.ObjectToString(remarks);



            var status = new SelectList(new List<SelectListItem> { new SelectListItem { Text = "Active", Value = "A" }, new SelectListItem { Text = "Inactive", Value = "I" }, }, "Value", "Text");
            ViewBag.Statuses = status;
            ViewBag.RName = new SelectList(db.CompanyResource, "Id", "Name");

            return View();
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,ClientId,ProjectGroupId,StartDate,EndDate,Status,Remarks")] Project project)
        {
            if (ModelState.IsValid)
            {
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(project);
        }


        //1/11/18
        public JsonResult getProjectSites(int ProjectId)
        {
            List<VMProjectSite> check = new List<VMProjectSite>();


            //getting project sites under one project 
            var siteListwithRes = (from projId in db.Project
                                   join projSite in db.ProjectSite on projId.Id equals projSite.ProjectId
                                   join siteRes in db.ProjectSiteResource on projSite.Id equals siteRes.ProjectSiteId
                                   where projId.Id == ProjectId
                                   select new { siteRes, projSite }).ToList();


            foreach (var i in siteListwithRes)
            {
                VMProjectSite projectSites = new VMProjectSite();
                projectSites.ProjectSiteId = i.projSite.Id;
                //projectSites.ProjectId = i.projSite.ProjectId;
                projectSites.SiteName = i.projSite.Name;
                projectSites.SiteLocation = i.projSite.Location;
                projectSites.SiteEngineerId = i.siteRes.CompanyResourceId;
                projectSites.SiteEngineer = i.siteRes.CompanyResource.Name;
                check.Add(projectSites);
            }

            return Json(check, JsonRequestBehavior.AllowGet);

        }



        //// GET: Projects/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    var siteList = db.ProjectSite.Where(x => x.ProjectId == id).ToList();
        //    if(siteList.Count != 0)
        //    {
        //        ViewBag.Exists = true;
        //        ViewBag.Count = siteList.Count;
        //    }

        //    Project project = db.Project.Find(id);
        //    if (project == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    return View(project);
        //}

        //// POST: Projects/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteConfirmed(int id)
        //{
        //    Project project = db.Project.Find(id);
        //    db.Project.Remove(project);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}




        //21jan18

        //[HttpPost]
        //public JsonResult DeleteProjectSites(int id, int ProjectId)
        //{
        //    var result = new
        //    {
        //        flag = false,
        //        message = "Error occured !!"
        //    };


        //    var projSiteList = db.ProcProject.FirstOrDefault(x => x.ProjectSiteId == id);
        //    if (projSiteList == null)
        //    {
        //        var resourceCheck = db.ProjectSiteResource.Where(x => x.ProjectSiteId == id).ToList();
        //        var checkProcProject = db.ProcProject.Where(x => x.ProjectSiteId == id).ToList();


        //        if (checkProcProject.Count == 0)
        //        {
        //            if (resourceCheck.Count > 0)
        //            {
        //                var deleteResource = db.ProjectSiteResource.SingleOrDefault(x => x.ProjectSiteId == id);
        //                db.ProjectSiteResource.Remove(deleteResource);
        //                db.SaveChanges();
        //            }

        //            var projectSite = db.ProjectSite.SingleOrDefault(x => x.Id == id);
        //            db.ProjectSite.Remove(projectSite);
        //            db.SaveChanges();

        //            result = new
        //            {
        //                flag = true,
        //                message = "Delete successful !!"
        //            };


        //        }
        //        else
        //        {
        //            result = new
        //            {
        //                flag = false,
        //                message = "Please delete ProcProject first!"
        //            };
        //        }

        //    }
        //    else
        //    {
        //        result = new
        //        {
        //            flag = false,
        //            message = "Delete failed!This site has been used!"
        //        };
        //    }

        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost]
        public JsonResult DeleteProjectSites(int ProjectSiteId)
        {
            var result = new
            {
                flag = false,
                message = "Error occured !!"
            };


            var projSiteList = db.ProcProject.Where(x => x.ProjectSiteId == ProjectSiteId).ToList();
            if (projSiteList.Count == 0)
            {

                result = new
                {
                    flag = true,
                    message = "Delete successful !!"
                };

            }
            else
            {
                result = new
                {
                    flag = false,
                    message = "Delete failed!This site has been used!"
                };
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }




        public JsonResult DeleteProjects(int id)
        {
            //string result = "";


            var checkProc = db.ProcProject.Where(x => x.ProjectSite.ProjectId == id).ToList();
            if (checkProc.Count == 0)
            {
                bool flag = false;
                try
                {
                    //Project project = db.Project.Find(id);
                    //db.Project.Remove(project);

                    var itemsToDeleteSiteResources = db.ProjectSiteResource.Where(x => x.ProjectSite.ProjectId == id);
                    db.ProjectSiteResource.RemoveRange(itemsToDeleteSiteResources);
                    var itemsToDeleteProjectSite = db.ProjectSite.Where(x => x.ProjectId == id);
                    db.ProjectSite.RemoveRange(itemsToDeleteProjectSite);


                    var itemsToDeleteResources = db.ProjectResource.Where(x => x.ProjectId == id);
                    db.ProjectResource.RemoveRange(itemsToDeleteResources);

                    var itemToDeleteProject = db.Project.Where(x => x.Id == id).FirstOrDefault();
                    db.Project.Remove(itemToDeleteProject);



                    flag = db.SaveChanges() > 0;

                }
                catch (Exception ex)
                {

                }

                if (flag)
                {
                    var result = new
                    {
                        flag = true,
                        message = "Project deletion successful!"
                    };
                    return Json(result, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    var result = new
                    {
                        flag = false,
                        message = "Project deletion failed!\nError Occured."
                    };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }


            }

            else
            {

                var result = new
                {
                    flag = false,
                    message = "Project deletion failed!\nDelete Project Costing first."
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }

        }
        //21janEnd


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}