using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Rocks;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Angular.Rocks
{
    public class AngularRockAndMilestones : BaseAngular
    {
        public AngularRockAndMilestones() { }
        public AngularRockAndMilestones(long id) : base(id) { }
        public AngularRockAndMilestones(RockModel rock, IEnumerable<Milestone> milestones) : base(rock.Id){
			Rock = new AngularRock(rock,false);
			Milestones = milestones.Select(x => new AngularMilestone(x)).ToList();
        }

		public AngularRock Rock { get; set; }
		public IEnumerable<AngularMilestone> Milestones { get; set; }

    }
}