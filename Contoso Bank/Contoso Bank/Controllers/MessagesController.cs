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
            // Global values
            bool boolAskedForChoice = false;
            //string strUserName = "";
            string userChoice = "";
            if (activity.Type == ActivityTypes.Message)
            {
                // Get any saved values
                StateClient sc = activity.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(activity.ChannelId, activity.Conversation.Id, activity.From.Id);
                boolAskedForChoice = userData.GetProperty<bool>("AskedForChoice");
                userChoice = userData.GetProperty<string>("Choice") ?? "";
                // Create text for a reply message   
                StringBuilder strReplyMessage = new StringBuilder();
                if (boolAskedForChoice == false) // Never asked for choice
                {
                    strReplyMessage.Append($"Welcome! I am Contoso Bank's Bot.");
                    strReplyMessage.Append($"\n");
                    strReplyMessage.Append($"Would you like to use 'Currency Rates' or 'Stocks'?");
                    // Set BotUserData
                    userData.SetProperty<bool>("AskedForChoice", true);
                }
                else // Have asked for choice
                {
                    if (userChoice == "") // Choice was never provided
                    {
                        userChoice = activity.Text; //Either Currency rates of Stocks
                        if (userChoice.ToLower() == "currency exchange") {
                            strReplyMessage.Append($"Your choice was: {userChoice}");
                        }

                        else if (userChoice.ToLower() == "stocks")
                        {
                            strReplyMessage.Append($"Your choice was: {userChoice}");
                        }

                    }
                    else // Choice was provided
                    {
                        strReplyMessage.Append($"You've been provided a choice before this.");
                    }
                }
                // Save BotUserData
                sc.BotState.SetPrivateConversationData(activity.ChannelId, activity.Conversation.Id, activity.From.Id, userData);
                // Create a reply message
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity replyMessage = activity.CreateReply(strReplyMessage.ToString());
                await connector.Conversations.ReplyToActivityAsync(replyMessage);
         


                /*ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                CurrencyExchange.RootObject rootObject;
                HttpClient client = new HttpClient();


                Activity reply = activity.CreateReply($"Welcome to Contoso Bank's Currency Converter. What is your intended base currency?");
                await connector.Conversations.ReplyToActivityAsync(reply);

                string baseCurrency = activity.Text;
                string newCurrency;

                string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + baseCurrency));

                rootObject = JsonConvert.DeserializeObject<CurrencyExchange.RootObject>(x);

                if (rootObject.error.Length <= 0) {
                    Activity reply2 = activity.CreateReply($"What is the currency you would like to convert into?");
                    await connector.Conversations.ReplyToActivityAsync(reply2);
                    newCurrency = activity.Text;

                    if (newCurrency.ToUpper() == "GBP")
                    {
                        rootObject = JsonConvert.DeserializeObject<CurrencyExchange.RootObject>(x);
                        string result = rootObject.rates.GBP + "";

                        //Activity reply3 = activity.CreateReply($"From: " + baseCurrency + " to " + newCurrency + " is: " +result );
                        //await connector.Conversations.ReplyToActivityAsync(reply3);
                    }
                } */



                /*StringBuilder strReplyMessage = new StringBuilder();
                strReplyMessage.Append($"Welcome to Contoso Bank's currency converter.");
                Activity replyMessage = activity.CreateReply(strReplyMessage.ToString());
                await connector.Conversations.ReplyToActivityAsync(replyMessage);

                Activity reply1 = activity.CreateReply($"Please state Base Currency.");
                await connector.Conversations.ReplyToActivityAsync(reply1);
                string baseCurrency = activity.Text;

                Activity reply2 = activity.CreateReply($"Please state New Currency.");
                await connector.Conversations.ReplyToActivityAsync(reply2);
                string newCurrency = activity.Text;

                Activity reply3 = activity.CreateReply($"Please state the amount you would like to convert.");
                await connector.Conversations.ReplyToActivityAsync(reply3);
                string amountToConvert = activity.Text;*/

                //string x = await client.GetStringAsync(new Uri("http://apilayer.net/api/convert?access_key=c5cc7aed368f5eda94741002077023e3&from=" + baseCurrency + "&to=" + newCurrency + "&amount=" + amountToConvert + ""));




                /*Dictionary<String, int> currenciesDict = { "AUD": 1, "BGN": rootObject.rates.AUD, "BRL": rootObject.rates.AUD, "CAD": rootObject.rates.AUD, "CHF": rootObject.rates.AUD, "CNY": rootObject.rates.AUD, "CZK": rootObject.rates.AUD, "DKK": rootObject.rates.AUD,  
                    "GBP": rootObject.rates.AUD, "HKD": rootObject.rates.AUD, "HRK": rootObject.rates.AUD, "HUF": rootObject.rates.AUD, "IDR": rootObject.rates.AUD, "ILS": rootObject.rates.AUD, "INR": rootObject.rates.AUD,  "JPY": rootObject.rates.AUD,
                    "KRW": rootObject.rates.AUD, "MXN": rootObject.rates.AUD, "MYR": rootObject.rates.AUD, "NOK": rootObject.rates.AUD, "NZD": rootObject.rates.AUD, "PHP": rootObject.rates.AUD, "PLN": rootObject.rates.AUD, "RON": rootObject.rates.AUD, 
                    "RUB": rootObject.rates.AUD, "SEK": rootObject.rates.AUD, "SGD": rootObject.rates.AUD, "THB": rootObject.rates.AUD, "TRY": rootObject.rates.AUD, "USD": rootObject.rates.AUD, "ZAR": rootObject.rates.AUD, "EUR": rootObject.rates.AUD;
                        };*/

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
                // Get BotUserData
                StateClient sc = message.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(
                    message.ChannelId, message.Conversation.Id, message.From.Id);
                // Set BotUserData
                userData.SetProperty<string>("Choice", "");
                userData.SetProperty<bool>("AskedForChoice", false);
                // Save BotUserData
                sc.BotState.SetPrivateConversationData(
                    message.ChannelId, message.Conversation.Id, message.From.Id, userData);
                // Create a reply message
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                Activity replyMessage = message.CreateReply("Personal data has been deleted.");
                return replyMessage;
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