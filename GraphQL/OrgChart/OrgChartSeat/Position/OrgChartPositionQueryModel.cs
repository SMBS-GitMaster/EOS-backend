namespace RadialReview.GraphQL.Models;

public class OrgChartPositionQueryModel
{
  public virtual long Id {get; set;}
  public virtual string Title {get; set;}
  public virtual OrgChartPositionRoleQueryModel[] Roles {get; set;}

  public static class Collections
  {
    public enum OrgChartPositionRole
    {
      Roles
    }
  }
}