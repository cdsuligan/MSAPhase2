using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Contoso_Bank.Models;
using System.Text;
using System.Collections.Generic;

namespace Contoso_Bank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient(); //Saves bot state
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id); //Invoking state client

                var userMessage = activity.Text; //The user's input

                Dictionary<string, int> currencyCodes = new Dictionary<string, int>()
                        { { "AUD", 1}, { "BGN", 1}, { "BRL", 3}, { "CAD", 1}, { "CHF", 1}, { "CNY", 6}, { "CZK", 2}, { "DKK", 6}, { "GBP", 0},
                            { "HKD", 7}, { "HRK", 7}, { "HUF", 2}, { "IDR", 0}, { "ILS", 3}, { "INR", 0}, { "JPY", 1}, { "KRW", 1}, { "MXN", 2},
                            { "MYR", 4}, { "NOK", 8}, { "NZD", 1}, { "PHP", 4}, { "PLN", 4}, { "RON", 4}, { "RUB", 6}, { "SEK", 9}, { "SGD", 1},
                            { "THB", 3}, { "TRY", 3}, { "ZAR", 1}, { "EUR", 0}, { "USD", 1} };


                string endOutput = "Welcome to Contoso Bank! Would you like to 'convert currencies' or 'view stocks'?";

                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hi again! What can I do for you, 'convert currencies' or 'view stocks'?";
                }

                else
                {
                    userData.SetProperty("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your personal data has been cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                }

                if (userMessage.ToLower().Contains("convert currencies"))
                { //The user wants Currency Exchange
                    endOutput = "Convert currencies from _?";
                    userData.SetProperty("UserWantsCurrencyRates", true); //Sets "UserWantsCurrencyRates" to TRUE
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userMessage.Length > 13)
                {
                    if (userMessage.ToLower().Substring(0, 12).Equals("set original")) //The user wants to set a default original (base) currency code
                    {
                        string original = userMessage.Substring(13);
                        if (currencyCodes.ContainsKey(original.ToUpper()))
                        {
                            userData.SetProperty<string>("DefaultBase", original.ToUpper());   //Setting a default original (base) currency code
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            string defaultBase = userData.GetProperty<string>("DefaultBase");
                            endOutput = "Original currency is now set to " + defaultBase + ".";
                        }

                        else {
                            endOutput = "Invalid currency code. Please try again.";
                        }
                    }
                }


                if (userMessage.ToLower().Equals("original")) {
                    string original = userData.GetProperty<string>("DefaultBase");   //Gets default original (base) currency code

                    if (original == null)
                    {
                        endOutput = "Original currency not assigned.";
                    }

                    else {
                        userData.SetProperty<string>("BaseCurrency", original); //Setting the BaseCurrency to userMessage (the user's input)
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        endOutput = "Convert from " + original + " to _?";
                    }
                }


                if (userData.GetProperty<bool>("UserWantsCurrencyRates") & (userMessage.Length == 3)) //Checks if the UserWantsCurrencyRates 
                {
                    if (currencyCodes.ContainsKey(userMessage.ToUpper())) //Checks if valid Currency Code
                    {
                        userData.SetProperty<string>("BaseCurrency", userMessage); //Setting the BaseCurrency to userMessage (the user's input)
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty("UserWantsCurrencyRates", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        string baseCurrency = userData.GetProperty<string>("BaseCurrency");

                        endOutput = "Convert from " + baseCurrency + " to _?";

                    }
                }

                if (userData.GetProperty<string>("BaseCurrency") != null) //Checks if BaseCurrency exists
                {
                    if ((userMessage.Length == 3) & (userMessage != userData.GetProperty<string>("BaseCurrency") & (userData.GetProperty<string>("BaseCurrency").Length == 3)))
                    {
                        if (currencyCodes.ContainsKey(userMessage.ToUpper())) //Checks if valid Currency Code
                        {
                            string baseCurrency = userData.GetProperty<string>("BaseCurrency");
                            string newCurrency = userMessage;
                            double result = 0.0;

                            CurrencyExchange.RootObject rootObject;
                            HttpClient client = new HttpClient();

                            string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + baseCurrency));

                            rootObject = JsonConvert.DeserializeObject<CurrencyExchange.RootObject>(x);

                            Dictionary<string, double> currencyCodesRootObjects = new Dictionary<string, double>(){
                                { "AUD", rootObject.rates.AUD}, { "BGN", rootObject.rates.BGN}, { "BRL", rootObject.rates.BRL}, { "CAD", rootObject.rates.CAD}, { "CHF", rootObject.rates.CHF}, { "CNY", rootObject.rates.CNY}, { "CZK", rootObject.rates.CZK}, { "DKK", rootObject.rates.DKK}, { "GBP", rootObject.rates.GBP},
    { "HKD", rootObject.rates.HKD}, { "HRK", rootObject.rates.HRK}, { "HUF", rootObject.rates.HUF}, { "IDR", rootObject.rates.IDR}, { "ILS", rootObject.rates.ILS}, { "INR", rootObject.rates.INR}, { "JPY", rootObject.rates.JPY}, { "KRW", rootObject.rates.KRW}, { "MXN", rootObject.rates.MXN},
    { "MYR", rootObject.rates.MYR}, { "NOK", rootObject.rates.NOK}, { "NZD", rootObject.rates.NZD}, { "PHP", rootObject.rates.PHP}, { "PLN", rootObject.rates.PLN}, { "RON", rootObject.rates.RON}, { "RUB", rootObject.rates.RUB}, { "SEK", rootObject.rates.SEK}, { "SGD", rootObject.rates.SGD},
    { "THB", rootObject.rates.THB}, { "TRY", rootObject.rates.TRY}, { "ZAR", rootObject.rates.ZAR}, { "EUR", rootObject.rates.EUR}, { "USD", rootObject.rates.USD}
                            };

                            result = currencyCodesRootObjects[newCurrency.ToUpper()];

                            endOutput = "1 " + baseCurrency.ToUpper() + " is equivalent to " + result.ToString() + " " + newCurrency.ToUpper() + ".";

                            userData.SetProperty<string>("BaseCurrency", "");
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        }

                        else
                        {
                            endOutput = "Invalid currency code. Please try again.";
                        }
                    }
                }


                Activity infoReply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(infoReply);

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {

            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}
