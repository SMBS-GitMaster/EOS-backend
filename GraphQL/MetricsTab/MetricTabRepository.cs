using Amazon.DynamoDBv2;
using FluentNHibernate.Conventions;
using Humanizer;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Core.Models.L10;
using RadialReview.Core.Repositories;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Rocks;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MilestoneStatus = RadialReview.Models.Rocks.MilestoneStatus;
using ModelIssue = RadialReview.Models.Issues.IssueModel;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    MetricsTabQueryModel GetMetricTabById(long id, CancellationToken cancellationToken);

    IQueryable<MetricsTabQueryModel> GetMetricTabsForMeeting(long id, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> AddMetricToTab(MetricAddToTabModel metricAddToTabModel);

    Task<IdModel> CreateMetricTab(MetricTabCreateModel metricTabCreateModel);

    Task<IdModel> DeleteMetricTab(MetricTabDeleteModel metricTabDeleteModel);

    Task<IdModel> EditMetricTab(MetricTabEditModel metricTabEditModel);

    Task<GraphQLResponseBase> RemoveAllMetricTabs(MetricRemoveAllMetricsFromTabModel metricRemoveAllMetricsFromTabModel);

    Task<IdModel> RemoveMetricFromTab(MetricRemoveFromTabModel metricTabRemoveFromTabMutationType);

    Task<IdModel> PinOrUnpinMetricTab(PinMetricTabModel model);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public MetricsTabQueryModel GetMetricTabById(long id, CancellationToken cancellationToken)
    {
      return RepositoryTransformers.TransformMetricTab(MetricTabAccessor.GetMetricTab(caller, id));
    }

    public IQueryable<MetricsTabQueryModel> GetMetricTabsForMeeting(long id, CancellationToken cancellationToken)
    {
      return MetricTabAccessor.GetMetricTabsForMeeting(caller, id)
        .Select(x => RepositoryTransformers.TransformMetricTab(x)).AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<IdModel> AddMetricToTab(MetricAddToTabModel model)
    {
      TrackedMetricColor color;
      Enum.TryParse(model.Color, out color);
      long id = await MetricTabAccessor.AddMetricToTab(caller, model.MetricsTabId, model.MetricId, color);
      return new IdModel(id);
    }

    public async Task<IdModel> CreateMetricTab(MetricTabCreateModel model)
    {
      UnitType units = Enum.Parse<UnitType>(model.Units, true);
      Frequency frequency = Enum.Parse<Frequency>(model.Frequency, true);

      long id = await MetricTabAccessor.AddMetricTab(caller, model.Name, units, frequency, model.IsPinnedToTabBar, model.MeetingId, model.IsVisibleForTeam);

      foreach (CreateTrackedMetricModel request in model.TrackedMetrics)
      {
        await AddMetricToTab(new MetricAddToTabModel()
        {
          Color = request.Color,
          MetricId = request.MetricId,
          MetricsTabId = id
        });
      }

      return new IdModel(id);
    }

    public async Task<IdModel> DeleteMetricTab(MetricTabDeleteModel model)
    {
      long id = await MetricTabAccessor.DeleteMetricTab(caller, model.Id, caller.Id);
      return new IdModel(id);
    }

    public async Task<IdModel> EditMetricTab(MetricTabEditModel model)
    {
      UnitType? units = null;
      Frequency? frequency = null;

      if (model.Frequency != null)
        frequency = Enum.Parse<Frequency>(model.Frequency, true);

      if (model.Units != null)
        units = Enum.Parse<UnitType>(model.Units, true);

      long id = await MetricTabAccessor.EditMetricTab(caller, model.Id, model.Name, units, frequency, model.MeetingId, model.IsVisibleForTeam);
      return new IdModel(id);
    }

    public async Task<GraphQLResponseBase> RemoveAllMetricTabs(MetricRemoveAllMetricsFromTabModel model)
    {
      try
      {
        CancellationToken cancellationToken = new CancellationToken();
        var metrics = GetTrackedMetricsForTab(model.MetricsTabId, cancellationToken).ToList();
        if (metrics.Count == 0)
          throw new Exception("MetricsTabId Does not exist");

        foreach (var metric in metrics)
        {
          await RemoveMetricFromTab(new MetricRemoveFromTabModel { TrackedMetricId = metric.Id });
        }

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return new GraphQLResponseBase(false, ex.Message);
      }
    }

    public async Task<IdModel> RemoveMetricFromTab(MetricRemoveFromTabModel model)
    {
      long id = await MetricTabAccessor.RemoveMetricFromTab(caller, caller.Id, model.TrackedMetricId);
      return new IdModel(id);
    }

    public async Task<IdModel> PinOrUnpinMetricTab(PinMetricTabModel pinMetricTabModel)
    {
      var tabId = await MetricTabAccessor.SetPinMetricTab(caller, pinMetricTabModel.Id, pinMetricTabModel.IsPinnedToTabBar);

      return new IdModel(tabId);
    }

    #endregion

  }

}