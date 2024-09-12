using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.ViewModels
{
    public class WebHookViewModel
    {
        public string Id { get; set; }

        [Required]
        [DisplayName("WebHookUri")]
        public Uri WebHookUri { get; set; }

        public string Description { get; set; }

        public List<SelectListItem> Events { get; set; }

        public string Eventnames { get; set; }

        public List<string> selected { get; set; }
    }

    public class WebHookEventsViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
