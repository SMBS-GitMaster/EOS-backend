using HotChocolate.Types;
using RadialReview.GraphQL.Types;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Types
{
  public class MeetingMetadataChangeType : MeetingMetadataQueryType
  {
    public MeetingMetadataChangeType()
        : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingMetadataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingMetadataModelChange");
    }
  }
  public class MeetingMetadataQueryType : MeetingMetadataType
  {
    protected readonly bool isSubscription;

    public MeetingMetadataQueryType()
      : this(isSubscription: false)
    {
    }

    protected MeetingMetadataQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingMetadataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingMetadataQueryModel");
    }
  }
}
