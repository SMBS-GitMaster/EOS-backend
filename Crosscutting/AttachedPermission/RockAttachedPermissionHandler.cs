﻿using System.Threading.Tasks;
using NHibernate;
using RadialReview.Api;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Utilities;

namespace RadialReview.Crosscutting.AttachedPermission {
	public class RockAttachedPermissionHandler : IAttachedPermissionHandler<AngularRock>
    {
        public async Task<PermissionObject> GetPermissionsForAdministration(ISession s, PermissionsUtility perm)
        {
            return new PermissionObject()
            {
                CanCreate = true,
                Name = "AngularRock"
            };
        }

        public async Task<PermissionDto> GetPermissionsForObject(ISession s, PermissionsUtility perm, IAttachedPermission t)
        {
            return new PermissionDto { CanEdit = false };
        }
    }
}