using KktixTicConsole;

try
{
    var purchaser = new KktixTicketPurchaser();

    // 1. 登入
    await purchaser.Login("street85240@yahoo.com.tw", "qwe321456e7d4c1");
    Console.WriteLine("登入成功");

    // 2. 演唱會頁面 URL
    string eventUrl = "https://edproduction.kktix.cc/events/chiaifujikawa-livetour-hk-2025";

    // 3. 取得活動資訊
    var eventInfo = await purchaser.GetEventInfo(eventUrl);
    Console.WriteLine("成功取得活動資訊");

    // 4. 新增門票到購物車 (使用你提供的 JSON 中的門票 ID)
    int ticketId = 848488; // HK$780 普通票
    int quantity = 1;

    bool addToCartResult = await purchaser.AddTicketToCart(eventUrl, ticketId, quantity);
    if (addToCartResult)
    {
        Console.WriteLine("成功將票券加入購物車");

        // 5. 假設系統返回了 registrationId
        string registrationId = "generated_registration_id";

        // 6. 提交聯絡人資訊
        var contactInfo = new Dictionary<string, string>
                {
                    { "field_text_934181", "您的姓名" },
                    { "field_email_934182", "your_email@example.com" },
                    { "field_text_934183", "您的手機號碼" },
                    { "field_radio_934184", "445507" } // 同意條款選項的 ID
                };

        bool contactInfoResult = await purchaser.SubmitContactInfo(eventUrl, registrationId, contactInfo);
        if (contactInfoResult)
        {
            Console.WriteLine("聯絡人資訊提交成功");

            // 7. 處理付款 (信用卡)
            bool paymentResult = await purchaser.ProcessPayment(eventUrl, registrationId, "credit_card");
            if (paymentResult)
            {
                Console.WriteLine("付款處理成功");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"發生錯誤: {ex.Message}");
}