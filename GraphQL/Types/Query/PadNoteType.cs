using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
  public class PadNoteType : ObjectType<PadNoteModel>
  {
    protected override void Configure(IObjectTypeDescriptor<PadNoteModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("text")
          .Type<StringType>()
          .Resolve((ctx) => ctx.Service<IDataContext>().GetNoteText(ctx.Parent<PadNoteModel>().Id))
          ;

      descriptor
          .Field("html")
          .Type<StringType>()
          .Resolve((ctx) => ctx.Service<IDataContext>().GetNoteHTML(ctx.Parent<PadNoteModel>().Id))
          ;

      descriptor
          .Field("type")
          .Type<NonNullType<StringType>>()
          .Resolve((ctx) => "note")
          ;

      descriptor
          .Field("id").IsProjected(true)
          .Type<NonNullType<StringType>>()
          .Resolve((ctx) => ctx.Parent<PadNoteModel>().Id)
          ;

      descriptor
          .Field("version")
          .Type<NonNullType<IntType>>()
          .Resolve((ctx) => default(int))
          ;

      descriptor
          .Field("lastUpdatedBy")
          .Type<StringType>()
          .Resolve((ctx) => default(string))
          ;      
          
      descriptor
          .Field("dateCreated")
          .Type<NonNullType<FloatType>>()
          .Resolve((ctx) => default(float))
          ;

      descriptor
          .Field("dateLastModified")
          .Type<NonNullType<FloatType>>()
          .Resolve((ctx) => default(float))
          ;

    }
  }
}