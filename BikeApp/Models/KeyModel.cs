using System.Collections.Generic;

namespace BikeApp.Models
{
    public class KeyModel
    {
        public string Key { get; set; }
        public List<Station> Stations { get; set; }
        public List<Station> AllStations { get; set; }
        public PaginationViewModel Pagination { get; set; }
    }
}
