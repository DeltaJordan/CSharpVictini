using Discord;

namespace CSharpDewott.Extensions
{
    public static class SystemColorToDiscord
    {
        public static EmbedBuilder WithColor(this EmbedBuilder builder, System.Drawing.Color color)
        {
            return builder.WithColor(new Color(color.R, color.G, color.B));
        }
    }
}
