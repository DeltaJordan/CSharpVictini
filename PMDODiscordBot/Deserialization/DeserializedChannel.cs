using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace CSharpDewott.Deserialization
{
    class DeserializedChannel : IMessageChannel
    {
        public ulong Id { get; }
        public DateTimeOffset CreatedAt { get; }
        public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public bool IsNsfw { get; }

        public DeserializedChannel(ulong Id, DateTimeOffset CreatedAt, string Name, bool IsNsfw)
        {
            this.Id = Id;
            this.CreatedAt = CreatedAt;
            this.Name = Name;
            this.IsNsfw = IsNsfw;
        }

        public Task<IUserMessage> SendMessageAsync(string text, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false,
            RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IMessage> GetMessageAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(int limit = 100, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir, int limit = 100, CacheMode mode = CacheMode.AllowDownload,
            RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir, int limit = 100, CacheMode mode = CacheMode.AllowDownload,
            RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteMessagesAsync(IEnumerable<IMessage> messages, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteMessagesAsync(IEnumerable<ulong> messageIds, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task TriggerTypingAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IDisposable EnterTypingState(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}
