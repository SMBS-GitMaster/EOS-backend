using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.PermissionsListers;
using System;
using System.Collections.Generic;
using System.Linq;
namespace RadialReview.Accessors {
  public class AssignableNodesCollection {
    public List<SelectListItem> AllNodes { get; set; }

    public DefaultDictionary<long, List<SelectListItem>> DirectChildren { get; set; }
  }

  public class SelectListAccessor {
    public static List<SelectListItem> GetL10RecurrenceAdminable(UserOrganizationModel caller, long userId, Func<NameIdPermissions, bool> selected = null, bool displayNonAdmin = true, bool allowHtmlText = true) {
      selected = selected ?? new Func<NameIdPermissions, bool>(x => false);
      var res = L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(caller, userId)
        .Where(x => displayNonAdmin || x.CanAdmin)
        .Select(x => new SelectListItem {
          Disabled = !x.CanAdmin,
          Selected = selected(x),
          Text = WrapName(x.Name, "meeting") + (x.CanAdmin || !allowHtmlText ? "" : " <small><i>(You are not an admin for this meeting)<i></small>"),
          Value = "" + x.Id
        }).OrderBy(x => x.Disabled)
        .ToList();
      return res;
    }

    public static IEnumerable<V3TinyRecurrence> GetL10RecurrenceAdminable(UserOrganizationModel caller, long userId)
    {
      var recurencesPermissions = L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(caller, userId);

      var tinyRecurrences = recurencesPermissions.Select(recPerms => new V3TinyRecurrence
      {
        Id = recPerms.Id,
        Name = recPerms.Name,
        CurrentUserCanAdmin = recPerms.CanAdmin,
    }).ToList();

      return tinyRecurrences;
    }

    public static List<SelectListItem> GetL10RecurrenceEditable(UserOrganizationModel caller, long userId, Func<NameIdPermissions, bool> selected = null, bool displayNonEditable = true, bool allowHtmlText = true) {
      selected = selected ?? new Func<NameIdPermissions, bool>(x => false);
      return L10PermissionsHelper.GetL10RecurrencesAndPermissionsForUser(caller, userId).Where(x => displayNonEditable || x.CanEdit).Select(x => new SelectListItem { Disabled = !x.CanEdit, Selected = selected(x), Text = WrapName(x.Name, "meeting") + (x.CanEdit || !allowHtmlText ? "" : " <small><i>(You are not permitted to edit this meeting)<i></small>"), Value = "" + x.Id }).OrderBy(x => x.Disabled).ToList();
    }

    public static List<SelectListItem> GetUsersWeCanCreateRocksFor(UserOrganizationModel caller, TermsCollection terms, Func<NameIdCreatablePermissions, bool> selected = null, bool displayNonCreatable = true) {
      selected = selected ?? new Func<NameIdCreatablePermissions, bool>(x => false);
      var GOALS = terms.GetTerm(TermKey.Goals);
      return UserPermissionsHelper.GetUsersWeCanCreateRocksFor(caller, caller.Id, caller.Organization.Id)
        .Where(x => displayNonCreatable || x.CanCreate)
        .Select(x => new SelectListItem {
          Disabled = !x.CanCreate,
          Selected = selected(x),
          Text = WrapName(x.Name, "user") + (x.CanCreate ? "" : $@" <small><i>(You are not permitted to edit {GOALS} for this user)<i></small>"),
          Value = "" + x.Id
        }).OrderBy(x => x.Disabled).ToList();
    }

    public static List<SelectListItem> GetUsersWeCanCreateMeaurableFor(UserOrganizationModel caller, TermsCollection terms, Func<NameIdCreatablePermissions, bool> selected = null, bool displayNonCreatable = true) {
      selected = selected ?? new Func<NameIdCreatablePermissions, bool>(x => false);
      var MEASURABLES = terms.GetTerm(TermKey.Measurables);
      return UserPermissionsHelper.GetUsersWeCanCreateMeasurablesFor(caller, caller.Id, caller.Organization.Id).Where(x => displayNonCreatable || x.CanCreate).Select(x => new SelectListItem { Disabled = !x.CanCreate, Selected = selected(x), Text = WrapName(x.Name, "user") + (x.CanCreate ? "" : $@" <small><i>(You are not permitted to edit {MEASURABLES}  for this user)<i></small>"), Value = "" + x.Id }).OrderBy(x => x.Disabled).ToList();
    }


