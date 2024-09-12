using System;
using System.Collections.Generic;
using RadialReview.Utilities.DataTypes;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RadialReview.Models.Reviews
{
    public class CustomizeSelector
    {
        public String Name { get; set; }
        public String UniqueId { get; set; }
        public List<WhoReviewsWho> Pairs { get; set; }
    }

    public class CustomizeModel
    {
        public List<Reviewer> Reviewers { get; set; }
        public List<Reviewee> AllReviewees { get; set; }
        public List<CustomizeSelector> Selectors { get; set; }
        public List<WhoReviewsWho> Selected { get; set; }
        public bool IsCustom { get; set; }
        public List<SelectListItem> Periods { get; set; }
        public List<long> MasterList { get; internal set; }
        public DefaultDictionary<string, ReviewerRevieweeInfo> Lookup { get; set; }

        public class ReviewerRevieweeInfo
        {
            public bool Selected { get; set; }
            public string Classes { get; set; }
        }
    }
}