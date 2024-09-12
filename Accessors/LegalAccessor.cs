using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Encrypt;
using System;
using System.Threading.Tasks;

namespace RadialReview.Accessors {


	public class TermsAcceptanceModel {
		public virtual string Id { get; set; }
		public virtual string UserId { get; set; }
		public virtual string UserName { get; set; }
		public virtual string Kind { get; set; }
		public virtual string Terms { get; set; }
		public virtual string TermsSHA1 { get; set; }
		public virtual DateTime DateGenerated { get; set; }
		public virtual DateTime? DateSubmitted { get; set; }
		public virtual bool Accepted { get; set; }
		public virtual bool IsLocked { get { return DateSubmitted != null; } }
		public class Map : ClassMap<TermsAcceptanceModel> {
			public Map() {
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);
				Map(x => x.UserId);
				Map(x => x.UserName);
				Map(x => x.Kind);
				Map(x => x.Terms).Length(40000);
				Map(x => x.TermsSHA1);
				Map(x => x.Accepted);
				Map(x => x.DateGenerated);
				Map(x => x.DateSubmitted);
			}

		}
	}

	public class TermsVM {
		public string Id { get; set; }
		public DateTime Generated { get; set; }
		public string Terms { get; set; }
		public string TermsSHA1 { get; set; }
		public TermsVM() { }

		public TermsVM(TermsAcceptanceModel model) {
			Generated = model.DateGenerated;
			Id = model.Id.ToString();
			Terms = model.Terms;
			TermsSHA1 = model.TermsSHA1;
		}

	}

	public class LegalAccessor {


		public static async Task<TermsVM> CreateTerms(UserModel caller, string terms,string kind) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var model = CreateTerms(s, caller, terms,kind);
					tx.Commit();
					s.Flush();
					return model;
				}
			}
		}

		public static TermsVM CreateTerms(ISession s, UserModel caller, string terms, string kind) {
			var model = new TermsAcceptanceModel {
				DateGenerated = DateTime.UtcNow,
				Terms = terms,
				TermsSHA1 = Crypto.UniqueHash(terms),
				UserName = caller.UserName,
				UserId = caller.Id,
				Kind = kind
			};
			s.Save(model);
			return new TermsVM(model);
		}

		public static async Task<bool> SubmitTerms(UserModel caller, Guid termsId, bool accept) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var t= await SubmitTerms(s, caller, termsId, accept);
					tx.Commit();
					s.Flush();
					return t;
				}
			}

		}

		public static async Task<bool> SubmitTerms(ISession s, UserModel caller, Guid termsId, bool accept) {
			var model = s.Get<TermsAcceptanceModel>(""+termsId);

			if (model==null) {
				throw new PermissionsException("Terms not found.");
			}

			if (model.IsLocked) {
				throw new PermissionsException("Terms already submitted.");
			}
			if (model.UserId != caller.Id) {
				throw new PermissionsException("Cannot accept terms on behalf of another person.");
			}
			if (model.UserName != caller.UserName) {
				throw new PermissionsException("Usernames do not match records.");
			}

			model.DateSubmitted = DateTime.UtcNow;
			model.Accepted = accept;

			s.Update(model);
			await HooksRegistry.Each<ILegalHooks>((ses, x) => x.SubmitTerms(ses, caller.Id, termsId.ToString(), model.Kind, model.TermsSHA1, accept));
			return accept;
		}
	}
}