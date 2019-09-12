using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrowser.Backend.Models {
    public class Project {
        public string ProjectName { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscordInvite { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public List<string> ProjectManagerIds { get; set; } = new List<string>();
        public List<string> ProjectLinks { get; set; } = new List<string>();
        public List<string> ProjectEventIds { get; set; } = new List<string>();

        public bool Validate() {
            return !string.IsNullOrWhiteSpace(ProjectName) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Guid.TryParse(Id, out Guid guidResult) &&
                Purpose != null &&
                Description != null &&
                DiscordInvite != null &&
                ExpirationDate != null &&
                DateTime.TryParse(ExpirationDate, out DateTime dateResult) &&
                ProjectManagerIds != null &&
                ProjectManagerIds.Count > 0 &&
                ProjectLinks != null &&
                ProjectEventIds != null;
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
                o.Purpose == Purpose &&
                o.Description == Description &&
                o.DiscordInvite == DiscordInvite &&
                o.ExpirationDate == ExpirationDate &&
                (ProjectManagerIds == o.ProjectManagerIds || (o.ProjectManagerIds?.SequenceEqual(ProjectManagerIds) ?? false)) &&
                (ProjectLinks == o.ProjectLinks || (o.ProjectLinks?.SequenceEqual(ProjectLinks) ?? false)) &&
                (ProjectManagerIds == o.ProjectEventIds || (o.ProjectEventIds?.SequenceEqual(ProjectEventIds) ?? false));
        }
  }
}
