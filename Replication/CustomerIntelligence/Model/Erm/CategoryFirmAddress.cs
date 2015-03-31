using NuClear.AdvancedSearch.Replication.Model;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Model.Erm
{
    public sealed class CategoryFirmAddress : IIdentifiable
    {
        public CategoryFirmAddress()
        {
            IsActive = true;
        }

        public long Id { get; set; }

        public long CategoryId { get; set; }

        public long FirmAddressId { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
    }
}