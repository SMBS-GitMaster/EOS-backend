using System;

namespace RadialReview.Models.Interfaces
{
	public interface IHistorical : IDeletable
	{
		DateTime CreateTime { get; set; }
	}


}
