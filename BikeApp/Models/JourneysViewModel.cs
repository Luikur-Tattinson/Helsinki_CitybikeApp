namespace BikeApp.Models
{
    public class JourneysViewModel
    {
        public List<JourneyModel> Journeys { get; set; }
        public PaginationViewModel Pagination { get; set; }
        public string OrderBy { get; set; }
        public string SortOrder { get; set; }
        public int SelectedMonth { get; set; }

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
