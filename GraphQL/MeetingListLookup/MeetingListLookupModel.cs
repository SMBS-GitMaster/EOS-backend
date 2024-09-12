using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RadialReview.Core.GraphQL.MeetingListLookup
{
  public class MeetingListLookupModel : MeetingLookupModel
  {
    #region Base Properties

    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion
    public bool UserIsAttendee { get; set; }
    public double? LastViewedTimestamp { get; set; }
    public double CreatedTimestamp { get; set; }
    public string MeetingType { get; set; }
    public int? FavoritedSortingPosition { get; set; }
    public long? FavoriteId { get; set; }
    public double? FavoritedTimestamp { get; set; }
    public long UserId { get; set; }
    public bool Archived { get; set; }
    public IQueryable<MeetingAttendeeQueryModelLookup> AttendeesLookup { get; set; }

    #region subscription collections

    public static class Collections
    {
      public enum MeetingAttendeeLookup
      {
        MeetingAttendeeLookups
      }
    }
    #endregion
  }
}