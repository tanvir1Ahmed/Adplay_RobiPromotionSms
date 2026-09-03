using RobiSendPromotionSms;

// Recipient and message. Change the number here to send somewhere else.
var msisdn = "8801852956967";
var message = "Hello world";

using var httpClient = new HttpClient();
var sender = new SmsSender(httpClient);

try
{
    Console.WriteLine($"Sending to {msisdn}...");

    var response = await sender.SendSms(msisdn, message);

    Console.WriteLine($"SMS sent to {msisdn}.");
    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
}
