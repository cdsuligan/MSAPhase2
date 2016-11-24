using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Contoso_Bank.DataModels
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<BankAccount> bankAccountTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("https://contosobankeasytables.azurewebsites.net/");
            this.bankAccountTable = this.client.GetTable<BankAccount>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task CreateNewAccount(BankAccount bankaccount)
        {
            await this.bankAccountTable.InsertAsync(bankaccount);
        }

        public async Task DeleteAccount(BankAccount bankaccount)
        {
            await this.bankAccountTable.DeleteAsync(bankaccount);
        }

        public async Task<List<BankAccount>> ViewAccountDetails()
        {
            return await this.bankAccountTable.ToListAsync();
        }

        public async Task Deposit(BankAccount bankaccount)
        {
            await this.bankAccountTable.UpdateAsync(bankaccount);
        }

        public async Task Withdraw(BankAccount bankaccount)
        {
            await this.bankAccountTable.UpdateAsync(bankaccount);
        }
    }
}