using System;
using System.Collections.Generic;
using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularAgendaItem : BaseAngular 
	{
		public AngularAgendaItem(long id,string name,string backendName) : base(id)
		{
			Duration = 5;
			Name = name;
			BackendName = backendName;
		}
		public string Name { get; set; }
		public string BackendName { get; set; }
		public decimal Duration { get; set; }
		public string TemplateUrl { get; set; }
		public decimal Ellapsed { get; set; }
		public DateTime PageStart { get; set; }
	}
	
	public class AngularAgendaItem_Rocks : AngularAgendaItem
	{
		public AngularAgendaItem_Rocks(long id, string name) : base(id,  name, "rocks") { }
		public List<AngularMeetingRock> Rocks { get; set; } 
	}
}