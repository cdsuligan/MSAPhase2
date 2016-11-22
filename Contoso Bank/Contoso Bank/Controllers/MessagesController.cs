﻿using System;
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


                string endOutput = "Welcome to Contoso Bank! Would you like currency rates or stocks?";

                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hi again! Welcome to Contoso Bank's currency exchange rates! Would you like currency rates or stocks?";
                }

                else
                {
                    userData.SetProperty("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    //^Syncing updated userData
                    //If we didn't sync up, the changes will not be reflected if we asked for the user data again. 
                    //In our first greeting, the Bot will say "Hello".
                    //When we greet the Bot again, it will say "Hello again.".
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your personal data has been cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                }

                if (userMessage.ToLower().Contains("currency rates"))
                { //The user wants Currency Exchange
                    endOutput = "Currency Rates is: " + userMessage + ". What is your base currency code?";
                    userData.SetProperty("UserWantsCurrencyRates", true);//Sets "UserWantsCurrencyRates" to TRUE
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                }


                if (userData.GetProperty<bool>("UserWantsCurrencyRates") & (userMessage.Length == 3)) //Checks if the UserWantsCurrencyRates 
                {
                    if (currencyCodes.ContainsKey(userMessage.ToUpper())) //Checks if valid Currency Code
                    {
                        userData.SetProperty<string>("BaseCurrency", userMessage); //Setting the BaseCurrency to userMessage (the user's input)
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty("UserWantsCurrencyRates", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        endOutput = "What is the new currency?";

                    }
                }

                if (userData.GetProperty<string>("BaseCurrency") != null) //Checks if BaseCurrency exists
                {
                    if ((userMessage.Length == 3) & (userMessage != userData.GetProperty<string>("BaseCurrency") & (userData.GetProperty<string>("BaseCurrency").Length ==3)))
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



                            if (newCurrency.ToUpper() == "AUD")
                            {
                                result = rootObject.rates.AUD;
                            }

                            else if (newCurrency.ToUpper() == "BGN")
                            {
                                result = rootObject.rates.BGN;
                            }

                            else if (newCurrency.ToUpper() == "BRL")
                            {
                                result = rootObject.rates.BRL;
                            }

                            endOutput = "BaseCurrency is: " + baseCurrency + " newCurrency is: " + userMessage + " and the result is: " + result.ToString();

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
