namespace Telecom.Models
{
    public class PackageDeal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool IsForIndividual { get; set; }
    }
}
