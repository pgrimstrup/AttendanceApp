namespace Attendance.Data;

// Calendar Days can appear in one of three state
// HasSpecialEvent or HasRecurringEvent - checked and green background
// CancelEvents - unchecked and yellow background. This overrides HasSpecialEvent and HasRecurringEvent.
//    If the day is re-checked, then this flag is cleared and HasSpecialEvent or HasRecurringEvent takes 
//    precedence again and will show as green. This flag has no effect if HasSpecialEvent AND HasRecurringEvent
//    are both false, since there is no match anyway. It should not be possible to cancel events when there are none.

// When a Special Event is created, it flags the specific days on which the event is being held. This should
// make the day green, unless it's already been cancelled and is showing as yellow, in which case it stays yellow
// until the day is manually checked.

// When a Recurring Event is created, it flags all future days on which the event is being held (option to back-fill)
// Same as above, if a day is cancelled, then it will show as yellow instead of green until manually rechecked.

// When a Special Event or Recurring Event is deleted, the system has to calculate all future dates to determine whether 
// there are any other Special Events or Recurring Events on the same day. Past and current dates are never altered.

// The check boxes on all past and current dates are to be disabled.
// The system should ensure that all dates through to the end of the next calendar year have been populated. 
// This will ensure there is always at least 12 months of the calendar to view (maximum viewable date).
// The minimum viewable month is set to July 2025, which is the start date of this system.

public class CalendarDay
{
    public DateOnly Date { get; set; }
    public int SpecialEvents { get; set; }
    public int RecurringEvents { get; set; }
    public bool CancelEvents { get; set; }
    public string? Description { get; set; }
}
