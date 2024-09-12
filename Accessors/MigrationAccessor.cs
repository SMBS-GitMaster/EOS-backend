using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Scheduler;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.L10.L10Recurrence;
using static RadialReview.Utilities.Testing.DeepAccessorEqualityTests.Tester;

namespace RadialReview.Accessors {
	public class MigrationAccessor {


		[Queue(HangfireQueues.Immediate.MIGRATION)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task AgendaAdjustments(PerformContext context) {
			var a = 0;
			var total = 0;
			var now = DateTime.UtcNow.ToJsMs();
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {
					var meetings = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime == null && x.MeetingType == MeetingType.L10)
						.Select(x => x.Id, x => x.OrganizationId)
						.List<object[]>().Select(x => new {
							recurId = (long)x[0],
							orgId = (long)x[1]
						}).ToList();

					var titles = new[] {
						new []{"Segue" },
						new []{"Scorecard" },
						new []{"Rock Review" },
						new []{"People Headlines" },
						new []{"To-do List" },
						new []{"Identify Discuss Solve", "IDS" },
						new []{"Conclude" }
					};

					var durations = new decimal[]{
						5m,
						5m,
						5m,
						5m,
						5m,
						60m,
						5m
					};

					var types = new L10PageType[]{
						L10PageType.Segue,
						L10PageType.Scorecard,
						L10PageType.Rocks,
						L10PageType.Headlines,
						L10PageType.Todo,
						L10PageType.IDS,
						L10PageType.Conclude
					};

					total = meetings.Count;
					var idx = 0.0;
					var bar = context.WriteProgressBar("Completed");
					foreach (var meeting in meetings) {
						bar.SetValue(idx / (double)total*100.0);
						idx += 1;
						var mid = meeting.recurId;
						var orgId = meeting.orgId;

						var pages = s.QueryOver<L10Recurrence_Page>()
							.Where(x => x.L10RecurrenceId == mid && x.DeleteTime == null)
							.List().OrderBy(x => x._Ordering)
							.ToArray();

						//Exact length
						if (pages.Length != 7) {
							continue;
						}

						//Titles Match
						var failed = false;
						for (var i = 0; i < titles.Length; i++) {
							if (!titles[i].Any(t => t == pages[i].Title))
								failed = true;
						}
						if (failed == true)
							continue;

						//Durations Match
						failed = false;
						for (var i = 0; i < durations.Length; i++) {
							if (pages[i].Minutes != durations[i])
								failed = true;
						}
						if (failed == true)
							continue;

						//Types Match
						failed = false;
						for (var i = 0; i < types.Length; i++) {
							if (pages[i].PageType != types[i])
								failed = true;
						}
						if (failed == true)
							continue;

						var oldValues = new List<L10PageAudit>();
						foreach (var p in pages) {
							oldValues.Add(new L10PageAudit() {
								OrgId = orgId,
								RecurId = mid,
								OldName = p.Title,
								OldOrder = p._Ordering,
								PageId = p.Id,
								Time = now,
								Reverted = false,
								ExpectedDuration = p.Minutes,
								ExpectedPageType = p.PageType
							});
						}


						//Update names
						pages[0].Title = "Check-in";
						pages[1].Title = "Metrics";
						pages[2].Title = "Goals";
						pages[3].Title = "Headlines";
						pages[4].Title = "To-dos";
						pages[5].Title = "Issues";
						pages[6].Title = "Wrap-up";


						//Swap order
						var oldMetricsOrder = pages[1]._Ordering;
						var oldGoalsOrder = pages[2]._Ordering;
						pages[1]._Ordering = oldGoalsOrder;
						pages[2]._Ordering = oldMetricsOrder;

						//finalize updates & save
						for (var i = 0; i < pages.Length; i++) {
							oldValues[i].NewName = pages[i].Title;
							oldValues[i].NewOrder = pages[i]._Ordering;
							s.Insert(oldValues[i]);
						}

						//Save
						for (var i = 0; i < pages.Length; i++) {
							s.Update(pages[i]);
						}

						a++;
					}

					tx.Commit();
				}
			}
			context.WriteLine("updated meetings:" + a + "/" + total + " @ " + now + "ms");
		}


