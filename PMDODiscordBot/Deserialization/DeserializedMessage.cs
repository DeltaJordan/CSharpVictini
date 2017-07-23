using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace CSharpDewott.Deserialization
{
    class DeserializedMessage : IMessage
    {
        public ulong Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        [Obsolete("Probably doesn't work", true)]
        public async Task DeleteAsync(RequestOptions options = null)
        {
            await this.Channel.DeleteMessagesAsync(new[] { this.Id });
        }

        public MessageType Type { get; set; }
        public MessageSource Source { get; set; }
        public bool IsTTS { get; set; }
        public bool IsPinned { get; set; }
        public string Content { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset? EditedTimestamp { get; set; }
        public IMessageChannel Channel { get; set; }
        public IUser Author { get; set; }
        public IReadOnlyCollection<IAttachment> Attachments { get; set; }
        public IReadOnlyCollection<IEmbed> Embeds { get; set; }
        public IReadOnlyCollection<ITag> Tags { get; set; }
        public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; }
        public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; }
        public IReadOnlyCollection<ulong> MentionedUserIds { get; set; }

        public DeserializedMessage(ulong Id, bool IsTTS, bool IsPinned, string Content, DateTimeOffset Timestamp, DateTimeOffset? EditedTimestamp, DateTimeOffset CreatedAt, DeserializableUser author)
        {
            this.Id = Id;
            this.IsTTS = IsTTS;
            this.IsPinned = IsPinned;
            this.Content = Content;
            this.Timestamp = Timestamp;
            this.EditedTimestamp = EditedTimestamp;
            this.CreatedAt = CreatedAt;
            this.Author = author;
        }
    }
}
