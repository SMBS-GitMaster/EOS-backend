namespace RadialReview.Repositories
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
  using RadialReview.Core.GraphQL.Enumerations;
  using RadialReview.Core.Repositories;
  using RadialReview.Crosscutting.Hooks;
  using RadialReview.DatabaseModel.Entities;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Models.Mutations;
  using RadialReview.Utilities;
  using RadialReview.Utilities.Hooks;
  using static RadialReview.Models.L10.L10Recurrence;


  public partial interface IRadialReviewRepository
  {
    #region Queries

    IQueryable<((long recurrenceId, long measurableId) keys, MetricDividerQueryModel divider)> GetMetricDividers(IReadOnlyList<(long recurrenceId, long measurableId)> keys);
    IQueryable<(long recurrenceId, MetricDividerQueryModel divider)> GetMetricDividersForMeetings(IReadOnlyList<long> keys);

    #endregion

    #region Mutations

    Task SortAndReorderMetrics(MetricDividerSortModel model, CancellationToken cancellationToken);

    #endregion

  }

  internal class MetricDividerAlternateKey
  {
    public long RecurrenceId { get; set; }
    public long MeasurableId { get; set; }
  }

  public partial class RadialReviewRepository
  {
    #region Queries

    public IQueryable<((long recurrenceId, long measurableId) keys, MetricDividerQueryModel divider)> GetMetricDividers(IReadOnlyList<(long recurrenceId, long measurableId)> keys)
    {
      var keys_ = keys.Select(t => new MetricDividerAlternateKey{ RecurrenceId = t.recurrenceId, MeasurableId = t.measurableId }).ToList();

      var metricsQuery =
          dbContext.L10recurrenceMeasurables
          .Where(rm => rm.DeleteTime == null)
          .Where(rm => rm.Measurable.Frequency == "WEEKLY")
          .Where(rm => rm.IsDivider == false)
          .Join(
            keys_.MakeQueryable(
              dbContext,
              [
                ("RecurrenceId", row => row.RecurrenceId),
                ("MeasurableId", row => row.MeasurableId)
              ]
            ),
            left  => new {RecurrenceId = left.L10recurrenceId.Value},
            right => new {RecurrenceId = right.RecurrenceId},
            (left, right) => left
          )
          ;

      var metrics =
          metricsQuery
          .OrderBy(metric => metric.IndexInTable)
          .AsNoTracking()
          .ToList()
          ;

      var dividersQuery =
          dbContext.L10recurrenceMetricDividers
          .Where(d => d.DeleteTime == null)
          .Where(d => d.Frequency == "WEEKLY")
          .Join(
            keys_.MakeQueryable(
              dbContext,
              [
                ("RecurrenceId", row => row.RecurrenceId),
                ("MeasurableId", row => row.MeasurableId)
              ]
            ),
            left  => left.L10recurrenceId,
            right => right.RecurrenceId,
            (left, right) => left
          )
          ;

      var dividers =
          dividersQuery
          .OrderBy(divider => divider.IndexInTable)
          .AsNoTracking()
          .ToList()
          ;

      var query =
          from divider in dividers
          let metric = metrics.Where(m => divider.IndexInTable < m.IndexInTable).FirstOrDefault()
          where metric != null
          join filter in keys_ on
            new {RecurrenceId = divider.L10recurrenceId.Value, MeasurableId = metric.MeasurableId.Value}
            equals
            new {RecurrenceId = filter.RecurrenceId, MeasurableId = filter.MeasurableId}
          select new {
              RecurrenceId = metric.L10recurrenceId.Value,
              MeasurableId = metric.MeasurableId.Value,
              IndexInTable = divider.IndexInTable,
              Divider = new L10Recurrence_MetricDivider{
                Id = divider.Id,
                Title = divider.Title,
                Height = divider.Height ?? 0
              },
            };

      var result = query.Select(x => (ValueTuple.Create(x.RecurrenceId, x.MeasurableId), x.Divider.Transform(x.IndexInTable.Value)));

      /*
      ** NOTE:
      ** LATERAL JOIN is needed to make this query a true queryable.
      ** We need to update the database to be MySQL 8.0 compatible
      ** to be able to use LATERAL JOIN.
      */
      return result.AsQueryable();
    }

    public IQueryable<(long recurrenceId, MetricDividerQueryModel divider)> GetMetricDividersForMeetings(IReadOnlyList<long> keys)
    {
      var keys_ = keys.Select(key => new MetricDividerAlternateKey{ RecurrenceId = key }).ToList();

      var dividers =
          dbContext.L10recurrenceMetricDividers
          .Where(d => d.DeleteTime == null)
          .Join(
            keys_.MakeQueryable(
              dbContext,
              [
                ("RecurrenceId", row => row.RecurrenceId),
              ]
            ),
            left  => left.L10recurrenceId.Value,
            right => right.RecurrenceId,
            (left, right) =>
              ValueTuple.Create(right.RecurrenceId, new MetricDividerQueryModel {
                Id = left.Id,
                IndexInTable = left.IndexInTable.Value,
                // DateCreated = left.DateCrated,
                Height = left.Height.Value,
                Title  = left.Title,
                // NOTE: Because Fequency is stored as a string in the database, the following conditional has to be used to read the frequency while keeping the expression queryable!
                Frequency =
                  left.Frequency == "DAILY"     ? gqlMetricFrequency.DAILY :
                  left.Frequency == "WEEKLY"    ? gqlMetricFrequency.WEEKLY :
                  left.Frequency == "MONTHLY"   ? gqlMetricFrequency.MONTHLY :
                  left.Frequency == "QUARTERLY" ? gqlMetricFrequency.QUARTERLY : gqlMetricFrequency.WEEKLY
              }
            )
          )
          ;

      return dividers;
    }

    #endregion

    #region Mutations

    public async Task SortAndReorderMetrics(MetricDividerSortModel model, CancellationToken cancellationToken)
    {
      using var session = HibernateSession.GetCurrentSession();
      using var txNH = session.BeginTransaction(); // Hooks will run when this scope completes

      dbContext.Database.SetDbConnection(session.Connection);

      var toMove =
          await dbContext.L10recurrenceMeasurables
          .Where(x => x.DeleteTime == null)
          .Where(x => x.L10recurrenceId == model.MeetingId && x.MeasurableId == model.Id)
          .Include(d => d.Recurrence)
          .Include(d => d.Measurable.AccountableUser)
          .AsNoTracking()
          .SingleOrDefaultAsync(cancellationToken)
          ;

      await SortAndReorderMetrics(session, model.MeetingId, toMove.Measurable.Frequency, toMove.IndexInTable.Value, model.SortOrder, cancellationToken);

      await txNH.CommitAsync(cancellationToken);
    }

    internal async Task SortAndReorderMetrics(global::NHibernate.ISession session, long recurrenceId, string frequency, int indexToMove, int sortOrder, CancellationToken cancellationToken)
    {
      var x = await SortAndReorderMetricsQueries(recurrenceId, frequency, indexToMove, sortOrder, cancellationToken);

      // Begin Bulk update rows

      await x.MeasurableBulk.ExecuteUpdateAsync(
        bulk =>
          bulk
          .SetProperty(x => x.IndexInTable, x => x.NewIndexInTable),
        cancellationToken
      );

      await x.DividerBulk.ExecuteUpdateAsync(
        bulk =>
          bulk
          .SetProperty(x => x.IndexInTable, x => x.NewIndexInTable),
        cancellationToken
      );

      // End Bulk update rows

      await HooksRegistry.Each<IMetricHook>((session, hook) => hook.SortMetricDivider(session, caller, x.Measurables, x.Dividers, x.Recurrence));
    }

    public class SortOrderUpdate
    {
      public long Id { get; set; }
      public int? IndexInTable { get; set; }
      public int NewIndexInTable { get; set; }
    }

    public record BulkQueries(IQueryable<SortOrderUpdate> MeasurableBulk, IQueryable<SortOrderUpdate> DividerBulk, long[] Measurables, long[] Dividers, Models.L10.L10Recurrence Recurrence);
    public abstract class Either<L, R>
    {
      protected Either(){}
      public abstract object Object { get; }

      public abstract C Case<C>(Func<L, C> f, Func<R, C> g);

      public sealed class Left(L item) : Either<L, R>
      {
        public override object Object { get { return Item; } }
        public L Item { get; init; } = item;

        public override C Case<C>(Func<L, C> f, Func<R, C> g)
        {
          return f(Item);
        }
      }

      public sealed class Right(R item) : Either<L, R>
      {
        public override object Object { get { return Item; } }
        public R Item { get; init; } = item;

        public override C Case<C>(Func<L, C> f, Func<R, C> g)
        {
          return g(Item);
        }
      }
    }

    public async Task<IEnumerable<Either<L10recurrenceMeasurable, L10recurrenceMetricDivider>>> SortableMetrics(long recurrenceId, string frequency, int indexToMove, int sortOrder, CancellationToken cancellationToken)
    {
      var upwardIteration = sortOrder < indexToMove;

      var (lower, upper) =
          upwardIteration
          ? (sortOrder, indexToMove)  // Moving to a lower index
          : (indexToMove, sortOrder)  // Moving to a higher index
          ;

      var recurrenceMeasurables = await
          dbContext.L10recurrenceMeasurables
            .Include(x => x.Measurable)
            .Include(x => x.Recurrence)
            .Where(x => x.DeleteTime == null)
            .Where(x =>
              x.L10recurrenceId      == recurrenceId &&
              x.Measurable.Frequency == frequency
            )
            .Where(x => lower <= x.IndexInTable && x.IndexInTable <= upper)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            ;

      var dividers = await
          dbContext.L10recurrenceMetricDividers
            .Where(x => x.DeleteTime == null)
            .Where(x =>
              x.L10recurrenceId == recurrenceId &&
              x.Frequency       == frequency
            )
            .Where(x => lower <= x.IndexInTable && x.IndexInTable <= upper)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            ;

      var ms = recurrenceMeasurables.Select(rm => new Either<L10recurrenceMeasurable, L10recurrenceMetricDivider>.Left(rm));
      var ds = dividers.Select(d => new Either<L10recurrenceMeasurable, L10recurrenceMetricDivider>.Right(d));
      var items = ms.Concat<Either<L10recurrenceMeasurable, L10recurrenceMetricDivider>>(ds).OrderBy(x => x.Case(x => x.IndexInTable, x => x.IndexInTable)).ToList();

      return items;
    }

    public async Task<BulkQueries> SortAndReorderMetricsQueries(long recurrenceId, string frequency, int indexToMove, int sortOrder, CancellationToken cancellationToken)
    {
      var upwardIteration = sortOrder < indexToMove;

      var items = await SortableMetrics(recurrenceId, frequency, indexToMove, sortOrder, cancellationToken);

      var map =
          items
          .ToDictionary(
            x => x.Case(m => m.IndexInTable.Value, d => d.IndexInTable.Value),
            x => x
          );

      var keys = map.Keys.OrderBy(key => key).ToArray();
      var keys_shifted = keys.Skip(1).Concat(keys.Take(1));

      var itemKeyPairs =
          upwardIteration
          ? keys.Zip(keys_shifted)
          : keys_shifted.Zip(keys)
          ;

      var measurableList = new List<Tuple<L10Recurrence_Measurable, int>>();
      var dividerList = new List<Tuple<L10Recurrence_MetricDivider, int>>();

      foreach ((int o, int n) in itemKeyPairs)
      {
        var @old = map[o];
        var @new = map[n];

        switch (@old.Object)
        {
          case L10recurrenceMeasurable rm:
            measurableList.Add(Tuple.Create(new L10Recurrence_Measurable(){
              Id = rm.Id,
              IndexInTable = rm.IndexInTable,
            }, @new.Case(m => m.IndexInTable.Value, d => d.IndexInTable.Value)));
            break;

          case L10recurrenceMetricDivider md:
            dividerList.Add(Tuple.Create(new L10Recurrence_MetricDivider(){
              Id = md.Id,
              IndexInTable = md.IndexInTable.Value,
            }, @new.Case(m => m.IndexInTable.Value, d => d.IndexInTable.Value)));
            break;
        }
      }

      var measurableListQueryable =
          measurableList
          .Select(x => new SortOrderUpdate {
            Id = x.Item1.Id,
            IndexInTable = x.Item1.IndexInTable,
            NewIndexInTable = x.Item2
          })
          .MakeQueryable(
            dbContext,
            [
              ("Id", row => row.Id),
              ("IndexInTable", row => row.IndexInTable),
              ("NewIndexInTable", row => row.NewIndexInTable)
            ]
          );

      var measurableBulk =
          dbContext.L10recurrenceMeasurables
          .Join(
            measurableListQueryable,
            left  => left.Id,
            right => right.Id,
            (left, right) => new SortOrderUpdate {
              Id = left.Id,
              IndexInTable = left.IndexInTable.Value,
              NewIndexInTable = right.NewIndexInTable
            }
          );

      var dividerListQueryable =
          dividerList
          .Select(x => new SortOrderUpdate {
            Id = x.Item1.Id,
            IndexInTable = x.Item1.IndexInTable,
            NewIndexInTable = x.Item2
          })
          .MakeQueryable(
            dbContext,
            [
              ("Id", row => row.Id),
              ("IndexInTable", row => row.IndexInTable),
              ("NewIndexInTable", row => row.NewIndexInTable)
            ]
          );

      var dividerBulk =
          dbContext.L10recurrenceMetricDividers
          .Join(
            dividerListQueryable,
            left  => left.Id,
            right => right.Id,
            (left, right) => new SortOrderUpdate {
              Id = left.Id,
              IndexInTable = left.IndexInTable.Value,
              NewIndexInTable = right.NewIndexInTable
            }
          );

      var recurrence = new Models.L10.L10Recurrence
      {
        Id = recurrenceId
      };

      return new BulkQueries(measurableBulk, dividerBulk, measurableList.Select(x => x.Item1.Id).ToArray(), dividerList.Select(x => x.Item1.Id).ToArray(), recurrence);
    }

    #endregion
  }
}
