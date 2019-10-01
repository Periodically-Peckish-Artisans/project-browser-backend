using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrowser.Backend.Models {
    public class Project : IDoc {
        public string ProjectName { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public List<string> ManagerIds { get; set; } = new List<string>();
        public string ProjectWebsite { get; set; } = string.Empty;
        public string ProjectLocation { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;

        public bool Validate() {
            return !string.IsNullOrWhiteSpace(ProjectName) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Guid.TryParse(Id, out Guid guidResult) &&
                Description != null &&
                ExpirationDate != null &&
                DateTime.TryParse(ExpirationDate, out DateTime dateResult) &&
                ManagerIds != null &&
                ManagerIds.Count > 0 &&
                ProjectWebsite != null;
        }

        public bool Equivalent(object obj) {
            if (obj == null) {
                return false;
            }

            if (obj == this) {
                return true;
            }

            Project o = obj as Project;
            if (o == null) {
                return false;
            }

            return o.ProjectName == ProjectName &&
                o.Id == Id &&
                o.Description == Description &&
                o.ExpirationDate == ExpirationDate &&
                (ManagerIds == o.ManagerIds || (o.ManagerIds?.SequenceEqual(ManagerIds) ?? false)) &&
                o.ProjectWebsite == ProjectWebsite &&
                o.ProjectLocation == ProjectLocation &&
                o.IsArchived == IsArchived;
        }
  }
}
