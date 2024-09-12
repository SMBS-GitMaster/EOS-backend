using NHibernate;
using RadialReview.Accessors.Payments;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.Accessors.PaymentAccessor;
using static RadialReview.Models.Variables.GetStartedMessages;
using static RadialReview.SettingsViewModel;

namespace RadialReview.Accessors.Payments {
  public class UserCalculator {

    public IEnumerable<UQ> AllPeopleList { get; protected set; }
    public SimplePlan Plan { get; protected set; }

    //public IEnumerable<UQ> QCUsers { get { return _Dedup.SortUsers(AllPeopleList, x => x.Email).Where(x => !x.IsClient).Where(x => !_Dedup.ShouldExcludeQCUser(x.Email)); } }


    //public IEnumerable<UQ> L10Users { get { return _Dedup.SortUsers(AllPeopleList, x => x.Email).Where(x => !x.EvalOnly).Where(x => !_Dedup.ShouldExcludingL10User(x.Email)); } }
    //public IEnumerable<UQ> L10UsersToChargeFor { get { return L10Users.Skip(Plan.FirstN_Users_Free); } }





    //public int NumberQCUsers { get { return QCUsers.Count(); } }
    //public int NumberTotalUsers { get { return AllPeopleList.Count(); } }

    //public int NumberQCUsersToChargeFor { get { return NumberQCUsers; } }
    //public int NumberL10UsersToChargeFor { get { return Math.Max(0, NumberL10Users - Plan.FirstN_Users_Free); } }

    //public int NumberL10Users { get { return L10Users.Count(); } }

    private Dedup _Dedup { get; set; }


    public UserCalculator(List<UQ> users, SimplePlan plan, Dedup dedup) {
      _Dedup = dedup;
      AllPeopleList = users;
      Plan = plan;

      //Update Users to include Weekly Meeting data
      TagMeetingUsers();

      //Update users to include People Tools data.
      TagPeopleToolsUsers();
    }

    private void TagPeopleToolsUsers() {
      if (Plan.PeopleToolsEnabled) {
        var peopleUsers = _Dedup.SortUsers(AllPeopleList.Where(x => !x.IsClient), x => x.Email);

        var counter = 0;
        foreach (var u in peopleUsers) {
          u.UsingPeopleTools = true;

          if (counter < Plan.FirstNUsersFree) {
            //First N are free.
            u.ChargeDescription.Add(new ChargeDescription(ChargeDescription.PEOPLE_TOOLS_INCLUDED_SEAT, ChargeProduct.PeopleTools, ChargeType.ForceFree));
          } else {
            //Charge the account...
            u.ChargeDescription.Add(new ChargeDescription(ChargeDescription.PEOPLE_TOOLS, ChargeProduct.PeopleTools, ChargeType.Default));

          }
          counter++;
        }
      }
    }

    private void TagMeetingUsers() {
      if (Plan.MeetingEnabled) {
        var l10Users = _Dedup.SortUsers(AllPeopleList.Where(x => !x.EvalOnly), x => x.Email);

        var counter = 0;
        foreach (var u in l10Users) {
          //Always true
          u.UsingWeeklyMeeting = true;
          //Can't be both free and already paid
          if (counter < Plan.FirstNUsersFree) {
            //Capture free first
            u.ChargeDescription.Add(new ChargeDescription(ChargeDescription.WEEKLY_MEETING_INCLUDED_SEAT, ChargeProduct.WeeklyMeeting, ChargeType.ForceFree));
          } else if (_Dedup.ShouldExcludingL10User(u.Email)) {
            //Capture duplicates
            u.ChargeDescription.Add(new ChargeDescription(ChargeDescription.WEEKLY_MEETING_ALREADY_PAID, ChargeProduct.WeeklyMeeting, ChargeType.ForceFree));
          } else {
            //Charge the account...
            u.ChargeDescription.Add(new ChargeDescription(ChargeDescription.WEEKLY_MEETING, ChargeProduct.WeeklyMeeting, ChargeType.Default));
          }

          if (counter >= Plan.FirstNUsersFree) {
            _Dedup.MarkSeenNotFree(u.Email);
          }

          counter+=1;
        }
      }
    }


