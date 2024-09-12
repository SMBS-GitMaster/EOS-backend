using DocumentFormat.OpenXml.Office2010.ExcelAc;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.DatabaseModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.GraphQL.Models
{
  public class WorkspaceQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public bool Archived { get; set; }

    public double CreatedTimestamp { get; set; }

    public string Name { get; set; }

    public long? FavoriteId { get; set; }

    public bool Favorited { get; set; }

    public double? FavoritedTimestamp { get; set; }

    public int? FavoritedSortingPosition { get; set; }

    public double? LastViewedTimestamp { get; set; }

    public long UserId { get; set; }

    public List<WorkspaceTileQueryModel> Tiles { get; set; }

    public bool IsPrimary { get; set; }

    public gqlDashboardType WorkspaceType { get; set; }

    public long? WorkspaceParentId { get; set; }

    #endregion

    #region Subscription Data

    public static class Associations
    {

      public enum WorkspaceUser
      {
        WorkspaceUser
      }

      public enum TileNodes2
      {
        Tiles
      }

      public enum PersonalNotes2
      {
        Notes
      }
    }

    public static class Collections
    {

      public enum TileNodes
      {
        Tiles
      }

      public enum PersonalNotes
      {
        Notes
      }

    }

    #endregion

  }
}