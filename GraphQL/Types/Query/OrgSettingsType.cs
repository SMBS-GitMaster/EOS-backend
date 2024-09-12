using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class OrgSettingsChangeType : OrgSettingsType
  {
    public OrgSettingsChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<OrgSettingsModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("OrgSettingsModelChange");
    } 
  }

  public class OrgSettingsType : ObjectType<OrgSettingsModel>
  {
    protected readonly bool isSubscription;

    public OrgSettingsType()
      : this(isSubscription: false)
    {
    }

    protected OrgSettingsType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<OrgSettingsModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "orgSettings")
      ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
        ;

    }
  }
}