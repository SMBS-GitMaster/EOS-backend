using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
    public class CheckInChangeType : CheckInType
    {
      public CheckInChangeType()
        : base(isSubscription: true)
      {
      }

      protected override void Configure(IObjectTypeDescriptor<CheckInModel> descriptor)
      {
        base.Configure(descriptor);
        descriptor.Name("CheckInChange");
      }
    }

    public class CheckInType : ObjectType<CheckInModel>
    {
        protected readonly bool isSubscription;

        public CheckInType()
          : this(isSubscription: false)
        {
        }

        protected CheckInType(bool isSubscription)
        {
          this.isSubscription = isSubscription;
        }

        protected override void Configure(IObjectTypeDescriptor<CheckInModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "checkIn")
            ;
        }
    }
}