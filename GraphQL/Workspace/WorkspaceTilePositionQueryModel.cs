using RadialReview.Models.Dashboard;

namespace RadialReview.GraphQL.Models
{
  public class WorkspaceTilePositionQueryModel
  {

    #region Properties

    public int X { get; set; }

    public int Y { get; set; }

    public int W { get; set; }

    public int H { get; set; }

    internal int Bottom { get => Y + H; }

    internal int Right { get => X + W; }

    #endregion

    #region Constructors

    public WorkspaceTilePositionQueryModel() { }

    public WorkspaceTilePositionQueryModel(int x, int y, int w, int h)
    {
      X = x;
      Y = y;
      W = w;
      H = h;
    }

    public WorkspaceTilePositionQueryModel(TileModel source)
    {
      X = source.X;
      Y = source.Y;
      W = source.Width;
      H = source.Height;
    }

    #endregion

    #region Public Methods

    public bool Overlaps(WorkspaceTilePositionQueryModel other)
    {
      return !(other.Bottom <= this.Y || other.Y >= this.Bottom || other.X >= this.Right || other.Right <= this.X);
    }

    #endregion

  }
}
