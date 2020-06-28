// OpenTween - Client of Twitter
// Copyright (c) 2016 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
//
// This file is part of OpenTween.
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System.Text;
using System.IO;
using System.Net.Http;
using Xunit;
using System.Threading.Tasks;
using System.Runtime.Serialization;
//using OpenTween.Api;
using System;

namespace OpenTween.Connection
{
    public class LazyJsonTest
    {
        [Fact]
        public async Task LoadJsonAsync_Test()
        {
            byte[] body = Encoding.UTF8.GetBytes("\"hogehoge\"");
            using var bodyStream = new MemoryStream(body);
            using var response = new HttpResponseMessage() { Content = new StreamContent(bodyStream) };
            using var lazyJson = new LazyJson<string>(response);

            // この時点ではまだレスポンスボディは読まれない
            Assert.Equal(0, bodyStream.Position);

            string result = await lazyJson.LoadJsonAsync().ConfigureAwait(false);

            Assert.Equal("hogehoge", result);
        }

        [Fact]
        public async Task LoadJsonAsync_InvalidJsonTest()
        {
            byte[] body = Encoding.UTF8.GetBytes("### Invalid JSON ###");
            using var bodyStream = new MemoryStream(body);
            using var response = new HttpResponseMessage() { Content = new StreamContent(bodyStream) };
            using var lazyJson = new LazyJson<string>(response);

            // この時点ではまだレスポンスボディは読まれない
            Assert.Equal(0, bodyStream.Position);

            var exception = await Assert.ThrowsAnyAsync<SerializationException>(() => lazyJson.LoadJsonAsync()) //var exception = await Assert.ThrowsAnyAsync<WebApiException>(() => lazyJson.LoadJsonAsync())
                .ConfigureAwait(false);

            Assert.IsType<SerializationException>(exception); //Assert.IsType<SerializationException>(exception.InnerException);
        }

        [Fact]
        public async Task IgnoreResponse_Test()
        {
            // IgnoreResponse() によってレスポンスの Stream が読まれずに破棄されることをテストするため、
            // 読み込みが行われると例外が発生する下記の InvalidStream クラスを bodyStream に使用している
            using var bodyStream = new InvalidStream();
            using var response = new HttpResponseMessage() { Content = new StreamContent(bodyStream) };
            using var lazyJson = new LazyJson<string>(response);

            Task<LazyJson<string>> task = Task.FromResult<LazyJson<string>>(lazyJson);
            await task.IgnoreResponse().ConfigureAwait(false); // レスポンスボディを読まずに破棄

            Assert.True(bodyStream.IsDisposed);
        }

        class InvalidStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => 100L;
            public override long Position
            {
                get => 0L;
                set => throw new NotSupportedException();
            }
            public bool IsDisposed { get; private set; } = false;

            public override void Flush()
                => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
                => throw new IOException();

            public override int Read(Span<byte> buffer)
                => throw new IOException();

            public override long Seek(long offset, SeekOrigin origin)
                => throw new NotSupportedException();

            public override void SetLength(long value)
                => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
                => throw new NotSupportedException();

            public override void Write(ReadOnlySpan<byte> buffer)
                => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                this.IsDisposed = true;
            }
        }
    }
}
