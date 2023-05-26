using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BikeApp.Models
{
    public class JourneyModel
    {
        [Column("Departure")]
        public DateTime Departure { get; set; }
        [Column("Return")]
        public DateTime Return { get; set; }

        [Column("Departure station id")]
        public int DepartureStationId { get; set; }

        [Column("Departure station name")]
        public string DepartureStationName { get; set; }

        [Column("Return station id")]
        public int ReturnStationId { get; set; }

        [Column("Return station name")]
        public string ReturnStationName { get; set; }

        [Column("Covered distance_m")]
        public int CoveredDistance_m { get; set; }

        [Column("Duration_sec")]
        public int Duration_sec { get; set; }
    }
}
