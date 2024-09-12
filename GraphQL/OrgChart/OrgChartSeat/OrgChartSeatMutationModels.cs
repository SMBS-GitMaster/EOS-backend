using HotChocolate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.OrgChart.OrgChartSeat
{
  public class OrgChartSeatEditModel
  {
    public long SeatId {  get; set; }
    [DefaultValue(null)] public string? PositionTitle {  get; set; }
    [DefaultValue(null)] public long[]? UserIds {  get; set; }
    [DefaultValue(null)] public long? SupervisorId { get; set; }
  }

  public class OrgChartSeatCreateModel
  {
    [GraphQLNonNullType] public string positionTitle { get; set; }
    [GraphQLNonNullType] public string[] roles { get; set; }
    [GraphQLNonNullType] public long[] userIds { get; set; }
    [DefaultValue(null)] public long? SupervisorId { get; set; }
  }
}
