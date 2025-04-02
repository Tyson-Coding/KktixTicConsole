using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;

var eventId = "5cee48b5";
var seatId = 839018;
var queueUrl = $@"https://queue.kktix.com/queue/{eventId}"; // + ?authenticity_token
var getParamUrl = $@"https://queue.kktix.com/queue/token/"; // + queue response Token
var landingUrl = $@"https://kktix.com/events/{eventId}/registrations/"; // + Param response To_param
var authenticity_token = @"4%2BSucpJWVmMuumIeI16OE0R7upU2IKHkrLmGBrmZSCs%2BxI41iTKDEeVTSJJi59abzxjZlRkGoVCc%2BBfqT7bEWQ%3D%3D";
var cookie = @"_fbp=fb.1.1743575019240.411827062547173178; _hjSession_1979059=eyJpZCI6IjI2ZjQwZDdkLWQ3YWUtNGZmMi1iZGRmLTIxNWQzMDVhZjBiNyIsImMiOjE3NDM1NzUwMTkzODEsInMiOjAsInIiOjAsInNiIjowLCJzciI6MCwic2UiOjAsImZzIjoxLCJzcCI6MH0=; _clck=18jokh0%7C2%7Cfuq%7C0%7C1918; _gid=GA1.2.1964172670.1743575020; _hjSessionUser_1979059=eyJpZCI6ImRjZjkyOThjLTM3N2ItNTAwMi05YzI2LTY3MjQ3NTY4YTNjNiIsImNyZWF0ZWQiOjE3NDM1NzUwMTkzODAsImV4aXN0aW5nIjp0cnVlfQ==; user_display_name_v2=%25E6%259E%2597%25E5%25AE%25B6%25E8%25B1%25AA; user_avatar_url_v2=https%3A%2F%2Fwww.gravatar.com%2Favatar%2F2f000d241af9eef893674727f0a272dd.png; user_id_v2=451791; user_path_v2=%2Fuser%2F451791; user_time_zone_v2=Asia%2FTaipei; user_time_zone_offset_v2=28800; kktix_session_token_v2=3ad701528d379f6f603da6e0429733f3; mobileNotVerified=0; locale=zh-TW; _ga=GA1.2.97419787.1743575019; _ga_WZBYP4N1ZG=GS1.2.1743575019.1.1.1743575156.55.0.0; _clsk=1xdz372%7C1743575751952%7C14%7C0%7Ci.clarity.ms%2Fcollect; _gali=registrationsNewApp; _ga_LWVPBSFGF6=GS1.1.1743575019.1.1.1743576006.60.0.0; _ga_SYRTJY65JB=GS1.1.1743575019.1.1.1743576006.60.0.0; _ga_GCLEH6R9MF=GS1.1.1743575079.1.1.1743576006.60.0.0; XSRF-TOKEN=Ghk51hi7ET3Z6dMJymMTkVgZSwfaEpyvOdLWRms46DbHORmRA9%2FETxIA%2BYWL2ksZ03ooB%2FU0nBsJk0eqnRdkRA%3D%3D";
var thread = 48;
List<int> list = new();
for (var i = 1; i <= thread; i++)
    list.Add(i);

try
{
    var cts = new CancellationTokenSource();
    await Parallel.ForEachAsync(list, new ParallelOptions { MaxDegreeOfParallelism = thread, CancellationToken = cts.Token }, async (item,token) => {
        while (true)
        {
            try
            {
                var model = new TicketRequest
                {
                    recaptcha = new { },
                    tickets = new List<Ticket>
                    {
                        new Ticket
                        {
                            id = seatId,
                            quantity = 3,
                        }
                    }
                };

                var queueResponse = JsonConvert.DeserializeObject<TicketResponse>(await PostApiAsync($@"{queueUrl}?authenticity_token={authenticity_token}", model));
                if (!string.IsNullOrWhiteSpace(queueResponse?.result) || string.IsNullOrWhiteSpace(queueResponse?.token))
                    continue;
                var tokenResponse = JsonConvert.DeserializeObject<Queue>(await GetApiAsync(getParamUrl + queueResponse.token));
                if (!string.IsNullOrWhiteSpace(tokenResponse?.result) || string.IsNullOrWhiteSpace(tokenResponse?.to_param))
                    continue;
                var url = landingUrl + tokenResponse.to_param;
                await SendEmail(url);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                continue;
            }
        }
        cts.Cancel();
        return;
    });
}
catch (Exception ex)
{
    Console.WriteLine($"發生錯誤: {ex.Message}");
}

async Task<string> GetApiAsync(string url)
{
    using (HttpClient client = new HttpClient())
    {
        // 設定請求標頭（可選）
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            // 發送 GET 請求
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode(); // 確保請求成功

            // 讀取回應內容
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"請求失敗: {e.Message}");
            return "";
        }
    }
}
async Task<string> PostApiAsync(string url, object data)
{
    using (HttpClient client = new HttpClient())
    {
        // 設定請求標頭（可選）
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            string json = JsonConvert.SerializeObject(data);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add(@"Cookie", cookie);
            client.DefaultRequestHeaders.Add(@"Sec-Fetch-Site", @"same-site");
            client.DefaultRequestHeaders.Add(@"User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.0.0");
            client.DefaultRequestHeaders.Add(@"Origin", @"https://kktix.com");

            // 發送 Post 請求
            HttpResponseMessage response = await client.PostAsync(url, content);
            //response.EnsureSuccessStatusCode(); // 確保請求成功

            string responseBody = await response.Content.ReadAsStringAsync();
            //if((int)response.StatusCode is not 200)

            //if (response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
            //{
            //    foreach (var cookie in setCookieValues)
            //    {
            //        Console.WriteLine($"Set-Cookie: {cookie}");
            //    }
            //}

            return responseBody;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"請求失敗: {e.Message}");
            return "";
        }
    }
}
async Task SendEmail(string url)
{
    // 建立郵件
    var message = new MimeMessage();
    // 添加寄件者
    message.From.Add(new MailboxAddress("Tyson-Cohesiondata", "tyson.lin@cohesiondata.com"));

    // 添加收件者
    message.To.Add(new MailboxAddress("Tyson", "gn00667340@gmail.com"));

    // 設定郵件標題
    message.Subject = "Yes!";

    // 設定郵件內容
    message.Body = new TextPart("plain")
    {
        Text = $"Tick Url: {url}"
    };
    using (var client = new SmtpClient())
    {
        var hostUrl = "smtp.gmail.com";
        var port = 465;
        var useSsl = true;

        // 連接 Mail Server (郵件伺服器網址, 連接埠, 是否使用 SSL)
        client.Connect(hostUrl, port, useSsl);

        // 如果需要的話，驗證一下
        client.Authenticate("tyson.lin@cohesiondata.com", "a0925825781");

        // 寄出郵件
        client.Send(message);

        // 中斷連線
        client.Disconnect(true);
    }
}

// 請求類別
public class TicketRequest
{
    public List<Ticket> tickets { get; set; }
    public string currency { get; set; } = "TWD";
    public object recaptcha { get; set; }
    public bool agreeTerm { get; set; } = true;
}

public class Ticket
{
    public int id { get; set; }
    public int quantity { get; set; }
    public List<string> invitationCodes { get; set; } = new List<string>();
    public string member_code { get; set; } = "";
    public string? use_qualification_id { get; set; } = null;
}

// 回應類別
public class TicketResponse
{
    public string? token { get; set; }
    public string? result { get; set; }
}

public class Queue
{
    public string? to_param { get; set; }

    public string? result { get; set; }

    public string? message { get; set; }
}
