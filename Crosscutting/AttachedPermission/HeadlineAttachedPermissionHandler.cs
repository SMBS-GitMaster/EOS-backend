﻿using System.Threading.Tasks;
using NHibernate;
using RadialReview.Api;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Utilities;

namespace RadialReview.Crosscutting.AttachedPermission {
	public class HeadlineAttachedPermissionHandler : IAttachedPermissionHandler<AngularHeadline>
    {
        public async Task<PermissionObject> GetPermissionsForAdministration(ISession s, PermissionsUtility perm)
        {
            return new PermissionObject()
            {
                CanCreate = true,
                Name = "AngularHeadline"
            };
        }

        public async Task<PermissionDto> GetPermissionsForObject(ISession s, PermissionsUtility perm, IAttachedPermission t)
        {
            return new PermissionDto { CanEdit = false };
        }
    }
}