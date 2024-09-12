using HotChocolate.Types;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations {
  public partial class CreateNoteMutationType : InputObjectType<NoteCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<NoteCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class EditNoteMutationType : InputObjectType<EditNoteModel> {
    protected override void Configure(IInputObjectTypeDescriptor<EditNoteModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    private static void AddMeetingNotesMutations(IObjectTypeDescriptor descriptor) {
      descriptor
            .Field("CreateMeetingNote")
            .Argument("input", a => a.Type<NonNullType<CreateNoteMutationType>>())
            .Authorize()
            .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMeetingNote(ctx.ArgumentValue<NoteCreateModel>("input")));

      descriptor
            .Field("EditMeetingNote")
            .Argument("input", a => a.Type<NonNullType<EditNoteMutationType>>())
            .Authorize()
            .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingNote(ctx.ArgumentValue<EditNoteModel>("input")));
    }
  }

}