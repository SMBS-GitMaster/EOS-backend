using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public class IOrgChartSeatHookUpdates
  {
    public bool SupervisorId { get; set; }
    public bool UserIds {  get; set; }
    public bool PositionTitle { get; set; }
    public bool Roles { get; set; }
  }
  public interface IOrgChartSeatHook : IHook
  {
    Task AttachOrgChartSeatUser(ISession s, UserOrganizationModel caller, AccountabilityNode node, long[] removedUser, long[] addUser);
    Task UpdateOrgChartSeat(ISession s, UserOrganizationModel caller, AccountabilityNode node, IOrgChartSeatHookUpdates updates);
    Task CreateOrgChartSeat(ISession s, UserOrganizationModel caller, AccountabilityNode node, bool rootNode = false);
    Task CreateOrgChartSeatRole(ISession s, UserOrganizationModel caller, SimpleRole role);
    Task UpdateOrgChartSeatRole(ISession s, UserOrganizationModel caller, long roleId);
    Task DeleteOrgChartSeatRole(ISession s, UserOrganizationModel caller, long roleId);
    Task DeleteOrgChartSeat(ISession s, AccountabilityNode node);
    Task AttachOrDetachOrgChartSeatSupervisor(ISession s, UserOrganizationModel caller, AccountabilityNode oldNode, AccountabilityNode newNode);
  }
}