		[Queue(HangfireQueues.Immediate.MIGRATION)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task AgendaAdjustmentsRevert(PerformContext context, long? time = null) {
			var errors = 0;
			var count = 0;
			var total = 0;
			var now = DateTime.UtcNow.ToJsMs();
			var bar = context.WriteProgressBar("Completed");
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {

					var q = s.QueryOver<L10PageAudit>().Where(x => x.Reverted == false);
					if (time != null)
						q = q.Where(x => x.Time == time);

					var res = q.List().ToList();

					var auditsByMeeting = res.GroupBy(x => x.RecurId).ToList();

					total = auditsByMeeting.Count;
					var idx = 0.0;

					foreach (var mAuditPages in auditsByMeeting) {
						bar.SetValue(idx / (double)total*100.0);
						idx += 1;
						var pageIds = mAuditPages.Select(x => x.PageId).ToArray();
						var livePages = s.QueryOver<L10Recurrence_Page>()
							.Where(x => x.DeleteTime == null)
							.WhereRestrictionOn(x => x.Id).IsIn(pageIds)
							.List().ToList();

						if (livePages.Count != 7)
							continue;

						//All match
						var failed = false;
						foreach (var auditPage in mAuditPages) {
							var liveMatch = livePages.FirstOrDefault(x => x.Id == auditPage.PageId);
							if (liveMatch == null ||
									liveMatch.Title != auditPage.NewName ||
									liveMatch._Ordering != auditPage.NewOrder ||
									liveMatch.Minutes != auditPage.ExpectedDuration ||
									liveMatch.PageType != auditPage.ExpectedPageType) {
								failed = true;
								context.WriteLine("Error recur:" + auditPage.RecurId + " page:" + auditPage.PageId);
							}
						}

						if (failed) {
							errors += 1;
							continue;
						}

						foreach (var auditPage in mAuditPages) {
							var liveMatch = livePages.FirstOrDefault(x => x.Id == auditPage.PageId);
							liveMatch.Title = auditPage.OldName;
							liveMatch._Ordering = auditPage.OldOrder;
							s.Update(liveMatch);
							auditPage.Reverted = true;
							auditPage.RevertTime = now;
							s.Update(auditPage);
						}

						count += 1;
					}

					tx.Commit();

				}
			}
			context.WriteLine("count:" + count + "/" + total + "  errors:" + errors + " @ " + now);

		}


		[Queue(HangfireQueues.Immediate.MIGRATION)]
		[AutomaticRetry(Attempts = 0)]
		public static object CreateSimpleRoles(string progressId, PerformContext context) {
			var now = DateTime.UtcNow;
			var orgIds = new List<Tuple<long, long>>();
			var prog = new ProgressModel { Id = progressId };
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					orgIds = s.QueryOver<OrganizationModel>()
						.Select(x => x.Id, x => x.AccountabilityChartId)
						.List<object[]>()
						.Select(x => Tuple.Create((long)x[0], (long)x[1]))
						.Where(x => x.Item2 != 0)
						.ToList();

					prog.Id = progressId;
					prog.OrgId = -1;
					prog.CompletedCount = 0;
					prog.TotalCount = orgIds.Count;
					prog.TaskName = "Create Simple Roles";
					prog.CreatedBy = -1;
					prog.CreateTime = now;

					s.SaveOrUpdate(prog);

