using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{


  public class DepartmentPlanRecordChangeType : DepartmentPlanRecordQueryType
  {
    public DepartmentPlanRecordChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<DepartmentPlanRecordQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("DepartmentPlanRecordChangeType");
    }
  }

  public class DepartmentPlanRecordQueryType : ObjectType<DepartmentPlanRecordQueryModel>
  {
    protected readonly bool isSubscription;

    public DepartmentPlanRecordQueryType()
      : this(false)
    {
    }

    protected DepartmentPlanRecordQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<DepartmentPlanRecordQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "departmentPlanRecord")
        ;

      descriptor
        .Field(t => t.Id).IsProjected(true)
        .Type<LongType>()
        ;

    }
  }
}