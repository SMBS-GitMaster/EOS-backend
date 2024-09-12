using Hangfire;
using log4net;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmailStrings = RadialReview.Core.Properties.EmailStrings;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors;

namespace RadialReview.Areas.People.Accessors {
  public class QuarterlyConversationAccessor {
    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

#pragma warning disable CS0618 // Type or member is obsolete

    public static IEnumerable<SurveyUserNode> AvailableAboutsForMe(UserOrganizationModel caller) {
      var nodes = GetLowerNodes(caller, true, false);
      //if (removeManager) {
      //	foreach (var n in nodes.Where(x => (x.About.UserOrganizationId == caller.Id && x.By.UserOrganizationId == caller.Id))) {
      //		n._Hidden = true;
      //	}
      //}

      return nodes.Select(x => {
        x.About._Hidden = x.About.UserOrganizationId == caller.Id;
        return x.About;
      }).Distinct(x => x._Hidden + "_" + x.ToViewModelKey());
    }

    public static IEnumerable<ByAboutSurveyUserNode> GetSelectedUserNodes(UserOrganizationModel caller, IEnumerable<SurveyUserNode> selected, bool includeSelf, bool supervisorLMA) {

      if (selected == null)
        return new List<ByAboutSurveyUserNode>();

      var userNodes = GetLowerNodes(caller, includeSelf, supervisorLMA);
      if (userNodes == null)
        return new List<ByAboutSurveyUserNode>();

      var filteredUsers = userNodes.Where(un => selected.Any(s => s.ToViewModelKey() == un.About.ToViewModelKey()));
      filteredUsers = filteredUsers.Where(fu => selected.Any(s => s.ToViewModelKey() == fu.By.ToViewModelKey()));
      return filteredUsers;
    }

    [Todo]
    private static SurveyUserNode SunGetter(
      Dictionary<string, SurveyUserNode> existingItems,
      AccountabilityNode toAdd,
      UserOrganizationModel user
    ) {
      var k = "sun_" + toAdd.Id + "_" + user.Id;//toAdd.ToKey();
      if (!existingItems.ContainsKey(k))
        existingItems[k] = new SurveyUserNode() {
          AccountabilityNodeId = toAdd.Id,
          User = user,
          AccountabilityNode = toAdd,
          UserOrganizationId = user.Id,
          UsersName = user.GetName(),
          PositionName = toAdd.AccountabilityRolesGroup.NotNull(x => x.PositionName)
        };

      return existingItems[k];
    }

    [Todo]
    public static IEnumerable<ByAboutSurveyUserNode> GetLowerNodes(UserOrganizationModel caller, bool includeSelf = false, bool supervisorLMA = false) {
      log.Info("\tStart\tAvailableByAboutsForMe- " + DateTime.UtcNow.ToJsMs());
      //20210130 to-test

      var allModels = new List<SurveyUserNode>();
      var sunDict = new Dictionary<string, SurveyUserNode>();
      var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
      var possible = new List<ByAboutSurveyUserNode>();
      //For all nodes the user occupies
      foreach (var node in nodes) {
        List<AccountabilityNode> reportSeats;
        try {
          reportSeats = DeepAccessor.Nodes.GetDirectReportsAndSelf(caller, node.Id);
        } catch (PermissionsException e) {
          continue;
        }
        reportSeats = reportSeats.Where(x => x.Id != node.Id).ToList();

        //if (!includeSelf) {
        //reportSeats = reportSeats.Where(x => x.Id != node.Id).ToList();
        //}

        var callerUN = SunGetter(sunDict, node, caller);

        allModels.Add(callerUN);

        if (includeSelf) {
          possible.Add(new ByAboutSurveyUserNode(callerUN, callerUN, AboutType.Self));
        }

        //for all reports and self
        foreach (var report in reportSeats) {
          var usersInSeat = report.GetUsers(null);


          if (report.Id == node.Id) {
            //Only want direct reports when there are multiple people in my seat.
            usersInSeat = usersInSeat.Where(x => x.Id == caller.Id).ToList();
          }

          if (!includeSelf) {
            //filter out self
            usersInSeat = usersInSeat.Where(x => x.Id != caller.Id).ToList();
          }


          //for all users
          foreach (var user in usersInSeat) {
            if (user != null) {
              var reportUN = SunGetter(sunDict, report, user);
              allModels.Add(reportUN);
              possible.Add(new ByAboutSurveyUserNode(callerUN, reportUN, AboutType.Subordinate));

              if (includeSelf) {
                possible.Add(new ByAboutSurveyUserNode(reportUN, reportUN, AboutType.Self));
              }
              if (supervisorLMA) {
                possible.Add(new ByAboutSurveyUserNode(reportUN, callerUN, AboutType.Manager));
              }
            }
          }
        }
      }

      var combined = possible.GroupBy(x => x.Key).Select(ba => {
        var about = AboutType.NoRelationship;
        foreach (var x in ba) {
          if (x.AboutIsThe.HasValue)
            about = about | x.AboutIsThe.Value;
        }
        return new ByAboutSurveyUserNode(ba.First().By, ba.First().About, about);
      });
      log.Info("\t End  \tAvailableByAboutsForMe- " + DateTime.UtcNow.ToJsMs());

      return combined.OrderBy(x => x.GetBy().ToPrettyString());
    }

