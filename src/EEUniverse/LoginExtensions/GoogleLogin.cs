using EEUniverse.Library;
using EEUniverse.LoginExtensions.Models;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EEUniverse.LoginExtensions
{
    public static class GoogleLogin
    {
        public static async Task<Client> GetClientFromCookieAsync(string cookie)
        {
            var request = new FlurlRequest();
            request.EnableCookies();

            var loginHint = await GetGoogleLoginHintAsync(request, cookie.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Split('=')).ToDictionary(k => k[0], v => string.Join("=", v.Skip(1))));
            var googleLoginToken = await GetGoogleLoginTokenAsync(request, loginHint.Sessions[0].Login_Hint);

            var eeuTokenResponse = await "https://auth.ee-universe.com/auth/token".PostJsonAsync(new {
                method = "google",
                token = googleLoginToken.Id_Token
            }).ReceiveJson<LoginResponse>();

            return new Client(eeuTokenResponse.Token);
        }

        private static async Task<GoogleLoginHintResponse> GetGoogleLoginHintAsync(IFlurlRequest request, Dictionary<string, string> cookies)
        {
            request.Url = "https://accounts.google.com/o/oauth2/iframerpc?action=listSessions&client_id=13527175071-voosomrakdr9q92gmsm32c1e3ml1mm6m.apps.googleusercontent.com&origin=https%3A%2F%2Fee-universe.com&scope=profile&ss_domain=https%3A%2F%2Fee-universe.com";
            return await request.WithCookies(cookies)
                .WithHeader("Accept", "*/*")
                .WithHeader("Accept-Language", "en-US,en;q=0.5")
                .WithHeader("Cache-Control", "no-cache")
                .WithHeader("Connection", "keep-alive")
                .WithHeader("DNT", "1")
                .WithHeader("Pragma", "no-cache")
                .WithHeader("Referer", "https://accounts.google.com/o/oauth2/iframe")
                .WithHeader("TE", "Trailers")
                .WithHeader("User-Agent", "EEUniverse/LoginExtensions")
                .WithHeader("X-Requested-With", "XmlHttpRequest")
                .GetJsonAsync<GoogleLoginHintResponse>();
        }

        private static async Task<GoogleLoginTokenResponse> GetGoogleLoginTokenAsync(IFlurlRequest request, string loginHint)
        {
            request.Url = $"https://accounts.google.com/o/oauth2/iframerpc?action=issueToken&response_type=token%20id_token&login_hint={loginHint}&client_id=13527175071-voosomrakdr9q92gmsm32c1e3ml1mm6m.apps.googleusercontent.com&origin=https%3A%2F%2Fee-universe.com&scope=profile&ss_domain=https%3A%2F%2Fee-universe.com";
            return await request.WithHeader("Accept", "*/*")
                .WithHeader("Accept-Language", "en-US,en;q=0.5")
                .WithHeader("Cache-Control", "no-cache")
                .WithHeader("Connection", "keep-alive")
                .WithHeader("DNT", "1")
                .WithHeader("Pragma", "no-cache")
                .WithHeader("Referer", "https://accounts.google.com/o/oauth2/iframe")
                .WithHeader("TE", "Trailers")
                .WithHeader("User-Agent", "EEUniverse/LoginExtensions")
                .WithHeader("X-Requested-With", "XmlHttpRequest")
                .GetJsonAsync<GoogleLoginTokenResponse>();
        }
    }
}
