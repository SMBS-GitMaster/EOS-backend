﻿using FluentNHibernate.Mapping;
using Mandrill.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.UserModels {
	public class TempUserModel : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long? OrganizationId { get; set; }
		public virtual String FirstName { get; set; }
		public virtual String LastName { get; set; }
		public virtual String Email { get; set; }
		public virtual DateTime Created { get; set; }
		public virtual DateTime? LastSent { get; set; }
		public virtual long UserOrganizationId { get; set; }

		public virtual String Guid { get; set; }
		public virtual WebHookEventType? EmailStatus { get; set; }
		public virtual bool EmailStatusUnseen { get; set; }
		public virtual long LastSentByUserId { get; set; }
		public virtual String ImageGuid { get; set; }
		public virtual String EmailTemplate { get; set; }
		public virtual bool EnableWhale { get; set; }

    public virtual bool? IsUsingV3 { get; set; }

		public virtual string Name(GivenNameFormat format = GivenNameFormat.FirstAndLast) {
			var possible= format.From(FirstName, LastName);
			if (String.IsNullOrWhiteSpace(possible)) {
				if (String.IsNullOrWhiteSpace(Email))
					return "TempUserId[" + Id + "]";
				return Email;
			}
			return possible;
		}

		public TempUserModel() {
			Created = DateTime.UtcNow;
		}
	}

	public class TempUserModelMap : ClassMap<TempUserModel> {
		public TempUserModelMap() {
			Id(x => x.Id);
			Map(x => x.ImageGuid);
			Map(x => x.FirstName);
			Map(x => x.LastName);
			Map(x => x.Email);
			Map(x => x.EnableWhale);
			Map(x => x.Guid);
			Map(x => x.Created);
			Map(x => x.LastSent);
			Map(x => x.OrganizationId);
			Map(x => x.UserOrganizationId);
			
			Map(x => x.LastSentByUserId);
			Map(x => x.EmailStatusUnseen);
			Map(x => x.EmailStatus).Nullable().CustomType<WebHookEventType>();

      Map(x => x.IsUsingV3);
		}
	}
}