    public static async Task<int> RemindAllIncompleteSurveys(UserOrganizationModel caller, long surveyContainerId) {
      List<IForModel> output = SurveyAccessor.GetForModelsWithIncompleteSurveysForSurveyContainers(caller, surveyContainerId);

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var userIds = output.Where(x => x.ModelType == ForModel.GetModelType<UserOrganizationModel>()).Select(x => x.ModelId).Distinct().ToList();
          var users = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(userIds).List().ToList();

          var availUsers = users.Where(x => {
            try {
              perms.ViewUserOrganization(x.Id, false);
              return true;
            } catch (PermissionsException e) {
              return false;
            }
#pragma warning disable CS0162 // Unreachable code detected
            return false;
#pragma warning restore CS0162 // Unreachable code detected
          }).ToList();

          var terms = TermsAccessor.GetTermsCollection(s, perms, caller.Organization.Id);
          var tasks = availUsers.Select(u => SendReminderUnsafe(s, u, terms, surveyContainerId)).ToList();
          await Task.WhenAll(tasks);

          return tasks.Count;
          //tx.Commit();
          //s.Flush();
        }
      }
    }

    public static async Task SendReminderUnsafe(ISession s, UserOrganizationModel user, TermsCollection terms, long surveyContainerId) {
      var email = user.GetEmail();
      var linkUrl = Config.BaseUrl(null, "/People/QuarterlyConversation/");
      var sc = s.Get<SurveyContainer>(surveyContainerId);
      await Emailer.SendEmail(
        Mail.To(EmailTypes.QuarterlyConversationReminder, email)
        .SubjectPlainText("[Reminder] Please complete your "+terms.GetTerm(TermKey.Quarterly1_1))
        .Body(EmailStrings.QuarterlyConversationReminder_Body, user.GetFirstName(), sc.DueDate.NotNull(x => x.Value.ToShortDateString()), linkUrl, linkUrl, Config.ProductName(), terms.GetTerm(TermKey.Quarterly1_1)));

    }

    public static void LockinSurvey(UserOrganizationModel caller, long surveyContainerId) {
      var output = SurveyAccessor.GetAngularSurveyContainerBy(caller, caller, surveyContainerId);
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var surveys = output.GetSurveys();
          foreach (var survey in surveys) {
            var surveyModel = s.Load<Survey>(survey.Id);

            perms.Self(surveyModel.By);

            surveyModel.LockedIn = true;
            s.Update(surveyModel);
          }


          tx.Commit();
          s.Flush();
        }
      }
    }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Converts the By's to UserOrganizationModels
    /// </summary>
    /// <returns></returns>


    public class QuarterlyConversationGeneration {
      public long SurveyContainerId { get; set; }
      public IEnumerable<Mail> UnsentEmail { get; set; }
      public List<String> Errors { get; set; }

      public QuarterlyConversationGeneration() {
        Errors = new List<string>();
        UnsentEmail = new List<Mail>();
      }
    }

    [Queue(HangfireQueues.Immediate.GENERATE_QC)]
    public static async Task<long> GenerateQuarterlyConversation(long callerId, string name, List<ByAboutSurveyUserNodeTiny> byAboutTiny, List<SurveyUserNodeTiny> selectedUsersTiny, DateRange quarterRange, DateTime dueDate, bool sendEmails) {
      UserOrganizationModel caller;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(callerId);
        }
      }

      var byAbout = byAboutTiny.Select(x => {
        SurveyUserNode by = SurveyUserNode.FromViewModelKey(x.ByKey);
        SurveyUserNode about = SurveyUserNode.FromViewModelKey(x.AboutKey);
        return new ByAboutSurveyUserNode(by, about, x.AboutIsThe);
      }).ToList();

      var selectedUsers = selectedUsersTiny.Select(x => SurveyUserNode.FromViewModelKey(x.Key)).ToList();

      return await GenerateQuarterlyConversation(caller, name, byAbout, selectedUsers, quarterRange, dueDate, sendEmails);
    }

    public static async Task<long> GenerateQuarterlyConversation(UserOrganizationModel caller, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, List<SurveyUserNode> selectedUsers, DateRange quarterRange, DateTime dueDate, bool sendEmails) {

      log.Info("Start\tQC Generator- " + DateTime.UtcNow.ToJsMs());
      try {
        log.Info("By-Abouts: " + string.Join(",", byAbout.Select(x => x.Key).ToList()));
      } catch (Exception e) {
        log.Info("By-Abouts Error:" + e.Message);
      }
      try {

        IEnumerable<ByAboutSurveyUserNode> possible = GetLowerNodes(caller, true, true);
        IEnumerable<ByAboutSurveyUserNode> invalid = byAbout.Where(selected => possible.All(avail => avail.GetViewModelKey() != selected.GetViewModelKey()));
        if (invalid.Any()) {
          Debug.WriteLine("Invalid");
          foreach (var i in invalid) {
            Debug.WriteLine("\tby:" + i.GetBy().ToKey() + "  about:" + i.GetAbout().ToKey());
          }
          throw new PermissionsException("Could not create. You cannot view these items.");
        }

        var populatedByAbouts = byAbout.Select(x => possible.First(y => x.GetViewModelKey() == y.GetViewModelKey())).ToList();

        QuarterlyConversationGeneration qcResult;
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            perms.CreateQuarterlyConversation(caller.Organization.Id);

            var terms = TermsAccessor.GetTermsCollection(s, perms, caller.Organization.Id);

            qcResult = await GenerateAndEmail(s, perms, terms, name, populatedByAbouts, selectedUsers, quarterRange, dueDate, sendEmails);

            tx.Commit();
            s.Flush();
          }
        }

        log.Info("End  \tQC Generator- " + DateTime.UtcNow.ToJsMs());

        return qcResult.SurveyContainerId;
      } catch (Exception e) {
        throw;
      }
    }

    public static async Task<QuarterlyConversationGeneration> GenerateAndEmail(ISession s, PermissionsUtility perms, TermsCollection terms, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, List<SurveyUserNode> selectedUsers, DateRange quarterRange, DateTime dueDate, bool sendEmails) {
      var qcResult = GenerateQuarterlyConversation_Unsafe(s, perms, terms, name, byAbout, selectedUsers, quarterRange, dueDate, sendEmails);


      if (qcResult.Errors.Any()) {
        await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationError(ses, perms.GetCaller(), QuarterlyConversationErrorType.CreationFailed, qcResult.Errors));
      } else {
        await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationCreated(ses, qcResult.SurveyContainerId));
        try {
          if (sendEmails) {
            await Emailer.SendEmails(qcResult.UnsentEmail);
            await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationEmailsSent(ses, qcResult.SurveyContainerId));
          }
        } catch (Exception e) {
          var q11 = terms.GetTerm(TermKey.Quarterly1_1);
          await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationError(ses, perms.GetCaller(), QuarterlyConversationErrorType.EmailsFailed, new List<string>() {
            "Created successfully "+q11+", but some email notifications failed to send."
          }));
        }
      }
      return qcResult;
    }

    public static QuarterlyConversationGeneration GenerateQuarterlyConversation_Unsafe(ISession s, PermissionsUtility perms, TermsCollection terms, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, List<SurveyUserNode> selectedUsers, DateRange quarterRange, DateTime dueDate, bool generateEmails) {
      log.Info("\tStart\tGenerateQuarterlyConversation_Unsafe- " + DateTime.UtcNow.ToJsMs());
      var creator = perms.GetCaller();

      List<ByAbout> reconstructed = byAbout.GroupBy(x => x.By.UserOrganizationId + "~" + x.About.ToViewModelKey())
        .OrderBy(x => x.First().AboutIsThe.NotNull(y => y.Value.Order()))
        .Select(ba => {
          return new ByAbout(ba.First().By.User, ba.First().About);
        }).ToList();

      SurveyUserNode element;
      foreach (var ba in reconstructed) {
        element = (SurveyUserNode)ba.About;
        if (element.Id != 0)
          continue;

        element.AccountabilityNode = s.Load<AccountabilityNode>(element.AccountabilityNodeId);
        element.User = s.Load<UserOrganizationModel>(element.UserOrganizationId);
        s.Save(element);
      }

      var engine = new SurveyBuilderEngine(
        new QuarterlyConversationInitializer(creator, name, creator.Organization.Id, quarterRange, dueDate, selectedUsers, terms),
        new SurveyBuilderEventsSaveStrategy(s),
        new TransformAboutAccountabilityNodes(s)
      );

      var container = engine.BuildSurveyContainer(reconstructed);
      var containerId = container.Id;
      var permItems = new[] {
            PermTiny.Creator(),
            PermTiny.Admins(),
            PermTiny.Members(true, true, false)
          };

      PermissionsAccessor.InitializePermItems_Unsafe(s, creator, PermItem.ResourceType.SurveyContainer, containerId, permItems);

      var result = new QuarterlyConversationGeneration() {
        SurveyContainerId = containerId,
      };

      if (!generateEmails) {
        log.Info("\tEnd  \tGenerateQuarterlyConversation_Unsafe- " + DateTime.UtcNow.ToJsMs());
        return result;
      }

      var emails = new List<Mail>();
      var allBys = container.GetSurveys()
              .Select(x => x.GetBy())
              .Distinct(x => x.ToKey());

      var linkUrl = Config.BaseUrl(null, "/People/QuarterlyConversation/");
      TinyUser user;
      Mail email;

      if (selectedUsers == null || selectedUsers.Count() == 0)
        return result;

      if (allBys == null || allBys.Count() == 0)
        return result;

      foreach (var byUser in allBys) {
        if (!(selectedUsers.Any(x => x.UserOrganizationId == byUser.ModelId)))
          continue;

        user = ForModelAccessor.GetTinyUser_Unsafe(s, byUser);
        if (user == null) {
          result.Errors.Add("By (" + byUser.ToKey() + ") was not a user.");
          continue;
        }

        if (string.IsNullOrEmpty(user.Email)) {
          result.Errors.Add("By (" + byUser.ToKey() + ") did not have an email.");
          continue;
        }

        email = Mail.To(EmailTypes.QuarterlyConversationIssued, user.Email)
          .SubjectPlainText("You have a "+terms.GetTerm(TermKey.Quarterly1_1)+" to complete")
          .Body(EmailStrings.QuarterlyConversation_Body, user.FirstName, dueDate.ToShortDateString(), linkUrl, linkUrl, Config.ProductName(), terms.GetTerm(TermKey.Quarterly1_1));

        emails.Add(email);
      }

      result.UnsentEmail = emails;

      log.Info("\tEnd  \tGenerateQuarterlyConversation_Unsafe- " + DateTime.UtcNow.ToJsMs());
      return result;
    }

    public static DefaultDictionary<string, string> TransformValueAnswer = new DefaultDictionary<string, string>(x => x);

    static QuarterlyConversationAccessor() {
      TransformValueAnswer["often"] = ""; // "👍";
      TransformValueAnswer["sometimes"] = ""; // "👉";
      TransformValueAnswer["not-often"] = ""; // "👎";
    }

    public static List<long> GetUsersWhosePeopleAnalyzersICanSee(ISession s, PermissionsUtility perms, long myUserId, long? recurrenceId = null) {
      perms.Self(myUserId);
      var callerMeetings = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, myUserId, onlyPersonallyAttending: true)
                      .Select(x => x.Id)
                      .ToArray();

      var shareingIdsQ = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null && x.SharePeopleAnalyzer == L10Recurrence.SharePeopleAnalyzer.Yes && x.User.Id != myUserId);

      if (recurrenceId == null) {
        shareingIdsQ = shareingIdsQ.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(callerMeetings);
      } else {
        if (!callerMeetings.Contains(recurrenceId.Value)) {
          //Hey. This permission is correct.
          throw new PermissionsException("Not an attendee.");
        }
        shareingIdsQ = shareingIdsQ.Where(x => x.L10Recurrence.Id == recurrenceId);
      }

      var shareingIds = shareingIdsQ.Select(x => x.User.Id).List<long>().Distinct().ToList();
      return (new[] { myUserId }).Union(shareingIds).Distinct().ToList();
    }

    public static AngularPeopleAnalyzer GetVisiblePeopleAnalyzers(UserOrganizationModel caller, long userId, long? recurrenceId = null, DateRange range = null) {
      List<long> allVisibleUsers = new List<long>();
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Self(userId);
          try {
            allVisibleUsers = GetUsersWhosePeopleAnalyzersICanSee(s, perms, userId, recurrenceId);
          } catch (Exception e) {
            //eat it.
          }
        }
      }

      var pas = allVisibleUsers.Select(id => GetPeopleAnalyzer(caller, id, range)).ToList();

      var flat = new AngularPeopleAnalyzer();
      var rows = new List<AngularPeopleAnalyzerRow>();
      var response = new List<AngularPeopleAnalyzerResponse>();
      var values = new List<PeopleAnalyzerValue>();
      var containers = new List<AngularSurveyContainer>();
      var lockins = new List<AngularLockedIn>();

      foreach (var p in pas) {
        flat.DateRange = p.DateRange;
        rows.AddRange(p.Rows);
        response.AddRange(p.Responses);
        values.AddRange(p.Values);
        values = values.Distinct(x => x.Key).ToList();
        containers.AddRange(p.SurveyContainers);
        containers = containers.Distinct(x => x.Key).ToList();
        lockins.AddRange(p.LockedIn);
      }
      flat.Rows = rows.OrderBy(x => x.About.PrettyString).ToList();
      flat.Responses = response.OrderBy(x => x.IssueDate).ToList();
      flat.Values = values;
      flat.SurveyContainers = containers.OrderBy(x => x.IssueDate).ToList();
      flat.LockedIn = lockins;

      return flat;
    }


    public static AngularPeopleAnalyzer GetPeopleAnalyzerSuperAdminFlat(UserOrganizationModel caller, long userId, DateRange range = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.RadialAdmin();

          SurveyItem si = null;
          SurveyItemFormat sif = null;
          SurveyUserNode about_sun = null;

          var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
            .JoinAlias(x => x.Item, () => si)
            .JoinAlias(x => x.ItemFormat, () => sif)
            .JoinAlias(x => x.About_SUN, () => about_sun)
            .Where(x => x.OrgId == caller.Organization.Id && x.SurveyType == SurveyType.QuarterlyConversation && x.About.ModelType == ForModel.GetModelType<SurveyUserNode>() && x.DeleteTime == null && x.Answer != null)
            .Where(x => x.Answer == "yes" || x.Answer == "no" || x.Answer == "not-often" || x.Answer == "often" || x.Answer == "sometimes")
            .Where(x => sif.QuestionIdentifier == SurveyQuestionIdentifier.GWC || sif.QuestionIdentifier == SurveyQuestionIdentifier.Value)
            .Where(range.Filter<SurveyResponse>())
            //Name,Answer,About_ModelId,About_ModelType,QuestionIdentifier,UsersName,PositionName,CompleteTime
            .Select(
              x => si.Name, x => x.Answer, x => sif.QuestionIdentifier,
              x => about_sun.UsersName, x => about_sun.PositionName, x => x.CompleteTime,
              x => x.SurveyContainerId, x => x.About.ModelId, x => x.About.ModelType,
              x => si.Source.ModelId, x => si.Source.ModelType, x => sif.Settings, x => x.Id,
              x => x.By.ModelId, x => x.By.ModelType
            ).List<object[]>()
            .Select(x => new {
              Question = (string)x[0],
              Answer = (string)x[1],
              QuestionIdentifier = (SurveyQuestionIdentifier)x[2],
              UserName = (string)x[3],
              PositionName = (string)x[4],
              CompleteTime = (DateTime?)x[5],
              SurveyContainerId = (long)x[6],
              About = new AngularForModel(new ForModel() {
                ModelId = (long)x[7],
                ModelType = (string)x[8],
                _PrettyString = (string)x[3] + " (" + (string)x[4] + ")"
              }),
              Source = (long?)x[9] == null ? null : new AngularForModel(new ForModel() {
                ModelId = ((long?)x[9]).Value,
                ModelType = (string)x[10]
              }),
              Settings = (string)x[11],
              Id = (long)x[12],
              By = new AngularForModel(new ForModel() {
                ModelId = (long)x[13],
                ModelType = (string)x[14],
              }),
            }).ToList();


          //Remap about based on name
          long iter = 1000L;
          var remapDict = new DefaultDictionary<string, long>(x => { var result = iter; iter++; return result; });
          foreach (var r in accountabiliyNodeResults) {
            r.About.ModelId = remapDict[r.About.PrettyString];
            r.About.ModelType = "remap";
          }


          var priority = new Func<string, int>(x => {
            switch (x) {
              case "no":
                return 0;
              case "yes":
                return 1;
              case "not-often":
                return 0;
              case "sometimes":
                return 1;
              case "often":
                return 2;
              default:
                return 8;
            }
          });


          var answerFormated = new DefaultDictionary<string, string>(x => x) {
            { "often", TransformValueAnswer["often"] },{ "sometimes", TransformValueAnswer["sometimes"] },  { "not-often",TransformValueAnswer["not-often"] },
            { "done", "done" }, { "not-done", "not done" },
            { "yes", "Y" }, { "no", "N" }
          };



          var responses = accountabiliyNodeResults
            .GroupBy(x => Tuple.Create(x.Question, x.UserName + "~" + x.PositionName/*,x.SurveyContainerId ??? */))
            .Select(x => x.OrderByDescending(y => y.SurveyContainerId).ThenBy(y => priority(y.Answer)).First())
            .Select(x => {
              var source = x.Source;
              var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(x.Settings);

              if (settings.ContainsKey("gwc")) {
                source = new AngularForModel() {
                  ModelId = -1,
                  ModelType = (string)settings["gwc"]//x.Question
                };
              }

              return new AngularPeopleAnalyzerResponse(new ByAbout(x.By, x.About), DateTime.UtcNow, DateTime.UtcNow, source, answerFormated[x.Answer] ?? x.Answer, x.Answer, 0, x.SurveyContainerId, x.About.ModelId);
            }).ToList();





          var rows = accountabiliyNodeResults.GroupBy(x => x.UserName + "~" + x.PositionName).Select(x => new AngularPeopleAnalyzerRow() {
            Id = x.First().UserName + "~" + x.First().PositionName,
            About = x.First().About,
            PersonallyOwning = true,
          }).ToList();

          var containerId = 0L;
          var containers = accountabiliyNodeResults.GroupBy(x => x.SurveyContainerId).Take(1).Select(x => {
            containerId = x.Key;
            return new AngularSurveyContainer() {
              Id = x.Key,
              IssueDate = DateTime.UtcNow
            };
          }).ToList();

          var values = accountabiliyNodeResults.Where(x => x.QuestionIdentifier == SurveyQuestionIdentifier.Value).GroupBy(x => x.Question).Select(x => {
            var value = new PeopleAnalyzerValue(x.First().Source, new List<long> { containerId });
            value.Source.PrettyString = x.First().Question;
            return value;
          }).ToList();


          return new AngularPeopleAnalyzer() {
            Responses = responses,
            DateRange = new AngularDateRange(new DateRange(DateTime.MinValue, DateTime.MaxValue)),
            Rows = rows,
            SurveyContainers = containers,
            Values = values
          };

        }
      }
    }

    [Todo]
    public static AngularPeopleAnalyzer GetPeopleAnalyzer(UserOrganizationModel caller, long userId, DateRange range = null, bool flat = false) {
      //Determine if self should be included.
      var includeSelf = caller.IsManager();
      if (includeSelf == false) {
        try {
          var rootId = AccountabilityAccessor.GetRoot(caller, caller.Organization.AccountabilityChartId).Id;
          includeSelf = includeSelf || AccountabilityAccessor.GetNodesForUser(caller, userId).Any(x => x.ParentNodeId == rootId);
        } catch (Exception e) {
          log.Error(e);
        }
      }

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewPeopleAnalyzer(userId);
          var myNodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
#pragma warning disable CS0618 // Type or member is obsolete
          var acNodeChildrenModels = myNodes.SelectMany(node => DeepAccessor.Nodes.GetChildrenAndSelfModels(s, caller, node.Id, allowAnyFromSameOrg: true)).ToList();
          if (!includeSelf) {
            acNodeChildrenModels = acNodeChildrenModels.Where(x => !myNodes.Any(y => y.Id == x.Id)).ToList();
          }

          var allManagedUserIds = acNodeChildrenModels.SelectMany(x => {
            if (myNodes.Any(y => y.Id == x.Id)) {
              //it's me
              return userId.AsList();
            } else {
              return x.GetUsers(null).Select(y => y.Id).ToList();
            }
          }).Distinct().ToArray();

          var allManagedNodeIds = acNodeChildrenModels.Select(x => x.Id).Distinct().ToArray();


#pragma warning restore CS0618 // Type or member is obsolete

          SurveyItem item = null;

          var allSurveyNodeItems = s.QueryOver<SurveyUserNode>().Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.User.Id).IsIn(allManagedUserIds)
                        .WhereRestrictionOn(x => x.AccountabilityNodeId).IsIn(allManagedNodeIds)
                        .Select(x => x.Id, x => x.AccountabilityNodeId, x => x.UserOrganizationId, x => x.UsersName, x => x.PositionName)
                        .List<object[]>()
                        .Select(x => {
                          var prettyString = (((string)x[3] ?? "") + ((string)x[4].NotNull(y => " (" + y + ")") ?? "")).Trim();
                          var acNode = ForModel.Create<AccountabilityNode>((long)x[1]);
                          acNode._PrettyString = prettyString;

                          return new {
                            Id = (long)x[0],
                            ModelId = (long)x[0],
                            AccountabilityNodeId = (long)x[1],
                            AccountabilityNode = acNode,
                            UserOrganizationId = (long)x[2],
                            ToKey = ForModel.GetModelType<SurveyUserNode>() + "_" + (long)x[0],
                            //PrettyString = prettyString
                          };
                        }).ToList();

          //Should be more of these which produce more accNodeResults...
          var availableSurveyNodes = allSurveyNodeItems.Where(n => acNodeChildrenModels.Any(x => x.Id == n.AccountabilityNodeId && x.GetUsers(null).Any(z => z.Id == n.UserOrganizationId))).ToList();
          var surveyNodeIds = availableSurveyNodes.Select(x => x.ModelId).ToList();

          var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
            .Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<SurveyUserNode>() && x.DeleteTime == null && x.Answer != null)
            .Where(range.Filter<SurveyResponse>())
            .WhereRestrictionOn(x => x.About.ModelId).IsIn(surveyNodeIds.ToArray())
            .List().ToList();

          var formats = s.QueryOver<SurveyItemFormat>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemFormatId).Distinct().ToArray()).Future();
          var items = s.QueryOver<SurveyItem>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();
          var surveys = s.QueryOver<Survey>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyId).Distinct().ToArray()).Future();
          var surveyContainers = s.QueryOver<SurveyContainer>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyContainerId).Distinct().ToArray()).Future();
          //var users = s.QueryOver<Angular>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();

          var formatsList = formats.ToList();

          var formatsLu = formatsList.ToDefaultDictionary(x => x.Id, x => x, x => null);
          var itemsLu = items.ToDefaultDictionary(x => x.Id, x => x, x => null);
          //	var userLu = allSurveyNodeItems.ToDefaultDictionary(x => x.ToKey(), x => x.ToPrettyString(), x => "n/a");// .NotNull(y => y.GetName()), x => "n/a");
          foreach (var t in items) {
            if (t.Source.NotNull(x => x.Is<CompanyValueModel>())) {
              t.Source._PrettyString = t.GetName();
            }
          }


          foreach (var result in accountabiliyNodeResults) {
            result._Item = itemsLu[result.ItemId];
            result._ItemFormat = formatsLu[result.ItemFormatId];
          }

          var analyzer = new AngularPeopleAnalyzer() { };
          var rows = new List<AngularPeopleAnalyzerRow>();
          var allValueIds = new List<long>();

          var sunToDiscriminator = availableSurveyNodes.ToDefaultDictionary(x => x.ToKey, x => x.AccountabilityNodeId + "_" + x.UserOrganizationId, x => "unknown SUN");
          var discriminatorToSun = availableSurveyNodes.ToDefaultDictionary(x => x.AccountabilityNodeId + "_" + x.UserOrganizationId, x => x, x => null);

          var sunToAccNode = availableSurveyNodes.ToDefaultDictionary(x => x.ToKey, x => x.AccountabilityNode, x => null);

          //Build response rows
          var allowedQuestionIdentifiers = new[] {
            SurveyQuestionIdentifier.GWC,
            SurveyQuestionIdentifier.Value
          };
          var groupByAbout_filtered = accountabiliyNodeResults
                          .Where(x => x._ItemFormat != null && allowedQuestionIdentifiers.Contains(x._ItemFormat.GetQuestionIdentifier()))
                          .GroupBy(x => sunToDiscriminator[x.About.ToKey()]);

          foreach (var row in groupByAbout_filtered) {
            var answersAbout = row.OrderByDescending(x => x.CompleteTime ?? DateTime.MinValue);

            var get = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "get").FirstOrDefault();
            var want = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "want").FirstOrDefault();
            var capacity = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "cap").FirstOrDefault();

            var yesNo = new Func<string, string>(x => {
              switch (x) {
                case "yes":
                  return "Y";
                case "no":
                  return "N";
                default:
                  return null;
              }
            });
            //var plusMinus = new Func<string, string>(x => {
            //	switch (x) {
            //		case "often":
            //			return "+";
            //		case "sometimes":
            //			return "+/–";
            //		case "not-often":
            //			return "–";
            //		default:
            //			return null;
            //	}
            //});
            //row.Key._PrettyString = userLu[row.Key];
            var sun = discriminatorToSun[row.Key];

            var arow = new AngularPeopleAnalyzerRow(sun.AccountabilityNode, !myNodes.Any(x => x.Id == sun.AccountabilityNodeId));
            rows.Add(arow);
          }

          var overridePriority = new DefaultDictionary<string, int>(x => 0) {
            { "often", 1 },{ "sometimes", 2 },  { "not-often", 3 },
            { "done", 1 }, { "not-done", 2 },
            { "yes", 1 }, { "no", 2 }
          };

          var rewrite = new DefaultDictionary<string, string>(x => x) {
            { "often", TransformValueAnswer["often"] },{ "sometimes", TransformValueAnswer["sometimes"] },  { "not-often",TransformValueAnswer["not-often"] },
            { "done", "done" }, { "not-done", "not done" },
            { "yes", "Y" }, { "no", "N" }
          };



          var surveyIssueDateLookup = surveys.ToDictionary(x => x.Id, x => x.GetIssueDate());
          var surveyLookup = surveys.ToDictionary(x => x.Id, x => x);
          var surveyItemLookup = items.ToDictionary(x => x.Id, x => x);
          var surveyItemFormatLookup = formats.ToDictionary(x => x.Id, x => x);

          var responses = new List<AngularPeopleAnalyzerResponse>();
          var accountabilityNodeResults_filtered = accountabiliyNodeResults.Where(x => allowedQuestionIdentifiers.Contains(x._ItemFormat.GetQuestionIdentifier()));

          foreach (var result in accountabilityNodeResults_filtered) {
            if (!surveyContainers.Any(x => x.Id == result.SurveyContainerId))
              continue;
            if (!surveys.Any(x => x.Id == result.SurveyId))
              continue;

            var answerDate = result.CompleteTime;

            var survey = surveyLookup[result.SurveyId];
            var issueDate = survey.GetIssueDate(); //surveyIssueDateLookup[result.SurveyId];
                                                   //var lockedIn = survey.LockedIn;
            var questionSource = surveyItemLookup[result.ItemId].GetSource();

            if (answerDate != null) {
              var byUser = result.By;
              var aboutUser = sunToAccNode[result.About.ToKey()];
              var answerFormatted = rewrite[result.Answer];
              var overrideAnswer = overridePriority[result.Answer];
              var format = surveyItemFormatLookup[result.ItemFormatId];
              var gwc = format.GetSetting<string>("gwc");
              var surveyContainerId = result.SurveyContainerId;

              if (gwc != null) {
                questionSource = new ForModel() {
                  ModelId = -1,
                  ModelType = gwc
                };
              }

              var response = new AngularPeopleAnalyzerResponse(
                        new ByAbout(byUser, aboutUser),
                        issueDate,
                        answerDate.Value,
                        questionSource,
                        answerFormatted,
                        result.Answer,
                        overrideAnswer,
                        surveyContainerId,
                        result.About.ModelId
                      );
              responses.Add(response);
            }
          }





          //foreach (var row in accountabiliyNodeResults.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>())) {
          //	values.GetOrAddDefault(row._Item.GetSource().ModelId, x => row._Item.GetName());
          //}
          var allLockedIn = new List<AngularLockedIn>();
          var userOrgSurveyLockinLookup = new DefaultDictionary<string, bool>(x => false);
          foreach (var survey in surveys) {
            userOrgSurveyLockinLookup[survey.By.ToKey()] = survey.LockedIn;
          }

          var userToNodes = new DefaultDictionary<long, List<ForModel>>(x => new List<ForModel>());

          foreach (var sun in allSurveyNodeItems) {
            userToNodes[sun.UserOrganizationId].Add(ForModel.Create<AccountabilityNode>(sun.AccountabilityNodeId));
            userToNodes[sun.UserOrganizationId] = userToNodes[sun.UserOrganizationId].Distinct(x => x.ToKey()).ToList();
          }

          foreach (var survey in surveys) {
            if (survey.By.Is<UserOrganizationModel>()) {

              var byUserOrgId = survey.By.ModelId;
              var lockedIn = survey.LockedIn;  //userOrgSurveyLockinLookup[ForModel.Create<UserOrganizationModel>(sun.UserOrganizationId).ToKey()];
              var surveyContainerId = survey.SurveyContainerId;

              foreach (var node in userToNodes[byUserOrgId]) {
                allLockedIn.Add(new AngularLockedIn() {
                  By = new AngularForModel(node),
                  LockedIn = lockedIn,
                  SurveyContainerId = surveyContainerId,
                  IssueDate = survey.GetIssueDate()
                });

              }
            }
          }
          //var allLockedIn = surveys.Select(x => new AngularLockedIn() {
          //	By = new AngularForModel(x.By),
          //	LockedIn = x.LockedIn,
          //	SurveyContainerId = x.SurveyContainerId,
          //});


          analyzer.Rows = rows;
          analyzer.Responses = responses;
          var values = accountabiliyNodeResults.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>());//new Dictionary<long, string>();

          //analyzer.Values = values.Select(x => new PeopleAnalyzerValue(surveyItemLookup[x.GetItemId()].GetSource()));
          analyzer.Values = new List<PeopleAnalyzerValue>();

          foreach (var value in values) {
            var model = surveyItemLookup[value.GetItemId()].GetSource();
            var ids = surveyItemLookup.Values.Where(x => x.Source.NotNull(y => x.GetSource().ModelId == model.ModelId)).Select(x => x.SurveyContainerId).Distinct().ToList();
            var peopleAnalyzer = new PeopleAnalyzerValue(model, ids);
            analyzer.Values = analyzer.Values.Concat(new[] { peopleAnalyzer });
          }

          analyzer.LockedIn = allLockedIn;

          analyzer.SurveyContainers = surveyContainers.Select(x => new AngularSurveyContainer(x, false, null));

          var issueDates = responses.Select(x => x.IssueDate.Value).ToList();

          var dateRange = new DateRange();
          if (issueDates.Any()) {
            dateRange = new DateRange(issueDates.Min(), issueDates.Max());
          }

          analyzer.DateRange = new AngularDateRange(dateRange);

          return analyzer;

        }
      }
    }
  }
}
