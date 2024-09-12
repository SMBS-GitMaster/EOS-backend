using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities {
	public static class DbUtil
	{
		public static void UpdateList<T>(this ISession s, IEnumerable<T> oldValues, IEnumerable<T> newValues, DateTime? now = null) where T : IOneToMany
		{
			now = now ?? DateTime.UtcNow;
			if (oldValues == null){
				foreach (var a in newValues)
					if (((ILongIdentifiable)a).Id >= 0)
						s.Save(a);
			}else{

				var update = SetUtility.AddRemove(oldValues, newValues, x => ((ILongIdentifiable)x).Id);
				foreach (var u in update.RemovedValues)
				{
					((IDeletable)u).DeleteTime = now;
					if (((ILongIdentifiable)u).Id>=0)
						s.Update(u);
				}
				foreach (var a in update.AddedValues)
					if (((ILongIdentifiable)a).Id >= 0)
						s.Save(a);

			}
		}


	}
}
