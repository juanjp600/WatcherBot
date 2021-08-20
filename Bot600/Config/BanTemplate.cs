using Microsoft.Extensions.Configuration;

namespace Bot600.Config
{
    public record BanTemplate(string Template, string DefaultAppeal)
    {
        public static BanTemplate FromConfig(Config config)
        {
            IConfigurationSection? banSection = config.Configuration.GetSection("Ban");
            string template = string.Join("\n", banSection.GetSection("Template").Get<string[]>());
            string defaultAppeal = banSection.GetSection("DefaultAppeal").Get<string>();
            return new BanTemplate(template, defaultAppeal);
        }
    }
}
