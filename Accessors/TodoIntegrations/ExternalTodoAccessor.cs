using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models;
using RadialReview.Models.Todo;
using RadialReview.Utilities;

namespace RadialReview.Accessors.TodoIntegrations {
	public class ExternalTodoAccessor
	{

		public static void AddLink(ISession s, PermissionsUtility perms, INotesProvider notesProvider, ForModel associatedWith, long forUserId, TodoModel model)
		{
			perms.EditForModel(associatedWith).ViewUserOrganization(forUserId,false);

			var creds = s.QueryOver<AbstractTodoCreds>().Where(x =>
					x.DeleteTime == null &&
					x.AssociatedWith.ModelId == associatedWith.ModelId &&
					x.AssociatedWith.ModelType == associatedWith.ModelType &&
					x.ForRGMId == forUserId
				).List().ToList();

			foreach (var c in creds){
				try{
					c.AddTodo(s, notesProvider, model);
				}catch (Exception){
					//What do?
				}
			}
		}

		public static List<AbstractTodoCreds> GetExternalLinksForModel(ISession s,PermissionsUtility perms, ForModel model)
		{
			perms.ViewForModel(model);

			var o= s.QueryOver<AbstractTodoCreds>().Where(x =>
				x.DeleteTime == null &&
				x.AssociatedWith.ModelType == model.ModelType &&
				x.AssociatedWith.ModelId == model.ModelId
			).List().ToList();

			foreach (var x in o){
				x.ForRGM.GetNameExtended();
				x.ForRGM.GetImageUrl();
			}
			return o;
		}

		public static void DetatchLink(UserOrganizationModel caller, long todoCredId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var cred = s.Get<AbstractTodoCreds>(todoCredId);

					PermissionsUtility.Create(s, caller).EditForModel(cred.AssociatedWith);
					cred.DeleteTime = DateTime.UtcNow;
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}
