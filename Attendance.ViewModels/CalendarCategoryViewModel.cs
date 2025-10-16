using System;
using System.Collections.Generic;
using System.Text;
using Attendance.Data;

namespace Attendance.ViewModels;

public enum CalendarEventType
{
    Other,
    PistolMatch,
    RangeClosed
}

public class CalendarCategoryViewModel
{
    readonly CalendarCategory _category;

    public CalendarCategoryViewModel(CalendarCategory category)
    {
        _category = category;
    }

    public CalendarCategory Entity => _category;

    public string Name => _category.Name;
    public string LastEvent => _category.LastEvent == DateOnly.MinValue ? "n/a" : _category.LastEvent.ToString("d");

    public CalendarEventType EventType
    {
        get
        {
            if(_category.IsPistolEvent)
                return CalendarEventType.PistolMatch;

            if(_category.IsClosedEvent)
                return CalendarEventType.RangeClosed;

            return CalendarEventType.Other;
        }
        set
        {
            if(value == CalendarEventType.PistolMatch)
            {
                _category.IsPistolEvent = true;
                _category.IsClosedEvent = false;
            }
            else if(value == CalendarEventType.RangeClosed)
            {
                _category.IsPistolEvent = false;
                _category.IsClosedEvent = true;
            }
            else
            {
                _category.IsPistolEvent = false;
                _category.IsClosedEvent = false;
            }
        }
    }
}
