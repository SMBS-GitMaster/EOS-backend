using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class LoadMeetingModel
  {

    #region Properties

    public bool LoadUsers { get; set; }
    public bool LoadMeasurables { get; set; }
    public bool LoadRocks { get; set; }
    public bool LoadVideos { get; set; }
    public bool LoadNotes { get; set; }
    public bool LoadPages { get; set; }
    public bool LoadAudio { get; set; }
    public bool LoadConclusionActions { get; set; }
    public bool LoadFavorites { get; set; }
    public bool LoadSettings { get; set; }

    #endregion

    #region Public Methods

    public static LoadMeetingModel False()
    {
      return new LoadMeetingModel();
    }

    public LoadMeeting ToLoadMeeting()
    {
      return new LoadMeeting()
      {
        LoadAudio = LoadAudio,
        LoadConclusionActions = LoadConclusionActions,
        LoadMeasurables = LoadMeasurables,
        LoadVideos = LoadVideos,
        LoadNotes = LoadNotes,
        LoadPages = LoadPages,
        LoadRocks = LoadRocks,
        LoadUsers = LoadUsers,
      };
    }

    public static LoadMeetingModel True()
    {
      return new LoadMeetingModel()
      {
        LoadMeasurables = true,
        LoadVideos = true,
        LoadRocks = true,
        LoadUsers = true,
        LoadPages = true,
        LoadNotes = true,
        LoadAudio = true,
        LoadConclusionActions = true,
        LoadSettings = true,
        LoadFavorites = true,
      };
    }


    #endregion

  }
}