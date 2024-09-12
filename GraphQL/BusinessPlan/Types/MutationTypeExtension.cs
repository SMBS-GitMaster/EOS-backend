using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Core.Repositories.Interfaces;
using RadialReview.BusinessPlan.Models.Inputs;
using RadialReview.BusinessPlan.Models.Utilities;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Crosscutting.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.BusinessPlan
{
  public class MutationTypeExtension
  {
    public static void BusinessPlanMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
            .Field("CreateBusinessPlanForMeeting")
            .Argument("input", a => a.Type<NonNullType<BusinessPlanCreateInput>>())
            .Type<ObjectType<BusinessPlanModel>>()
            .Resolve(async ctx =>
            {
              try
              {
                var response = await ctx.Service<IBusinessPlanRepository>().Create(ctx.ArgumentValue<BusinessPlanModel>("input"), false);

                return response;
              }
              catch (Exception e)
              {
                Console.WriteLine(e.Message);

                throw new GraphQLException(e.Message);
              }
            })
            .Authorize()
            ;

      descriptor
          .Field("EditBusinessPlanTile")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanTileEditInput>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .UpdateBusinessPlanTile(ctx.ArgumentValue<BusinessPlanTileEditInputModel>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize();

      descriptor
        .Field("editBusinessPlanListCollection")
        .Argument("input", a => a.Type<NonNullType<BusinessPlanListCollectionEditInput>>())
        .Type<ObjectType<GraphQLResponseBase>>()
        .Resolve(async ctx =>
        {
          MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
           .UpdateBusinessPlanListCollection(ctx.ArgumentValue<BusineesPlanListCollectionEditInputModel>("input"));

          if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
          else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
        })
        .Authorize();


      descriptor
        .Field("editBusinessPlanTilePositions")
        .Argument("input", a => a.Type<NonNullType<BusinessPlanTilePositionEditInput>>())
        .Type<ObjectType<GraphQLResponseBase>>()
        .Resolve(async ctx =>
        {
          MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
            .UpdateBusinessPlanTilePositions(ctx.ArgumentValue<BusinessPlanTilePositionEditInputModel>("input"));

          if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
          else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
        })
        .Authorize();

      descriptor
        .Field("editBusinessPlanTilePortraitPdfPositions")
        .Argument("input", a => a.Type<NonNullType<BusinessPlanTilePortraitPDFEditInput>>())
        .Type<ObjectType<GraphQLResponseBase>>()
        .Resolve(async ctx =>
        {
          MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
            .UpdateBusinessPlanTilePortraitPdfPositions(ctx.ArgumentValue<BusinessPlanTilePortraitPDFEditInputModel>("input"));

          if(response.Status) return new GraphQLResponseBase(response.Status, response.Message);
          else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
        })
        .Authorize();

      descriptor
        .Field("editBusinessPlanTileLandscapePdfPositions")
        .Argument("input", a => a.Type<NonNullType<BusinessPlanTileLandscapePDFEditInput>>())
        .Type<ObjectType<GraphQLResponseBase>>()
        .Resolve(async ctx =>
        {
          MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
            .UpdateBusinessPlanTileLandscapePdfPositions(ctx.ArgumentValue<BusinessPlanTileLandscapePDFEditInputModel>("input"));

          if(response.Status) return new GraphQLResponseBase(response.Status, response.Message);
          else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
        })
        .Authorize();

      descriptor
          .Field("EditBusinessPlan")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanEditInput>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .Update(ctx.ArgumentValue<BusinessPlanEditInputModel>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize()
          ;

      descriptor
          .Field("duplicateBusinessPlanTile")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanDuplicateTileInput>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
                .DuplicateTile(ctx.ArgumentValue<BusinessPlanDuplicateTileInputModel>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize()
    ;

      descriptor
          .Field("deleteBusinessPlanListItem")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanListItemDeleteInputType>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .DeleteBusinessPlanListItem(ctx.ArgumentValue<BusinessPlanListItemDeleteInput>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize()
          ;

      descriptor
          .Field("editBusinessPlanListItem")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanListItemEditInputType>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .UpdateBusinessPlanListItem(ctx.ArgumentValue<BusinessPlanListItemEditInput>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize();

      descriptor
          .Field("addListItemToBusinessPlanListCollection")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanListItemCreateInputType>>())
          .Type<ObjectType<UIDModel>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .CreateBusinessPlanListItem(ctx.ArgumentValue<BusinessPlanListItemCreateInput>("input"));

            if (response.Status) return new UIDModel(response.Id);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize()
          ;

      descriptor
          .Field("sortAndReorderBusinessPlanListItems")
          .Argument("input", a => a.Type<NonNullType<BusinessPlanListItemsSortInput>>())
          .Type<ObjectType<GraphQLResponseBase>>()
          .Resolve(async ctx =>
          {
            MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
              .SetsortAndReorderBusinessPlanListItems(ctx.ArgumentValue<BusinessPlanListItemsSortModel>("input"));

            if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
            else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
          })
          .Authorize()
          ;

      descriptor
      .Field("transferBusinessPlanFromV1")
      .Argument("input", a => a.Type<NonNullType<BusinessPlanTransferFromV1Input>>())
      .Type<ObjectType<IdModel>>()
      .Resolve(async ctx =>
      {
        var response = await ctx.Service<IBusinessPlanRepository>().TransferBusinessPlanFromV1(ctx.ArgumentValue<BusinessPlanTransferFromV1InputModel>("input"));

        return new IdModel(response);
      })
      .Authorize()
      ;

    }
  }
}
