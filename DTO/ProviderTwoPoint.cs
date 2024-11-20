namespace RouteDemo.DTO
{
    public class ProviderTwoPoint
    {
        // Mandatory
        // Name of point, e.g. Moscow\Sochi
        public string Point { get; set; }

        // Mandatory
        // Date for point in Route, e.g. Point = Moscow, Date = 2023-01-01 15-00-00
        public DateTime Date { get; set; }
    }
}
