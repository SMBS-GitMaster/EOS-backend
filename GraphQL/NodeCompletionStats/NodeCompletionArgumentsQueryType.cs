namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;

  public class NodeCompletionArgumentsQueryType : InputObjectType<NodeCompletionArgumentsQueryModel>
  {

    protected override void Configure(IInputObjectTypeDescriptor<NodeCompletionArgumentsQueryModel> descriptor)
    {
      base.Configure(descriptor);
    }

  }
}
