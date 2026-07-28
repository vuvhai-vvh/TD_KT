namespace TD_KT.ViewModels
{
    public class FilterOption
    {
        public int Value { get; set; }
        public string DisplayText { get; set; } = "";
    }

    public class OrgUnitFilterOption
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsAll { get; set; }

        public string DisplayName => IsAll ? "Tất cả" : Name;
    }
}