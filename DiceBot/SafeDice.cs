﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Security.Cryptography;
namespace DiceBot
{
    class SafeDice : DiceSite
    {
        string accesstoken = "";

        bool ispd = true;
        DateTime LastBalance = DateTime.Now;
        public SafeDice(cDiceBot Parent)
        {
            AutoInvest = false;
            AutoWithdraw = true;
            ChangeSeed = true;
            AutoLogin = false;
            BetURL = "https://safedice.com/bets/";
            Thread t = new Thread(GetBalanceThread);
            t.Start();
            this.Parent = Parent;
            Name = "SafeDice";
            edge = 0.5m;
        }

        public void GetBalanceThread()
        {
            while (ispd)
            {
                try
                {
                    if ((DateTime.Now - LastBalance).TotalMinutes > 1 && accesstoken != "" && accesstoken != null)
                    {
                        HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/1101/sites/1/me");
                        loginrequest.CookieContainer = new CookieContainer();
                        loginrequest.CookieContainer.Add(new System.Net.Cookie("token", accesstoken, "", "safedice.com"));
                        loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                        HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                        string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                        SafeDiceWalletInfo tmp2 = json.JsonDeserialize<SafeDiceWalletInfo>(sEmitResponse);
                        balance = tmp2.balance;
                        Parent.updateBalance((decimal)balance);
                    }

                    if (accesstoken != "" && accesstoken != null)
                    {
                        HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/chats/en_US");
                        //loginrequest.Accept = "application/json, text/plain, */*";
                        loginrequest.ContentType = " application/json;charset=utf-8";
                        loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                        loginrequest.CookieContainer = new CookieContainer();
                        loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                        HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                        string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                        SDChat[] sdchat = json.JsonDeserialize<SDChat[]>(sEmitResponse);

                        foreach (SDChat chat in sdchat)
                        {
                            if (chat.id > lastchat)
                            {
                                lastchat = chat.id;
                                ReceivedChatMessage(string.Format("{0:hh:mm} ({1}) <{2}> {3}", chat.Time, chat.username, chat.accountId, chat.message));
                            }
                        }
                    }
                }
                catch
                {

                }
                Thread.Sleep(1500);
            }
        }

        
        public override void Login(string Username, string Password)
        {
            Login(Username, Password, "");
        }
        string serverhash = "";
        string client = "";
        int wins = 0;
        int losses = 0;
        double wagered = 0;
        int nonce = 0;
        int UID = 0;
        public override void Login(string Username, string Password, string twofa)
        {
            try
            {
                HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/auth/local");
                if (Prox != null)
                    loginrequest.Proxy = Prox;
                loginrequest.Method = "POST";
                string post = "username=" + Username + "&password=" + Password + "&code=" + twofa;
                loginrequest.ContentLength = post.Length;
                loginrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                {

                    writer.Write(post);
                }
                HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                SafeDiceLogin tmp = json.JsonDeserialize<SafeDiceLogin>(sEmitResponse);
                accesstoken = tmp.token;
                if (accesstoken == "")
                    finishedlogin(false);
                else
                {
                    loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/me?token=" + accesstoken);
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                    loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                    EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                    SafeDicegetUserInfo tmp1 = json.JsonDeserialize<SafeDicegetUserInfo>(sEmitResponse);
                    loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/" + tmp1.id + "/sites/1/me");
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "", "safedice.com"));
                    loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                    EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                    SafeDiceWalletInfo tmp2 = json.JsonDeserialize<SafeDiceWalletInfo>(sEmitResponse);
                    Parent.updateBalance(tmp2.balance / 100000000m);
                    balance = tmp2.balance / 100000000.0;

                    Parent.updateBets(tmp2.win + tmp2.lose);
                    Parent.updateLosses(tmp2.lose);
                    wins = tmp2.win;
                    losses = tmp2.lose;
                    Parent.updateProfit((tmp2.amountWin - tmp2.amountLose) / 100000000.0);
                    profit = (tmp2.amountWin - tmp2.amountLose) / 100000000.0;
                    Parent.updateWagered(tmp2.wagered / 100000000.0);
                    wagered = tmp2.wagered / 100000000.0;
                    Parent.updateWins(tmp2.win);
                    Parent.updateStatus("Logged in");
                    serverhash = tmp1.serverSeedHash;
                    client = tmp1.accountSeed;
                    nonce = tmp1.nonce;
                    UID = tmp1.id;
                    Parent.updateDeposit(GetDepositAddress());
                    finishedlogin(true);
                }

            }
            catch (WebException e)
            {
                if (e.Response != null)
                {

                    string sEmitResponse = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    Parent.updateStatus(sEmitResponse);
                    if (e.Message.Contains("401"))
                    {
                        //System.Windows.Forms.MessageBox.Show("Could not log in. Please ensure the username, passowrd and 2fa code are all correct.");
                    }

                }
                finishedlogin(false);

            }
        }

        void PlaceBetThread(object High)
        {
            try
            {
                Parent.updateStatus(string.Format("Betting: {0:0.00000000} at {1:0.00000000} {2}", amount, chance, (bool)High ? "High" : "Low"));
                HttpWebRequest betrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/dicebets");
                if (Prox != null)
                    betrequest.Proxy = Prox;
                betrequest.Method = "POST";
                SafeDiceBet tmpBet = new SafeDiceBet
                {
                    siteId = 1,
                    amount = (int)(amount * 100000000),
                    payout = (double)(((int)((99.5 / chance) * 100000000)) / 100000000.0),
                    isFixedPayout = false,
                    isRollLow = !(bool)High,
                    target = ((bool)High) ? (999999 - ((int)(chance * 10000))).ToString() : ((int)(chance * 10000)).ToString()
                };
                string post = json.JsonSerializer<SafeDiceBet>(tmpBet);

                betrequest.ContentLength = post.Length;
                betrequest.ContentType = " application/json;charset=utf-8";
                betrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                betrequest.CookieContainer = new CookieContainer();
                using (var writer = new StreamWriter(betrequest.GetRequestStream()))
                {

                    writer.Write(post);
                    writer.Flush();
                    writer.Close();
                }
                string tmp = betrequest.ToString();
                HttpWebResponse EmitResponse = (HttpWebResponse)betrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                SafeDiceBetResult tmpResult = json.JsonDeserialize<SafeDiceBetResult>(sEmitResponse);
                Bet bet = new Bet();
                bet.Amount = (decimal)tmpResult.amount / 100000000m;
                bet.date = json.ToDateTime2(tmpResult.processTime);
                bet.Chance = (!tmpResult.isRollLow ? 100m - (decimal)tmpResult.target / 1000000m * 100m : (decimal)tmpResult.target / 1000000m * 100m);
                bet.high = !tmpResult.isRollLow;
                bet.clientseed = client;
                bet.Id = tmpResult.id;
                bet.nonce = nonce++;
                bet.Profit = tmpResult.profit / 100000000m;
                bet.Roll = tmpResult.roll / 10000;
                bet.serverhash = serverhash;
                bet.uid = tmpResult.accountId;
                balance += (double)bet.Profit;
                Parent.updateBalance((decimal)balance);
                Parent.updateBets(++bets);
                Parent.updateWagered(wagered += (double)bet.Amount);
                bool win = false;
                if (tmpResult.isRollLow && tmpResult.roll < tmpResult.target)
                    win = true;
                else if (!tmpResult.isRollLow && tmpResult.roll > tmpResult.target)
                    win = true;
                if (win)
                    Parent.updateWins(++wins);
                else
                    Parent.updateLosses(++losses);
                Parent.updateProfit(profit += (double)bet.Profit);
                Parent.AddBet(bet);
                Parent.GetBetResult(balance, bet);

            }
            catch (WebException e)
            {
                if (e.Response != null)
                {

                    string sEmitResponse = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    Parent.updateStatus(sEmitResponse);
                    System.Windows.Forms.MessageBox.Show("Error placing bet. Betting stopped");
                    /*if (e.Message.Contains("401"))
                    {
                        System.Windows.Forms.MessageBox.Show("Could not log in. Please ensure the username, passowrd and 2fa code are all correct.");
                    }*/

                }
            }

        }

        public override void PlaceBet(bool High)
        {
            Thread t = new Thread(new ParameterizedThreadStart(PlaceBetThread));
            t.Start(High);

        }


        public override void ResetSeed()
        {
            try
            {
                HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/randomizeseed");
                if (Prox != null)
                    loginrequest.Proxy = Prox;
                loginrequest.Method = "GET";

                loginrequest.Accept = "application/json, text/plain, */*";

                loginrequest.ContentType = " application/json;charset=utf-8";
                loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                loginrequest.CookieContainer = new CookieContainer();
                loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();

                SDRandomize tmp = json.JsonDeserialize<SDRandomize>(sEmitResponse);
                serverhash = tmp.serverSeedHash;
                nonce = 1;
            }
            catch
            {

            }

        }

        public override void SetClientSeed(string Seed)
        {
            throw new NotImplementedException();
        }

        public override string GetSiteProfitValue()
        {
            throw new NotImplementedException();
        }

        public override string GetTotalBets()
        {
            throw new NotImplementedException();
        }

        public override string GetMyProfit()
        {
            throw new NotImplementedException();
        }

        public override bool ReadyToBet()
        {
            return true;
        }

        public override void Disconnect()
        {
            ispd = false;
        }

        public override void GetSeed(long BetID)
        {
            throw new NotImplementedException();
        }
        void sendChatThread(object Message)
        {
            if (accesstoken != "" && accesstoken != null)
            {
                try
                {
                    HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/chats/en_US/create");
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.Method = "POST";

                    loginrequest.Accept = "application/json, text/plain, */*";

                    loginrequest.ContentType = " application/json;charset=utf-8";
                    loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                    string post = json.JsonSerializer<SDSendChat>(new SDSendChat { message = (string)Message });

                    using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                    {

                        writer.Write(post);
                        writer.Flush();
                        writer.Close();
                    }
                    HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                }
                catch
                {

                }
            }
        }

        public override void SendChatMessage(string Message)
        {
            Thread t = new Thread(new ParameterizedThreadStart(sendChatThread));
            t.Start(Message);

        }

        public override bool Withdraw(double Amount, string Address)
        {
            try
            {
                Parent.updateStatus(string.Format("Withdrawing {0:0.00000000} to {1}", Amount, Address));
                HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/" + UID + "/sites/1/withdraw");
                if (Prox != null)
                    loginrequest.Proxy = Prox;

                loginrequest.Method = "PUT";

                loginrequest.Accept = "application/json, text/plain, */*";

                loginrequest.ContentType = " application/json;charset=utf-8";
                loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                loginrequest.CookieContainer = new CookieContainer();
                loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                string post = json.JsonSerializer<SDSendWIthdraw>(new SDSendWIthdraw { amount = (int)(Amount * 100000000), address = Address });

                using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                {

                    writer.Write(post);
                    writer.Flush();
                    writer.Close();
                }
                HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();

                
                balance -= Amount;
                Parent.updateBalance(balance);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Register(string username, string password)
        {
            //System.Windows.Forms.MessageBox.Show("Registration is temporarily disabled for Safe Dice. Please use the site https://safedice.com to register and then log in using the bot.");
            //return false;
            try
            {
                HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts");
                if (Prox != null)
                    loginrequest.Proxy = Prox;
                loginrequest.Method = "POST";
                string post = "username=" + username + "&referralId=" + 1050;
                loginrequest.ContentLength = post.Length;
                loginrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                {

                    writer.Write(post);
                }
                HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                SafeDiceLogin tmp = json.JsonDeserialize<SafeDiceLogin>(sEmitResponse);
                accesstoken = tmp.token;
                if (accesstoken == "")
                    finishedlogin(false);
                else
                {
                    



                    loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/me?token=" + accesstoken);
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                    loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                    EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                    SafeDicegetUserInfo tmp1 = json.JsonDeserialize<SafeDicegetUserInfo>(sEmitResponse);
                    loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/" + tmp1.id + "/sites/1/me");
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "", "safedice.com"));
                    loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                    EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                    SafeDiceWalletInfo tmp2 = json.JsonDeserialize<SafeDiceWalletInfo>(sEmitResponse);



                    loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/userpass");
                    if (Prox != null)
                        loginrequest.Proxy = Prox;
                    loginrequest.ContentType = "application/json;charset=utf-8";
                    loginrequest.Method = "PUT";
                    loginrequest.CookieContainer = new CookieContainer();
                    loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                    loginrequest.Headers.Add("Authorization", "Bearer " + accesstoken);
                    post = json.JsonSerializer<SDSetPw>(new SDSetPw { username = username, password = password });
                    loginrequest.ContentLength = post.Length;
                    using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                    {

                        writer.Write(post);
                    }
                    EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                    sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();


                    Parent.updateBalance(tmp2.balance / 100000000m);
                    balance = tmp2.balance / 100000000.0;
                    
                    Parent.updateBets(tmp2.win + tmp2.lose);
                    Parent.updateLosses(tmp2.lose);
                    wins = tmp2.win;
                    losses = tmp2.lose;
                    Parent.updateProfit((tmp2.amountWin - tmp2.amountLose) / 100000000.0);
                    profit = (tmp2.amountWin - tmp2.amountLose) / 100000000.0;
                    Parent.updateWagered(tmp2.wagered / 100000000.0);
                    wagered = tmp2.wagered / 100000000.0;
                    Parent.updateWins(tmp2.win);
                    Parent.updateStatus("Logged in");
                    serverhash = tmp1.serverSeedHash;
                    client = tmp1.accountSeed;
                    nonce = tmp1.nonce;
                    UID = tmp1.id;
                    Parent.updateDeposit(GetDepositAddress());
                    finishedlogin(true);
                }

            }
            catch (WebException e)
            {
                if (e.Response != null)
                {

                    string sEmitResponse = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    //Parent.updateStatus(sEmitResponse);
                    if (e.Message.Contains("401"))
                    {
                        //System.Windows.Forms.MessageBox.Show("Could not log in. Please ensure the username, passowrd and 2fa code are all correct.");
                    }

                }
                finishedlogin(false);

            }
            return false;
        }

        public override double GetLucky(string server, string client, int nonce)
        {
            string comb = nonce + ":" + client + server + ":" + nonce;

            SHA512 betgenerator = SHA512.Create();


            int charstouse = 5;

            List<byte> buffer = new List<byte>();

            foreach (char c in comb)
            {
                buffer.Add(Convert.ToByte(c));
            }

            //compute first hash
            byte[] hash = betgenerator.ComputeHash(buffer.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);

            comb = hex.ToString();
            buffer = new List<byte>();

            //convert hash to new byte array
            foreach (char c in comb)
            {
                buffer.Add(Convert.ToByte(c));
            }

            hash = betgenerator.ComputeHash(buffer.ToArray());

            hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);

            for (int i = 0; i < hex.Length; i += charstouse)
            {

                string s = hex.ToString().Substring(i, charstouse);

                double lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (lucky < 1000000)
                    return lucky / 10000;
            }
            return 0;
        }

        public override bool Invest(double Amount)
        {
            try
            {
                Parent.updateStatus(string.Format("Investing {0:0.00000000}", Amount));
                HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/" + UID + "/sites/1/invest");
                if (Prox != null)
                    loginrequest.Proxy = Prox;

                loginrequest.Method = "POST";

                loginrequest.Accept = "application/json, text/plain, */*";

                loginrequest.ContentType = " application/json;charset=utf-8";
                loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
                loginrequest.CookieContainer = new CookieContainer();
                loginrequest.CookieContainer.Add(new Cookie("token", accesstoken, "/", "safedice.com"));
                string post = json.JsonSerializer<SDSendInvest>(new SDSendInvest { amount = (int)(Amount * 100000000) });

                using (var writer = new StreamWriter(loginrequest.GetRequestStream()))
                {

                    writer.Write(post);
                    writer.Flush();
                    writer.Close();
                }
                HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();

                
                balance -= Amount;
                Parent.updateBalance(balance);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static double sGetLucky(string server, string client, int nonce)
        {
            string comb = nonce + ":" + client + server + ":" + nonce;

            SHA512 betgenerator = SHA512.Create();


            int charstouse = 5;

            List<byte> buffer = new List<byte>();

            foreach (char c in comb)
            {
                buffer.Add(Convert.ToByte(c));
            }

            //compute first hash
            byte[] hash = betgenerator.ComputeHash(buffer.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);

            comb = hex.ToString();
            buffer = new List<byte>();

            //convert hash to new byte array
            foreach (char c in comb)
            {
                buffer.Add(Convert.ToByte(c));
            }

            hash = betgenerator.ComputeHash(buffer.ToArray());

            hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);

            for (int i = 0; i < hex.Length; i += charstouse)
            {

                string s = hex.ToString().Substring(i, charstouse);

                double lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (lucky < 1000000)
                    return lucky / 10000;
            }
            return 0;
        }

        string GetDepositAddress()
        {
            HttpWebRequest loginrequest = (HttpWebRequest)HttpWebRequest.Create("https://safedice.com/api/accounts/"+UID+"/sites/1/deposit");
            if (Prox != null)
                loginrequest.Proxy = Prox;
            loginrequest.Method = "GET";
            
            
            loginrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            loginrequest.Headers.Add("authorization", "Bearer " + accesstoken);
            
            HttpWebResponse EmitResponse = (HttpWebResponse)loginrequest.GetResponse();
            string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();

            return json.JsonDeserialize<SDDEpost>(sEmitResponse).address;
        }

        int lastchat = 0;

    }
    public class SDSendInvest
    {
        public double amount { get; set; }
    }
    public class SDChat
    {
        public int id { get; set; }
        public string target_id { get; set; }
        public string room { get; set; }
        public string target_username { get; set; }
        public string username { get; set; }
        public int role { get; set; }
        public string time { get; set; }
        public DateTime Time { get; set; }
        public string message { get; set; }
        public int accountId { get; set; }
        public string targetUsername { get; set; }
    }

    public class SafeDiceLogin
    {
        public string token { get; set; }
    }


    public class SafeDicegetUserInfo
    {
        public int id { get; set; }
        public string username { get; set; }
        public string authHashLink { get; set; }
        //public int referralId { get; set; }
        public string accountSeed { get; set; }
        public string serverSeedHash { get; set; }
        public int nonce { get; set; }
        public int role { get; set; }
        public bool isInvestmentEnabled { get; set; }

    }

    public class SafeDiceWalletInfo
    {
        public int balance { get; set; }
        public double shares { get; set; }
        public double kelly { get; set; }
        public int win { get; set; }
        public int lose { get; set; }
        public int amountLose { get; set; }
        public int amountWin { get; set; }
        public int wagered { get; set; }
        
    }
     public class SafeDiceBet
     {
         public int siteId { get; set; }
         public int amount { get; set; }
         public string target { get; set; }
         public double payout { get; set; }
         public bool isFixedPayout { get; set; }
         public bool isRollLow { get; set; }
     }
    public class SafeDiceBetResult
    {
        public int id { get; set; }
        public int accountId { get; set; }
        public string processTime { get; set; }
        public int amount { get; set; }
        public int profit { get; set; }
        public int roll { get; set; }
        public int target { get; set; }
        public bool isRollLow { get; set; }
        public double payout { get; set; }
    }

    public class SDRandomize
    {
        public string serverSeedHash { get; set; }
    }
    public class SDSendWIthdraw
    {
        public int amount { get; set; }
        public string address { get; set; }
    }
    public class SDSendChat
    {
        public string message { get; set; }
    }
    public class SDDiceBetCookie
    {
        public bool isRollLow { get; set; }
        public double pChance { get; set; }
        public bool isHotKeysEnabled { get; set; }
        public bool isFixedPayout { get; set; }
        public bool showAutoRoll { get; set; }
        public double autoRollLossMultiplier { get; set; }
    }
    public class SDSetPw
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    public class SDDEpost
    {
        public string address { get; set; }
    }
}
