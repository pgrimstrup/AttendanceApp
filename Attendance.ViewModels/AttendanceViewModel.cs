using System;
using System.Collections.Generic;
using System.Text;
using Attendance.Data;
using Microsoft.Identity.Client;

namespace Attendance.ViewModels
{
    public class AttendanceViewModel
    {
        readonly AttendanceRecord _record;

        public AttendanceViewModel(AttendanceRecord record)
        {
            _record = record;
        }

        public string EventTitle
        {
            get
            {
                if (_record.IsAwayEvent)
                    return _record.EventDate.ToString("dddd, d MMMM") + " (Away)";
                return _record.EventDate.ToString("dddd, d MMMM");
            }
        }

        public DateTime EventDate => _record.EventDate;
        public string EntryTime => _record.EntryTime.HasValue ? _record.EntryTime.Value.ToString("hh:mm tt") : "n/a";
        public string RangeTime => _record.RangeTime.HasValue ? _record.RangeTime.Value.ToString("hh:mm tt") : "";
        public string ExitTime => _record.ExitTime.HasValue ? _record.ExitTime.Value.ToString("hh:mm tt") : "n/a";

        public bool IsMatchDay => _record.IsMatchDay;
        public bool IsAwayEvent => _record.IsAwayEvent;

        public bool IsCounted
        {
            get
            {
                if (_record.IsMatchDay && _record.IsPistolSection)
                {
                    if (_record.IsIncluded)
                        return true;

                    if (_record.IsExcluded)
                        return false;

                    return _record.RangeTime.HasValue;
                }
                return false;
            }
        }


        public string AwayEventName => $"{_record.AwayEventName} - {_record.AwayEventLocation}".Trim(' ', '-');

        public string Name => $"{_record.FirstName} {_record.LastName.Substring(0, 1)}";
        public string CardNumber => _record.SwipedCardNumber;
        public int PersonId => _record.PersonId;

        public string TimeOnRange
        {
            get
            {
                if (_record.EntryTime.HasValue && _record.ExitTime.HasValue)
                {
                    var span = _record.ExitTime.Value - _record.EntryTime.Value;
                    return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
                }
                else if (_record.EntryTime.HasValue && _record.RangeTime.HasValue)
                {
                    var span = _record.RangeTime.Value - _record.EntryTime.Value;
                    return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
                }
                else if (_record.RangeTime.HasValue && _record.ExitTime.HasValue)
                {
                    var span = _record.ExitTime.Value - _record.RangeTime.Value;
                    return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
                }
                else if (_record.RangeTime.HasValue)
                {
                    return "0h 00m";
                }
                return "n/a";
            }
        }
    }
}
