using HotChocolate.Types;
using RadialReview.Core.GraphQL.Types.Subscriptions;

namespace RadialReview.Core.GraphQL.Types
{
    public abstract class ChangeType<TChange> : UnionType<TChange>
  {
    protected void ConfigureCreateUpdateDelete<T, TChangeType, TKey>(IUnionTypeDescriptor descriptor, string modelType)
      where TChangeType : ObjectType
      where T : class
    {
      descriptor.Type(new ObjectType<Created<TChange, T, TKey>>(d => {
        d.Name($"Created_{modelType}");
        d.Field("action").Resolve(_ => ChangeAction.Created);
        d.Field("modelType").Resolve(_ => modelType);
        d.Field(t => t.Id).Type<NonNullType<CustomKeyType>>();
        d.Field(t => t.Value).Type<TChangeType>();
      }));

      descriptor.Type(new ObjectType<Updated<TChange, T, TKey>>(d => {
        d.Name($"Updated_{modelType}");
        d.Field("action").Resolve(_ => ChangeAction.Updated);
        d.Field(t => t.Id)
       .Type<NonNullType<CustomKeyType>>().Resolve(ctx => ctx.Parent<Updated<TChange, T, TKey>>().Id);
        d.Field("modelType").Resolve(_ => modelType);
        d.Field(t => t.Value).Type<TChangeType>();
        d
          .Field(t => t.ContainerTargets)
          .Name("targets").Type<ListType<ContainerTargetQueryType>>()
          .Resolve(ctx => ctx.Parent<Updated<TChange, T, TKey>>().ContainerTargets) 
          .IsProjected(true);
      }));

      descriptor.Type(new ObjectType<Deleted<TChange, T, TKey>>(d => {
        d.Name($"Deleted_{modelType}");
        d.Field("action").Resolve(_ => ChangeAction.Deleted);
        d.Field(t => t.Id).Type<NonNullType<CustomKeyType>>();
        d.Field("modelType").Resolve(_ => modelType);
      }));
    }

    protected void ConfigureCollections<TProperty, T, TChangeType, TKey>(IUnionTypeDescriptor descriptor, string modelType, string propertyType)
      where TChangeType : ObjectType
      where T : class
    {
      descriptor.Type(new ObjectType<Inserted<TChange, TProperty, T, TKey>>(d => {
        d.Name($"Inserted_{modelType}_{propertyType}");
        d.Field("action").Resolve(_ => ChangeAction.Inserted);
        d.Field(t => t.Id).Type<NonNullType<CustomKeyType>>();
        d.Field("modelType").Resolve(_ => modelType);
        d.Field("propertyType").Resolve(_ => propertyType);
        d.Field(t => t.Value).Type<TChangeType>();
      }));

      descriptor.Type(new ObjectType<Removed<TChange, TProperty, T, TKey>>(d => {
        d.Name($"Removed_{modelType}_{propertyType}");
        d.Field("action").Resolve(_ => ChangeAction.Removed);
        d.Field(t => t.Id).Type<NonNullType<CustomKeyType>>();
        d.Field("modelType").Resolve(_ => modelType);
        d.Field("propertyType").Resolve(_ => propertyType);
      }));

      descriptor.Type(new ObjectType<Emptied<TChange, TProperty, T, TKey>>(d => {
        d.Name($"Emptied_{modelType}_{propertyType}");
        d.Field("action").Resolve(_ => ChangeAction.Emptied);
        d.Field("modelType").Resolve(_ => modelType);
        d.Field("propertyType").Resolve(_ => propertyType);
      }));
    }

    protected void ConfigureAssociation<TProperty, T, TChangeType, TKey>(IUnionTypeDescriptor descriptor, string modelType, string propertyType)
      where TChangeType : ObjectType
      where T : class
    {
      descriptor.Type(new ObjectType<UpdatedAssociation<TChange, TProperty, T, TKey>>(d => {
        d.Name($"UpdatedAssociation_{modelType}_{propertyType}");
        d.Field("action").Resolve(_ => ChangeAction.UpdatedAssociation);
        d.Field("modelType").Resolve(_ => modelType);
        d.Field("propertyType").Resolve(_ => propertyType);
        d.Field(t => t.Value).Type<TChangeType>();
      }));
    }
  }
}
