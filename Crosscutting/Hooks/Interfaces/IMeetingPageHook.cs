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
  public interface IMeetingPageHook : IHook
  {
    Task CreatePage(ISession s, L10Recurrence.L10Recurrence_Page recurPage);

    Task UpdatePage(ISession s, L10Recurrence.L10Recurrence_Page recurPage);
  }
}