    public ItemizedCharge GenerateInvoiceAndUpdateDedup() {
      var invoice = GenerateInvoice();
      UpdateDedup(invoice);
      return invoice;
    }

    [Obsolete("Consider using GenerateInvoiceAndUpdateDedup instead")]
    public ItemizedCharge GenerateInvoice() {
      var output = new ItemizedCharge() {
        ItemizedList = new List<Itemized>(),
        ChargedFor = new List<ItemizedCharge.UserCharge>(),
        DiscountApplied = 0.0m,
        ShouldDedupMeetingUsers = false,
        ShouldDedupPeopleToolsUsers = false
      };

      var businessUnit = Plan.OrganizationName+" ["+Plan.OrganizationId+"]";

      //Base Fee
      AppendBaseFee(output, businessUnit);

      //Meeting Items
      AppendMeetingFees(output, businessUnit);

      //People Tools Items
      AppendPeopleToolFees(output, businessUnit);

      //Discount everything if it's still free.
      AdjustForFreeAccount(output, businessUnit);

      //Discount Special Accounts
      AdjustForSpecialAccounts(output, businessUnit);

      //Add charged for details
      AddChargeDetails(output);

      //Clear everything for cancelled accounts
      AdjustForCancelledAccounts(output);

      return output;
    }

    public void UpdateDedup(ItemizedCharge itemized) {
      if (itemized.ShouldDedupMeetingUsers) {
        foreach (var u in GetMeetingUsers().WhereNonFree()) {
          _Dedup.SetL10UserAsExcluded(u.Email);
        }
      }

      if (itemized.ShouldDedupPeopleToolsUsers) {
        foreach (var u in GetPeopleToolsUsers().WhereNonFree()) {
          _Dedup.SetQCUserAsExcluded(u.Email);
        }
      }
    }
    private void AdjustForCancelledAccounts(ItemizedCharge output) {
      if (Plan.AccountType == AccountType.Cancelled) {
        output.ItemizedList = new List<Itemized>();
        output.ChargedFor = new List<ItemizedCharge.UserCharge>();
        output.ShouldDedupPeopleToolsUsers=false;
        output.ShouldDedupMeetingUsers=false;
      }
    }

    private void AddChargeDetails(ItemizedCharge output) {
      output.ChargedFor.AddRange(AllPeopleList.Select(x => new ItemizedCharge.UserCharge() {
        AttachTime = x.AttachTime,
        DeleteTime = x.DeleteTime,
        ChargedFor = new HashSet<string>(x.ChargeDescription.Select(x => x.Description).ToList()),
        Email = x.Email,
        Name = string.Join(" ", new[] { x.FirstName, x.LastName }),
        FirstName = x.FirstName,
        LastName = x.LastName,
        UserOrgId = x.UserOrgId,
        OrgId = x.OrgId
      }));
    }

    private void AdjustForSpecialAccounts(ItemizedCharge output, string businessUnit) {
      var discountLookup = new Dictionary<AccountType, string>() {
                    {AccountType.Implementer,"Discount (Bloom Growth Guide)" },
                    {AccountType.Dormant,"Inactive Account" },
                    {AccountType.SwanServices, "Demo Account (Swan Services)" },
                    {AccountType.Other, "Discount (Special Account)" },
                    {AccountType.UserGroup, "Discount (User Group)" },
                    {AccountType.Coach, "Discount (Coach)" },
                    {AccountType.FreeForever, "Discount (Free)" },
        };

      if (discountLookup.ContainsKey(Plan.AccountType) && output.ItemizedList.Sum(x => x.Total()) != 0) {
        var total = output.ItemizedList.Sum(x => x.Total());
        output.ItemizedList.Add(new Itemized() {
          BusinessUnit = businessUnit,
          Name = discountLookup[Plan.AccountType],
          Price = -1 * total,
          Quantity = 1,
          Type = ItemizedType.Discount
        });
        output.ShouldDedupPeopleToolsUsers=false;
        output.ShouldDedupMeetingUsers=false;
      }
    }

