using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Angular.Process;
using RadialReview.Models.Process;
using RadialReview.Models.Process.Execution;
using RadialReview.Models.Process.ViewModels;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class FolderVM {
		public FolderContentVM Folder { get; set; }
		public List<FolderContentVM> Contents { get; set; }
		public List<FolderNameParentVM> Path { get; set; }
		public List<FolderContentVM> Favorites { get; set; }
		public List<FolderContentVM> Recent { get; set; }

		public bool ShowFavorites { get; set; }
		public bool ShowRecent { get; set; }


	}

	public enum FolderContentType {
		Folder,
		Process
	}

	public class FolderContentVM {
		public FolderContentVM() { }
		public FolderContentVM(ProcessFolder f, bool isMainFolder, string parentFolderName, bool canAdmin) {
			Id = f.Id;
			CreateTime = f.CreateTime;
			Name = f.Name;
			ImageUrl = f.ImageUrl;
			Type = FolderContentType.Folder;
			IsMainFolder = isMainFolder;
			ParentFolderId = f.ParentFolderId;
			ParentfolderName = parentFolderName;
			CanAdmin = canAdmin;
		}

		public FolderContentVM(ProcessModel f, string parentFolderName, bool canAdmin) {
			Id = f.Id;
			CreateTime = f.CreateTime;
			Name = f.Name;
			ImageUrl = f.ImageUrl;
			Details = f.Description;
			Type = FolderContentType.Process;
			ParentFolderId = f.ProcessFolderId;
			ParentfolderName = parentFolderName;
			CanAdmin = canAdmin;

		}

		public long Id { get; set; }
		public string Name { get; set; }
		public string Details { get; set; }
		public string ImageUrl { get; set; }
		public FolderContentType Type { get; set; }
		public DateTime CreateTime { get; set; }
		public bool IsMainFolder { get; set; }
		public long? ParentFolderId { get; set; }
		public string ParentfolderName { get; set; }
		public bool CanAdmin { get; set; }

	}
	public class FolderNameParentVM {
		public string Name { get; set; }
		public long FolderId { get; set; }
		public long? ParentFolderId { get; set; }
	}


	public class ProcessAccessor : BaseAccessor {

		#region Folder

		public static async Task<FolderVM> CreateProcessFolder(UserOrganizationModel caller, long orgId, string name, long parentFolderId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					perms.AdminProcessFolder(parentFolderId);

					var folder = new ProcessFolder() {
						Name = name,
						ParentFolderId = parentFolderId,
						OrgId = orgId
					};

					s.Save(folder);
					var org = s.Get<OrganizationModel>(folder.OrgId);
					var teamId = OrganizationAccessor.GetAllMembersTeamId(s, perms, orgId);
					PermissionsAccessor.InitializePermItems_Unsafe(s, caller, PermItem.ResourceType.ProcessFolder, folder.Id, PermTiny.RGM(teamId), PermTiny.Creator(), PermTiny.Admins());


					var parentFolder = s.Get<ProcessFolder>(parentFolderId);

					var path = await GetPath(s, perms, folder.Id);

					tx.Commit();
					s.Flush();
					return new FolderVM() {
						Folder = new FolderContentVM(folder, false, parentFolder.Name, true),
						Contents = new List<FolderContentVM>(),
						Path = path
					};
				}
			}
		}

		public static async Task<List<FolderContentVM>> GetAllProcessFolders(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					var org = s.Get<OrganizationModel>(orgId);

					var folders = s.QueryOver<ProcessFolder>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List().ToList();
					var nameLookup = folders.ToDefaultDictionary(x => (long?)x.Id, x => x.Name, x => null);


					var output = folders.Where(x => perms.IsPermitted(y => y.ViewProcessFolder(x.Id)))
						.Select(x => new FolderContentVM(x, x.Id == org.ProcessMainFolderId, nameLookup[x.ParentFolderId], false))
						.ToList();
					return output;
				}
			}
		}

		public async static Task<long> GetMainFolder(UserOrganizationModel caller, long orgId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					var org = s.Get<OrganizationModel>(orgId);
					if (org.ProcessMainFolderId == null || org.ProcessMainFolderId <= 0) {
						var folder = new ProcessFolder() {
							Name = "Process",
							OrgId = orgId,
							ParentFolderId = null,
							Root = true,
						};
						s.Save(folder);
						org.ProcessMainFolderId = folder.Id;


						var teamId = OrganizationAccessor.GetAllMembersTeamId(s, perms, orgId);
						PermissionsAccessor.InitializePermItems_Unsafe(s, caller, PermItem.ResourceType.ProcessFolder, folder.Id, PermTiny.RGM(teamId, true, true, false), PermTiny.Admins());


						s.Update(org);
						tx.Commit();
						s.Flush();
					}
					return org.ProcessMainFolderId.Value;

				}
			}
		}

		public static async Task<bool> MarkFavorite(UserOrganizationModel caller, long userId, long processId, bool favorite) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					perms.ViewProcess(processId);

					var found = s.QueryOver<ProcessFavorite>()
						.Where(x => x.DeleteTime == null && x.ProcessId == processId && x.ForUser == userId)
						.List().ToList();

					var wasupdated = false;
					if (favorite) {
						if (!found.Any()) {
							var f = new ProcessFavorite() {
								ForUser = userId,
								ProcessId = processId
							};
							s.Save(f);

							wasupdated = true;
						}
					} else {
						var now = DateTime.UtcNow;
						foreach (var f in found) {
							f.DeleteTime = now;
							s.Update(f);
							wasupdated = true;
						}
					}

					tx.Commit();
					s.Flush();
					return wasupdated;
				}
			}
		}

		public static async Task<FolderVM> GetFolderContents(UserOrganizationModel caller, long folderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewProcessFolder(folderId);
					var canAdmin = perms.IsPermitted(x => x.AdminProcessFolder(folderId));

					var folder = s.Get<ProcessFolder>(folderId);

					if (folder.DeleteTime != null)
						throw new PermissionsException("Folder was deleted.");

					var orgFolderId = s.Get<OrganizationModel>(folder.OrgId).ProcessMainFolderId;


					var foldersQ = s.QueryOver<ProcessFolder>().Where(x => x.DeleteTime == null && x.ParentFolderId == folderId).Future();
					var filesQ = s.QueryOver<ProcessModel>().Where(x => x.DeleteTime == null && x.ProcessFolderId == folderId).Future();

					var folders = foldersQ.ToList();
					var files = filesQ.ToList();

					var contents = new List<FolderContentVM>();
					var parentFolderName = folder.ParentFolderId.HasValue ? s.Get<ProcessFolder>(folder.ParentFolderId.Value).Name : (string)null;

					foreach (var f in folders) {
						contents.Add(new FolderContentVM(f, orgFolderId == f.Id, folder.Name, false));
					}

					foreach (var f in files) {
						contents.Add(new FolderContentVM(f, folder.Name, false));
					}
					var path = await GetPath(s, perms, folder.Id);

					var isMain = orgFolderId == folderId;

					var result = new FolderVM {
						Contents = contents,
						Folder = new FolderContentVM(folder, isMain, parentFolderName, canAdmin),
						Path = path,
					};

					if (isMain) {

						//Favorites
						var favoriteIds = s.QueryOver<ProcessFavorite>()
							.Where(x => x.DeleteTime == null && x.ForUser == caller.Id)
							.Select(x => x.ProcessId)
							.List<long>().Distinct().ToArray();


						//Recent
						var recentIds = s.QueryOver<ProcessExecution>()
							.Where(x => x.ExecutedBy == caller.Id && x.CreateTime > DateTime.UtcNow.AddDays(-90))
							.OrderBy(x => x.CreateTime).Desc
							.Select(x => x.ProcessId)
							.List<long>().Distinct()
							.Take(8).ToArray();

						var allIds = recentIds.Union(favoriteIds).Distinct().ToArray();

						var allProcess = s.QueryOver<ProcessModel>()
							.Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id)
							.WhereRestrictionOn(x => x.Id).IsIn(allIds)
							.List()
							.ToList()
							.Where(x => perms.IsPermitted(y => y.ViewProcess(x.Id)))
							.Select(x => new FolderContentVM(x, folder.Name, false))
							.ToList();

						result.Favorites = allProcess.Where(x => favoriteIds.Contains(x.Id)).ToList();
						result.Recent = allProcess.Where(x => recentIds.Contains(x.Id)).ToList();

						result.ShowFavorites = true;
						result.ShowRecent = true;
					}



					return result;
				}
			}
		}

		public static async Task ForceConclude(UserOrganizationModel caller, List<long> executionIds) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var tested = new HashSet<long>();
					var now = DateTime.UtcNow;
					var any = false;
					var execModels = new List<ProcessExecution>();
					foreach (var executionId in executionIds) {
						var processExec = s.Get<ProcessExecution>(executionId);
						execModels.Add(processExec);
						if (!tested.Contains(processExec.ProcessId)) {
							perms.ViewProcess(processExec.ProcessId);
							tested.Add(processExec.ProcessId);
						}

						processExec.DeleteTime = now;
						s.Update(processExec);
						any = true;
					}

					if (any) {

						foreach (var e in execModels) {
							await HooksRegistry.Each<IProcessExecutionHook>((ss, x) => x.ProcessExecutionConclusionForced(ss, caller.Id, e));
						}

						tx.Commit();
						s.Flush();
					}


				}
			}
		}

		public static async Task<FolderContentVM> EditProcessFolder(UserOrganizationModel caller, long folderId, string name = null, long? parentFolder = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditProcessFolder(folderId);

					var folder = s.Get<ProcessFolder>(folderId);

					if (folder.DeleteTime != null)
						throw new PermissionsException("Folder was deleted.");


					if (name != null && folder.Name != name) {
						folder.Name = name;
					}

					if (parentFolder != null && folder.ParentFolderId != parentFolder) {
						perms.AdminProcessFolder(parentFolder.Value);
						if (folder.ParentFolderId.HasValue) {
							perms.AdminProcessFolder(folder.ParentFolderId.Value);
						} else {
							throw new PermissionsException("Cannot move the root folder");
						}
						folder.ParentFolderId = parentFolder.Value;
					}
					tx.Commit();
					s.Flush();
				}
			}
			return (await GetFolderContents(caller, folderId)).Folder;
		}
		public static async Task<long?> DeleteFolder(UserOrganizationModel caller, long folderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.AdminProcessFolder(folderId);
					var folder = s.Get<ProcessFolder>(folderId);
					var org = s.Get<OrganizationModel>(folder.OrgId);
					if (org.ProcessMainFolderId == folderId) {
						throw new PermissionsException("Cannot delete the top-level folder");
					}
					if (folder.DeleteTime != null) {
						throw new PermissionsException("Folder was already deleted.");
					}

					folder.DeleteTime = DateTime.UtcNow;
					s.Update(folder);

					tx.Commit();
					s.Flush();
					return folder.ParentFolderId;
				}
			}
		}

		[Obsolete("Unsafe")]
		private static FolderNameParentVM GetFolderName_Unsafe(ISession s, long folderId) {
			var folder = s.Get<ProcessFolder>(folderId);
			return new FolderNameParentVM {
				FolderId = folder.Id,
				Name = folder.Name,
				ParentFolderId = folder.ParentFolderId
			};
		}
		public static async Task<List<FolderNameParentVM>> GetPath(ISession s, PermissionsUtility perms, long folderId) {
			long? foId = folderId;
			if (foId != null) {
				perms.ViewProcessFolder(foId.Value);
			}

			var output = new List<FolderNameParentVM>();
			while (foId != null) {
				var o = GetFolderName_Unsafe(s, foId.Value);
				output.Insert(0, o);
				foId = o.ParentFolderId;
			}
			return output;
		}

		#endregion
		#region Process
		public static async Task<long> CreateProcess(UserOrganizationModel caller, long orgId, long folderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					perms.CreateProcessUnderProcessFolder(folderId);

					var process = new ProcessModel() {
						CreatorId = caller.Id,
						OrgId = orgId,
						ProcessFolderId = folderId,
					};

					s.Save(process);
					var teamId = OrganizationAccessor.GetAllMembersTeamId(s, perms, orgId);
					PermissionsAccessor.InitializePermItems_Unsafe(s, caller, PermItem.ResourceType.Process, process.Id, PermTiny.RGM(teamId), PermTiny.Creator(), PermTiny.Admins());


					tx.Commit();
					s.Flush();
					await HooksRegistry.Each<IProcessHook>((ss, x) => x.CreateProcess(ss, process));

					return process.Id;
				}
			}
		}
		public async static Task<ProcessVM> GetProcess(UserOrganizationModel caller, long processId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewProcess(processId);
					var p = s.Get<ProcessModel>(processId);
					p._Editable = perms.IsPermitted(x => x.EditProcess(processId));

					var isFavorite = IsFavorite_Unsafe(s, caller.Id, processId);

					var res = ProcessVM.CreateFromProcess(p, isFavorite);

					var stepsList = s.QueryOver<ProcessStep>()
										.Where(x => x.DeleteTime == null && x.ProcessId == processId)
										.List().ToList();
					var steps = stepsList.GroupBy(x => x.ParentStepId)
										.ToDefaultDictionary(
											x => x.Key ?? -1,
											x => x.OrderBy(y => y.Ordering).Select(z => new ProcessStepVM(z)).ToList(),
											x => new List<ProcessStepVM>()
										);


					var allZaps = GetStepsWithZaps(s, p.OrgId, stepsList.Select(x => x.Id).ToArray());



					var levelSteps = steps[-1];
					res.Substeps = levelSteps;
					foreach (var step in res.Substeps) {
						divePopulateSubsteps(step, steps, ModifiersFunc(allZaps));
					}

					return res;
				}
			}
		}
		public static async Task<ProcessVM> EditProcess(UserOrganizationModel caller, long processId, string name = null, string description = null, long? folderId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditProcess(processId);

					var p = s.Get<ProcessModel>(processId);
					var updates = new IProcessHookUpdates();

					if (name != null && p.Name != name) {
						p.Name = name;
						updates.NameChanged = true;
					}

					if (p.Description != description) {
						p.Description = description;
						updates.DescriptionChanged = true;
					}

					if (folderId != null && p.ProcessFolderId != folderId) {
						perms.AdminProcessFolder(p.ProcessFolderId);
						perms.AdminProcessFolder(folderId.Value);
						p.ProcessFolderId = folderId.Value;
						updates.FolderChanged = true;
					}

					if (updates.AnyUpdates()) {
						p.LastEdit = DateTime.UtcNow;
					}

					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IProcessHook>((ss, x) => x.UpdateProcess(ss, p, updates));

					bool isFavorite = IsFavorite_Unsafe(s, caller.Id, processId);
					return ProcessVM.CreateFromProcess(p, isFavorite);
				}
			}
		}

		public static bool IsFavorite_Unsafe(ISession s, long callerId, long processId) {
			return s.QueryOver<ProcessFavorite>().Where(x => x.DeleteTime == null && x.ForUser == callerId && x.ProcessId == processId).Take(1).RowCount() > 0;
		}

		public static async Task<long> DeleteProcess(UserOrganizationModel caller, long processId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditProcess(processId);
					var process = s.Get<ProcessModel>(processId);
					process.DeleteTime = DateTime.UtcNow;
					s.Update(process);

					tx.Commit();
					s.Flush();
					return process.ProcessFolderId;
				}
			}
		}
		private static void divePopulateSubsteps(ProcessStepVM step, DefaultDictionary<long, List<ProcessStepVM>> steps, Action<long, HashSet<string>> modifiers) {

			if (modifiers != null) {
				var hs = new HashSet<string>();
				modifiers(step.Id, hs);
				step.Modifiers.AddRange(hs);
			}

			step.Substeps = steps[step.Id];
			foreach (var s in step.Substeps) {
				divePopulateSubsteps(s, steps, modifiers);
			}
		}

		#endregion
		#region Step
		public async static Task<ProcessStep> AppendStep(UserOrganizationModel caller, long processId, long? parentStepId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditProcess(processId);

					var process = s.Get<ProcessModel>(processId);

					if (parentStepId != null) {
						var parent = s.Get<ProcessStep>(parentStepId);
						if (parent.ProcessId != processId) {
							throw new PermissionsException("Parent Step does not match the process.");
						}
					}

					var order = s.QueryOver<ProcessStep>().Where(x => x.DeleteTime == null && x.ProcessId == processId && x.ParentStepId == parentStepId).RowCount();

					var step = new ProcessStep() {
						ProcessId = processId,
						ParentStepId = parentStepId,
						Ordering = order,
						OrgId = process.OrgId,
					};

					s.Save(step);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IProcessHook>((ss, x) => x.UpdateProcess(ss, process, new IProcessHookUpdates() {
						StepAltered = true,
						StepUpdates = new IProcessHookUpdates_StepUpdate() {
							Kind = StepUpdateKind.AppendStep,
							ForStepId = step.Id,
						}
					}));

					return step;
				}
			}
		}
		public static async Task<ProcessStep> EditStep(UserOrganizationModel caller, long stepId, string name = null, string details = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var step = s.Get<ProcessStep>(stepId);
					perms.EditProcess(step.ProcessId);

					var process = s.Get<ProcessModel>(step.ProcessId);

					var updates = new IProcessHookUpdates_StepUpdate() {
						Kind = StepUpdateKind.EditStep,
						ForStepId = step.Id,
					};

					if (step.Name != name) {
						step.Name = name;
						updates.NameChanged = true;
					}
					if (step.Details != details) {
						step.Details = details;
						updates.DetailsChanged = true;
					}

					s.Update(step);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IProcessHook>((ss, x) => x.UpdateProcess(ss, process, new IProcessHookUpdates() {
						StepAltered = true,
						StepUpdates = updates
					}));

					return step;
				}
			}
		}
		public static async Task ReorderStep(UserOrganizationModel caller, long stepId, long? newParentStepId, int newIndex) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var step = s.Get<ProcessStep>(stepId);
					var processId = step.ProcessId;
					perms.EditProcess(processId);

					var process = s.Get<ProcessModel>(processId);


					ProcessStep newParentStep = null;
					if (newParentStepId != null) {
						newParentStep = s.Get<ProcessStep>(newParentStepId);
						if (newParentStep.ProcessId != processId)
							throw new PermissionsException("Not the same process.");
					}

					var oldParentStepId = step.ParentStepId;
					var oldIndex = step.Ordering;

					if (oldParentStepId == newParentStepId) {
						//Parents are the same.. just reorder
						var existing = s.QueryOver<ProcessStep>().Where(x => x.DeleteTime == null && x.ProcessId == processId && x.ParentStepId == oldParentStepId).List().ToList();
						Reordering.CreateRecurrence<ProcessStep>(existing, stepId, null, oldIndex, newIndex, x => x.Ordering, x => x.Id)
							.ApplyReorder(s);
					} else {
						//Remove from one, add to the other.. update order
						step.ParentStepId = newParentStepId;
						step.Ordering = int.MaxValue;
						s.Update(step);

						var oldExisting = s.QueryOver<ProcessStep>().Where(x => x.DeleteTime == null && x.ProcessId == processId && x.ParentStepId == oldParentStepId).List().ToList();
						foreach (var o in oldExisting.Where(x => x.Ordering > oldIndex)) {
							o.Ordering -= 1;
							s.Update(o);
						}

						var newExisting = s.QueryOver<ProcessStep>().Where(x => x.DeleteTime == null && x.ProcessId == processId && x.ParentStepId == newParentStepId).List().ToList();
						Reordering.CreateRecurrence<ProcessStep>(newExisting, stepId, null, newExisting.Count - 1, newIndex, x => x.Ordering, x => x.Id)
							.ApplyReorder(s);
					}

					tx.Commit();
					s.Flush();


					await HooksRegistry.Each<IProcessHook>((ss, x) => x.UpdateProcess(ss, process, new IProcessHookUpdates() {
						StepAltered = true,
						StepUpdates = new IProcessHookUpdates_StepUpdate() {
							Kind = StepUpdateKind.ReorderStep,
							ForStepId = step.Id,
							OldStepIndex = oldIndex,
							NewStepIndex = newIndex,
							OldStepParent = oldParentStepId,
							NewStepParent = newParentStepId
						}
					}));
				}
			}
		}
		public static async Task DeleteStep(UserOrganizationModel caller, long stepId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var step = s.Get<ProcessStep>(stepId);
					perms.EditProcess(step.ProcessId);

					var process = s.Get<ProcessModel>(step.ProcessId);
					step.DeleteTime = DateTime.UtcNow;

					s.Update(step);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IProcessHook>((ss, x) => x.UpdateProcess(ss, process, new IProcessHookUpdates() {
						StepAltered = true,
						StepUpdates = new IProcessHookUpdates_StepUpdate() {
							Kind = StepUpdateKind.RemoveStep,
							ForStepId = step.Id,
						}
					}));
				}
			}
		}

		#endregion
		#region Execution

		public async static Task<ProcessVM> StartProcessExecution(UserOrganizationModel caller, long processId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewProcess(processId);

					var process = s.Get<ProcessModel>(processId);
					var steps = s.QueryOver<ProcessStep>().Where(x => x.DeleteTime == null && x.ProcessId == processId).List().ToList();

					var startTime = DateTime.UtcNow;
					var exec = new ProcessExecution() {
						CompleteTime = steps.Any() ? null : (DateTime?)startTime,
						DeleteTime = null,
						CreateTime = startTime,
						Description = process.Description,
						ExecutedBy = caller.Id,
						Name = process.Name,
						OrgId = process.OrgId,
						ProcessId = process.Id,
						ProcessVersion = process.LastEdit,
						TotalSteps = steps.Count,
						CompletedSteps = 0,

					};
					s.Save(exec);
					var stepToExecStep = new DefaultDictionary<long?, long?>(x => null);
					var execStepToParentStepId = new DefaultDictionary<long?, long?>(x => null);
					var execSteps = new List<ProcessExecutionStep>();
					foreach (var step in steps) {
						var execStep = new ProcessExecutionStep() {
							CompleteTime = null,
							CreateTime = startTime,
							Details = step.Details,
							Name = step.Name,
							Ordering = step.Ordering,
							//ParentId set later
							ProcessExecutionId = exec.Id,
							StepId = step.Id,
							OrgId = process.OrgId,
							ProcessId = step.ProcessId,
						};
						s.Save(execStep);
						stepToExecStep[step.Id] = execStep.Id;
						execStepToParentStepId[execStep.Id] = step.ParentStepId;
						execSteps.Add(execStep);
					}

					foreach (var es in execSteps) {
						es.ParentExectutionStepId = stepToExecStep[execStepToParentStepId[es.Id]];
						s.Update(es);
					}

					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IProcessExecutionHook>((ss, x) => x.ProcessExecutionStarted(ss, caller.Id, exec));


					var allZaps = GetStepsWithZaps(s, process.OrgId, steps.Select(x => x.Id).ToArray());
					var isFavorite = IsFavorite_Unsafe(s, caller.Id, processId);

					return ProcessVM.CreateFromProcessExecution(process, exec, execSteps, isFavorite, ModifiersFunc(allZaps));
				}
			}
		}


		public async static Task<ProcessVM> GetProcessExecution(UserOrganizationModel caller, long processExecutionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var processExec = s.Get<ProcessExecution>(processExecutionId);
					perms.ViewProcess(processExec.ProcessId);
					var process = s.Get<ProcessModel>(processExec.ProcessId);
					var execSteps = s.QueryOver<ProcessExecutionStep>().Where(x => x.DeleteTime == null && x.ProcessExecutionId == processExecutionId).List().ToList();

					var stepsList = execSteps.Select(x => x.StepId).ToArray();
					var allZaps = GetStepsWithZaps(s, process.OrgId, stepsList);

					var isFavorite = IsFavorite_Unsafe(s, caller.Id, process.Id);

					return ProcessVM.CreateFromProcessExecution(process, processExec, execSteps, isFavorite, ModifiersFunc(allZaps));
				}
			}
		}

		private static HashSet<long> GetStepsWithZaps(ISession s, long orgId, long[] allStepIds) {
			var steps = s.QueryOver<ZapierSubscription>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null && x.Event == ZapierEvents.complete_process_step)
						.WhereRestrictionOn(x => x.FilterOnItemId).IsIn(allStepIds)
						.Select(x => x.FilterOnItemId)
						.List<long>()
						.Distinct()
						.ToArray();

			var o = new HashSet<long>();
			foreach (var step in steps) {
				o.Add(step);
			}
			return o;
		}

		public async static Task MarkStepCompletion(UserOrganizationModel caller, long executionStepId, bool complete) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var processExecStep = s.Get<ProcessExecutionStep>(executionStepId);
					perms.ViewProcess(processExecStep.ProcessId);
					var processExec = s.Get<ProcessExecution>(processExecStep.ProcessExecutionId);
					bool updateProcessExec = false;

					var now = (DateTime?)DateTime.UtcNow;

					if (processExecStep.CompleteTime == null && complete) {
						processExec.CompletedSteps += 1;
						processExec.LastModified = now.Value;
						updateProcessExec = true;
					}

					if (processExecStep.CompleteTime != null && !complete) {
						processExec.CompletedSteps -= 1;
						processExec.LastModified = now.Value;
						updateProcessExec = true;
					}


					processExecStep.CompleteTime = complete ? now : null;



					s.Update(processExecStep);

					var completions = s.QueryOver<ProcessExecutionStep>()
										.Where(x => x.DeleteTime == null && x.ProcessExecutionId == processExecStep.ProcessExecutionId)
										.Select(x => x.CompleteTime)
										.List<DateTime?>()
										.ToList();


					await HooksRegistry.Each<IProcessExecutionStepHook>((ss, x) => x.CompleteStep(ss, complete, caller.Id, processExec, processExecStep));

					if (completions.All(x => x.HasValue)) {
						if (processExec.CompleteTime == null) {
							processExec.CompleteTime = now;
							updateProcessExec = true;

							await HooksRegistry.Each<IProcessExecutionHook>((ss, x) => x.ProcessExecutionCompleted(ss, true, caller.Id, processExec));
						}

					} else {
						if (processExec.CompleteTime != null) {
							processExec.CompleteTime = null;
							updateProcessExec = true;

							await HooksRegistry.Each<IProcessExecutionHook>((ss, x) => x.ProcessExecutionCompleted(ss, false, caller.Id, processExec));

						}
					}

					if (updateProcessExec) {
						s.Update(processExec);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task<List<ProcessVM>> GetAllProcesses(UserOrganizationModel caller, long orgId, long? folderId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);

					var processesQ = s.QueryOver<ProcessModel>().Where(x => x.DeleteTime == null && x.OrgId == orgId);
					if (folderId != null) {
						processesQ = processesQ.Where(x => x.ProcessFolderId == folderId);
					}

					var processes = processesQ.List().Where(x => perms.IsPermitted(y => y.ViewProcess(x.Id))).ToList();

					/*Incorrect but faster to calculate*/

					var favoriteIds = s.QueryOver<ProcessFavorite>()
						.Where(x => x.DeleteTime == null && x.ForUser == caller.Id)
						.Select(x => x.ProcessId).List<long>()
						.ToList();


					return processes.Select(x => ProcessVM.CreateFromProcess(x, favoriteIds.Contains(x.Id))).ToList();
				}
			}
		}


		public static async Task<List<AngularProcessAndExecution>> GetActiveProcessExecutionsForUser(UserOrganizationModel caller, long userId, bool activeOnly = true, bool includeRecent = true, bool includeFavorite = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewUserOrganization(userId, false);

					var exec = s.QueryOver<ProcessExecution>()
						.Where(x => x.CompleteTime == null && x.ExecutedBy == userId && x.DeleteTime == null)
						.List()
						.Select(x => new AngularProcessExecution(x))
						.ToList();

					if (activeOnly == false)
						throw new NotImplementedException("activeOnly=false not implemented");

					var processIds = exec.Where(x => x.ProcessId.HasValue).Select(x => x.ProcessId.Value).ToList();

					if (includeRecent) {
						var recentIds = s.QueryOver<ProcessExecution>()
											.Where(x => x.ExecutedBy == userId && x.CreateTime > DateTime.UtcNow.AddDays(-90))
											.OrderBy(x => x.CreateTime).Desc
											.Select(x => x.ProcessId)
											.List<long>()
											.Distinct()
											.Take(8).ToArray();
						processIds.AddRange(recentIds);
					}


					var favoriteIds = s.QueryOver<ProcessFavorite>()
										.Where(x => x.ForUser == userId && x.DeleteTime == null)
										.Select(x => x.ProcessId)
										.List<long>()
										.Distinct()
										.Take(8).ToArray();
					if (includeFavorite) {
						processIds.AddRange(favoriteIds);
					}

					processIds = processIds.Distinct().ToList();
					var processes = s.QueryOver<ProcessModel>()
										.Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id)
										.WhereRestrictionOn(x => x.Id).IsIn(processIds)
										.List().ToList();

					var output = new List<AngularProcessAndExecution>();
					foreach (var a in processes) {
						if (perms.IsPermitted(x => x.ViewProcess(a.Id))) {
							output.Add(new AngularProcessAndExecution(a.Id) {
								Process = new AngularProcess(a, favoriteIds.Contains(a.Id)),
								ProcessExecutions = exec.Where(x => x.ProcessId == a.Id).ToList()
							});
						}
					}



					return output;
				}
			}
		}
		private class Description {
			public string DateRangeString { get; set; }
			public string ForUserString { get; set; }
			public string ForProcessString { get; set; }

			public override string ToString() {
				var builder = new List<string>();
				if (ForUserString != null) {
					builder.Add("for " + ForUserString);
				}
				if (ForProcessString != null) {
					builder.Add("for " + ForProcessString);
				}
				if (DateRangeString != null) {
					builder.Add(DateRangeString);
				}
				return string.Join(", ", builder);
			}

		}

		public async static Task<AngularProcessList> GetProcessExecutionsForOrg(UserOrganizationModel caller, long orgId, DateTime now, long? filterOnUser = null, long? filterOnProcessId = null, DateRange completeRange = null, DateRange startRange = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					var description = new Description();


					var execQ = s.QueryOver<ProcessExecution>().Where(x => x.OrgId == orgId && x.DeleteTime == null);
					if (completeRange == null) {
						execQ = execQ.Where(x => x.CompleteTime == null);
						description.DateRangeString = "outstanding only";
					} else {
						var start = completeRange.StartTime;
						var end = completeRange.EndTime;

						if (now <= end) {
							execQ = execQ.Where(x => x.CompleteTime == null || (x.CompleteTime >= start && x.CompleteTime <= end));
						} else {
							execQ = execQ.Where(x => (x.CompleteTime >= start && x.CompleteTime <= end));
						}
						description.DateRangeString = completeRange.ToFriendlyString(now, caller.GetTimeSettings());
					}

					if (startRange != null) {
						var start = startRange.StartTime;
						var end = startRange.EndTime;
						execQ = execQ.Where(x => x.CreateTime >= start && x.CreateTime <= end);
						description.DateRangeString = startRange.ToFriendlyString(now, caller.GetTimeSettings());
					}


					if (filterOnUser != null) {
						perms.ViewUserOrganization(filterOnUser.Value, false);
						execQ = execQ.Where(x => x.ExecutedBy == filterOnUser.Value);
						var user = s.Get<UserOrganizationModel>(filterOnUser.Value);
						description.ForUserString = user.GetName();
					}
					if (filterOnProcessId != null) {
						perms.ViewProcess(filterOnProcessId.Value);
						execQ = execQ.Where(x => x.ProcessId == filterOnProcessId.Value);
						var process = s.Get<ProcessModel>(filterOnProcessId.Value);
						description.ForProcessString = process.Name;
					}

					var exec = execQ.List().Select(x => new AngularProcessExecution(x)).ToList();

					//Populate Names
					var userList = exec.Select(x => x.Creator.NotNull(y => y.Id)).Distinct().ToArray();
					var users = s.QueryOver<UserLookup>()
									.Where(x => x.DeleteTime == null)
									.WhereRestrictionOn(x => x.UserId).IsIn(userList)
									.List()
									.Where(x => perms.IsPermitted(y => y.ViewUserOrganization(x.UserId, false)))
									.ToDefaultDictionary(x => x.UserId, x => x.Name, x => "unknown");

					foreach (var e in exec) {
						if (e.Creator != null) {
							e.Creator.Name = users[e.Creator.Id];
						}
					}

					var processIds = exec.Select(x => x.ProcessId).Distinct().ToList();
					var processes = s.QueryOver<ProcessModel>()
										.WhereRestrictionOn(x => x.Id).IsIn(processIds)
										.List().ToList();

					bool anyInvisible = false;
					var output = new List<AngularProcessAndExecution>();
					foreach (var a in processes) {
						if (perms.IsPermitted(x => x.ViewProcess(a.Id))) {
							output.Add(new AngularProcessAndExecution(a.Id) {
								Process = new AngularProcess(a, null),
								ProcessExecutions = exec.Where(x => x.ProcessId == a.Id).ToList()
							});
							;
						} else {
							anyInvisible = true;
						}
					}


					return new AngularProcessList() {
						Description = description.ToString(),
						ProcessList = output
					};
				}
			}
		}

		#endregion


		#region Helpers

		private static Action<long, HashSet<string>> ModifiersFunc(HashSet<long> allZaps) {
			return (id, map) => {
				map.AddIf("has-zapier", allZaps.Contains(id));
			};
		}

		#endregion
	}
}
