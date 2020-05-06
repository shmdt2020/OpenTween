// OpenTween - Client of Twitter
// Copyright (c) 2017 kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenTween.Api.DataModel;
using OpenTween.Connection;

namespace OpenTween.Api
{
    public sealed class MastodonApi : IDisposable
    {
        public IMastodonApiConnection Connection { get; }
        public Uri InstanceUri { get; }

        public MastodonApi(Uri instanceUri) : this(instanceUri, null)
        {
        }

        public MastodonApi(Uri instanceUri, string? accessToken)
        {
            this.Connection = new MastodonApiConnection(instanceUri, accessToken);
            this.InstanceUri = instanceUri;
        }

        // アプリケーションを登録して client_id, client_secret の組を取得する
        public async Task<MastodonRegisteredApp> AppsRegister(string clientName,
                                                              Uri redirectUris,
                                                              string? scopes = null,
                                                              string? website = null)
        {
            var endpoint = new Uri("/api/v1/apps", UriKind.Relative);
            var param = new Dictionary<string, string>
            {
                ["client_name"]   = clientName,
                ["redirect_uris"] = redirectUris.OriginalString,
            };
            if (scopes  != null) param["scopes"]  = scopes;
            if (website != null) param["website"] = website;

            var response = await this.Connection.PostLazyAsync<MastodonRegisteredApp>(endpoint, param).ConfigureAwait(false);
            return await response.LoadJsonAsync().ConfigureAwait(false);
        }

        // ユーザの認可を求め、認可コードを発行するページの Uri を返す。この API はブラウザ経由でアクセスする
        public Uri OAuthAuthorize(string? forceLogin,
                                  string responseType,
                                  string clientId,
                                  Uri redirectUri,
                                  string? scope)
        {
            var endpoint = new Uri("/oauth/authorize", UriKind.Relative);
            var param = new Dictionary<string, string>
            {
                ["response_type"] = responseType,
                ["client_id"]     = clientId,
                ["redirect_uri"]  = redirectUri.OriginalString,
            };
            if (forceLogin != null) param["force_login"] = forceLogin;
            if (scope      != null) param["scope"]       = scope;

            return new Uri(new Uri(this.InstanceUri, endpoint),
                           "?" + MyCommon.BuildQueryString(param));
        }

        // アクセストークンを取得する
        public async Task<MastodonAccessToken> OAuthToken(string clientId,
                                                          string clientSecret,
                                                          Uri redirectUri,
                                                          string? scope = null,
                                                          string? code = null, // 認可コード
                                                          string grantType = "authorization_code")
        {
            var endpoint = new Uri("/oauth/token", UriKind.Relative);
            var param = new Dictionary<string, string>
            {
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"]  = redirectUri.OriginalString,
                ["grant_type"]    = grantType,
            };
            if (scope != null) param["scope"] = scope;
            if (code  != null) param["code"]  = code;

            var response = await this.Connection.PostLazyAsync<MastodonAccessToken>(endpoint, param).ConfigureAwait(false);
            return await response.LoadJsonAsync().ConfigureAwait(false);
        }

        public Task<MastodonAccount> AccountsVerifyCredentials()
        {
            var endpoint = new Uri("/api/v1/accounts/verify_credentials", UriKind.Relative);
            return this.Connection.GetAsync<MastodonAccount>(endpoint, null);
        }
        /*
        public Task<MastodonStatus[]> TimelinesHome(long? maxId = null,
                                                    long? sinceId = null,
                                                    int? limit = null)
        {
            var endpoint = new Uri("/api/v1/timelines/home", UriKind.Relative);
            var param = new Dictionary<string, string>();

            if (maxId   != null) param["max_id"]   = maxId.ToString();
            if (sinceId != null) param["since_id"] = sinceId.ToString();
            if (limit   != null) param["limit"]    = limit.ToString();

            return this.Connection.GetAsync<MastodonStatus[]>(endpoint, param);
        }

        public Task<LazyJson<MastodonStatus>> StatusesPost(string status,
                                                           long? inReplyToId = null,
                                                           IReadOnlyList<long>? mediaIds = null,
                                                           bool? sensitive = null,
                                                           string? spoilerText = null,
                                                           string? visibility = null)
        {
            var endpoint = new Uri("/api/v1/statuses", UriKind.Relative);
            var param = new Dictionary<string, string>
            {
                ["status"] = status,
            };

            if (inReplyToId != null) param["in_reply_to_id"] = inReplyToId.ToString();
            if (sensitive   != null) param["sensitive"]      = sensitive.Value ? "true" : "false";
            if (spoilerText != null) param["spoiler_text"]   = spoilerText;
            if (visibility  != null) param["visibility"]     = visibility;

            var paramList = param.ToList();

            foreach (var mediaId in mediaIds ?? Enumerable.Empty<long>())
            {
                paramList.Add(new KeyValuePair<string, string>("media_ids[]",
                                                               mediaId.ToString()));
            }

            return this.Connection.PostLazyAsync<MastodonStatus>(endpoint, paramList);
        }

        public Task StatusesDelete(long statusId)
        {
            var endpoint = new Uri($"/api/v1/statuses/{statusId}", UriKind.Relative);

            return this.Connection.PostLazyAsync<object>(HttpMethod.Delete, endpoint, null).IgnoreResponse();
        }

        public Task<LazyJson<MastodonStatus>> StatusesFavourite(long statusId)
        {
            var endpoint = new Uri($"/api/v1/statuses/{statusId}/favourite", UriKind.Relative);

            return this.Connection.PostLazyAsync<MastodonStatus>(endpoint, null);
        }

        public Task<LazyJson<MastodonStatus>> StatusesUnfavourite(long statusId)
        {
            var endpoint = new Uri($"/api/v1/statuses/{statusId}/unfavourite", UriKind.Relative);

            return this.Connection.PostLazyAsync<MastodonStatus>(endpoint, null);
        }

        public Task<LazyJson<MastodonStatus>> StatusesReblog(long statusId)
        {
            var endpoint = new Uri($"/api/v1/statuses/{statusId}/reblog", UriKind.Relative);

            return this.Connection.PostLazyAsync<MastodonStatus>(endpoint, null);
        }

        public Task<LazyJson<MastodonStatus>> StatusesUnreblog(long statusId)
        {
            var endpoint = new Uri($"/api/v1/statuses/{statusId}/unreblog", UriKind.Relative);

            return this.Connection.PostLazyAsync<MastodonStatus>(endpoint, null);
        }
        */
        public Task<MastodonInstance> Instance()
        {
            var endpoint = new Uri("/api/v1/instance", UriKind.Relative);
            return this.Connection.GetAsync<MastodonInstance>(endpoint, null);
        }

        public void Dispose() => this.Connection.Dispose();
    }
}