    private void AdjustForFreeAccount(ItemizedCharge output, string businessUnit) {
      if (Plan.FreeUntil.Date > Plan.ExecutionTime) {
        var total = output.ItemizedList.Sum(x => x.Total());
        output.ItemizedList.Add(new Itemized() {
          BusinessUnit = businessUnit,
          Name = "Discount",
          Price = -1 * total,
          Quantity = 1,
          Type = ItemizedType.Discount
        });
        output.ShouldDedupPeopleToolsUsers=false;
        output.ShouldDedupMeetingUsers=false;
      }
    }

    private void AppendPeopleToolFees(ItemizedCharge output, string businessUnit) {
      if (Plan.PeopleToolsEnabled) {
        var peopleUsers = GetPeopleToolsUsers();
        var peopleSeatCount = peopleUsers.GetNumberOfUsersChargedFor();

        var peopleItem = new Itemized() {
          BusinessUnit = businessUnit,
          Name = "People Tools™" +  Plan.DurationDescription,
          Price = Plan.PeopleToolsPricePerPerson * Plan.DurationMultiplier,
          Quantity = peopleSeatCount,
          Type = ItemizedType.PeopleToolsFee
        };
        output.ItemizedList.Add(peopleItem);


        if (Plan.PeopleToolsFreeUntil>=Plan.ExecutionTime && peopleSeatCount!=0) {
          output.ItemizedList.Add(peopleItem.GenerateDiscount(businessUnit));
          output.ShouldDedupPeopleToolsUsers = false;
        } else {
          output.ShouldDedupPeopleToolsUsers = true;
        }
      }
    }

    private void AppendMeetingFees(ItemizedCharge output, string businessUnit) {
      if (Plan.MeetingEnabled) {
        var meetingUsers = GetMeetingUsers();
        var meetingSeatCount = meetingUsers.GetNumberOfUsersChargedFor();

        if (meetingSeatCount > 0) {
          var meetingItem = new Itemized() {
            BusinessUnit = businessUnit,
            Name = "Weekly Meeting Software"  + Plan.DurationDescription,
            Price = Plan.MeetingPricePerPerson * Plan.DurationMultiplier,
            Quantity = meetingSeatCount,
            Type = ItemizedType.MeetingFee
          };
          output.ItemizedList.Add(meetingItem);

          if (Plan.MeetingFreeUntil>=Plan.ExecutionTime && meetingSeatCount!=0) {
            output.ItemizedList.Add(meetingItem.GenerateDiscount(businessUnit));
            output.ShouldDedupMeetingUsers = false;
          } else {
            output.ShouldDedupMeetingUsers = true;
          }
        }
      }
    }

    private void AppendBaseFee(ItemizedCharge ic, string businessUnit) {
      if (Plan.BaselinePrice > 0) {
        var baseItem = new Itemized() {
          BusinessUnit = businessUnit,
          Name = "Bloom Growth™" + Plan.DurationDescription,
          Price = Plan.BaselinePrice * Plan.DurationMultiplier,
          Quantity = 1,
          Type = ItemizedType.BaseFee
        };
        ic.ItemizedList.Add(baseItem);
      }
    }

    public GroupCalculator GetPeopleToolsUsers() {
      var peopleToolsUsers = AllPeopleList.Where(x => x.UsingPeopleTools).ToList();
      return new GroupCalculator(peopleToolsUsers, ChargeProduct.PeopleTools);
    }


    public GroupCalculator GetMeetingUsers() {
      var weeklyMeetingUsers = AllPeopleList.Where(x => x.UsingWeeklyMeeting).ToList();
      return new GroupCalculator(weeklyMeetingUsers, ChargeProduct.WeeklyMeeting);
    }

    //public static UserCalculator Create(ISession s, long orgId, string orgName, PaymentPlanModel planModel, DateRange range, bool meetingEnabled, bool peopleEnabled, DateTime executionTime,AccountType accountType, Dedup dedup) {

    //  //throw new NotImplementedException("Use other UserCalculator");
    //  if (NHibernateUtil.GetClass(planModel) != typeof(PaymentPlan_Monthly)) {
    //    throw new PermissionsException("Unhandled Payment Plan");
    //  }

