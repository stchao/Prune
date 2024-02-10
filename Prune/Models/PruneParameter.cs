namespace Prune.Models
{
    internal class PruneParameter
    {
        public static readonly string Key = "PruneDirectories";
        public static readonly string DefaultKey = "Default";

        public string Path { get; set; } = string.Empty;

        public string FileNamePattern { get; set; } = string.Empty;

        public int KeepLast { get; set; } = -1;

        public int KeepHourly { get; set; } = -1;

        public int KeepDaily { get; set; } = -1;

        public int KeepWeekly { get; set; } = -1;

        public int KeepMonthly { get; set; } = -1;

        public int KeepYearly { get; set; } = -1;

        public void SetInvalidProperties(PruneParameter defaultParameter)
        {
            var properties = GetType().GetProperties();

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = (string)property.GetValue(this)!;
                    var defaultValue = (string)property.GetValue(defaultParameter)!;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        property.SetValue(this, defaultValue, null);
                    }
                }
                else if (property.PropertyType == typeof(int))
                {
                    var value = (int)property.GetValue(this)!;
                    var defaultValue = (int)property.GetValue(defaultParameter)!;

                    if (value < 0)
                    {
                        property.SetValue(this, defaultValue, null);
                    }
                }
            }
        }
    }
}