					tx.Commit();
					s.Flush();
				}
			}


			var i = 1;
			var totalC = orgIds.Count;


			var progress = context.WriteProgressBar();

			foreach (var o in orgIds) {
				var orgId = o.Item1;
				var chartId = o.Item2;
				Scheduler.Enqueue(() => MigrateRoles(progressId, now, orgId, chartId, null));
			}


			return new {
				orgCount = orgIds.Count,
				progressId
			};
		}

		[Queue(HangfireQueues.Immediate.MIGRATION)]
		[AutomaticRetry(Attempts = 0)]
		public static void MigrateRoles(string progressId, DateTime now, long orgId, long chartId, PerformContext context) {
			try {

				var organizations = 0;
				var updateRoles = 0;
				var totalRoles = 0;
				var updatePositionDurations = 0;
				var totalPositionDurations = 0;
				var updateRoleGroups = 0;
				var totalRoleGroups = 0;

				context.WriteLine(orgId);
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, UserOrganizationModel.CreateAdmin());

						perms.GetCaller().Organization = s.Get<OrganizationModel>(orgId);

						var chart = AccountabilityAccessor.AADeprecated.GetTree_Deprecated(s, perms, chartId);
						var existingSimpleRoles = s.QueryOver<SimpleRole>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List().ToList();
						chart.Dive(n => {
							if (n.Group != null && n.Group.RoleGroups != null) {
								foreach (var g in n.Group.RoleGroups) {
									foreach (var role in g.Roles) {
										if (!existingSimpleRoles.Any(x => x.OrgId == orgId && x.Name == role.Name && x.NodeId == n.Id)) {
											s.Save(new SimpleRole() {
												AttachType_Deprecated = g.AttachType.NotNull(x => x.ToString()),
												Name = role.Name,
												CreateTime = role.CreateTime ?? now,
												NodeId = n.Id,
												Ordering = role.Ordering ?? 0,
												OrgId = orgId,
											});
											updateRoles += 1;
										} else {
											int a = 0;
										}
										totalRoles += 1;
									}
								}
							}
						});


						var posDur = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.OrganizationId == orgId).List().ToList();
						foreach (var pd in posDur) {
							if (pd.PositionName == null) {
								pd.PositionName = pd.DepricatedPosition.CustomName;
								s.Update(pd);
								updatePositionDurations += 1;
							}
							totalPositionDurations += 1;
						}


						var roleGroup = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.DeleteTime == null && x.OrganizationId == orgId).List().ToList();
						foreach (var rg in roleGroup) {
							if (rg.PositionName == null && rg.DepricatedPosition != null) {
								rg.PositionName = rg.DepricatedPosition.NotNull(x => x.CustomName);
								s.Update(rg);
								updateRoleGroups += 1;
							}
							totalRoleGroups += 1;
						}


						tx.Commit();
						s.Flush();
					}
				}
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var prog = s.Get<ProgressModel>(progressId, LockMode.Upgrade);
						prog.CompletedCount += 1;

						var d = prog.Data.NotNull(x => JsonConvert.DeserializeObject<ProgData>(x)) ?? new ProgData();
						d.organizations += organizations;
						d.updateRoles += updateRoles;
						d.totalRoles += totalRoles;
						d.updatePositionDurations += updatePositionDurations;
						d.totalPositionDurations += totalPositionDurations;
						d.updateRoleGroups += updateRoleGroups;
						d.totalRoleGroups += totalRoleGroups;
						prog.Data = JsonConvert.SerializeObject(d);

						s.Update(prog);

						tx.Commit();
						s.Flush();
					}
				}
			} catch (Exception e) {
				context.WriteLine(ConsoleTextColor.Red, "Org:" + orgId);
				context.WriteLine(ConsoleTextColor.Red, e.Message + "\n" + e.StackTrace);
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var prog = s.Get<ProgressModel>(progressId, LockMode.Upgrade);

						var d = prog.Data.NotNull(x => JsonConvert.DeserializeObject<ProgData>(prog.Data)) ?? new ProgData();
						d.errorCount += 1;
						d.ErrorOrgs.Add(orgId);
						d.Errors.Add("[" + orgId + "]" + e.Message + "\n" + e.StackTrace);
						prog.Data = JsonConvert.SerializeObject(d);

						s.Update(prog);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}



		public class ProgData {

			public int errorCount { get; set; }
			public int updateRoles { get; set; }
			public int totalRoles { get; set; }
			public int updatePositionDurations { get; set; }
			public int totalPositionDurations { get; set; }
			public int updateRoleGroups { get; set; }
			public int totalRoleGroups { get; set; }
			public int organizations { get; set; }
			public List<string> Errors { get; set; }
			public List<long> ErrorOrgs { get; set; }
			public ProgData() {
				Errors = new List<string>();
				ErrorOrgs = new List<long>();
			}
		}

		private static OnDifference SaveOnDifference(PerformContext context) {
			OnDifference del = ((method, message, a, b, c, set, oldException, newException) => {
				var lines = new List<string>() {
						"=====================================================",
						method+"("+string.Join(",",a,b,c)+")",
						message,
						set.NotNull(x=>x.GetStringDifference(8)) ?? "        -no set-",
						"Old:",
						set.NotNull(x=>JsonConvert.SerializeObject(x.OldValues,Formatting.Indented)),
						"New:",
						set.NotNull(x=>JsonConvert.SerializeObject(x.NewValues,Formatting.Indented)),
						oldException.NotNull(x=>"~~~~\nOld:"+x.Message+"\n"+x.StackTrace),
						newException.NotNull(x=>"~~~~\nNew:"+x.Message+"\n"+x.StackTrace),
						""
					};

				context.SetTextColor(ConsoleTextColor.DarkYellow);
				context.WriteLine(string.Join("\n", lines));
			});
			return del;
		}

		[Queue(HangfireQueues.Immediate.MIGRATION)]
		public static object RunMigrationTests(double minutes, int samples, bool overrideSafety, PerformContext context) {

			if (overrideSafety || Config.IsStaging()) {
				context.SetTextColor(ConsoleTextColor.White);
				context.WriteLine("Starting.");
				var progress = context.WriteProgressBar();

				DeepAccessorEqualityTests.Users_GetDirectReportsAndSelfModels().TestWith("Users_GetDirectReportsAndSelfModels", 589351, 589351, -1, SaveOnDifference(context));
				DeepAccessorEqualityTests.Users_GetDirectReportsAndSelfModels().TestWith("Users_GetDirectReportsAndSelfModels", 611965, 589351, -1, SaveOnDifference(context));
				DeepAccessorEqualityTests.Users_GetDirectReportsAndSelfModels().TestWith("Users_GetDirectReportsAndSelfModels", 612126, 589351, -1, SaveOnDifference(context));
				DeepAccessorEqualityTests.GetChildrenAndSelfModels().TestWith("GetChildrenAndSelfModels", 3459, 1526, -1, SaveOnDifference(context));
				DeepAccessorEqualityTests.GetChildrenAndSelf().TestWith("GetChildrenAndSelf", 3610, 3754, -1, SaveOnDifference(context));
				DeepAccessorEqualityTests.GetChildrenAndSelf().TestWith("GetChildrenAndSelf", 284413, 219180, -1, SaveOnDifference(context));


				var idx = 1;
				Stopwatch timeout = Stopwatch.StartNew();
				while (timeout.Elapsed < TimeSpan.FromMinutes(minutes)) {
					progress.SetValue(timeout.Elapsed.TotalMinutes / minutes * 100);

					var sw = Stopwatch.StartNew();
					try {
						foreach (var t in DeepAccessorEqualityTests.GetAllTests()) {
							try {
								var meta = t.TestSampling(samples, SaveOnDifference(context));
							} catch (Exception e) {
								var st = e.StackTrace.NotNull(x => x.Replace("\n", "\n\t\t"));
								context.SetTextColor(ConsoleTextColor.Red);
								context.WriteLine(e.Message + "\n");
								context.SetTextColor(ConsoleTextColor.DarkRed);
								context.WriteLine(st);
							}
						}

					} catch (Exception e) {
						var st = e.StackTrace.NotNull(x => x.Replace("\n", "\n\t\t"));
						context.SetTextColor(ConsoleTextColor.Red);
						context.WriteLine("Fatal: " + e.Message);
						context.SetTextColor(ConsoleTextColor.DarkRed);
						context.WriteLine(st);

					} finally {
					}
					idx += 1;
				}
				context.SetTextColor(ConsoleTextColor.White);
				context.WriteLine("Complete.");
				progress.SetValue(100);
				return new {
					samples,
					count = idx,
					duration = timeout.Elapsed.TotalSeconds + "s"
				};
			} else {
				throw new Exception("Not on staging. Manually bypass safety if required.");
			}
		}


		public static async Task<string> UpdateUserCache(int divisor) {
			var groups = divisor;
			var updates = 0;
			long ms = 0;

			return BatchJob.StartNew(x => {
				for (var i = 0; i < groups; i++) {
					x.Enqueue(() => UpdateUserCache_Mod(i, divisor));
				}
			});
		}


		[Queue(HangfireQueues.Immediate.MIGRATION)]
		public static async Task UpdateUserCache_Mod(int remainder, int divisor) {
			var sw = Stopwatch.StartNew();
			var updates = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var users = s.QueryOver<UserOrganizationModel>()
						.Where(CriterionUtility.Mod<UserOrganizationModel>(x => x.Id, divisor, remainder))
						.List().ToList();

					foreach (var u in users) {
						u.UpdateCache(s);
						updates += 1;
					}


					tx.Commit();
					s.Flush();
				}
			}
			sw.Stop();
			await Task.Delay(150);
		}
	}
}
