namespace RadialReview.Repositories
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using global::NHibernate;
  using global::NHibernate.Linq;
  using RadialReview.Core.GraphQL.Models.Mutations;
  using RadialReview.Crosscutting.Hooks;
  using RadialReview.GraphQL.Models.Mutations;
  using RadialReview.Models;
  using RadialReview.Models.L10;
  using RadialReview.Models.Scorecard;
  using RadialReview.Utilities;
  using RadialReview.Utilities.Hooks;
  using static RadialReview.Models.L10.L10Recurrence;

  public partial interface IRadialReviewRepository
  {
    #region Mutations

    Task<IdModel> CreateMetricDivider(MetricDividerCreateModel model, CancellationToken cancellationToken);
    Task<IdModel> EditMetricDivider(MetricDividerEditModel model, CancellationToken cancellationToken);
    Task<IdModel> DeleteMetricDivider(MetricDividerDeleteModel model, CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository
  {
    #region Mutations

    public async Task<IdModel> CreateMetricDivider(MetricDividerCreateModel model, CancellationToken cancellationToken)
    {
      using var session = HibernateSession.GetCurrentSession();
      using var tx = session.BeginTransaction();

      var isValidFrequency = Enum.TryParse<Models.Enums.Frequency>(model.Frequency, out Models.Enums.Frequency frequency);
      if(!isValidFrequency)
        throw new Exception("Invalid Frequency value");

      Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SetDbConnection(dbContext.Database, session.Connection);

      int lastRM = dbContext.L10recurrenceMeasurables.Where(x => x.DeleteTime == null && x.L10recurrenceId == model.MeetingId && x.Measurable.Frequency == model.Frequency && x.IsDivider == false).Select(x => x.IndexInTable.Value).DefaultIfEmpty().Max();
      int lastMD = dbContext.L10recurrenceMetricDividers.Where(x => x.DeleteTime == null && x.L10recurrenceId == model.MeetingId && x.Frequency == model.Frequency).Select(x => x.IndexInTable.Value).DefaultIfEmpty().Max();
      int indexInTable = 1 + Math.Max(lastRM, lastMD);

      int firstRM = dbContext.L10recurrenceMeasurables.Where(x => x.DeleteTime == null && x.L10recurrenceId == model.MeetingId && x.Measurable.Frequency == model.Frequency && x.IsDivider == false).Select(x => x.IndexInTable.Value).DefaultIfEmpty().Min();
      int firstMD = dbContext.L10recurrenceMetricDividers.Where(x => x.DeleteTime == null && x.L10recurrenceId == model.MeetingId && x.Frequency == model.Frequency).Select(x => x.IndexInTable.Value).DefaultIfEmpty().Min();
      int first = Math.Min(firstRM, firstMD);

      var items = await SortableMetrics(model.MeetingId, model.Frequency, first, indexInTable, cancellationToken);
      var recurrenceMeasurable =
          items.Any() && items.First().Object is DatabaseModel.Entities.L10recurrenceMeasurable x
          ? x
          : items.Zip(items.Skip(1)).Where(p => p.First.Object is DatabaseModel.Entities.L10recurrenceMeasurable && p.Second.Object is DatabaseModel.Entities.L10recurrenceMeasurable).Select(p => p.Second.Case(x => x, x => null)).FirstOrDefault();

      var recurrence = recurrenceMeasurable.Recurrence;
      var measurable = recurrenceMeasurable.Measurable;

      if (measurable != null)
      {
        PermissionsUtility
          .Create(session, caller)
          .Or(
            // perm => perm.AdminMeasurable(metricId),
            perm => perm.EditMeasurable(measurable.Id)
          );
      }

      var divider = new L10Recurrence_MetricDivider(){
        CreateTime = DateTime.Now,

        Title = model.Title,
        Height = model.Height,
        IndexInTable = indexInTable,
        Frequency = frequency,
        L10Recurrence = session.Load<L10Recurrence>(model.MeetingId),
      };

      await session.SaveAsync(divider);

      if(recurrenceMeasurable != null)
      {
        await SortAndReorderMetrics(session, model.MeetingId, model.Frequency, divider.IndexInTable, recurrenceMeasurable.IndexInTable.Value, cancellationToken);
      }
      await HooksRegistry.Each<IMetricHook>((session, hook) => hook.CreateMetricDivider(session, caller, divider, session.Get<L10Recurrence_Measurable>(recurrenceMeasurable.Id), session.Get<L10Recurrence>(recurrence.Id)));

      await tx.CommitAsync();
      await session.FlushAsync();

      var idModel = new IdModel(divider.Id);

      return idModel;
    }

    private async Task<L10Recurrence_Measurable> GetL10RecurrenceMeasurableById(ISession s ,long RecurrenceMeasurableId) {
          MeasurableModel alias = null;
          var query =  s.QueryOver<L10Recurrence_Measurable>()
            .JoinAlias(x => x.Measurable, () => alias)
            .Where(x => x.DeleteTime == null && x.Id == RecurrenceMeasurableId);
          query = query.Fetch(SelectMode.Fetch, x => x.L10Recurrence);
          query = query.Fetch(SelectMode.Fetch, x => x.Measurable.AccountableUser);
          var dividerRecurrenceMeasurable= await query.SingleOrDefaultAsync();

          return dividerRecurrenceMeasurable;
    }

    public async Task<IdModel> EditMetricDivider(MetricDividerEditModel model, CancellationToken cancellationToken)
    {
      using var session = HibernateSession.GetCurrentSession();
      using var tx = session.BeginTransaction();

      var divider = await session.GetAsync<L10Recurrence_MetricDivider>(model.Id);
      var dividerRecurrenceMeasurable = await GetFirstRecurrenceMetricAfterDivider(session, divider);
      var divMeasurable = dividerRecurrenceMeasurable.Measurable;

      var perm =
          PermissionsUtility
            .Create(session, caller)
            .EditMeasurable(divMeasurable.Id);

      if (model.Height.HasValue)
      {
        divider.Height = model.Height.Value.Value;
      }

      if (model.Title.HasValue)
      {
        divider.Title = model.Title.Value;
      }

      if (model.MetricId.HasValue && model.MetricId.Value.HasValue)
      {
        var y = await GetFirstRecurrenceMetricAfterDivider(session, divider);

        if (model.MetricId.Value.Value != y.Measurable.Id)
        {
          perm.EditMeasurable(model.MetricId.Value.Value);
          MeasurableModel alias = null;
          var queryRecurrenceMeasurable =
              session.QueryOver<L10Recurrence_Measurable>()
              .JoinAlias(x => x.Measurable, () => alias)
              .Where(x => x.DeleteTime == null)
              .Where(x =>
                x.L10Recurrence.Id == y.L10Recurrence.Id &&
                x.Measurable.Id == model.MetricId.Value
              );

          queryRecurrenceMeasurable = queryRecurrenceMeasurable.Fetch(SelectMode.Fetch, x => x.L10Recurrence);
          queryRecurrenceMeasurable = queryRecurrenceMeasurable.Fetch(SelectMode.Fetch, x => x.Measurable.AccountableUser);
          var recurrenceMeasurable = await queryRecurrenceMeasurable.SingleOrDefaultAsync();

          var recurrence = recurrenceMeasurable.L10Recurrence;
          var measurable = recurrenceMeasurable.Measurable;

          var toDelete =
              session.Query<L10Recurrence_MetricDivider>()
              .Where(x => x.Id != divider.Id)
              .Where(x => x.DeleteTime == null && x.Frequency == x.Frequency)
              .Where(x => x.IndexInTable < recurrenceMeasurable.IndexInTable)
              .OrderByDescending(x => x.IndexInTable)
              .FirstOrDefault()
              ;

          if (false && toDelete != null)
          {
            toDelete.DeleteTime = DateTime.Now;
            await session.SaveAsync(toDelete);
            await HooksRegistry.Each<IMetricHook>((session, hook) => hook.DeleteMetricDivider(session, caller, toDelete, recurrenceMeasurable, recurrence));
          }

          var currentDivider = divider;
          var upward = divider.IndexInTable < recurrenceMeasurable.IndexInTable.Value;
          /*
          ** NOTE: SortAndReorderMetrics will cause the divider to take the position of object at targetIndex.
          **       Because we want divider to preceed the the metric to which it is associated we need the index of the
          **       measurable before the one pointed to by recurrenceMeasurable.  However, it is safe to an index one less that recurrenceMeasurable.IndexInTable
          **       because whatever is at that location will be pushed upward (or downward).
          */
          var targetIndex = recurrenceMeasurable.IndexInTable.Value + (upward ? -1 : 0);
          await SortAndReorderMetrics(session, recurrenceMeasurable.L10Recurrence.Id, recurrenceMeasurable.Measurable.Frequency.ToString(), divider.IndexInTable, targetIndex, cancellationToken);
          await HooksRegistry.Each<IMetricHook>((session, hook) => hook.EditMetricDivider(session, caller, currentDivider, recurrenceMeasurable, recurrenceMeasurable.L10Recurrence));

        }
      }

      await session.SaveAsync(divider);
      await HooksRegistry.Each<IMetricHook>((session, hook) => hook.EditMetricDivider(session, caller, divider, dividerRecurrenceMeasurable, dividerRecurrenceMeasurable.L10Recurrence));
      await tx.CommitAsync();
      await session.FlushAsync();

      var idModel = new IdModel(divider.Id);

      return idModel;
    }

    protected async Task<L10Recurrence_Measurable> GetFirstRecurrenceMetricAfterDivider(ISession session, L10Recurrence_MetricDivider divider)
    {
      var recurrenceMeasurableId = await
          session.Query<L10Recurrence_Measurable>()
          .Where(x => x.DeleteTime == null)
          .Where(x => x.Measurable.Frequency == divider.Frequency && x.L10Recurrence.Id == divider.L10Recurrence.Id)
          .Where(x => divider.IndexInTable < x.IndexInTable)
          .OrderBy(x => x.IndexInTable)
          .Select(x => x.Id)
          .FirstOrDefaultAsync()
          ;

      var recurrenceMeasurable = await GetL10RecurrenceMeasurableById(session, recurrenceMeasurableId);

      return recurrenceMeasurable;
    }

    public async Task<IdModel> DeleteMetricDivider(MetricDividerDeleteModel model, CancellationToken cancellationToken)
    {
      using var session = HibernateSession.GetCurrentSession();
      using var tx = session.BeginTransaction();

      var divider = await session.GetAsync<L10Recurrence_MetricDivider>(model.Id);
      var recurrenceMeasurable = await GetFirstRecurrenceMetricAfterDivider(session, divider);

      PermissionsUtility
        .Create(session, caller)
        .EditMeasurable(recurrenceMeasurable.Measurable.Id);

      var recurrence = recurrenceMeasurable.L10Recurrence;
      var measurable = recurrenceMeasurable.Measurable;

      divider.DeleteTime = DateTime.Now;

      await session.SaveAsync(divider);
      await HooksRegistry.Each<IMetricHook>((session, hook) => hook.DeleteMetricDivider(session, caller, divider, recurrenceMeasurable, recurrence));

      await tx.CommitAsync();
      await session.FlushAsync();

      var idModel = new IdModel(divider.Id);

      return idModel;
    }
    #endregion
  }
}
