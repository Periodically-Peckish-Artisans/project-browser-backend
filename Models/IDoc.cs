using System.Collections.Generic;

namespace ProjectBrowser.Backend.Models {
  interface IDoc {
    string Id { get; set; }
    List<string> ManagerIds { get; set; }
    int Version { get; set; }
    string DocCreationDate { get; set; }

    bool Equivalent(object obj);
    bool Validate();
    string GetDocType();
  }
}