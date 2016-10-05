﻿using System.Linq;
using System.Web.Mvc;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Areas.Admin.Controllers
{
    public class DeviceController : BaseController
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IRoomMetadataRepository _roomMetadataRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IBuildingRepository _buildingRepository;

        public DeviceController(IDeviceRepository deviceRepository, IRoomMetadataRepository roomMetadataRepository, IFloorRepository floorRepository, IBuildingRepository buildingRepository, IOrganizationRepository organizationRepository, IGlobalAdministratorRepository globalAdministratorRepository) : base(organizationRepository, globalAdministratorRepository)
        {
            _deviceRepository = deviceRepository;
            _roomMetadataRepository = roomMetadataRepository;
            _floorRepository = floorRepository;
            _buildingRepository = buildingRepository;
        }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (filterContext.Result == null && null == CurrentOrganization)
            {
                filterContext.Result = RedirectToAction("Index", "Organization");
            }
        }

        public ActionResult Index()
        {
            var buildings = _buildingRepository.GetAll(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            ViewBag.Buildings = buildings;
            var floors = _floorRepository.GetAllByOrganization(CurrentOrganization.Id).ToDictionary(_ => _.Id, _ => _.Name);
            ViewBag.Rooms = _roomMetadataRepository.GetRoomInfosForOrganization(CurrentOrganization.Id)
                .ToDictionary(_ => _.RoomAddress, _ => string.Format("{0} - {1} - {2}", buildings.TryGetValue(_.BuildingId), floors.TryGetValue(_.FloorId), _.RoomAddress));
            return View(_deviceRepository.GetForOrganization(CurrentOrganization.Id));
        }

        public ActionResult Create()
        {
            return View("Edit");
        }

        [HttpPost]
        public ActionResult Create(DeviceEntity model)
        {
            var building = _buildingRepository.Get(model.BuildingId);
            var room = _roomMetadataRepository.GetRoomInfo(model.ControlledRoomAddresses.FirstOrDefault(), CurrentOrganization.Id);
            if (building.OrganizationId != CurrentOrganization.Id || (model.ControlledRoomAddresses.Any() && null == room))
            {
                return HttpNotFound();
            }

            model.Id = null;
            model.OrganizationId = CurrentOrganization.Id;
            _deviceRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            var model = _deviceRepository.Get(id);
            if (null == model || model.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            var room = _roomMetadataRepository.GetRoomInfo(model.ControlledRoomAddresses.FirstOrDefault(), CurrentOrganization.Id);
            ViewBag.Building = _buildingRepository.Get(model.BuildingId)?.Name;
            ViewBag.Floor = _floorRepository.Get(room?.FloorId)?.Name;
            ViewBag.Room = room?.RoomAddress;
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(DeviceEntity model)
        {
            var device = _deviceRepository.Get(model.Id);
            if (null == device || device.OrganizationId != CurrentOrganization.Id)
            {
                return HttpNotFound();
            }

            var room = _roomMetadataRepository.GetRoomInfo(model.ControlledRoomAddresses.FirstOrDefault(), CurrentOrganization.Id);
            if (model.ControlledRoomAddresses.Any() && room == null)
            {
                return HttpNotFound();
            }

            model.OrganizationId = CurrentOrganization.Id;
            model.BuildingId = room?.BuildingId;

            _deviceRepository.Update(model);
            return RedirectToAction("Index");
        }

        public ActionResult Details(string id)
        {
            return Edit(id);
        } 
    }
}