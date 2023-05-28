using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace BikeApp.Models
{
    public class KeyModel
    {
        public string Key { get; set; }
        public List<Station> Stations { get; set; }
        public List<Station> AllStations { get; set; }
        public PaginationViewModel Pagination { get; set; }
        public string OrderBy { get; set; }
        public string SortOrder { get; set; }

        public string SortOrderFor(string column)
        {
            if (OrderBy == column)
            {
                return SortOrder == "asc" ? "desc" : "asc";
            }
            return "asc";
        }

    }
}
