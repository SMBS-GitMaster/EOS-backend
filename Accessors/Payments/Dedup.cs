using RadialReview.Accessors.Payments;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {

  public class Dedup {

    private static string FormatDedupEmail(string email) {
      return (email??"--").Trim().ToLower();
    }
    private HashSet<string> ExcludeL10Users { get; set; }
    private HashSet<string> ExcludeQCUsers { get; set; }

    public bool ShouldExcludingL10User(string email) {
      if (email == null)
        return false;
      return ExcludeL10Users.Contains(FormatDedupEmail(email));
    }
    public bool ShouldExcludeQCUser(string email) {
      if (email == null)
        return false;
      return ExcludeQCUsers.Contains(FormatDedupEmail(email));
    }

    public void SetQCUserAsExcluded(string email) {
      ExcludeQCUsers.Add(FormatDedupEmail(email));
    }
    public void SetL10UserAsExcluded(string email) {
      ExcludeL10Users.Add(FormatDedupEmail(email));
    }

    public Dedup() {
      ExcludeQCUsers = new HashSet<string>();
      ExcludeL10Users = new HashSet<string>();
      PredupWithPrices = new DefaultDictionary<string, decimal>(x => 0.0m);
      SeenNotFree = new HashSet<string>();
    }



    /// <summary>
    /// For ordering the users in such a way as to maximize amount charged for joint accounts..
    ///
    /// Algorithm is to make a list of users, order by the sum of what they'd be charged (if not for free seats)
    /// orgs = organizations ordered by per-seat price, descending
    /// for each org in orgs
    ///   list1 = seenNotFree seats ordered by user-charge list desending
    ///   list2 = notSeenOrFree seats order by user-charge list desending
    ///   list = list1 + list2
    ///
    ///   Apply free seats to first N users
    ///   charge for all SeenNotFree seats
    ///   mark all SeenNotFree seats
    /// 
    /// </summary>


    private DefaultDictionary<string, decimal> PredupWithPrices { get; set; }
    private HashSet<string> SeenNotFree { get; set; }

    public void PreAddUser(UQ user, SimplePlan plan) {
      var email = user.Email;
      var pricePerUser = 0.0m;
      if (plan.MeetingEnabled)
        pricePerUser+= plan.MeetingPricePerPerson;
      if (plan.PeopleToolsEnabled)
        pricePerUser+= plan.PeopleToolsPricePerPerson;
      PredupWithPrices[FormatDedupEmail(email)] += pricePerUser;
    }

    public IEnumerable<T> SortUsers<T>(IEnumerable<T> list, Func<T, string> emailSelector) {

      //var orderedKeys = PredupWithPrices.OrderByDescending(x => x.Value).ToList();
      var seenNotFree = list.Where(x => SeenNotFree.Contains(FormatDedupEmail(emailSelector(x)))).ToList();
      var notSeenOrFree = list.Where(x => !SeenNotFree.Contains(FormatDedupEmail(emailSelector(x)))).ToList();

      var seenNotFreeOrdered = seenNotFree.OrderByDescending(x => PredupWithPrices[FormatDedupEmail(emailSelector(x))]).ThenBy(x=> FormatDedupEmail(emailSelector(x))).ToList();
      var notSeenOrFreeOrdered = notSeenOrFree.OrderByDescending(x => PredupWithPrices[FormatDedupEmail(emailSelector(x))]).ThenBy(x => FormatDedupEmail(emailSelector(x))).ToList();

      var output = new List<T>();

      output.AddRange(seenNotFreeOrdered);
      output.AddRange(notSeenOrFreeOrdered);
      return output;
    }

    public void MarkSeenNotFree(string email) {
      SeenNotFree.Add(FormatDedupEmail(email));
    }

  }
}
