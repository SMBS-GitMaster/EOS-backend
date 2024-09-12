using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RadialReview.Models {
	public class WebhookDetails : IHistorical {
		public WebhookDetails() {
			CreateTime = DateTime.UtcNow;
		}

		public virtual string Id { get; set; }
		public virtual string Email { get; set; }
		public virtual string UserId { get; set; }
		[JsonIgnore]
		public virtual UserModel User { get; set; }
		public virtual string ProtectedData { get; set; }
		public virtual IList<WebhookEventsSubscription> WebhookEventsSubscription { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public class Map : ClassMap<WebhookDetails> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Email).Length(256);
				References(x => x.User).Column("UserId").LazyLoad().ReadOnly();
				Map(x => x.UserId).Length(64).Column("UserId");
				Map(x => x.ProtectedData);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				HasMany(x => x.WebhookEventsSubscription).LazyLoad();
			}
		}
	}

	public class WebhookEventsSubscription : IHistorical {
		public virtual long Id { get; set; }
		public virtual string WebhookId { get; set; }
		public virtual string EventName { get; set; }
		public virtual WebhookDetails Webhook { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public class Map : ClassMap<WebhookEventsSubscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.WebhookId).Column("WebhookId");
				Map(x => x.EventName);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				References(x => x.Webhook).Column("WebhookId").LazyLoad().ReadOnly();
			}
		}
	}
}