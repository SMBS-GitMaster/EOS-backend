using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    Task<IdModel> CreateUser(UserCreateModel userCreateModel);

    Task<IdModel> EditUser(UserEditModel userEditModel);

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    public async Task<IdModel> CreateUser(UserCreateModel m)
    {
      ErrorOnNonDefault(m, x => x.Timezone);
      ErrorOnNonDefault(m, x => x.Avatar);
      ErrorOnNonDefault(m, x => x.Notifications);
      ErrorOnNonDefault(m, x => x.Workspaces);

      var org = OrganizationAccessor.GetOrganization(caller, caller.Organization.Id);

      //!! Has missing data
      var ucom = new CreateUserOrganizationViewModel()
      {
        Email = m.Email,
        FirstName = m.FirstName,
        LastName = m.LastName,
        OrgId = caller.Organization.Id,
        RecurrenceIds = m.Meetings,
        SendEmail = m.SendInvite ?? org.SendEmailImmediately,
        SetOrgAdmin = false, //todo?
        IsManager = false, //todo?
        ManagerNodeId = null, //todo?
        NodeId = null, //todo?
        PositionName = null, //todo?
        OnLeadershipTeam = false, //todo?
        PlaceholderOnly = false, //todo?
        EvalOnly = false,//todo

        //old ...
        IsClient = false,//todo
        PhoneNumber = null, //todo?
        ClientOrganizationName = null,

      };

      var res = await UserAccessor.CreateUser(caller, ucom);
      var createdUser = res.CreatedUser;

      foreach (var mid in m.Meetings)
      {
        await L10Accessor.AddAttendee(caller, mid, createdUser.Id);
      }

      return new IdModel(createdUser.Id);
    }

    public async Task<IdModel> EditUser(UserEditModel userEditModel)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);

          await UserAccessor.EditUser(caller, userEditModel);

          perms.EditUserDetails(userEditModel.UserId);

          return new IdModel(userEditModel.UserId);
        }
      }
    }

  }
}