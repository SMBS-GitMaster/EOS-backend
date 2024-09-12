using HotChocolate.Types;

namespace RadialReview.GraphQL.Models.Mutations
{
    public class FavoriteCreateMutationModel
    {

        public string ParentType { get; set; }
        public long ParentId { get; set; }
        public int Position { get; set; }
        public double PostedTimestamp { get; set; }
        public long User { get; set; }
    }

    public class FavoriteEditMutationModel
    {

        public long FavoriteId { get; set; }

        [DefaultValue(null)] public string ParentType { get; set; }

        public long? ParentId { get; set; }

        public int? Position { get; set; }

        public long? User { get; set; }
    }

    public class FavoriteDeleteMutationModel
    {

        public long FavoriteId { get; set; }

        public string ParentType { get; set; }

    }
}
