using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KktixTicConsole;
public class KktixTicketPurchaser
{
    private readonly HttpClient _client;
    private string? _csrfToken;
    private CookieContainer _cookieContainer;

    public KktixTicketPurchaser()
    {
        _cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public async Task Login(string email, string password)
    {
        // 先取得 CSRF Token
        var response = await _client.GetAsync("https://kktix.com/users/sign_in");
        var content = await response.Content.ReadAsStringAsync();

        // 使用正則表達式從 HTML 中提取 CSRF Token
        var match = Regex.Match(content, "<meta name=\"csrf-token\" content=\"([^\"]+)\"");
        if (match.Success)
        {
            _csrfToken = match.Groups[1].Value;
        }

        // 登入請求
        var loginData = new Dictionary<string, string>
        {
            { "user[email]", email },
            { "user[password]", password },
            { "authenticity_token", _csrfToken }
        };

        var loginContent = new FormUrlEncodedContent(loginData);
        response = await _client.PostAsync("https://kktix.com/users/sign_in", loginContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("登入失敗");
        }
    }

    public async Task<string> GetEventInfo(string eventUrl)
    {
        var response = await _client.GetAsync(eventUrl);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<bool> AddTicketToCart(string eventUrl, int ticketId, int quantity)
    {
        var cartData = new Dictionary<string, object>
        {
            { "tickets", new Dictionary<string, int> { { ticketId.ToString(), quantity } } },
            { "authenticity_token", _csrfToken }
        };

        var json = JsonConvert.SerializeObject(cartData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"{eventUrl}/registrations", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SubmitContactInfo(string eventUrl, string registrationId, Dictionary<string, string> contactInfo)
    {
        var contactData = new Dictionary<string, object>
        {
            { "contact", contactInfo },
            { "authenticity_token", _csrfToken }
        };

        var json = JsonConvert.SerializeObject(contactData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"{eventUrl}/registrations/{registrationId}", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ProcessPayment(string eventUrl, string registrationId, string paymentMethod)
    {
        var paymentData = new Dictionary<string, object>
        {
            { "payment_method", paymentMethod },
            { "authenticity_token", _csrfToken }
        };

        var json = JsonConvert.SerializeObject(paymentData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"{eventUrl}/registrations/{registrationId}/payment", content);
        return response.IsSuccessStatusCode;
    }
}


