using System.Collections.Generic;

namespace RadialReview.Api {
    public class PermissionObject
    {
        public string Name { get; set; }
        public bool CanCreate { get; set; }

        public PermissionObject()
        {
            this.Name = "";
            this.CanCreate = true;
        }
    }
}