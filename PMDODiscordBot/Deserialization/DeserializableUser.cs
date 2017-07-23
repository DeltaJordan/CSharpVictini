using System;
using System.Threading.Tasks;
using Discord;

namespace CSharpDewott.Deserialization
{
    class DeserializableUser : IUser
    {
        public ulong Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Mention { get; set; }
        public Game? Game { get; set; }
        public UserStatus Status { get; set; }

        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            return null;
        }

        public Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null)
        {
            return null;
        }

        public string AvatarId { get; set; }
        public string Discriminator { get; set; }
        public ushort DiscriminatorValue { get; set; }
        public bool IsBot { get; set; }
        public bool IsWebhook { get; set; }
        public string Username { get; set; }

        public DeserializableUser(ulong Id, DateTimeOffset CreatedAt, string Mention, string AvatarId, ushort DiscriminatorValue, string Discrimitator, bool IsBot, bool IsWebhook, string Username)
        {
            this.Id = Id;
            this.CreatedAt = CreatedAt;
            this.Mention = Mention;
            this.AvatarId = AvatarId;
            this.DiscriminatorValue = DiscriminatorValue;
            this.Discriminator = Discrimitator;
            this.IsBot = IsBot;
            this.IsWebhook = IsWebhook;
            this.Username = Username;
        }
    }
}
