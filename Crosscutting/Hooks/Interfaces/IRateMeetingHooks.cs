using NHibernate;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IRateMeetingHooks : IHook
  {
    Task AddRating(ISession session, L10Meeting meeting);

    Task UpdateRating (ISession session, L10Meeting meeting);
  }
}
