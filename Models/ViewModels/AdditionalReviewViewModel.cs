using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels
{
    public class AdditionalReviewViewModel
    {
        public long Id { get; set; }
        public string[] Users { get; set; }
        public long Page { get; set; }
        public List<SelectListItem> Possible { get; set; }
    }
}