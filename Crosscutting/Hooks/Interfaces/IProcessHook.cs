using NHibernate;
using RadialReview.Models.Process;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {

	public enum StepUpdateKind {
		AppendStep,
		EditStep,
		RemoveStep,
		ReorderStep
	}

	public class IProcessHookUpdates_StepUpdate {
		public StepUpdateKind Kind { get; set; }
		public long ForStepId { get; set; }

		public bool NameChanged { get; set; }
		public bool DetailsChanged { get; set; }


		public long? OldStepParent { get; set; }
		public long? NewStepParent { get; set; }
		public int? OldStepIndex { get; set; }
		public int? NewStepIndex { get; set; }

	}

	public class IProcessHookUpdates {
		public bool NameChanged { get; set; }
		public bool DescriptionChanged { get; set; }
		public bool ImageChanged { get; set; }
		public bool FolderChanged { get; set; }
		public bool OwnerChanged { get; set; }
		public bool StepAltered { get; set; }
		public IProcessHookUpdates_StepUpdate StepUpdates { get; set; }



		public bool AnyUpdates() {
			return NameChanged || DescriptionChanged || ImageChanged || FolderChanged || OwnerChanged || StepAltered;
		}

	}

	public interface IProcessHook : IHook {
		Task CreateProcess(ISession s, ProcessModel process);
		Task UpdateProcess(ISession s, ProcessModel process, IProcessHookUpdates updates);
	}
}
