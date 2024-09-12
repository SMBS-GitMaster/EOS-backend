namespace RadialReview.Core.GraphQL.MetricFormulaLookup
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;

  public class MetricFormulaLookupChangeType : MetricFormulaLookupQueryType
  {
    public MetricFormulaLookupChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricFormulaLookupQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricFormulaLookupModelChange");
    }
  }
  public class MetricFormulaLookupQueryType : ObjectType<MetricFormulaLookupQueryModel>
  {
    protected readonly bool isSubscription;

    public MetricFormulaLookupQueryType()
      : this(false)
    {
    }

    protected MetricFormulaLookupQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }
    protected override void Configure(IObjectTypeDescriptor<MetricFormulaLookupQueryModel> descriptor)
    {
        base.Configure(descriptor);

        descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricFormulaLookup");

        descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

    }

  }
}
