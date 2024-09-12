using NHibernate;
using NHibernate.Criterion;
using RadialReview.Accessors;
using RadialReview.Accessors.Old;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RadialReview.Utilities.Testing {
	public enum DAType {
		None,
		NodeId,
		UserOrganizationId
	}
	public class DeepAccessorEqualityTests {

		#region Meta and Tester
		public class Meta {
			public TimeSpan TotalDuration { get; set; }
			public TimeSpan NewMethodDuration { get; set; }
			public TimeSpan OldMethodDuration { get; set; }
			public List<string> Differences { get; set; }

			public int[] Counts;
			public double[] CountPercentage {
				get {
					var n = CalcTotal();
					return Counts.Select(x => ((double)x) / n).ToArray();
				}
			}

			public double CalcTotal() {
				return Counts.Sum(x => x) + ExpectedErrorCount;
			}

			public int ExpectedErrorCount { get; set; }

			public string CountsToString() {
				var builder = string.Format("{0,5} = {1,5:0.00}  {2,-8}", "Len", "%", "Count") + "\n";

				var maxI = Counts.Select((x, i) => x > 0 ? i : 0).Max() + 1;
				var cp = CountPercentage;
				for (var i = 0; i < maxI; i++) {
					builder += string.Format("{0,5} = {1,5:0.00}%  {2,-8}", i, cp[i] * 100, Counts[i]);
					builder += "\n";
				}

				builder += string.Format("{0,5} = {1,5:0.00}%  {2,-8}", "E[err]", ExpectedErrorCount / CalcTotal() * 100, ExpectedErrorCount);

				return builder;
			}

			public Meta() {
				Differences = new List<string>();
				Counts = new int[100];
			}

			public bool HasErrors() {
				if (CalcTotal() == 0.0)
					return true;
				return Differences.Count > 0;
			}


			public override string ToString() {
				return Differences.Count() + "/" + CalcTotal() + " " + NewMethodDuration.TotalSeconds + "s\t\t" + OldMethodDuration.TotalSeconds + "s";
			}
		}



		public class Tester {

			private Tester(String name, DAType arg1Type, DAType arg2Type, DAType arg3Type, MethodStr3 oldMethod, MethodStr3 newMethod) {
				Arg1Type = arg1Type;
				Arg2Type = arg2Type;
				Arg3Type = arg3Type;
				this.oldMethod = oldMethod;
				this.newMethod = newMethod;
				Name = name;

			}

			public static Tester CreateLong(String name, DAType arg1Type, DAType arg2Type, MethodLong oldMethod, MethodLong newMethod) {
				return new Tester(name, arg1Type, arg2Type, DAType.None,
					(s, a, b, c) => oldMethod(s, a, b).Select(x => "" + x).ToList(),
					(s, a, b, c) => newMethod(s, a, b).Select(x => "" + x).ToList()
				);
			}
			public static Tester CreateString(String name, DAType arg1Type, DAType arg2Type, MethodStr oldMethod, MethodStr newMethod) {
				return new Tester(name, arg1Type, arg2Type, DAType.None,
					(s, a, b, c) => oldMethod(s, a, b),
					(s, a, b, c) => newMethod(s, a, b)
				);
			}
			public static Tester CreateBool(String name, DAType arg1Type, DAType arg2Type, MethodBool oldMethod, MethodBool newMethod) {
				return new Tester(name, arg1Type, arg2Type, DAType.None,
					(s, a, b, c) => new List<string>() { "" + oldMethod(s, a, b) },
					(s, a, b, c) => new List<string>() { "" + newMethod(s, a, b) }
				);
			}
			public static Tester CreateBool(String name, DAType arg1Type, DAType arg2Type, DAType arg3Type, MethodBool3 oldMethod, MethodBool3 newMethod) {
				return new Tester(name, arg1Type, arg2Type, arg3Type,
					(s, a, b, c) => new List<string>() { "" + oldMethod(s, a, b, c) },
					(s, a, b, c) => new List<string>() { "" + newMethod(s, a, b, c) }
				);
			}
			public static Tester CreateAccountabilityNode(String name, DAType arg1Type, DAType arg2Type, MethodACNode oldMethod, MethodACNode newMethod) {
				return new Tester(name, arg1Type, arg2Type, DAType.None,
					(s, a, b, c) => oldMethod(s, a, b).Select(x => FromNode(x, true)).ToList(),
					(s, a, b, c) => newMethod(s, a, b).Select(x => FromNode(x, false)).ToList()
				);
			}
			public static Tester CreateUserOrg(String name, DAType arg1Type, DAType arg2Type, MethodUserOrg oldMethod, MethodUserOrg newMethod) {
				return new Tester(name, arg1Type, arg2Type, DAType.None,
					(s, a, b, c) => oldMethod(s, a, b).Select(x => FromUserOrg(x)).ToList(),
					(s, a, b, c) => newMethod(s, a, b).Select(x => FromUserOrg(x)).ToList()
				);
			}

			public static string Builder(params object[] items) {
				var b = string.Join("~", items.Select(x => "" + x));
				return b;
			}
			private static string FromNode(AccountabilityNode x, bool old) {
				var user = old ? x.DeprecatedUser : x.GetUsers(null).FirstOrDefault();

				if (!old && x.GetUsers(null).Count > 1) {
					throw new Exception("Not expecting multiple users. Found " + x.GetUsers(null).Count);
				}

				if (old && x.DeleteTime != null) {
					user = null;
				}

				return Builder(
					x.Id,
					x.CreateTime,
					x.DeleteTime,
					x.GetPositionName(),
					user.NotNull(y => y.Id),
					user.NotNull(y => y.GetName()),
					user.NotNull(y => y.GetEmail()),
					user.NotNull(y => y.DeleteTime),
					user.NotNull(y => y.NotNull(z => z.TempUser.Id)),
					x.GetAccountabilityRolesGroupId(),
					x.ParentNodeId,
					x.AccountabilityChartId,
					x.ModelId,
					x.ModelType,
					x.Ordering,
					x.OrganizationId
				);
			}
			private static string FromUserOrg(UserOrganizationModel x) {
				return Builder(
					x.Id,
					x.CreateTime,
					x.DeleteTime,
					x.GetEmail(),
					x.GetName(),
					x.Cache.Id,
					x.Cache.Name,
					x.TempUser.NotNull(y => y.Name())
				);
			}

			private DAType Arg1Type { get; set; }
			private DAType Arg2Type { get; set; }
			private DAType Arg3Type { get; set; }
			private MethodStr3 oldMethod { get; set; }
			private MethodStr3 newMethod { get; set; }
			private string Name { get; set; }

			public delegate IEnumerable<string> MethodStr(ISession s, long a, long b);
			public delegate IEnumerable<string> MethodStr3(ISession s, long a, long b, long c);
			public delegate IEnumerable<long> MethodLong(ISession s, long a, long b);
			public delegate bool MethodBool(ISession s, long a, long b);
			public delegate bool MethodBool3(ISession s, long a, long b, long c);
			public delegate void OnDifference(string method, string message, long a, long b, long c, SetUtility.AddedRemoved<string> set, Exception oldException, Exception newException);

			public delegate IEnumerable<AccountabilityNode> MethodACNode(ISession s, long a, long b);
			public delegate IEnumerable<UserOrganizationModel> MethodUserOrg(ISession s, long a, long b);


			public Tester TestWith(string name, long a, long b, long c, OnDifference onDifference) {
				var m = new Meta();

				OnDifference evts = PrintDifferenceFull;
				if (onDifference != null)
					evts += onDifference;

				ExecuteBoth(name, m, oldMethod, newMethod, a, b, c, evts);
				return this;
			}

			private List<long> GenerateIds(ISession s, DAType type, long[] orgIds) {
				switch (type) {
					case DAType.NodeId:
						return s.QueryOver<AccountabilityNode>()
							.WhereRestrictionOn(x => x.OrganizationId).IsIn(orgIds)
							.Select(x => x.Id)
							.List<long>().ToList();
					case DAType.UserOrganizationId:
						return s.QueryOver<UserOrganizationModel>()
							.WhereRestrictionOn(x => x.Organization.Id).IsIn(orgIds)
							.Select(x => x.Id).List<long>().ToList();
					case DAType.None:
						return new List<long>() { -1 };
					default:
						throw new NotImplementedException();
				}
			}

			private void PrintDifferenceFull(string method, string message, long a, long b, long c, SetUtility.AddedRemoved<string> set, Exception oldException, Exception newException) {
				Console.WriteLine();
				Console.WriteLine(message);
				if (set != null) {
					set.PrintDifference(4);
				}
			}

			private void PrintDifference(string method, string message, long a, long b, long c, SetUtility.AddedRemoved<string> set, Exception oldException, Exception newException) {
				Console.WriteLine();
				Console.WriteLine(message);
			}

			public Meta TestSampling(int samples = 300, OnDifference onDifference = null) {
				return TestSampling(Name, Arg1Type, Arg2Type, Arg3Type, oldMethod, newMethod, onDifference, samples);
			}
			private Meta TestSampling(string name, DAType arg1Type, DAType arg2Type, DAType arg3Type, MethodStr3 oldMethod, MethodStr3 newMethod, OnDifference onDifference, int samples = 300) {
				var meta = new Meta();
				List<long> type1Ids, type2Ids, type3Ids;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var allOrgs = s.QueryOver<OrganizationModel>()
										.Where(x => x.DeleteTime == null)
										.Select(x => x.Id)
										.List<long>()
										.ToArray();

						allOrgs = allOrgs.Shuffle().Take(2).ToArray();

						type1Ids = GenerateIds(s, arg1Type, allOrgs);
						type2Ids = (arg1Type == arg2Type) ? type1Ids : GenerateIds(s, arg2Type, allOrgs);
						type3Ids = (arg1Type == arg3Type) ? type1Ids : ((arg2Type == arg3Type) ? type2Ids : GenerateIds(s, arg3Type, allOrgs));

					}
				}

				var parameters = 0.0;
				var mult = 1.0;
				if (arg1Type != DAType.None) {
					parameters += 1;
				}
				if (arg2Type != DAType.None) {
					parameters += 1;
				}
				if (arg3Type != DAType.None) {
					parameters += 1;
				}

				if (arg1Type != DAType.None && arg1Type == arg2Type && arg1Type == arg3Type) {
					mult *= 3;
				} else if (arg1Type != DAType.None && arg1Type == arg2Type) {
					mult *= 2;
				}

				if (arg1Type != DAType.None && arg2Type == arg3Type) {
					mult *= 2;
				}

				var n = (int)Math.Ceiling(Math.Pow(samples / mult, 1 / (parameters)));

				var A = type1Ids.Shuffle().Take(n).ToList();
				var B = type2Ids.Shuffle().Take(n).ToList();
				var C = type3Ids.Shuffle().Take(n).ToList();

				if (arg1Type == arg2Type) {
					A.AddRange(B);
				}
				if (arg1Type == arg3Type) {
					A.AddRange(C);
				}
				if (arg2Type == arg3Type) {
					B.AddRange(C);
				}

				A = A.Distinct().ToList();
				B = B.Distinct().ToList();
				C = C.Distinct().ToList();

				int t = A.Count * B.Count * C.Count;
				int p = t / 100;
				var sw = Stopwatch.StartNew();

				OnDifference evts = PrintDifference;
				if (onDifference != null)
					evts += onDifference;

				Console.WriteLine("----------------------------------------------");
				Console.WriteLine("Starting: " + name + " (" + t + ")");
				foreach (var a in A) {
					foreach (var b in B) {
						foreach (var c in C) {
							ExecuteBoth(name, meta, oldMethod, newMethod, a, b, c, evts);


							if (p == 0 || ((int)meta.CalcTotal()) % p == 0) {
								Console.Write("\r" + meta.CalcTotal() * 100 / t + "%           ");
							}
						}
					}
				}
				meta.TotalDuration = sw.Elapsed;
				Console.WriteLine();
				Console.WriteLine("Completed. " + (Math.Round(meta.TotalDuration.TotalSeconds, 3)) + "s");

				return meta;
			}

			private static void ExecuteBoth(string name, Meta meta, MethodStr3 oldMethod, MethodStr3 newMethod, long a, long b, long c, OnDifference onDifference) {
				List<string> oldRes = null, newRes = null;
				bool oldHasError = false;
				bool newHasError = false;
				Exception oldException = null;
				Exception newException = null;
				string oldErrorMessage = null;
				string newErrorMessage = null;
				{
					var oldSw = Stopwatch.StartNew();
					try {
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								oldRes = oldMethod(s, a, b, c).ToList();
							}
						}
					} catch (Exception e) {
						oldRes = new List<string> { "err" };
						oldHasError = true;
						oldErrorMessage = e.Message;
						oldException = e;
					}
					meta.OldMethodDuration = meta.OldMethodDuration.Add(oldSw.Elapsed);
				}
				{
					var newSw = Stopwatch.StartNew();
					try {
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								newRes = newMethod(s, a, b, c).ToList();
							}
						}
					} catch (Exception e) {
						newErrorMessage = e.Message;
						newException = e;
						newRes = new List<string> { "err" };
						newHasError = true;
						if (oldHasError) {
							if (e.Message == oldErrorMessage) {
								meta.ExpectedErrorCount += 1;
							} else {
								var message = "\tMessages are different:\n\t\t" + oldErrorMessage + "\n\t\t" + e.Message;
								onDifference(name, message, a, b, c, null, oldException, newException);
								throw new Exception(message);
							}
						} else {
							var message = "\tOnly new method has error:\n\t\t" + e.Message;
							onDifference(name, message, a, b, c, null, oldException, newException);
							throw new Exception(message);
						}
					}
					if (oldHasError && !newHasError) {
						var message = "\tOnly old method has error:\n\t\t" + oldErrorMessage;
						onDifference(name, message, a, b, c, null, oldException, newException);
						throw new Exception(message);
					}

					meta.NewMethodDuration = meta.NewMethodDuration.Add(newSw.Elapsed);
				}
				if (!oldHasError && !newHasError) {
					meta.Counts[Math.Min(newRes.Count, meta.Counts.Length - 1)] += 1;
					var set = SetUtility.AddRemove(oldRes, newRes);

					if (!set.AreSame()) {
						var message = "Difference with " + a + "," + b + "," + c;
						onDifference(name, message, a, b, c, set, oldException, newException);
						meta.Differences.Add(message);
					}

				}
			}
		}
		#endregion


		public static IEnumerable<Tester> GetAllTests() {
			yield return GetDirectReportsAndSelf();
			yield return DeepAccessorConfirmWeOwnUser();
			yield return DeepAccessorGetAllChildren();
			yield return GetChildrenAndSelfModels();
			yield return GetChildrenAndSelf();
			yield return ManagesNode();

			yield return Users_GetSubordinatesAndSelf(true);
			yield return Users_GetSubordinatesAndSelf(false);
			yield return Users_HasChildren();

			yield return Users_GetDirectReportsAndSelfModels();
			yield return Users_ManagesUser(false);
			yield return Users_ManagesUser(true);
			yield return Users_GetNodesForUser();


		}


		public static Tester DeepAccessorConfirmWeOwnUser() {
			return Tester.CreateLong("DeepAccessorConfirmWeOwnUser", DAType.UserOrganizationId, DAType.UserOrganizationId,
				(s, callerId, userId) => {
					//OLD
					AccountabilityNode parent = null;
					AccountabilityNode child1 = null;
					var found = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null)
						.JoinAlias(x => x.Parent, () => parent)
						.JoinAlias(x => x.Child, () => child1)
							.Where(x => parent.DeleteTime == null && child1.DeleteTime == null && parent.DeprecatedUserId == callerId && child1.DeprecatedUserId == userId)
							.Select(x => x.Id)
							.OrderBy(x => x.Id).Asc
							.Take(1)
							.SingleOrDefault<long>();
					return found.AsList();
				},
				(s, callerId, userId) => {
					//NEW
					var parentNodeIds = QueryOver.Of<AccountabilityNodeUserMap>()
								.Where(x => x.DeleteTime == null && x.UserId == callerId)
								.Select(Projections.Distinct(Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId)));
					var childNodeIds = QueryOver.Of<AccountabilityNodeUserMap>()
						.Where(x => x.DeleteTime == null && x.UserId == userId)
						.Select(Projections.Distinct(Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId)));

					IEnumerable<UserOrganizationModel> childUsers1 = null;
					var found1 = s.QueryOver<DeepAccountability>()
						.Where(x => x.DeleteTime == null)
						.WithSubquery.WhereProperty(x => x.Parent.Id).In(parentNodeIds)
						.WithSubquery.WhereProperty(x => x.Child.Id).In(childNodeIds)
						.Select(x => x.Id)
						.OrderBy(x => x.Id).Asc
						.Take(1)
						.SingleOrDefault<long>();
					return found1.AsList();
				});
		}
		public static Tester DeepAccessorGetAllChildren() {
			return Tester.CreateLong("DeepAccessorGetAllChildren", DAType.UserOrganizationId, DAType.UserOrganizationId,
				(s, callerId, userId) => {
					var user = s.Get<UserOrganizationModel>(userId);
					var allMyNodesQuery = QueryOver.Of<AccountabilityNode>()
													.Where(x => x.DeleteTime == null && x.OrganizationId == user.Organization.Id && x.DeprecatedUserId == userId)
													.Select(x => x.Id);



					AccountabilityNode child = null;
					var subordinateQueries = new List<IEnumerable<long>>();


					var res = s.QueryOver<DeepAccountability>()
							.Where(x => x.DeleteTime == null)
							////////////////
							//Added vvv
							.WithSubquery.WhereProperty(x => x.ParentId).In(allMyNodesQuery)
							//removed vvv
							.JoinAlias(x => x.Child, () => child)
								.Where(x => child.DeleteTime == null && child.DeprecatedUserId != null)
								.Select(x => child.DeprecatedUserId)
								.Future<long>();


					return res.ToList();
				},
				(s, callerId, userId) => {
					AccountabilityNode nodeAlias1 = null;
					AccountabilityNode nodeAlias2 = null;
					var allNodesForUserQuery = QueryOver.Of<AccountabilityNodeUserMap>()
							.JoinAlias(x => x.AccountabilityNode, () => nodeAlias1)
							.Where(x => x.DeleteTime == null && x.UserId == userId && nodeAlias1.DeleteTime == null)
							.Select(Projections.Distinct(Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId)));

					var childAcIdsWhereUserIsTheParent = QueryOver.Of<DeepAccountability>()
								.JoinAlias(x => x.Child, () => nodeAlias2)
								.Where(x => x.DeleteTime == null && nodeAlias2.DeleteTime == null)
								.WithSubquery.WhereProperty(x => x.ParentId).In(allNodesForUserQuery)
								.Select(Projections.Distinct(Projections.Property<DeepAccountability>(x => x.ChildId)));


					var childUserIds = s.QueryOver<AccountabilityNodeUserMap>()
						.Where(x => x.DeleteTime == null)
						.WithSubquery.WhereProperty(x => x.AccountabilityNodeId)
						.In(childAcIdsWhereUserIsTheParent)
						.Select(x => x.UserId)
						.Future<long>();
					return childUserIds.ToList();
				});

		}



		public static Tester GetChildrenAndSelfModels() {
			return Tester.CreateAccountabilityNode("GetChildrenAndSelfModels", DAType.UserOrganizationId, DAType.NodeId,
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return Old.DeepAccessor.GetChildrenAndSelfModels(s, caller, nodeId);
					},
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return DeepAccessor.Nodes.GetChildrenAndSelfModels(s, caller, nodeId);
					});
		}
		public static Tester GetChildrenAndSelf() {
			return Tester.CreateLong("GetChildrenAndSelf", DAType.UserOrganizationId, DAType.NodeId,
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return Old.DeepAccessor.GetChildrenAndSelf(s, caller, nodeId);
					},
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return DeepAccessor.Nodes.GetChildrenAndSelf(s, caller, nodeId);
					});
		}
		public static Tester GetDirectReportsAndSelf() {
			return Tester.CreateAccountabilityNode("GetDirectReportsAndSelf", DAType.UserOrganizationId, DAType.NodeId,
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.GetDirectReportsAndSelf(s, perms, nodeId);
					},
					(s, callerId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Nodes.GetDirectReportsAndSelf(s, perms, nodeId);
					});
		}


		public static Tester ManagesNode() {
			return Tester.CreateBool("ManagesNode", DAType.UserOrganizationId, DAType.UserOrganizationId, DAType.NodeId,
					(s, callerId, managerUserId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.ManagesNode(s, perms, managerUserId, nodeId);
					},
					(s, callerId, managerUserId, nodeId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Permissions.ManagesNode(s, perms, managerUserId, nodeId);
					});
		}

		public static Tester Users_GetSubordinatesAndSelf(bool tempUsers) {
			return Tester.CreateLong("Users_GetSubordinatesAndSelf(" + tempUsers + ")", DAType.UserOrganizationId, DAType.UserOrganizationId,
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return Old.DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, userId, null, tempUsers);
					},
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						return DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, userId, tempUsers);
					});
		}
		public static Tester Users_HasChildren() {
			return Tester.CreateBool("Users_HasChildren", DAType.UserOrganizationId, DAType.UserOrganizationId,
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.Users.HasChildren(s, perms, userId);
					},
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Users.HasChildren(s, perms, userId);
					});
		}
		public static Tester Users_GetDirectReportsAndSelfModels() {
			return Tester.CreateUserOrg("Users_GetDirectReportsAndSelfModels", DAType.UserOrganizationId, DAType.UserOrganizationId,
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.Users.GetDirectReportsAndSelfModels(s, perms, userId);
					},
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Users.GetDirectReportsAndSelfModels(s, perms, userId);
					});
		}
		public static Tester Users_ManagesUser(bool allowDeletedSubordinateUser) {
			return Tester.CreateBool("Users_ManagesUser(" + allowDeletedSubordinateUser + ")", DAType.UserOrganizationId, DAType.UserOrganizationId, DAType.UserOrganizationId,
					(s, callerId, managerId, subordinateId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.Users.ManagesUser(s, perms, managerId, subordinateId, allowDeletedSubordinateUser);
					},
					(s, callerId, managerId, subordinateId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Users.ManagesUser(s, perms, managerId, subordinateId, allowDeletedSubordinateUser);
					});
		}


		public static Tester Users_GetNodesForUser() {
			return Tester.CreateAccountabilityNode("Users_GetNodesForUser", DAType.UserOrganizationId, DAType.UserOrganizationId,
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return Old.DeepAccessor.Users.GetNodesForUser(s, perms, userId);
					},
					(s, callerId, userId) => {
						var caller = s.Get<UserOrganizationModel>(callerId);
						var perms = PermissionsUtility.Create(s, caller);
						return DeepAccessor.Users.GetNodesForUser(s, perms, userId);
					});
		}



	}
}