    public static AssignableNodesCollection GetNodesWeCanAssignUsersTo(UserOrganizationModel caller, TermsCollection terms, Func<NameIdCreatablePermissions, bool> selected = null, bool displayNonCreatable = true, bool allowCreateSeat = true, bool allowNoManager = true, bool onlyNoteworthy = true) {
      selected = selected ?? new Func<NameIdCreatablePermissions, bool>(x => false);
      var nodesAndRoot = UserPermissionsHelper.GetNodesWeCanCreateUsersUnder(caller, caller.Id, caller.Organization.Id, true);
      var nodesWeCanCreate = nodesAndRoot.Nodes.Where(x => displayNonCreatable || x.CanCreate);
      if (onlyNoteworthy && nodesWeCanCreate.Any(x => x.Noteworthy))
        nodesWeCanCreate = nodesWeCanCreate.Where(x => x.Noteworthy);
      //manager selection
      var allNodes = nodesWeCanCreate.Select(x => new SelectListItem { Disabled = !x.CanCreate, Selected = selected(x), Text = WrapName(x.Name, "seat") + (x.CanCreate ? "" : " <small><i>(You are not permitted add seats under this seat)<i></small>"), Value = "" + x.Id });
      if (allowNoManager) {
        allNodes = (new SelectListItem() { Text = "<i class='no-manager'>No manager</i>", Value = "" + AccountabilityAccessor.MANAGERNODE_NO_MANAGER }.AsList()).Union(allNodes);
      }

      allNodes = allNodes.OrderBy(x => x.Disabled);
      //Seat selection
      var createNewSeatText = "<i>Create a new seat</i>";
      var seatLookup = nodesWeCanCreate.GroupBy(x => x.GroupId).ToDefaultDictionary(x => x.Key, list => {
        var seatsForKey = list.Select(x => new SelectListItem { Disabled = !x.CanCreate, Selected = selected(x), Text = WrapName(x.Name, "seat") + (x.CanCreate ? "" : " <small><i>(You are not permitted add users under this seat)<i></small>"), Value = "" + x.Id });
        if (allowCreateSeat) {
          seatsForKey = new SelectListItem() { Text = createNewSeatText, Value = "" + AccountabilityAccessor.SEATNODE_CREATE_SEAT, }.AsList().Union(seatsForKey);
        }

        return seatsForKey.OrderBy(x => x.Disabled).ToList();
      }, x => {
        var deflt = new List<SelectListItem>();
        if (allowCreateSeat) {
          deflt.Add(new SelectListItem() { Text = createNewSeatText, Value = "" + AccountabilityAccessor.SEATNODE_CREATE_SEAT, });
        }

        return deflt;
      });
      if (allowNoManager) {

        seatLookup[AccountabilityAccessor.MANAGERNODE_NO_MANAGER] = new List<SelectListItem>()
        {new SelectListItem()
        {Text = "<i>Outside of the "+terms.GetTerm(TermKey.OrganizationalChart)+"</i>", Value = "" + AccountabilityAccessor.SEATNODE_OUTSIDE_ACCOUNTABILITY_CHART, }, new SelectListItem()
        {Text = "<i>Top of the "+terms.GetTerm(TermKey.OrganizationalChart)+"</i>", Value = "" + AccountabilityAccessor.SEATNODE_CREATE_SEAT, }};
      }

      return new AssignableNodesCollection() { AllNodes = allNodes.ToList(), DirectChildren = seatLookup, };
    }

    private static string WrapName(string name, string type) {
      if (string.IsNullOrWhiteSpace(name))
        return "<i>-unnamed " + type + "-</i>";
      return name;
    }
  }
}
