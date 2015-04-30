using NuClear.AdvancedSearch.Replication.Model;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Model.Facts
{
    public sealed class CategoryFirmAddress : IIdentifiableObject
    {
        public long Id { get; set; }

        public long CategoryId { get; set; }

        public long FirmAddressId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CategoryFirmAddress && IdentifiableObjectEqualityComparer<CategoryFirmAddress>.Default.Equals(this, (CategoryFirmAddress)obj);
        }

        public override int GetHashCode()
        {
            return IdentifiableObjectEqualityComparer<CategoryFirmAddress>.Default.GetHashCode(this);
        }
    }
}