using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<BloomLookupModel> GetBloomLookupNodeAsync(string id, object cancellationToken);

    Task<List<TimeZoneQueryModel>> GetTimeZoneLookup();

  }

  public partial class DataContext : IDataContext
  {

    public async Task<BloomLookupModel> GetBloomLookupNodeAsync(string id, object cancellationToken)
    {
      return repository.GetBloomLookupNode(id, cancellationToken);
    }

    public async Task<List<TimeZoneQueryModel>> GetTimeZoneLookup()
    {
      return await repository.GetTimeZoneLookup();
    }

  }
}