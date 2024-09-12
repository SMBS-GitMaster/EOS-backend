namespace RadialReview.GraphQL.Types {
  using System.Linq;
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class SegueChangeType : SegueType
  {
    public SegueChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<SegueModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("SegueChange");
    }
  }

  public class SegueType : ObjectType<SegueModel> {
    protected bool isSubscription;

    public SegueType()
      : this(isSubscription: false)
    {
    }

    protected SegueType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<SegueModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "segue");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();
    }
  }

}