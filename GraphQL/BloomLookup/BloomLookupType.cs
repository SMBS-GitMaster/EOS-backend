using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
  public class BloomLookupChangeType : BloomLookupType
  {
    public BloomLookupChangeType()
        : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<BloomLookupModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("BloomLookupModelChange");
    }
  }

  public class BloomLookupType : ObjectType<BloomLookupModel>
  {
    protected readonly bool isSubscription;

    public BloomLookupType()
        : this(isSubscription: false)
    {
    }

    protected BloomLookupType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<BloomLookupModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "bloomLookupNode")
          ;

      descriptor
        .Field("timezones")
        .Type<ListType<TimeZoneQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetTimeZoneLookup())
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;

      descriptor
          .Field(t => t.Id)
          .Type<StringType>()
          ;
    }
  }
}