    //  var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(planModel);

    //  if (plan.OrgId != orgId) {
    //    throw new Exception("Org Id do not match");
    //  }

    //  var rangeStart = range.StartTime;
    //  var rangeEnd = range.EndTime;


    //  UserModel uAlias = null;
    //  TempUserModel tuAlias = null;
    //  UserOrganizationModel uoAlias = null;
    //  var allPeopleList = s.QueryOver<UserOrganizationModel>(() => uoAlias)
    //            .Left.JoinAlias(() => uoAlias.User, () => uAlias)                 //Existed any time during this range.
    //            .Left.JoinAlias(() => uoAlias.TempUser, () => tuAlias)
    //            //.Where(() => uo.Organization.Id == orgId && uo.CreateTime <= rangeEnd && (uo.DeleteTime == null || uo.DeleteTime > rangeStart) && !uo.IsRadialAdmin && !uo.IsFreeUser)
    //            .Where(() => uoAlias.Organization.Id == orgId && uoAlias.CreateTime <= rangeEnd && (uoAlias.DeleteTime == null || uoAlias.DeleteTime > rangeStart) && !uoAlias.IsRadialAdmin)
    //              //.Select(x => x.Id, x => u.IsRadialAdmin, x => x.IsClient, x => x.User.Id, x => x.EvalOnly, x => u.FirstName, x => u.LastName, x => u.UserName, x => x.EmailAtOrganization, x => x.AttachTime, x => tu.FirstName, x => tu.LastName, x => tu.Email)
    //              .Select(x =>
    //                      x.Id,                           //0
    //                      x => uAlias.IsRadialAdmin,      //1
    //                      x => x.IsClient,                //2
    //                      x => x.User.Id,                 //3
    //                      x => x.EvalOnly,                //4
    //                      x => uAlias.FirstName,          //5
    //                      x => uAlias.LastName,           //6
    //                      x => uAlias.UserName,           //7
    //                      x => x.EmailAtOrganization,     //8
    //                      x => x.AttachTime,              //9
    //                      x => tuAlias.FirstName,         //10
    //                      x => tuAlias.LastName,          //11
    //                      x => tuAlias.Email,             //12
    //                      x => uoAlias.IsFreeUser,        //13
    //                      x => uoAlias.DeleteTime         //14
    //             )
    //            .List<object[]>()
    //            .Select(x => new UQ {
    //              UserOrgId = (long)x[0],
    //              IsRadialAdmin = (bool?)x[1],
    //              IsClient = (bool)x[2],
    //              UserId = (string)x[3],
    //              IsRegistered = x[3] != null,
    //              EvalOnly = (bool?)x[4] ?? false,
    //              FirstName = (string)x[5] ?? (string)x[10],
    //              LastName = (string)x[6] ?? (string)x[11],
    //              Email = ((string)x[7]) ?? (string)x[12] ?? ((string)x[8]),
    //              AttachTime = (x[3] == null) ? ((DateTime?)null) : ((DateTime?)x[9]),
    //              IsFreeUser = (bool)(x[13] ?? false),
    //              DeleteTime = (x[3] == null) ? ((DateTime?)null) : ((DateTime?)x[14]),
    //            })
    //            .Where(x => x.IsRadialAdmin == null || (bool)x.IsRadialAdmin == false)
    //            .Where(x => !x.IsFreeUser)
    //            .ToList();
    //  if (plan.NoChargeForClients) {
    //    allPeopleList = allPeopleList.Where(x => x.IsClient == false).ToList();
    //  }
    //  if (plan.NoChargeForUnregisteredUsers) {
    //    allPeopleList = allPeopleList.Where(x => x.IsRegistered).ToList();
    //  }

    //  return new UserCalculator(allPeopleList, new SimplePlan(orgId, orgName, accountType, executionTime, plan, meetingEnabled, peopleEnabled), dedup);

    //}

    //public static ChargeDescription INCLUDED_SEAT_TAG = new ChargeDescription("Included Seat", ChargeType.ForceFree);
    //public static ChargeDescription ALREADY_PAID_TAG = ;


  }


}
