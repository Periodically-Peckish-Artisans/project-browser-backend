using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBrowser.Backend.Models {

    public class DocumentRef {
        public string DocType { get; set; } = string.Empty;
        public string DocId { get; set; } = string.Empty;
    }

    class DocCompare : IEqualityComparer<DocumentRef>
    {
      public bool Equals(DocumentRef x, DocumentRef y)
      {
          return x.DocType == y.DocType;
      }

      public int GetHashCode(DocumentRef obj)
      {
          unchecked
          {
              int hash = 17;
              hash = hash * 31 + obj.DocId.GetHashCode();
              hash = hash * 31 + obj.DocType.GetHashCode();
              return hash;
          }
      }
    }

    public class Profile : IDoc {
        public string Id { get; set; } = string.Empty;
        public List<string> ManagerIds { get; set; } = new List<string>();
        public bool IsArchived { get; set; } = false;
        public List<DocumentRef> OwnedDocs { get; set; } = new List<DocumentRef>();
        public List<string> SubscribedProjects { get; set; } = new List<string>();
        public int Version { get; set; } = Ver.Current;
        public string DocCreationDate { get; set; } = DateTime.UtcNow.ToString();

        public string GetDocType() => "profile";

        public bool Validate() {
            return !string.IsNullOrWhiteSpace(Id) &&
                ManagerIds != null &&
                ManagerIds.Count > 0 &&
                OwnedDocs != null &&
                !OwnedDocs.Any(e => e.DocId == null || e.DocType == null) &&
                SubscribedProjects != null &&
                DocCreationDate != null &&
                DateTime.TryParse(DocCreationDate, out DateTime dateResult2) &&
                Version == Ver.Current;
        }

        public bool Equivalent(object obj) {
            if (obj == null) {
                return false;
            }

            if (obj == this) {
                return true;
            }

            Profile o = obj as Profile;
            if (o == null) {
                return false;
            }

            return o.Id == Id &&
                (ManagerIds == o.ManagerIds || (o.ManagerIds?.SequenceEqual(ManagerIds) ?? false)) &&
                o.IsArchived == IsArchived &&
                (OwnedDocs == o.OwnedDocs || (o.OwnedDocs?.SequenceEqual(OwnedDocs, new DocCompare()) ?? false)) &&
                (SubscribedProjects == o.SubscribedProjects || (o.SubscribedProjects?.SequenceEqual(SubscribedProjects) ?? false)) &&
                o.Version == Version &&
                o.DocCreationDate == DocCreationDate;
        }
    }
}
