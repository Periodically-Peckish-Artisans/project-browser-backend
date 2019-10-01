using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ProjectBrowser.Backend.Models {

    /// <summary>
    /// I'm restricting these quite a bit. My reasoning is:
    /// - Daily or bi/tri-weekly is too much! People probably would not regularly attend, and it clutters up the calendar.
    /// - Bi-monthly / every other month is too difficult to explain to someone or input onto the calendar with precision. For our use case,
    /// it's better to place odd meeting times as a series of 'Once' events, so they can be adjusted on a whim (when most can attend).
    /// </summary>
    public enum EventRecurrenceMode {
        /// <summary>
        /// Event happens only once ever, on the Start Date Time.true Recommended use for most events since otherwise is quite the commitment!
        /// </summary>
        Once,

        /// <summary>
        /// Event happens every week, on the day of Start Date Time
        /// </summary>
        Weekly,

        /// <summary>
        /// Event happens once per month on the day of week of the Start Date Time.
        /// The event can occur on the first, second, third, or fourth week of the month, determined also by the Start Date Time.
        /// It is easy to describe to somebody that you meet on 'the first saturday of the month' this way
        /// </summary>
        OncePerMonth
    }

    public class PublicEvent : IDoc {
        public string EventTitle { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
        public bool IsArchived { get; set; } = false;
        public string EventLocation { get; set; }
        public string StartDateTime { get; set; }
        public string ProjectId { get; set; }
        public int Version { get; set; } = Ver.Current;
        public string DocCreationDate { get; set; } = DateTime.UtcNow.ToString();

        [JsonConverter(typeof(StringEnumConverter))]
        public EventRecurrenceMode RecurrenceMode { get; set; } = EventRecurrenceMode.Once;

        /// <summary>
        /// This will send a message to an advertising committee to consider
        /// Advertising the event on Facebook / Meetup, etc.
        /// </summary>
        public bool AdvertiseEvent { get; set; } = false;

        public List<string> ManagerIds { get; set; } = new List<string>();

        public string GetDocType() => "event";

        public bool Equivalent(object obj)
        {
            if (obj == null) {
                return false;
            }

            if (obj == this) {
                return true;
            }

            PublicEvent o = obj as PublicEvent;
            if (o == null) {
                return false;
            }

            return o.EventTitle == EventTitle &&
                o.Id == Id &&
                o.Description == Description &&
                o.IsArchived == IsArchived &&
                o.EventLocation == EventLocation &&
                o.StartDateTime == StartDateTime &&
                o.ProjectId == ProjectId &&
                (ManagerIds == o.ManagerIds || (o.ManagerIds?.SequenceEqual(ManagerIds) ?? false)) &&
                o.Version == Version &&
                o.DocCreationDate == DocCreationDate;
        }

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(EventTitle) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Guid.TryParse(Id, out Guid guidResult) &&
                Description != null &&
                EventLocation != null &&
                StartDateTime != null &&
                DateTime.TryParse(StartDateTime, out DateTime dateResult) &&
                ManagerIds != null &&
                ManagerIds.Count > 0 &&
                DocCreationDate != null &&
                DateTime.TryParse(DocCreationDate, out DateTime dateResult2) &&
                Version == Ver.Current;
        }
    }
}