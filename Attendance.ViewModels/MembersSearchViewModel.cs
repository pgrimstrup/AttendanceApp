using System;
using System.Collections.Generic;
using System.Text;
using Attendance.Data;

namespace Attendance.ViewModels;

public class MembersSearchViewModel
{
    public List<MemberRecord> AllMembers { get; } = new();

    public string FilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of members that match the current filter criteria.
    /// </summary>
    /// <remarks>The returned collection includes only those members whose combined identifying fields contain
    /// the filter text, using a case-insensitive comparison. Filtering is applied only when the filter text is at least
    /// three characters long; otherwise, the collection is empty.</remarks>
    public IEnumerable<MemberRecord> FilteredMembers
    {
        get
        {
            if (FilterText.Length >= 3)
            {
                foreach (MemberRecord member in AllMembers)
                {
                    string text = $"{member.PersonId}|{member.SportyCardNumber}|{member.FirstName}|{member.LastName}|{member.FALNumber}|{member.PNZNumber}|{member.MobileNumber}|{member.EmailAddress}|{member.EntraPersonId}|{member.EntraCardNumber}|{member.EntraCardUserName}";
                    if (String.IsNullOrWhiteSpace(FilterText) || text.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return member;
                    }
                }
            }
        }
    }

    public MembersSearchViewModel(MemberRecord[]? members = null)
    {
        if (members != null)
        {
            AllMembers.AddRange(members
                .OrderBy(m => m.LastName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.FirstName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.PersonId.GetValueOrDefault(m.EntraPersonId.GetValueOrDefault())));
        }
    }
}
