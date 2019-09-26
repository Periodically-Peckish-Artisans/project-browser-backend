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

    public class PublicEvent {
        public string EventTitle { get; set; }
        public string Id { get; set; }
        public string EventDescription { get; set; }
        public bool IsArchived { get; set; } = false;
        public string EventLocation { get; set; }
        public string StartDateTime { get; set; }

        public EventRecurrenceMode RecurrenceMode { get; set; } = EventRecurrenceMode.Once;

        /// <summary>
        /// This will send a message to an advertising committee to consider
        /// Advertising the event on Facebook / Meetup, etc.
        /// </summary>
        public bool AdvertiseEvent { get; set; }
    }
}