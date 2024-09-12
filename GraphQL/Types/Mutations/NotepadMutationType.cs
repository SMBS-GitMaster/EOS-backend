using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddNotepadMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("CreateNote")
        .Argument("text", a => a.Type<StringType>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().CreateNote(ctx.ArgumentValue<string>("text")));
    }
  }
}
