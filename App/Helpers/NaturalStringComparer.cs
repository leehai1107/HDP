using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace App.Helpers
{
    /// <summary>
    /// Natural string comparer for sorting file names with numbers correctly
    /// (e.g., 1, 2, 10, 11 instead of 1, 10, 11, 2)
    /// </summary>
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var regex = new Regex(@"(\d+)|(\D+)");
            var xParts = regex.Matches(x);
            var yParts = regex.Matches(y);

            for (int i = 0; i < Math.Min(xParts.Count, yParts.Count); i++)
            {
                var xPart = xParts[i].Value;
                var yPart = yParts[i].Value;

                // If both parts are numbers, compare numerically
                if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
                {
                    int numCompare = xNum.CompareTo(yNum);
                    if (numCompare != 0) return numCompare;
                }
                else
                {
                    // Otherwise compare as strings (case-insensitive)
                    int strCompare = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
                    if (strCompare != 0) return strCompare;
                }
            }

            // If all parts are equal, compare lengths
            return xParts.Count.CompareTo(yParts.Count);
        }
    }
}
