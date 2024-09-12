﻿using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IHeadlineHookUpdates {
		public bool MessageChanged { get; set; }
        public bool OwnerChanged { get; set; }
    }

    public interface IHeadlineHook : IHook {

        Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline);
        Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates);
        Task ArchiveHeadline(ISession s, PeopleHeadline headline);
        Task UnArchiveHeadline(ISession s, PeopleHeadline headline);
    }
}
