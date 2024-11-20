namespace RouteDemo.DTO
{
    public class SearchRequest
    {
        // Mandatory
        // Start point of route, e.g. Moscow 
        public string Origin { get; set; }

        // Mandatory
        // End point of route, e.g. Sochi
        public string Destination { get; set; }

        // Mandatory
        // Start date of route
        public DateTime OriginDateTime { get; set; }

        // Optional
        public SearchFilters? Filters { get; set; }
    }
}
