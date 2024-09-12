namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;

  public class MetricDividerChangeType : MetricDividerType
  {
    public MetricDividerChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricDividerQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricDividerModelChange");
    }
  }

  public class MetricDividerType : ObjectType<MetricDividerQueryModel>
  {
    protected bool isSubscription;

    public MetricDividerType()
      : this(false)
    {
    }

    protected MetricDividerType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }
    protected override void Configure(IObjectTypeDescriptor<MetricDividerQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricDivider");

      descriptor
        .Field(t => t.IndexInTable)
        .Type<LongType>();

    }
  }
}
