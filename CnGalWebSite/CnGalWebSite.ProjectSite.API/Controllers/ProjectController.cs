﻿using CnGalWebSite.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CnGalWebSite.ProjectSite.Models.ViewModels.Projects;
using CnGalWebSite.ProjectSite.Models.DataModels;
using CnGalWebSite.ProjectSite.API.DataReositories;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using CnGalWebSite.DataModel.ViewModel.Commodities;
using CnGalWebSite.ProjectSite.API.Services.Users;

namespace CnGalWebSite.ProjectSite.API.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly IRepository<Project, long> _projectRepository;
        private readonly IUserService _userService;


        public ProjectController(IRepository<Project, long> projectRepository, IUserService userService)
        {
            _projectRepository = projectRepository;
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<ProjectViewModel>> GetAsync([FromQuery] long id)
        {
            var item = await _projectRepository.GetAll()
                .Include(s => s.Images)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (item == null)
            {
                return NotFound("无法找到该目标");
            }

            var model = new ProjectViewModel
            {
                Contact = item.Contact,
                Description = item.Description,
                Name = item.Name,
                EndTime = item.EndTime,
                CreateTime = item.CreateTime,
                UpdateTime = item.UpdateTime,
                Id = id,
                Images = item.Images.Select(s => new ProjectImageViewModel
                {
                    Image = s.Image,
                    Note = s.Note,
                    Priority = s.Priority,
                }).ToList(),

                Positions = item.Positions.Select(s => new ProjectPositionViewModel
                {
                    BudgetNote = s.BudgetNote,
                    DeadLine = s.DeadLine,
                    Description = s.Description,
                    PositionType = s.PositionType,
                    PositionTypeName = s.PositionTypeName,
                    BudgetMax = s.BudgetMax,
                    BudgetMin = s.BudgetMin,
                    BudgetType = s.BudgetType,
                    Percentage = s.Percentage,
                    UrgencyType = s.UrgencyType,
                    Type = s.Type,
                }).ToList(),
                CreateUser = await _userService.GetUserInfo(item.CreateUserId)
            };

            return model;
        }


        [HttpGet]
        public async Task<ActionResult<ProjectEditModel>> EditAsync([FromQuery] long id)
        {
            if (id == 0)
            {
                return new ProjectEditModel
                {
                    EndTime = DateTime.Now.ToCstTime().AddDays(60)
                };
            }

            var user = await _userService.GetCurrentUserAsync();
            var admin = _userService.CheckCurrentUserRole("Admin");

            var item = await _projectRepository.GetAll()
                .Include(s => s.Images)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.Id == id && (s.CreateUserId == user.Id || admin));

            if (item == null)
            {
                return NotFound("无法找到该目标");
            }

            var model = new ProjectEditModel
            {
                Contact = item.Contact,
                Description = item.Description,
                Name = item.Name,
                EndTime = item.EndTime,
                Id = id,
                Images = item.Images.Select(s => new ProjectImageEditModel
                {
                    Id = s.Id,
                    Image = s.Image,
                    Note = s.Note,
                    Priority = s.Priority,
                }).ToList(),

                Positions = item.Positions.Select(s => new ProjectPositionEditModel
                {
                    BudgetNote = s.BudgetNote,
                    DeadLine = s.DeadLine,
                    Description = s.Description,
                    PositionType = s.PositionType,
                    PositionTypeName = s.PositionTypeName,
                    BudgetMax = s.BudgetMax,
                    BudgetMin = s.BudgetMin,
                    BudgetType = s.BudgetType,
                    Id = s.Id,
                    Percentage = s.Percentage,
                    UrgencyType = s.UrgencyType,
                    Type = s.Type,
                }).ToList()
            };

            return model;
        }

        [HttpPost]
        public async Task<Result> EditAsync(ProjectEditModel model)
        {
            var vail = model.Validate();
            if (!vail.Success)
            {
                return vail;
            }
            var user = await _userService.GetCurrentUserAsync();

            Project item = null;
            if (model.Id == 0)
            {
                item = await _projectRepository.InsertAsync(new Project
                {
                    Description = model.Description,
                    Name = model.Name,
                    Contact = model.Contact,
                    EndTime = model.EndTime,
                    CreateTime = DateTime.Now.ToCstTime(),
                    CreateUserId = user.Id
                });
                model.Id = item.Id;
                _projectRepository.Clear();
            }

            var admin = _userService.CheckCurrentUserRole("Admin");

            item = await _projectRepository.GetAll()
                .Include(s => s.Images)
                .Include(s => s.Positions)
                .FirstOrDefaultAsync(s => s.Id == model.Id && (s.CreateUserId == user.Id || admin));

            if (item == null)
            {
                return new Result { Success = false, Message = "项目不存在" };
            }

            item.Description = model.Description;
            item.Name = model.Name;
            item.Contact = model.Contact;
            item.EndTime = model.EndTime;

            //相册
            item.Images.RemoveAll(s => model.Images.Select(s => s.Id).Contains(s.Id) == false);
            foreach (var info in item.Images)
            {
                var temp = model.Images.FirstOrDefault(s => s.Id == info.Id);
                if (temp != null)
                {
                    info.Image = temp.Image;
                    info.Note = temp.Note;
                    info.Priority = temp.Priority;
                }
            }
            item.Images.AddRange(model.Images.Where(s => s.Id == 0).Select(s => new ProjectImage
            {
                Image = s.Image,
                Note = s.Note,
                Priority = s.Priority,
            }));

            //职位
            item.Positions.RemoveAll(s => model.Positions.Select(s => s.Id).Contains(s.Id) == false);
            foreach (var info in item.Positions)
            {
                var temp = model.Positions.FirstOrDefault(s => s.Id == info.Id);
                if (temp != null)
                {
                    info.DeadLine = temp.DeadLine;
                    info.Description = temp.Description;
                    info.PositionType = temp.PositionType;
                    info.PositionTypeName = temp.PositionTypeName;
                    info.Type = temp.Type;
                    info.BudgetMax = temp.BudgetMax;
                    info.BudgetMin = temp.BudgetMin;
                    info.BudgetType = temp.BudgetType;
                    info.Id = temp.Id;
                    info.Percentage = temp.Percentage;
                    info.UrgencyType = temp.UrgencyType;
                    info.BudgetNote = temp.BudgetNote;
                }
            }
            item.Positions.AddRange(model.Positions.Where(s => s.Id == 0).Select(s => new ProjectPosition
            {
                DeadLine = s.DeadLine,
                Description = s.Description,
                PositionType = s.PositionType,
                PositionTypeName = s.PositionTypeName,
                Type = s.Type,
                BudgetMax = s.BudgetMax,
                BudgetMin = s.BudgetMin,
                BudgetType = s.BudgetType,
                Id = s.Id,
                Percentage = s.Percentage,
                UrgencyType = s.UrgencyType,
                BudgetNote = s.BudgetNote,
                ProjectId = model.Id
            }));

            item.UpdateTime = DateTime.Now.ToCstTime();

            await _projectRepository.UpdateAsync(item);

            return new Result { Success = true ,Message=model.Id.ToString()};
        }
    }
}