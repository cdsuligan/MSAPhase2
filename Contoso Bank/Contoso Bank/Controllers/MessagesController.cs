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
using System.Collections.Generic;
using Contoso_Bank.DataModels;

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
                    userData.SetProperty<bool>("NeedsACard", false);
                }

                else
                {

                    userData.SetProperty("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    userData.SetProperty<bool>("NeedsACard", false);
                }



                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your personal data has been cleared.";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
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

                        else
                        {
                            endOutput = "Invalid currency code. Please try again.";
                        }
                    }
                    userData.SetProperty("NeedsACard", false);
                }


                if (userMessage.ToLower().Equals("original"))
                {
                    string original = userData.GetProperty<string>("DefaultBase");   //Gets default original (base) currency code

                    if (original == null)
                    {
                        endOutput = "Original currency not assigned.";
                        //userData.SetProperty("NeedsACard", false);
                    }

                    else
                    {
                        userData.SetProperty<string>("BaseCurrency", original); //Setting the BaseCurrency to userMessage (the user's input)
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        endOutput = "Convert from " + original + " to _?";
                    }
                    userData.SetProperty("NeedsACard", false);

                }


                /**************************** CONTOSO BANK CARD STARTS ********************************/

                if (userMessage.ToLower().Equals("cb") | userMessage.ToLower().Equals("contoso") | userMessage.ToLower().Equals("contoso bank"))
                {
                    string strCurrentURL =
                    Url.Request.RequestUri.AbsoluteUri.Replace(@"api/messages", "");

                    Activity replyToConversation = activity.CreateReply("");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn4.iconfinder.com/data/icons/web-development-5/500/internet-network-128.png"));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://www.facebook.com/Contoso-Bank-411866388937777/",
                        Type = "openUrl",
                        Title = "VISIT US ON FACEBOOK"
                    };
                    cardButtons.Add(plButton);

                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Contoso Bank",
                        Subtitle = "THE WORLD'S NO.1 BANK.",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                /**************************** CONTOSO BANK CARD ENDS ********************************/

                if (userMessage.ToLower().Contains("convert currencies"))
                { //The user wants Currency Exchange
                    endOutput = "Convert currencies from _?";
                    userData.SetProperty("UserWantsCurrencyRates", true); //Sets "UserWantsCurrencyRates" to TRUE
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userData.GetProperty<bool>("UserWantsCurrencyRates") & (userMessage.Length == 3)) //Checks if the UserWantsCurrencyRates 
                {
                    endOutput = userData.GetProperty<bool>("UserWantsCurrencyRates").ToString() + " length: " + userMessage.Length.ToString();

                    if (currencyCodes.ContainsKey(userMessage.ToUpper())) //Checks if valid Currency Code
                    {
                        userData.SetProperty<string>("BaseCurrency", userMessage); //Setting the BaseCurrency to userMessage (the user's input)

                        userData.SetProperty("UserWantsCurrencyRates", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        string baseCurrency = userData.GetProperty<string>("BaseCurrency");

                        endOutput = "Convert from " + baseCurrency + " to _?";
                    }
                }

                //USER WANTS TO VIEW BANK ACCOUNT DETAILS
                if (userMessage.Length == 19)
                {
                    if (userMessage.ToLower().Substring(0,12).Equals("view account"))
                    {
                        List<BankAccount> bankaccounts = await AzureManager.AzureManagerInstance.ViewAccountDetails();
                        endOutput = "";
                        string acctNo = userMessage.Substring(13);

                        foreach (BankAccount ba in bankaccounts)
                        {
                            if (ba.AcctNo == acctNo)
                            {
                                endOutput = "Hi " + ba.FirstName + " " + ba.LastName + ", your remaining balance is $" + ba.Balance + ".";
                            }
                        }
                    }

                    userData.SetProperty<bool>("NeedsACard", false);
                }

                
                if (userMessage.Length > 14) {
                    //USER WANTS TO CREATE A NEW BANK ACCOUNT
                    if (userMessage.ToLower().Substring(0, 14).Equals("create account"))
                    {
                        List<BankAccount> bankaccounts = await AzureManager.AzureManagerInstance.ViewAccountDetails();
                        int bankAcctSize = bankaccounts.Count();

                        int newAcctNo;
                        string fullName = userMessage.Substring(14);
                        string[] fullNameList = fullName.Split(' ');

                        string firstName = fullNameList[1];
                        string lastName = fullNameList[2];
                        int newID = 0;
                        int intID = 0;

                        Random rnd = new Random();
                        newAcctNo = rnd.Next(100000, 1000000); // Creates a number between 100000 and 999999
                        bool newAcctNoIsUnique = false;

                        while (!newAcctNoIsUnique)
                        {
                            foreach (BankAccount ba in bankaccounts)
                            {
                                if (ba.AcctNo != newAcctNo.ToString())
                                {
                                    bankAcctSize -= 1;
                                    if (bankAcctSize == 0)
                                    {
                                        newAcctNoIsUnique = true;
                                    }
                                }

                                else
                                {
                                    newAcctNo = rnd.Next(100000, 1000000); // Creates a number between 100000 and 999999
                                    newAcctNoIsUnique = false;
                                    bankAcctSize = bankaccounts.Count;
                                    break;
                                }

                                int.TryParse(ba.ID, out intID);
                                if (intID > newID)
                                {
                                    newID = intID;
                                }
                            }
                        }
                        newID += 1;
                        bankAcctSize = bankaccounts.Count();

                        BankAccount bankaccount = new BankAccount()
                        {
                            ID = newID.ToString(),
                            FirstName = firstName,
                            LastName = lastName,
                            Balance = 0.00,
                            AcctNo = newAcctNo.ToString()
                        };

                        await AzureManager.AzureManagerInstance.CreateNewAccount(bankaccount);

                        endOutput = "Congratulations, " + firstName + " " + lastName + "! Your new account number is " + newAcctNo.ToString()
                            + " and your current balance is: $0.00.";

                    }

                    //USER WANTS TO DELETE THEIR ACCOUNT
                    else if (userMessage.ToLower().Substring(0, 14).Equals("delete account"))
                    {
                        List<BankAccount> bankaccounts = await AzureManager.AzureManagerInstance.ViewAccountDetails();
                        string acctNo = userMessage.Substring(15);

                        foreach (BankAccount ba in bankaccounts)
                        {
                            if (ba.AcctNo == acctNo.ToString())
                            {
                                await AzureManager.AzureManagerInstance.DeleteAccount(ba);
                                endOutput = "Hi, " + ba.FirstName + " " + ba.LastName + ", your remaining balance of $" + ba.Balance.ToString() +
                                    " has now been transferred to your secondary account. Your bank account with account number: " + ba.AcctNo
                                    + " has now been closed.";
                            }
                        }
                    }

                    //USER WANTS TO DEPOSIT INTO ACCOUNT
                    else if (userMessage.ToLower().Substring(0, 7).Equals("deposit")) {
                        List<BankAccount> bankaccounts = await AzureManager.AzureManagerInstance.ViewAccountDetails();
                        string[] depositDetails = userMessage.Substring(8).Split();
                        string acctNo = depositDetails[0];
                        double amount; //Amount to be deposited
                        double.TryParse(depositDetails[1], out amount);

                        foreach (BankAccount ba in bankaccounts)
                        {
                            if (ba.AcctNo == acctNo.ToString())
                            {
                                double oldBalance = ba.Balance;
                                double newBalance = oldBalance + amount;
                                ba.Balance = newBalance;
                                await AzureManager.AzureManagerInstance.Deposit(ba);
                                endOutput = "Hi, " + ba.FirstName + " " + ba.LastName + "! Your deposit has been completed. Balance from $" + oldBalance.ToString()
                                    + " to $" + ba.Balance.ToString() + ".";
                            }
                        }

                    }


                    else if (userMessage.ToLower().Substring(0, 8).Equals("withdraw"))
                    {
                        List<BankAccount> bankaccounts = await AzureManager.AzureManagerInstance.ViewAccountDetails();
                        string[] depositDetails = userMessage.Substring(9).Split();
                        string acctNo = depositDetails[0];
                        double amount; //Amount to be withdrawn
                        double.TryParse(depositDetails[1], out amount);

                        foreach (BankAccount ba in bankaccounts)
                        {
                            if (ba.AcctNo == acctNo.ToString())
                            {
                                double oldBalance = ba.Balance;
                                double newBalance = oldBalance - amount;
                                ba.Balance = newBalance;
                                await AzureManager.AzureManagerInstance.Deposit(ba);
                                endOutput = "Hi, " + ba.FirstName + " " + ba.LastName + "! Your withdrawal has been completed. Balance from $" + oldBalance.ToString()
                                    + " to $" + ba.Balance.ToString() + ".";
                            }
                        }

                    }


                    userData.SetProperty<bool>("NeedsACard", false);
                }


                if (userData.GetProperty<string>("BaseCurrency") != null) //Checks if BaseCurrency exists
                {
                    if ((userMessage.Length == 3) & (userMessage != userData.GetProperty<string>("BaseCurrency") & (userData.GetProperty<string>("BaseCurrency").Length == 3)))
                    {
                        if (currencyCodes.ContainsKey(userMessage.ToUpper())) //Checks if valid Currency Code
                        {
                            userData.SetProperty<bool>("NeedsACard", true);
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

                            //endOutput = "1 " + baseCurrency.ToUpper() + " = " + result.ToString() + " " + newCurrency.ToUpper() + ".";

                            Activity conversionReply = activity.CreateReply($"DISCLAIMER: Foreign exchange rates are published by the European Central Bank.");
                            conversionReply.Recipient = activity.From;
                            conversionReply.Type = "message";
                            conversionReply.Attachments = new List<Attachment>();

                            List<CardImage> cardImages = new List<CardImage>();
                            cardImages.Add(new CardImage(url: "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cb/Logo_European_Central_Bank.svg/2000px-Logo_European_Central_Bank.svg.png"));

                            List<CardAction> cardButtons = new List<CardAction>();
                            CardAction plButton = new CardAction()
                            {
                                Value = "https://sdw.ecb.europa.eu/curConverter.do",
                                Type = "openUrl",
                                Title = "MORE INFO"
                            };
                            cardButtons.Add(plButton);

                            ThumbnailCard plCard = new ThumbnailCard()
                            {
                                Title = baseCurrency.ToUpper() + " to " + newCurrency.ToUpper(),
                                Subtitle = "1 " + baseCurrency.ToUpper() + " = " + result.ToString() + " " + newCurrency.ToUpper() + ".",
                                Images = cardImages,
                                Buttons = cardButtons
                            };

                            Attachment plAttachment = plCard.ToAttachment();
                            conversionReply.Attachments.Add(plAttachment);
                            await connector.Conversations.SendToConversationAsync(conversionReply);

                            userData.SetProperty<string>("BaseCurrency", "");
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        }

                        else
                        {
                            endOutput = "Invalid currency code. Please try again.";
                        }
                    }
                }

                if (!userData.GetProperty<bool>("NeedsACard"))
                {
                    Activity infoReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(infoReply);
                }

                userData.SetProperty<bool>("NeedsACard", false);
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
