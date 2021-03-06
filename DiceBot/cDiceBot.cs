﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using System.IO;
using System.Resources;
using System.Net;
using System.Media;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using SharpLua;
using WMPLib;
using System.Globalization;
using System.Reflection;

namespace DiceBot
{
    
    public partial class cDiceBot : Form
    {
        Control[] ControlsToDisable;
        private const string vers = "2.5.5";
        DateTime OpenTime = DateTime.UtcNow;
        Random r = new Random();
        Graph LiveGraph;
        Stats StatsWindows = new Stats();
        Simulate SimWindow;
        
        #region Variables
        Random rand = new Random();
        bool retriedbet = false;
        double StartBalance = 0;        
        double Lastbet = -1;
        double MinBet = 0;
        double Multiplier = 0;
        double WinMultiplier = 0;
        double Limit = 0;
        double Amount = 0;
        
        double LargestBet = 0;
        double LargestWin = 0;
        double LargestLoss = 0;
        double LowerLimit = 0;
        double Devider = 0;
        double WinDevider = 0;
        double Chance = 0;
        double avgloss = 0;
        double avgwin = 0;
        double avgstreak = 0;
        double currentprofit = 0;
        double profit = 0;
        double luck = 0;
        int numwinstreasks = 0;
        int numlosesreaks = 0;
        int numstreaks = 0;
        int Wins = 0;
        int Losses = 0;
        int Winstreak = 0;
        int BestStreak = 0;
        int WorstStreak = 0;
        int BestStreak2 = 0;
        int WorstStreak2 = 0;
        int BestStreak3 = 0;
        int WorstStreak3 = 0;
        int Losestreak = 0;
        int timecounter = 0;
        
        int iMultiplyCounter = 0;
        int MaxMultiplies = 0;
        int WinMaxMultiplies = 0;
        int Devidecounter = 0;
        int WinDevidecounter = 0;
        int SoundStreakCount = 15;
        int restartcounter = 0;
        
        int laststreaklose = 0;
        int laststreakwin = 0;
        int Currency = 0;
        bool stop = true;
        bool withdraw = false;
        bool invest = false;
        bool reset = false;
        bool running = false;
        bool stoponwin = false;
        bool loggedin = false;
        public bool tray = false;
        public bool Sound = true;
        public bool SoundWithdraw=true;
        public bool SoundLow = true;
        public bool SoundStreak = false;
        public bool autologin = false;
        public bool autostart = false;
        bool high = true;
        bool starthigh = true;
        private bool withdrew;
        DateTime dtStarted = new DateTime();
        DateTime dtLastBet = new DateTime();
        TimeSpan TotalTime = new TimeSpan(0, 0, 0);
        public string username = "";
        public string password = "";        
        public string Botname = "";
        public Email Emails { get; set; }
        Simulation lastsim;
        string ching = "";
        string salarm = "";

        //labouchere
        List<double> LabList = new List<double>();
        #endregion

        DiceSite CurrentSite;
        private double dPreviousBalance;
        delegate void dpopFibonacci();
        void populateFiboNacci()
        {
            if (InvokeRequired)
            {
                Invoke(new dpopFibonacci(populateFiboNacci));
                return;
            }
            else
            {
            decimal Previous = 0;
            decimal Current = (decimal)MinBet ;
            lstFibonacci.Items.Clear();
            for (int i =0; i<100; i++)
            {
                lstFibonacci.Items.Add(string.Format("{0}. {1}", i, Current));
                decimal tmp = Current;
                Current += Previous;
                Previous = tmp;
            }
            }
        }

        public double PreviousBalance
        {
            get { return dPreviousBalance; }
            set 
            {
               
                dPreviousBalance = value; 
            }
        }

        delegate void dDobet(Bet bet);
        public void GetBetResult(double Balance, Bet bet)
        {
            PreviousBalance = (double)Balance;
            profit += (double)bet.Profit;
            if (!RunningSimulation)
            {
                AddChartPoint(profit);
            }
            if (InvokeRequired)
            {
                Invoke(new dDobet(DoBet),bet);
            }
            else
                DoBet(bet);
            
            
            
        }

        delegate void dAddChartPoint(double Profit);
        void AddChartPoint(double Profit)
        {
            if (InvokeRequired)
            {
                Invoke(new dAddChartPoint(AddChartPoint), profit);
            }
            else
            {
                if (chrtEmbeddedLiveChart.Enabled)
                chrtEmbeddedLiveChart.Series[0].Points.AddY(Profit);
            }
        }

        void EnableNotLoggedInControls(bool Enabled)
        { 
            foreach (Control c in ControlsToDisable)
            {
                c.Enabled = Enabled;
            }
            if (Enabled)
            { 
                btnRegister.Enabled = false;
                btnLogIn.Text = "Logout";
            }
            else
            {
                btnRegister.Enabled = true;
                btnLogIn.Text = "Log In";
            }
        }
        
        public cDiceBot()
        {

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture =  new CultureInfo("en-US");
            sqlite_helper.CheckDBS();
            InitializeComponent(); 
            PopoutChat.SendMessage += PopoutChat_SendMessage;
            SimWindow = new Simulate(this);
            StatsWindows.btnResetStats.Click += btnResetStats_Click;
            
            ControlsToDisable = new Control[] { btnApiBetHigh, btnApiBetLow, btnWithdraw, btnInvest, btnTip, btnStartHigh, btnStartLow, btnStartHigh2, btnStartLow2 };
            EnableNotLoggedInControls(false);
            basicToolStripMenuItem.Checked = true;
            chrtEmbeddedLiveChart.Series[0].Points.AddXY(0, 0);
            chrtEmbeddedLiveChart.ChartAreas[0].AxisX.Minimum = 0;
            #region tooltip Texts
            ToolTip tt = new ToolTip();
            tt.SetToolTip(gbZigZag , "After every n bets/wins/losses \n(as specified to the right), \nthe bot will switch from \nbetting high to low or vica verca");
            tt.SetToolTip(lblLowLimit,
                "When your balance goes below this\n" +
                "value, the bot will stop playing.\n" +
                "actions specified below");

            tt.SetToolTip(lblLimit,
                    "When your balance reaches this\n"+
                    "value, the bot will do one of \n" +
                    "the actions specified below.");
                    
                tt.SetToolTip(lblLowLimit,
                "When your balance goes below this\n" +
                "value, the bot will stop playing.\n" +
                "actions specified below");
                tt.SetToolTip( lblAction,
                "The selected action will occur when\n" +
                "your balance goes above the limit as\n" +
                "specified above"); 
                tt.SetToolTip( lblAmount,
                "The amount that will be invested\n" +
                "or deposited when the limit is reached"); 
                tt.SetToolTip( lblAddress,
                "Btc Address that the funds get" +
                "withdrawn to\n"); 
                tt.SetToolTip( lblMinBet,
                "This is the first bet to be placed,\n" +
                "upon win, bet will reset to this value\n"); 
                tt.SetToolTip( lblChance,
                "Chance of winning, as entered into \n" +
                "the site\n");
                tt.SetToolTip( lblMultiplier,
                "Upon a loss, the bet will be \n" +
                "multiplied by this value. See\n" +
                "Max multiplies and After as well"); 
            tt.SetToolTip( lblMaxMultiplier,
                "In a losing streak, the bet will\n" +
                "will be multiplied untill the streak\n" +
                "reaches "+nudMaxMultiplies.Value.ToString("0.00000") +" bets. The following bets\n"+
                "will be with the same amount"); 
                tt.SetToolTip( lblAfter,
                "with every " + nudNbets.Value + " losses in a row,\n" +
                "the muliplier will be multiplied with\n" +
                nudDevider.Value.ToString("0.00000")+". The idea is to decrease the size\n"+
                "the multiplier, keep the value between\n"+
                "0.9 and 0.5. Minimum Multiplier is 1"); 
                tt.SetToolTip( lblAfter2,
            "with every " + nudNbets.Value.ToString() + " losses in a row,\n" +
            "the muliplier will be multiplied with\n" +
            nudDevider.Value.ToString("0.00000") + ". The idea is to decrease the size\n" +
            "the multiplier, keep the value between\n" +
            "0.9 and 0.5. Minimum Multiplier is 1"); 
                tt.SetToolTip( lblDevider,
            "with every " + nudNbets.Value.ToString() + " losses in a row,\n" +
            "the muliplier will be multiplied with\n" +
            nudDevider.Value.ToString("0.00000") + ". The idea is to decrease the size\n" +
            "the multiplier, keep the value between\n" +
            "0.9 and 0.5. Minimum Multiplier is 1"); 
                


            #endregion
                primeDiceToolStripMenuItem.Checked = true;

                bool frst = true;
            foreach (string s in dice999.cCurrencies)
            {
                ToolStripMenuItem tmpItem = new ToolStripMenuItem{ Text=s};

                if (frst)
                {
                    tmpItem.Checked = true;
                    frst = false;
                }

                diceToolStripMenuItem.DropDown.Items.Add(tmpItem);
                tmpItem.Click += btcToolStripMenuItem_Click;
                
                tmpItem.CheckedChanged += btcToolStripMenuItem_CheckedChanged;
                
            }
            if (!File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings"))
            {
                if (MessageBox.Show("Dice Bot has detected that there are no default settings saved on this computer."+
                    "If this is the first time you are running Dice Bot, it is highly recommended you see the begginners guide"+
                    "\n\nGo to Beginners Guide now?", "Warning", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    Process.Start("https://bitcointalk.org/index.php?topic=391870");
                }
                try
                {

                    Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2");
                }
                catch
                {

                }
            }
            else
            {
                if (Emails == null)
                    Emails = new Email("", "");
                load();
                loadsettings();
                
            }
            
            tmStop.Enabled = true;
            
            Thread tGetVers = new Thread(new ThreadStart(getversion));
            tGetVers.Start();
            populateFiboNacci();
            
            if (autologin)
            {
                CurrentSite.Login(username, password , txtApi2fa.Text);
                
            }
            Lua.RegisterFunction("withdraw",this, new dWithdraw(luaWithdraw).Method);
            Lua.RegisterFunction("invest", this, new dInvest(luainvest).Method);
            Lua.RegisterFunction("tip", this, new dtip(luatip).Method);
            Lua.RegisterFunction("stop", this, new dStop(Stop).Method);
            Lua.RegisterFunction("resetseed", this, new dResetSeed(luaResetSeed).Method);
            Lua.RegisterFunction("print", this, new dWriteConsole(WriteConsole).Method);

            Lua.RegisterFunction("runsim",this, new dRunsim(runsim).Method);
        }
        delegate void dRunsim(double startingabalance, int bets);
        void runsim(double startingbalance, int bets)
        {
            if (stop)
            {
                LuaRuntime.SetLua(Lua);
                GetLuaVars();
                LuaRuntime.Run(richTextBox3.Text);
                SimWindow.nudSimBalance.Value = (decimal)startingbalance;
                SimWindow.nudSimNumBets.Value = (decimal)bets;
               WriteConsole("Running " + bets + " bets Simulation with starting balance of " + startingbalance);
               btnSim_Click(SimWindow.btnSim, new EventArgs());
            }
            else
            {
                WriteConsole("Bot currently betting. Please stop betting before running simulation.");
            }
        }

        void luaResetSeed()
        {
            WriteConsole("Resetting Seed!");
            if (CurrentSite.ChangeSeed)
                CurrentSite.ResetSeed();
        }
        void luaWithdraw(double amount, string address)
        {
            WriteConsole("Withdrawing " +amount + " to " + address);
            if (CurrentSite.AutoWithdraw)
                CurrentSite.Withdraw(amount, address);
        }

        void luainvest(double amount)
        {
            WriteConsole("investing " + amount);
            if (CurrentSite.AutoInvest)
                CurrentSite.Invest(amount);
        }
        void luatip(string username, double amount)
        {
            WriteConsole("Tipping "+ amount + " to "+username);
            if (CurrentSite.Tip)
                CurrentSite.SendTip(username, amount);
        }
        void CurrentSite_FinishedLogin(bool LoggedIn)
        {
            if (InvokeRequired)
                Invoke(new DiceSite.dFinishedLogin(CurrentSite_FinishedLogin), LoggedIn);
            else
            {
                if (LoggedIn)
                {
                    txtApiPassword.Text = "";
                    EnableNotLoggedInControls(true);
                    MessageBox.Show("Successfully Logged in or registered.");
                    updateStatus("Logged in.");
                }
                else
                {
                    MessageBox.Show("Failed to log in or register new account!", "Failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    updateStatus("Disconnected");
                }

            }
        }

        

        //check if the current version of the bot is the latest version available
        void getversion()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://seuntjie.com/Dicebot/vs.html");
                HttpWebResponse EmitResponse = (HttpWebResponse)request.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                string[] ss = sEmitResponse.Split('|');
                if (ss[0]!=vers)
                {
                    string newfeatures = ss.Length>1?"New features include: "+ss[1]:"";
                    if (MessageBox.Show("A new version of DiceBot is available. "+newfeatures+" \n\nDo you want to go to the download page now?","Update Available", MessageBoxButtons.YesNo)== System.Windows.Forms.DialogResult.Yes)
                    {
                        Process.Start("http://sourceforge.net/projects/seuntjiejddb");
                    }
                }
            }
            catch
            {

            }
        }

               
        private void Form1_Load(object sender, EventArgs e)
        {
            
            testInputs();
        }

        //Statistics
        //includes - 
        //updatestats();
        //maxbets();//start
        //maxbets();//recursive
        //btnStreakTable_Click()
        #region Statistics
        delegate void dUpdateStats();
        private void UpdateStats()
        {
            
            if (InvokeRequired)
            {
                Invoke(new dUpdateStats(UpdateStats));
                return;
            }
            else
            {
                lblLosses2.Text = Losses.ToString();
                lblLosses2.Text = Losses.ToString();
                lblProfit2.Text = profit.ToString("0.00000000");
                if (Winstreak == 0)
                {
                    lblCustreak2.Text = Losestreak.ToString();
                    lblCustreak2.ForeColor =  Color.Red;
                }
                else
                {
                    lblCustreak2.Text =  Winstreak.ToString();
                    lblCustreak2.ForeColor =  Color.Green;

                }
                lblWins2.Text = Wins.ToString();
                if (StatsWindows!= null)
                {
                    if (!StatsWindows.IsDisposed)
                    {
                        StatsWindows.lblLoseStreak.Text = WorstStreak.ToString();
                        lblLosses2.Text = StatsWindows.lblLosses.Text = Losses.ToString();


                        lblProfit2.Text = StatsWindows.lblProfit.Text = profit.ToString("0.00000000");
                        StatsWindows.lblBalance.Text = PreviousBalance.ToString("0.00000000");
                        if (profit < 0)
                        {
                            StatsWindows.lblProfit.ForeColor = Color.Red;

                        }
                        else
                        {
                            StatsWindows.lblProfit.ForeColor = Color.Green;

                        }
                        if (profit > 0.001)
                        {
                            donateToolStripMenuItem.ForeColor = Color.Green;
                            donateToolStripMenuItem.BackColor = Color.LightBlue;
                        }
                        if (Winstreak == 0)
                        {
                            lblCustreak2.Text = StatsWindows.lblCustreak.Text = Losestreak.ToString();
                            lblCustreak2.ForeColor = StatsWindows.lblCustreak.ForeColor = Color.Red;
                        }
                        else
                        {
                            lblCustreak2.Text = StatsWindows.lblCustreak.Text = Winstreak.ToString();
                            lblCustreak2.ForeColor = StatsWindows.lblCustreak.ForeColor = Color.Green;

                        }

                        lblWins2.Text = StatsWindows.lblWins.Text = Wins.ToString();
                        StatsWindows.lblWinStreak.Text = BestStreak.ToString();
                        TimeSpan curtime = DateTime.Now - dtStarted;
                        lblBets2.Text = StatsWindows.lblBets.Text = (Wins + Losses).ToString();
                        decimal profpB = 0;
                        if (Wins + Losses > 0)
                            profpB = (decimal)profit / (decimal)(Wins + Losses);
                        decimal betsps = 0;

                        if (curtime.TotalSeconds > 0.0)
                            betsps = (decimal)(Wins + Losses) / (decimal)(curtime.TotalSeconds);
                        decimal profph = 0;
                        if (profpB > 0 && betsps > 0)
                            profph = (profpB / betsps) * (decimal)60.0 * (decimal)60.0;
                        StatsWindows.lblProfpb.Text = profpB.ToString("0.00000000");
                        StatsWindows.lblProfitph.Text = (profpB * (decimal)60.0 * (decimal)60.0).ToString("0.00000000");
                        StatsWindows.lblProfit24.Text = (profpB * (decimal)60.0 * (decimal)60.0 * (decimal)24.0).ToString("0.00000000");

                        int imaxbets = maxbets();
                        if (imaxbets == -500)
                            StatsWindows.lblMaxBets.Text = "500+";
                        else
                            StatsWindows.lblMaxBets.Text = imaxbets.ToString();
                        if (imaxbets > 20)
                        {
                            StatsWindows.lblMaxBets.ForeColor = Color.Blue;
                        }
                        else if (imaxbets > 15)
                        {
                            StatsWindows.lblMaxBets.ForeColor = Color.Green;
                        }
                        else if (imaxbets > 10)
                        {
                            StatsWindows.lblMaxBets.ForeColor = Color.DarkOrange;
                        }
                        else
                        {
                            StatsWindows.lblMaxBets.ForeColor = Color.Red;
                        }
                        StatsWindows.lblAvgWinStreak.Text = avgwin.ToString("0.000000");
                        StatsWindows.lblAvgLoseStreak.Text = avgloss.ToString("0.000000");
                        StatsWindows.lblAvgStreak.Text = avgstreak.ToString("0.000000");
                        if (avgstreak > 0)
                            StatsWindows.lblAvgStreak.ForeColor = Color.Green;
                        else StatsWindows.lblAvgStreak.ForeColor = Color.Red;
                        StatsWindows.lbl3Best.Text = BestStreak.ToString() + "\n" + BestStreak2.ToString() + "\n" + BestStreak3.ToString();
                        StatsWindows.lbl3Worst.Text = WorstStreak.ToString() + "\n" + WorstStreak2.ToString() + "\n" + WorstStreak3.ToString();
                        StatsWindows.lblLastStreakLose.Text = laststreaklose.ToString();
                        StatsWindows.lblLastStreakWin.Text = laststreakwin.ToString();
                        StatsWindows.lblLargestBet.Text = LargestBet.ToString("0.00000000");
                        StatsWindows.lblLargestLoss.Text = LargestLoss.ToString("0.00000000");
                        StatsWindows.lblLargestWin.Text = LargestWin.ToString("0.00000000");
                        if (Losses != 0)
                        {
                            StatsWindows.lblLuck.Text = luck.ToString("00.00") + "%";
                        }
                    }
                }

            }
        }

        int maxbets()//Start
        {
            if (PreviousBalance != 0)
            {
                if (rdbConstant.Checked)
                    return maxbetsconstant();
                else if (rdbDevider.Checked)
                    return maxbetsVariable();
                else if (rdbMaxMultiplier.Checked)
                    return maxbetsMaxMultiplies();
                else if (rdbReduce.Checked) 
                return maxbetsChangeOnce();
            }
            return 0;
        }
        bool convert = false;
        int maxbetsconstant()
        {
            double total = 0;
            int bets = 0;
            double curbet = MinBet;
            
            double Multiplier = (double)(nudMultiplier.Value);

            while (total < PreviousBalance)
            {
                if (bets > 0)
                {
                    curbet *= Multiplier;
                }
                if (bets == nudChangeLoseStreak.Value && chkChangeLoseStreak.Checked)
                {
                    curbet = (double)nudChangeLoseStreakTo.Value;
                }
                bets++;
                total += curbet;
                if (bets > 500)
                    return -500;  
            }
            return bets;
        }

        int maxbetsVariable()
        {
            double total = 0;
            int bets = 0;
            double curbet = MinBet;
            int n = Devidecounter;
            double dmultiplier = (double)(nudMultiplier.Value);
            while (total < PreviousBalance)
            {
                if (bets > 0)
                {
                    if (bets % Devidecounter == 0)
                        dmultiplier *= Devider;

                    curbet *= dmultiplier;
                }
                if (bets == nudChangeLoseStreak.Value && chkChangeLoseStreak.Checked)
                {
                    curbet = (double)nudChangeLoseStreakTo.Value;
                }
                bets++;
                total += curbet;
                if (bets > 500)
                    return -500;
            }
            return bets;
        }

        int maxbetsMaxMultiplies()
        {
            double total = 0;
            int bets = 0;
            double curbet = MinBet;
            int n = Devidecounter;
            double dmultiplier = (double)(nudMultiplier.Value);
            while (total < PreviousBalance)
            {
                if (bets > 0)
                {
                    if (bets > MaxMultiplies)
                        dmultiplier = 1;

                    curbet *= dmultiplier;
                }
                if (bets == nudChangeLoseStreak.Value && chkChangeLoseStreak.Checked)
                {
                    curbet = (double)nudChangeLoseStreakTo.Value;
                }
                bets++;
                total += curbet;
                if (bets > 500)
                    return -500;
            }
            return bets;
        }

        int maxbetsChangeOnce()
        {
            double total = 0;
            int bets = 0;
            double curbet = MinBet;
            int n = Devidecounter;
            double dmultiplier = (double)(nudMultiplier.Value);
            while (total < PreviousBalance)
            {
                if (bets > 0)
                {
                    if (bets == Devidecounter)
                        dmultiplier *= Devider;

                    curbet *= dmultiplier;
                }
                if (bets == nudChangeLoseStreak.Value && chkChangeLoseStreak.Checked)
                {
                    curbet = (double)nudChangeLoseStreakTo.Value;
                }
                bets++;
                total += curbet;
                if (bets > 500)
                    return -500;
            }
            return bets;
        }
                   

        private void btnStreakTable_Click(object sender, EventArgs e)
        {
            int mode = 0;
            if (rdbConstant.Checked)
                mode = 0;
            else if (rdbDevider.Checked)
                mode = 1;
            else if (rdbMaxMultiplier.Checked)
                mode = 2;
            else if (rdbReduce.Checked)
                mode = 3;
            new StreakTable(MinBet, Multiplier, Devider, Devidecounter, MaxMultiplies, mode, Chance).Show();
        }

        private void CalculateLuck(bool win)
        {
            decimal lucktotal = (decimal)luck * (decimal)((Wins + Losses) - 1);
            if (win)
                lucktotal += (decimal)((decimal)100 / (decimal)Chance)*(decimal)100;
            double tmp = (double)(lucktotal / (decimal)(Wins + Losses));
            luck = tmp;
        }
        #endregion
         
        //Core Program
        //includes -
        //Stop()
        //Getbalance()
        //Withdraw()
        //Invest()
        //Start()
        //tmBetting_Tick()
        //dobet()
        //placebet()
        //tmstop_tick
        #region Core Program

        private void Stop()
        {
            //tmBetting.Enabled = false;
            WriteConsole("Betting Stopped!");
            double dBalance = CurrentSite.GetbalanceValue();
            stop = true;
            TotalTime += (DateTime.Now - dtStarted);
            if (RunningSimulation)
            {
                WriteConsole(string.Format("Simulation finished. Bets:{0} Wins:{1} Losses:{2} Balance:{3} Profit:{4} Worst Streak:{5} Best Streak:{6}", 
                    Losses+Wins, Wins, Losses, PreviousBalance, profit, WorstStreak, BestStreak ));
                Updatetext(SimWindow.lblSimLosses, Losses.ToString());
                Updatetext(SimWindow.lblSimProfit, profit.ToString("0.00000000"));
                Updatetext(SimWindow.lblSimWins, Wins.ToString());
                Updatetext(SimWindow.lblSimEndBalance, PreviousBalance.ToString("0.00000000"));
                Updatetext(SimWindow.lblSimLoseStreak, WorstStreak.ToString());
                Updatetext(SimWindow.lblSimWinStreak, BestStreak.ToString());
                using (StreamWriter sw = File.AppendText(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim"))
                {
                    foreach (string tmpbet in tempsim.bets)
                    {
                        sw.WriteLine(tmpbet);
                    }
                    tempsim.bets.Clear();
                }
                //return tempsim;
                //RunningSimulation = false;
                PreviousBalance=tmpbalance;
                Wins=  tmpwins ;
                Losses = tmplosses;
                StartBalance = tmpStartBalance ;
            }
        }

      private void Reset()
        {
          if (rdbMartingale.Checked)
          {
              Lastbet = MinBet;
          }
          else if (rdbLabEnable.Checked)
          {
              string[] ss = GetLabList();
              LabList = new List<double>();
              foreach (string s in ss)
              {
                  LabList.Add(dparse(s, ref convert));
              }
              if (LabList.Count == 1)
                  Lastbet = LabList[0];
              else if (LabList.Count > 1)
                  Lastbet = LabList[0] + LabList[LabList.Count - 1];
          }
          else if (rdbFibonacci.Checked)
          {
              FibonacciLevel = 0;
              Lastbet = double.Parse(lstFibonacci.Items[FibonacciLevel].ToString().Substring(lstFibonacci.Items[FibonacciLevel].ToString().IndexOf(" ") + 1));
          }
          else if (rdbAlembert.Checked)
          {
              Lastbet = MinBet;
          }
          else if (rdbPreset.Checked)
          {
              presetLevel = 0;
              double Betval = -1;
              if (presetLevel < rtbPresetList.Lines.Length)
              {
                  if (double.TryParse(rtbPresetList.Lines[presetLevel], out Betval))
                  {
                      Lastbet = Betval;
                  }
                  else
                  {
                      Stop();
                      MessageBox.Show("Invalid bet in list. Please make sure there is only one bet per line and no other charachters or letters in the list.");
                  }
              }
          }
        }
        
        void PlaceBet()
        {
            try
            {
                
                CurrentSite.amount=(Lastbet);
                
                if (!CurrentSite.ReadyToBet())
                    return;

                CurrentSite.chance = Chance;
                CurrentSite.PlaceBet(high);
                    
                dtLastBet = DateTime.Now;
                EnableTimer(tmBet, false);
                
            }
            catch
            {

            }
        }

        void Withdraw()
        {
            
            if (CurrentSite.AutoWithdraw)
                if (CurrentSite.Withdraw((double)(nudAmount.Value), txtTo.Text))
                {

                    
                    TrayIcon.BalloonTipText = "Withdraw " + nudAmount.Value + " Complete\nRestarting Bets";
                    TrayIcon.ShowBalloonTip(1000);
                    try
                    {
                        if (Sound && SoundWithdraw)
                        {
                            if (ching == "")
                            {
                                (new SoundPlayer(@"media\withdraw.wav")).Play();
                            }
                            else
                            {
                                if (ching.Substring(ching.LastIndexOf(".")).ToLower() == "mp3")
                                {
                                    WindowsMediaPlayer player = new WindowsMediaPlayer();
                                    player.URL = ching;
                                    player.controls.play();
                                }
                                else
                                {
                                    (new SoundPlayer(ching)).Play();
                                }
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Failed to play CHING, pelase make sure file exists");
                    }
                    
                    Emails.SendWithdraw(Amount, PreviousBalance - Amount, txtTo.Text);
                    StartBalance -= Amount;
                    //Start(true);
                }
        }

        void Invest()
        {

            if (CurrentSite.AutoInvest)
            {
                if (CurrentSite.Invest((double)(nudAmount.Value)))
                {
                    //invest = false;
                    TrayIcon.BalloonTipText = "Invest " + nudAmount.Value + "Complete\nRestarting Bets";
                    TrayIcon.ShowBalloonTip(1000);
                    try
                    {
                        if (Sound && SoundWithdraw)
                        {
                            if (ching == "")
                            {
                                (new SoundPlayer(@"media\withdraw.wav")).Play();
                            }
                            else
                            {
                                if (ching.Substring(ching.LastIndexOf(".")).ToLower() == "mp3")
                                {
                                    WindowsMediaPlayer player = new WindowsMediaPlayer();
                                    player.URL = ching;
                                    player.controls.play();
                                }
                                else
                                {
                                    (new SoundPlayer(ching)).Play();
                                }
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Failed to play CHING, pelase make sure file exists");
                    }
                    
                    Emails.SendInvest(Amount, CurrentSite.GetbalanceValue(), dparse("-0", ref convert));
                    StartBalance -= Amount;
                    
                }

            }
        }

        void ResetSeed()
        {
            if (CurrentSite.ChangeSeed)
            {

                CurrentSite.ResetSeed();

            }
        }

        void Start(bool Continue)
        {
            if (!Continue)
            {
                Winstreak = 0;
                Losestreak = 0;
                save();

                stoponwin = false;
                if (!programmerToolStripMenuItem.Checked)
                Chance = (double)nudChance.Value;
                CurrentSite.chance =(Chance);

                dtStarted = DateTime.Now;
            }
            if (testInputs())
            {
                stop = false;
                if (rdbLabEnable.Checked)
                {
                    LabList = new List<double>();
                    string[] lines = GetLabList();
                    foreach (string s in lines)
                    {
                        LabList.Add(dparse(s,ref convert));
                    }
                }
                if (!Continue)
                {
                    if (!programmerToolStripMenuItem.Checked)
                    {
                        Lastbet = MinBet;
                        if (!programmerToolStripMenuItem.Checked)
                        Chance = (double)nudChance.Value;
                    }
                    else
                    {
                        if (Lastbet <0)
                        {
                            WriteConsole("Please set starting bet using nextbet = x.xxxxxxxx");
                        }
                        if (Chance==0)
                        {

                            WriteConsole("Please set starting chance using chance = yy.yyyy");
                        }
                    }
                    if (rdbLabEnable.Checked)
                    {
                        if (LabList.Count > 0)
                        {
                            if (LabList.Count == 1)
                                Lastbet = LabList[0];
                            else
                                Lastbet = LabList[0] + LabList[LabList.Count - 1];
                        }
                        else
                        {
                            MessageBox.Show("Please enter a list of bets into the bet box on the labouchere tab, 1 bet per line.");
                            Stop();
                            return;
                        }
                    }
                    if (nudMutawaMultiplier.Value != 0)
                    {
                        mutawaprev = (double)nudChangeWinStreakTo.Value / (double)nudMutawaMultiplier.Value;
                    }
                }
                if (RunningSimulation)
                {
                    setInterval(tmBet, 1);
                    Simbet();
                }
                else
                {
                    setInterval(tmBet, 10);
                    EnableTimer(tmBet, true);
                }
            }
        }


        private void tmBetting_Tick(object sender, EventArgs e)
        {
            
            if (!RunningSimulation)
            {
                double dBalance = PreviousBalance;
                if (CurrentSite != null)
                dBalance = CurrentSite.GetbalanceValue();
                if ((dBalance != PreviousBalance && convert || withdrew) && dBalance > 0)
                {
                    if (PreviousBalance == 0)
                        StartBalance = dBalance;
                    PreviousBalance = dBalance;

                    try
                    {
                        string bets = "";
                        double dbets = 0;
                        string myprofit = "";
                        double dprof = 0;
                        bets = CurrentSite.GetTotalBets().Replace(",", "");
                        dbets = dparse(bets, ref convert);
                        myprofit = CurrentSite.GetMyProfit().Replace(",", "");
                        dprof = dparse(myprofit, ref convert);

                      
                    }
                    catch
                    {

                    }

                }
                else if (dBalance == PreviousBalance && convert || withdrew)
                {
                    if ((DateTime.Now - dtLastBet).TotalSeconds > 30 && !stop)
                    {
                        if (!retriedbet)
                        {
                            retriedbet = true;
                            EnableTimer(tmBet, true);

                        }
                    }
                    if ((DateTime.Now - dtLastBet).TotalSeconds > 120 && !stop)
                    {

                        
                            dtLastBet = DateTime.Now;
                            restartcounter = 0;
                        

                    }
                    
                    if (restartcounter > 50 && restartcounter < 51 && !stop)
                    {
                        Start(true);
                        restartcounter++;
                    }
                }
                if (withdraw)
                {
                    Withdraw();
                }
                if (invest)
                {
                    Invest();
                }
                if (reset)
                {
                    ResetSeed();
                }

            }
        }

        double mutawaprev = 0;
        bool trazelmultiply = false;
        int trazelwin = 0;
        
        void Labouchere(bool Win)
        {
            if (Win)
            {
                if (rdbLabEnable.Checked)
                {
                    if (chkReverseLab.Checked)
                    {
                        if (LabList.Count == 1)
                            LabList.Add(LabList[0]);
                        else
                            LabList.Add(LabList[0] + LabList[LabList.Count - 1]);
                    }
                    else if (LabList.Count > 1)
                    {
                        LabList.RemoveAt(0);
                        LabList.RemoveAt(LabList.Count - 1);
                        if (LabList.Count == 0)
                        {
                            if (rdbLabStop.Checked)
                                Stop();
                            else
                            {
                                Reset();
                            }
                        }

                    }
                    else
                    {
                        if (rdbLabStop.Checked)
                            Stop();
                        else
                        {
                            string[] ss = GetLabList();
                            LabList = new List<double>();
                            foreach (string s in ss)
                            {
                                LabList.Add(dparse(s, ref convert));
                            }
                            if (LabList.Count == 1)
                                Lastbet = LabList[0];
                            else if (LabList.Count > 1)
                                Lastbet = LabList[0] + LabList[LabList.Count - 1];
                        }
                    }
                }

                
            }
            else
            {
                //do laboucghere logic
                if (rdbLabEnable.Checked)
                {
                    if (!chkReverseLab.Checked)
                    {
                        if (LabList.Count == 1)
                            LabList.Add(LabList[0]);
                        else
                            LabList.Add(LabList[0] + LabList[LabList.Count - 1]);
                    }
                    else
                    {
                        if (LabList.Count > 1)
                        {
                            LabList.RemoveAt(0);
                            LabList.RemoveAt(LabList.Count - 1);
                            if (LabList.Count == 0)
                                Stop();
                        }
                        else
                        {
                            if (rdbLabStop.Checked)
                                Stop();
                            else
                            {
                                string[] ss = GetLabList();
                                LabList = new List<double>();
                                foreach (string s in ss)
                                {
                                    LabList.Add(dparse(s, ref convert));
                                }
                                if (LabList.Count == 1)
                                    Lastbet = LabList[0];
                                else if (LabList.Count > 1)
                                    Lastbet = LabList[0] + LabList[LabList.Count - 1];
                            }
                        }
                    }
                }


                //end labouchere logic
            }
            
                if (LabList.Count == 1)
                    Lastbet = LabList[0];
                else if (LabList.Count > 1)
                    Lastbet = LabList[0] + LabList[LabList.Count - 1];
                else
                {
                    if (rdbLabStop.Checked)
                        Stop();
                    else
                    {
                        string[] ss = GetLabList();
                        LabList = new List<double>();
                        foreach (string s in ss)
                        {
                            LabList.Add(dparse(s, ref convert));
                        }
                        if (LabList.Count == 1)
                            Lastbet = LabList[0];
                        else if (LabList.Count > 1)
                            Lastbet = LabList[0] + LabList[LabList.Count - 1];
                    }

                
            }

        }

        void martingale(bool Win)
        {
            if (Win)
            {
                if (rdbWinMaxMultiplier.Checked && Winstreak >= WinMaxMultiplies)
                {
                    WinMultiplier = 1;
                }
                else if (rdbWinDevider.Checked && Winstreak % WinDevidecounter == 1 && Winstreak > 0)
                {
                    WinMultiplier *= WinDevider;
                }
                else if (rdbWinReduce.Checked && Winstreak == WinDevidecounter && Winstreak > 0)
                {
                    WinMultiplier *= WinDevider;
                }
                Lastbet *= WinMultiplier;
                if (Winstreak == 1)
                {
                    if(chkFirstResetWin.Checked && !chkMK.Checked)
                    {
                        Lastbet = MinBet;
                    }
                    try
                    {
                        Chance = (double)(nudChance.Value);
                        if (!RunningSimulation)
                            CurrentSite.chance =(Chance);
                    }
                    catch
                    {

                    }
                }
                if (chkTrazel.Checked)
                {
                    high = starthigh;
                }
                if (chkMK.Checked)
                {
                    if (double.Parse((Lastbet - (double)nudMKDecrement.Value).ToString("0.00000000"), System.Globalization.CultureInfo.InvariantCulture) > 0)
                    {
                        Lastbet -= (double)nudMKDecrement.Value;
                    }
                }
                if (chkTrazel.Checked && trazelwin % (double)nudTrazelWin.Value == 0 && trazelwin != 0)
                {
                    Lastbet = (double)nudtrazelwinto.Value;
                    trazelwin = -1;
                    trazelmultiply = true;
                    high = !starthigh;
                }
                else
                {
                    if (chkTrazel.Checked)
                    {
                        Lastbet = MinBet;
                        trazelmultiply = false;
                    }
                }

                
                if (chkChangeWinStreak.Checked && (Winstreak == nudChangeWinStreak.Value))
                {
                    Lastbet = (double)nudChangeWinStreakTo.Value;
                }
                if (checkBox1.Checked)
                {
                    if (Winstreak == nudMutawaWins.Value)
                        Lastbet = mutawaprev *= (double)nudMutawaMultiplier.Value;
                    if (Winstreak == nudMutawaWins.Value + 1)
                    {
                        Lastbet = MinBet;
                        mutawaprev = (double)nudChangeWinStreakTo.Value / (double)nudMutawaMultiplier.Value;
                    }

                }
                if (chkChangeChanceWin.Checked && (Winstreak == nudChangeChanceWinStreak.Value))
                {
                    try
                    {
                        Chance = (double)nudChangeChanceWinTo.Value;
                        if (!RunningSimulation)
                            CurrentSite.chance = ((double)nudChangeChanceWinTo.Value);

                    }
                    catch
                    {

                    }
                }
                        

            }
            else
            {
                //stop multiplying if at max or if it goes below 1
                if (rdbMaxMultiplier.Checked && Losestreak >= MaxMultiplies)
                {
                    Multiplier = 1;
                }
                else if (rdbDevider.Checked && Losestreak % Devidecounter == 0 && Losestreak > 0)
                {
                    Multiplier *= Devider;
                    if (Multiplier < 1)
                        Multiplier = 1;
                }
                //adjust multiplier according to devider
                else if (rdbReduce.Checked && Losestreak == Devidecounter && Losestreak > 0)
                {
                    Multiplier *= Devider;
                }
                if (chkTrazel.Checked && trazelmultiply)
                {
                    Multiplier = (double)nudTrazelMultiplier.Value;
                }
                if (chkTrazel.Checked)
                {
                    high = starthigh;
                }
                if (chkTrazel.Checked && Losestreak + 1 >= (double)NudTrazelLose.Value && !trazelmultiply)
                {
                    Lastbet = (double)nudtrazelloseto.Value;
                    trazelmultiply = true;
                    high = !starthigh;
                }
                if (trazelmultiply)
                {
                    trazelwin = -1;

                }
                else
                {
                    trazelwin = 0;
                }
                //set new bet size
                Lastbet *= Multiplier;
                if (Losestreak == 1)
                {
                    if (chkFirstResetLoss.Checked)
                    {
                        Lastbet = MinBet;
                    }
                }
                if (chkMK.Checked)
                {
                    Lastbet += (double)nudMKIncrement.Value;
                }
                if (checkBox1.Checked)
                {
                    Lastbet = MinBet;
                }

               
                //change bet after a certain losing streak
                if (chkChangeLoseStreak.Checked && (Losestreak == nudChangeLoseStreak.Value))
                {
                    Lastbet = (double)nudChangeLoseStreakTo.Value;
                }
            }
            if (chkPercentage.Checked)
            {
                Lastbet = (double)(nudPercentage.Value / (decimal)100.0) * dPreviousBalance;
            }
        }
        int FibonacciLevel = 0;
        void Fibonacci(bool Win)
        {
            if (Win)
            {
                if (rdbFiboWinIncrement.Checked)
                {
                    FibonacciLevel += (int)nudFiboWinIncrement.Value;
                }
                else if (rdbFiboWinReset.Checked)
                {
                    FibonacciLevel = 0;
                }
                else
                {
                    FibonacciLevel = 0;
                    Stop();
                }
            }
            else
            {
                if (rdbFiboLossIncrement.Checked)
                {
                    FibonacciLevel += (int)nudFiboLossIncrement.Value;
                }
                else if (rdbFiboLossReset.Checked)
                {
                    FibonacciLevel = 0;
                }
                else
                {
                    FibonacciLevel = 0;
                    Stop();
                }
            }
            if (FibonacciLevel < 0)
                FibonacciLevel = 0;
            
            if (FibonacciLevel>= (int)nudFiboLeve.Value & chkFiboLevel.Checked)
            {
                if (rdbFiboLevelReset.Checked)
                    FibonacciLevel = 0;
                else
                {
                    FibonacciLevel = 0;
                    Stop();
                }
            }
            Lastbet = double.Parse(lstFibonacci.Items[FibonacciLevel].ToString().Substring(lstFibonacci.Items[FibonacciLevel].ToString().IndexOf(" ")+1));
        }

        void Alembert(bool Win)
        {
            if (Win)
            {
                
                if ((Winstreak) % (nudAlembertStretchWin.Value +1) == 0)
                {
                    Lastbet += (double)nudAlembertIncrementWin.Value;
                }
            }
            else
            {
                if ((Losestreak) % (nudAlembertStretchLoss.Value + 1) == 0)
                {
                    Lastbet += (double)nudAlembertIncrementLoss.Value;
                }
            }
            if (Lastbet < MinBet)
                Lastbet = MinBet;
        }

        int presetLevel = 0;
        void PresetList(bool Win)
        {
            if (Win)
            {
                if (rdbPresetWinStep.Checked)
                {
                    presetLevel += (int)nudPresetWinStep.Value;
                }
                else if (rdbPresetWinReset.Checked)
                {
                    presetLevel = 0;
                }
                else
                {
                    presetLevel = 0; 
                    Stop();
                }
            }
            else
            {
                if (rdbPresetLossStep.Checked)
                {
                    presetLevel += (int)nudPresetLossStep.Value;
                }
                else if (rdbPresetLossReset.Checked)
                {
                    presetLevel = 0;
                }
                else
                {
                    presetLevel = 0;
                    Stop();
                }
            }
            if (presetLevel < 0)
                presetLevel = 0;
            if (presetLevel > rtbPresetList.Lines.Length-1)
            {
                if (rdbPresetEndStop.Checked)
                {
                    Stop();
                }
                else if (rdbPresetEndStep.Checked)
                {
                    while (presetLevel > rtbPresetList.Lines.Length - 1)
                    {
                        presetLevel -= (int)nudPresetEndStep.Value;
                    }
                }
                else
                {
                    presetLevel = 0;
                }
            }
            double Betval = -1;
            if (presetLevel < rtbPresetList.Lines.Length)
            {
                if (double.TryParse(rtbPresetList.Lines[presetLevel], out Betval))
                {
                    Lastbet = Betval;
                }
                else
                {
                    Stop();
                    MessageBox.Show("Invalid bet in list. Please make sure there is only one bet per line and no other charachters or letters in the list.");
                }
            }
        }

        double ProfitSinceLastReset = 0;
        double StreakProfitSinceLastReset = 0;
        double StreakLossSinceLastReset = 0;
        

        public void DoBet(Bet bet)
        {
            bool Win = !(((bool)bet.high ? (decimal)bet.Roll< 100m - (decimal)(bet.Chance) : (decimal)bet.Roll > (decimal)(bet.Chance)));
            double profit = (Double)bet.Profit;
            retriedbet = false;
            if (!stop && !reset)
            {
                if (Win)
                {
                    if (LargestWin < profit)
                        LargestWin = profit;
                }
                else
                {
                    if (LargestLoss < -profit)
                        LargestLoss = -profit;
                }

                if (LargestBet < Lastbet)
                    LargestBet = Lastbet;

                //if its a win
                if (Win && !(reset))
                {

                    if (PreviousBalance != 0)
                    {
                       
                        if (Winstreak == 0)
                        {
                            currentprofit = 0;
                            StreakProfitSinceLastReset = 0;
                            StreakLossSinceLastReset = 0;
                        }                        
                        
                        currentprofit += (Lastbet*(99/Chance))-Lastbet;
                        ProfitSinceLastReset += (Lastbet*(99/Chance))-Lastbet;
                        StreakProfitSinceLastReset += (Lastbet * (99 / Chance)) - Lastbet;
                        
                       
                        
                        Wins++;
                        Winstreak++;
                        trazelwin++;
                        CalculateLuck(true);

                        if (StatsWindows != null)
                            if (!StatsWindows.IsDisposed)
                            {
                                if (Winstreak >= StatsWindows.nudLastStreakWin.Value)
                                    laststreakwin = Winstreak;
                            }
                        
                        if (!programmerToolStripMenuItem.Checked)
                        {
                            if (chkResetBetWins.Checked && Winstreak % nudResetWins.Value == 0)
                            {
                                Reset();
                            }
                            if (currentprofit >= ((double)nudStopWinBtcStreak.Value) && chkStopWinBtcStreak.Checked)
                            {
                                Stop();
                            }
                            if (Winstreak >= nudStopWinStreak.Value && chkStopWinStreak.Checked)
                            {
                                Stop();
                            }
                            if (profit >= (double)nudStopWinBtc.Value && chkStopWinBtc.Checked)
                            {
                                Stop();
                            }
                            if (StreakProfitSinceLastReset >= (double)nudResetBtcStreakProfit.Value && chkResetBtcStreakProfit.Checked)
                            {
                                Reset();
                                StreakProfitSinceLastReset -= (double)nudResetBtcStreakProfit.Value;
                            }
                            if (ProfitSinceLastReset> (double)nudResetBtcProfit.Value && chkResetBtcProfit.Checked)
                            {
                                Reset();
                                ProfitSinceLastReset -= (double)nudResetBtcProfit.Value;
                            }
                        }
                        if (Losestreak != 0)
                        {
                            double avglosecalc = avgloss * numlosesreaks;
                            avglosecalc += Losestreak;
                            avglosecalc /= ++numlosesreaks;
                            avgloss = avglosecalc;
                            double avgbetcalc = avgstreak * numstreaks;
                            avgbetcalc -= Losestreak;
                            avgbetcalc /= ++numstreaks;
                            avgstreak = avgbetcalc;
                            if (Losestreak > WorstStreak3)
                            {
                                WorstStreak3 = Losestreak;
                                if (Losestreak > WorstStreak2)
                                {
                                    WorstStreak3 = WorstStreak2;
                                    WorstStreak2 = Losestreak;
                                    if (Losestreak > WorstStreak)
                                    {
                                        WorstStreak2 = WorstStreak;
                                        WorstStreak = Losestreak;
                                    }
                                }
                            }
                        }
                        Losestreak = 0;
                    }
                    
                    if (stoponwin)
                    {
                        Stop();
                    }
                    iMultiplyCounter = 0;                    
                    Multiplier = (double)(nudMultiplier.Value);
                    if ((!programmerToolStripMenuItem.Checked))
                    {
                        if (chkZigZagWins.Checked && Wins% (int)nudZigZagWins.Value==0 && Wins!=0)
                        {
                             
                                high = !high;
                            
                        }
                        if (chkZigZagWinsStreak.Checked && Winstreak % (int)nudZigZagWinsStreak.Value ==0 && Winstreak!=0)
                        {
                            high = !high;
                        }
                    }
                }

                    //if its a loss
                else if (!Win && !(reset))
                {
                    
                    //do i use this line?
                    iMultiplyCounter++;

                    //reset current profit when switching from a winning streak to a losing streak
                    if (Losestreak == 0)
                    {
                        currentprofit = 0;
                        StreakProfitSinceLastReset = 0;
                        StreakLossSinceLastReset = 0;
                    }
                    
                    //adjust profit
                    currentprofit -= Lastbet;
                    ProfitSinceLastReset -= Lastbet;
                    
                    StreakLossSinceLastReset -= Lastbet;
                    //increase losses and losestreak
                    Losses++;
                    Losestreak++;
                    
                    CalculateLuck(false);
                    
                    //update last losing streak if it is above the specified value to show in the stats
                    if (StatsWindows != null)
                        if (!StatsWindows.IsDisposed)
                            if (Losestreak >= StatsWindows.nudLastStreakLose.Value)
                                laststreaklose = Losestreak;

                    //switch high low if applied in the zig zag tab
                    if ((!programmerToolStripMenuItem.Checked))
                    {
                        if (chkZigZagLoss.Checked && Losses % (int)nudZigZagLoss.Value == 0 && Losses != 0)
                        {

                            high = !high;

                        }
                        if (chkZigZagLossStreak.Checked && Losestreak % (int)nudZigZagLossStreak.Value == 0 && Losestreak != 0)
                        {
                            high = !high;
                        }
                    }

                   //change chance after a certain losing streak
                    if (chkChangeChanceLose.Checked && (Losestreak == nudChangeChanceLoseStreak.Value))
                    {
                        try
                        {
                            Chance = (double)nudChangeChanceLoseTo.Value;
                            if (!RunningSimulation)
                                CurrentSite.chance = (double)(nudChangeChanceLoseTo.Value);
                            
                            
                        }
                        catch
                        {

                        }
                    }

                    if (!programmerToolStripMenuItem.Checked)
                    {
                        if (chkResetBetLoss.Checked && Losestreak %nudResetBetLoss.Value == 0)
                        {
                            Reset();
                        }
                        //stop conditions:
                        //stop if lose streak is higher than specified
                        if (Losestreak >= nudStopLossStreak.Value && chkStopLossStreak.Checked)
                        {
                            Stop();
                        }

                        //stop if current profit drops below specified value/ loss is larger than specified value
                        if (currentprofit <= (0.0 - (double)nudStopLossBtcStreal.Value) && chkStopLossBtcStreak.Checked)
                        {
                            Stop();
                        }

                        // stop if total profit/total loss is below/above certain value
                        if (profit <= 0.0 - (double)nudStopLossBtc.Value && chkStopLossBtc.Checked)
                        {
                            Stop();
                        }
                        if (StreakLossSinceLastReset <= -(double)nudResetBtcStreakLoss.Value && chkResetBtcStreakLoss.Checked)
                        {
                            Reset();
                            StreakLossSinceLastReset += (double)nudResetBtcStreakLoss.Value;
                        }
                        if (ProfitSinceLastReset < -(double)nudResetBtcLoss.Value && chkResetBtcLoss.Checked)
                        {
                            Reset();
                            ProfitSinceLastReset += (double)nudResetBtcLoss.Value;
                        }
                    }
                    //when switching from win streak to lose streak, calculate some stats
                    if (Winstreak != 0)
                    {
                        double avgwincalc = avgwin * numwinstreasks;
                        avgwincalc += Winstreak;
                        avgwincalc /= ++numwinstreasks;
                        avgwin = avgwincalc;
                        double avgbetcalc = avgstreak * numstreaks;
                        avgbetcalc += Winstreak;
                        avgbetcalc /= ++numstreaks;
                        avgstreak = avgbetcalc;
                        if (Winstreak > BestStreak3)
                        {
                            BestStreak3 = Winstreak;
                            if (Winstreak > BestStreak2)
                            {
                                BestStreak3 = BestStreak2;
                                BestStreak2 = Winstreak;
                                if (Winstreak > BestStreak)
                                {
                                    BestStreak2 = BestStreak;
                                    BestStreak = Winstreak;
                                }
                            }
                        }
                    }

                    //reset win streak
                    Winstreak = 0;

                    //sounds
                    if (!RunningSimulation)
                    if (Sound && SoundStreak && Losestreak > SoundStreakCount)
                        playalarm();
                    //email
                    if (!RunningSimulation)
                    if (Emails.Streak && Losestreak > Emails.StreakSize)
                        Emails.SendStreak(Losestreak, Emails.StreakSize, dPreviousBalance);

                    
                    //update worst streaks
                    if (!RunningSimulation)
                    if (Losestreak > WorstStreak)
                        WorstStreak = Losestreak;

                    //reset win multplier
                    WinMultiplier = (double)(nudWinMultiplier.Value);

                }
                if (chkZigZagBets.Checked && !programmerToolStripMenuItem.Checked)
                {
                    if ((Wins+Losses) % (int)nudZigZagBets.Value == 0 && (Wins+Losses)!=0 )
                    {
                        high = !high;
                    }
                }
                if (!RunningSimulation)
                if (dPreviousBalance >= Limit && chkLimit.Checked && (!programmerToolStripMenuItem.Checked))
                {

                    if (rdbStop.Checked)
                    {
                        Stop();
                    }
                    else if (rdbWithdraw.Checked)
                    {
                        Withdraw();

                    }
                    else if (rdbInvest.Checked)
                    {
                        Invest();

                    }
                }
                if (!RunningSimulation)
                if (dPreviousBalance - Lastbet <= LowerLimit && chkLowerLimit.Checked &&(!programmerToolStripMenuItem.Checked))
                {
                    TrayIcon.BalloonTipText = "Balance lower than " + nudLowerLimit.Value + "\nStopping Bets...";
                    TrayIcon.ShowBalloonTip(1000);
                    Stop();
                    if (Sound && SoundLow)
                        playalarm();
                    TrayIcon.BalloonTipText = "DiceBot has Stopped Betting\nThe next bet will will have put your Balance below your lower limit";

                    if (Emails.Lower)
                        Emails.SendLowLimit(dPreviousBalance, LowerLimit, Lastbet);
                }


                if (!RunningSimulation)
                if ( Wins!=0 && Losses!=0 && chkResetSeed.Checked && (!programmerToolStripMenuItem.Checked))
                {
                    if ( ((rdbResetSeedBets.Checked && (Wins+Losses) % nudResetSeed.Value == 0) ||
                       (rdbResetSeedWins.Checked && Wins % nudResetSeed.Value == 0 && Losestreak==0)||
                       (rdbResetSeedLosses.Checked && Losses % nudResetSeed.Value == 0 && Winstreak == 0)) && !withdrew)
                    {
                        
                   
                        ResetSeed();
                    }
                    
                }

                try
                {
                   
                    UpdateStats();
                }
                catch
                {

                }
                if (RunningSimulation && (Wins + Losses > SimWindow.nudSimNumBets.Value || Lastbet > PreviousBalance))
                {
                    Stop();
                }
                
                if (!(stop ||reset || withdraw ||invest))
                {
                    if (programmerToolStripMenuItem.Checked)
                    {
                        parseScript(bet);
                    }
                    else
                    {
                        if (rdbMartingale.Checked)
                        {
                            martingale(Win);
                        }
                        else if (rdbLabEnable.Checked)
                        {
                            Labouchere(Win);
                        }
                        else if (rdbFibonacci.Checked)
                        {
                            Fibonacci(Win);
                        }
                        else if (rdbAlembert.Checked)
                        {
                            Alembert(Win);
                        }
                        else if (rdbPreset.Checked)
                        {
                            PresetList(Win);
                        }
                    }
                    if (!stop)
                    {
                        WriteConsole("Betting " + Lastbet + " at " + Chance +"% chance to win, "+ (high?"high":"low"));
                        EnableTimer(tmBet, true);

                        withdrew = false;
                    }
                }


            }
            if (RunningSimulation && stop)
            {
                RunningSimulation = false;
            }

        }

        System.Collections.ArrayList Vars = new System.Collections.ArrayList();
        private void parseScript(Bet bet)
        {

            try
            {
                bool Win = !(((bool)bet.high ? (decimal)bet.Roll < 100m - (decimal)(bet.Chance) : (decimal)bet.Roll > (decimal)(bet.Chance)));
                SetLuaVars();
                Lua["win"] = Win;
                Lua["currentprofit"] = ((double)(bet.Profit * 100000000)) / 100000000.0;
                Lua["lastBet"] = bet;
                LuaRuntime.SetLua(Lua);
                LuaRuntime.Run("dobet()");
                GetLuaVars();
            }
            catch
            {

            }
                        
        }

        void WriteConsole(string Message)
        {
            rtbConsole.AppendText(Message+"\r\n");
            if (rtbConsole.Lines.Length>500)
            {
                List<string> lines = new List<string>(rtbConsole.Lines);
                while (lines.Count>450)
                {
                    lines.RemoveAt(0);
                }
                rtbConsole.Lines = lines.ToArray();
            }
        }
        delegate void dWriteConsole(string Message);
        delegate void dWithdraw(double Amount, string Address);
        delegate void dInvest(double Amount);
        delegate void dtip(string username, double amount);
        delegate void dStop();
        delegate void dResetSeed();
    delegate void dEnableTimer(System.Windows.Forms.Timer tmr, bool enabled);
    void EnableTimer(System.Windows.Forms.Timer tmr, bool enabled)
    {
        if (InvokeRequired)
        {
            Invoke(new dEnableTimer(EnableTimer), tmr, enabled);
            return;
        }
        else
        {
            tmr.Enabled = enabled;
        }

    }
    delegate void dSetTimerInterval(System.Windows.Forms.Timer tmr, int Interval);
    void setInterval(System.Windows.Forms.Timer tmr, int Interval)
    {
        if (InvokeRequired)
        {
            Invoke(new dSetTimerInterval(setInterval), tmr, Interval);
            return;
        }
        else
        {
            tmr.Interval = Interval;
        }

    }

        delegate string[] dGetLabList();
        string[] GetLabList()
        {
            if (InvokeRequired)
            {
                return (string[])Invoke(new dGetLabList(GetLabList));
                
            }
            else
            {
                return rtbBets.Lines;
            }
        }

        bool RunningSimulation = false;
        private void tmBet_Tick(object sender, EventArgs e)
        {

            try
            {
                bool valid = true;
                if (chkBotSpeed.Checked)
                {
                    if ((DateAndTime.Now - dtLastBet).Ticks < new TimeSpan(0, 0, 0, 0, (int)((decimal)1000.0 / nudBotSpeed.Value)).Ticks)
                    {
                        valid = false;
                    }
                }

                if (RunningSimulation)
                {
                    Simbet();
                }
                else
                if (CurrentSite.ReadyToBet() && valid)
                {
                    
                    PlaceBet();
                }
               
            }
            catch
            {

            }

        }

      
        void playalarm()
        {
            try
            {
                
                    if (salarm == "")
                    {
                        (new SoundPlayer(@"media\alarm.wav")).Play();
                    }
                    else
                    {
                        int ext = salarm.LastIndexOf(".")+1;
                        if (salarm.Substring(ext).ToLower() == "mp3")
                        {
                            WindowsMediaPlayer player = new WindowsMediaPlayer();
                            player.URL = salarm;
                            player.controls.play();
                        }
                        else
                        {
                            (new SoundPlayer(salarm)).Play();
                        }
                    }
                
            }
            catch
            {
                MessageBox.Show("Failed to play Alarm, pelase make sure file exists");
            }
        }

        private void tmStop_Tick(object sender, EventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.S) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                Stop();
            }
           
            if (!stop && timecounter > 10)
            {
                if (StatsWindows!=null)
                {
                    if (!StatsWindows.IsDisposed)
                    {
                        StatsWindows.lblTime.Text = (TotalTime + (DateTime.Now - dtStarted)).ToString(@"hh\:mm\:ss");
                    }
                }
                
                timecounter = 0;
            }
            timecounter++;
            if (autostart && !running)
            {
                Start(false);
                running = true;
            }
        }

        #endregion
        

        protected override void OnClosing(CancelEventArgs e)
        {
            Stop();
            if (CurrentSite != null)
            {
                CurrentSite.Disconnect();
            }
            save();
            if (File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim"))
            {
                File.Delete(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim");
            }
            if (File.Exists("currentprofitbet.txt"))
            {
                File.Delete("currentprofitbet.txt");
            }
            if (File.Exists("currentprofittime.txt"))
            {
                File.Delete("currentprofittime.txt");
             }
            string[] files = Directory.GetFiles(".");
            foreach (string F in files)
            {
                if (F.StartsWith(".\\tmp_"))
                {
                    File.Delete(F);
                }
            }
            base.OnClosing(e);
            Application.Exit();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
           
           
            if ((sender as Button).Name.ToUpper().Contains("HIGH"))
            {
                starthigh = high = true;

            }
            else
            {
                starthigh = high = false;
            }
            Start(false);
        }

        //stop button pressed
        private void btnStop_Click(object sender, EventArgs e)
        {
            
            Stop();
        }

        #region Save and load settings
        delegate void dsave();
        void save()
        {
            if (InvokeRequired)
            {
                Invoke( new  dsave(save));
                return;
            }
            save(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings");
            if (programmerToolStripMenuItem.Checked)
            {
                try
                {
                    File.WriteAllText("TempCodeBackup.txt", richTextBox3.Text);
                }
                catch
                {

                }
            }
            savepersonal();
        }

        void savepersonal()
        {
            using (StreamWriter sw = new StreamWriter(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings3"))
            {
                sw.WriteLine("Amount|" + nudAmount.Value);
                sw.WriteLine("Limit|" + nudLimit.Value);
                if (chkLimit.Checked)
                    sw.WriteLine("LimitEnabled|1");
                else
                    sw.WriteLine("LimitEnabled|0");
                sw.WriteLine("LowerLimit|" + nudLowerLimit.Value);
                sw.Write("LowerLimitEnabled|");
                if (chkLowerLimit.Checked)
                    sw.WriteLine("1");
                else
                    sw.WriteLine("0");
                
                sw.WriteLine("To|" + txtTo.Text);
                sw.Write("OnStop|");
                if (rdbInvest.Checked)
                {
                    sw.WriteLine("0");
                }
                else if (rdbStop.Checked)
                {
                    sw.WriteLine("1");
                }
                else
                {
                    sw.WriteLine("2");
                }
                
                sw.WriteLine("LastStreakWin|" + StatsWindows.nudLastStreakWin.Value.ToString("00"));
                sw.WriteLine("LastStreakLose|" + StatsWindows.nudLastStreakLose.Value.ToString("00"));
                string msg = "";
                if (chkBotSpeed.Checked)
                    msg = "1";
                else msg = "0";
                sw.WriteLine("BotSpeedEnabled|" + msg);
                sw.WriteLine("BotSpeedValue|" + nudBotSpeed.Value.ToString());
                if (chkResetSeed.Checked)
                    msg = "1";
                else msg = "0";
                sw.WriteLine("ResetSeedEnabled|" + msg);

                if (rdbResetSeedBets.Checked)
                    msg = "0";
                else if (rdbResetSeedWins.Checked)
                    msg = "1";
                else if (rdbResetSeedLosses.Checked)
                    msg = "2";
                sw.WriteLine("ResetSeedMode|" + msg);
                sw.WriteLine("ResetSeedValue|" + nudResetSeed.Value.ToString());
                sw.WriteLine("QuickSwitchFolder|" + txtQuickSwitch.Text);
                sw.WriteLine("SettingsMode|" + (basicToolStripMenuItem.Checked?"0":advancedToolStripMenuItem.Checked?"1":"2"));
                sw.WriteLine("Site|" + (justDiceToolStripMenuItem.Checked?"0":primeDiceToolStripMenuItem.Checked?"1":pocketRocketsCasinoToolStripMenuItem.Checked?"2": diceToolStripMenuItem.Checked?"3":safediceToolStripMenuItem.Checked?"4":"1"));
            }
        }
        
        delegate void dSave(string file);

        void save(string file)
        {
            if (InvokeRequired)
            {
                Invoke(new dSave(save), file);
                return;
            }
            using (StreamWriter sw = new StreamWriter(file))
            {
               
                try
                {
                    sw.WriteLine("SaveVersion|" + "3");
                    sw.WriteLine("MinBet|"+nudMinBet.Value);
                    sw.WriteLine("Multiplier|"+nudMultiplier.Value);
                    sw.WriteLine("Chance|"+nudChance.Value);
                    sw.WriteLine("MaxMultiply|"+nudMaxMultiplies.Value);
                    sw.WriteLine("NBets|"+nudNbets.Value);
                    sw.WriteLine("Devider|"+ nudDevider.Value);
                    sw.Write("MultiplierMode|");
                    if (rdbMaxMultiplier.Checked)
                        sw.WriteLine("0");
                    else if (rdbDevider.Checked)
                        sw.WriteLine("1");
                    else if (rdbConstant.Checked)
                        sw.WriteLine("2");
                    else sw.WriteLine("3");
                    sw.Write("ReverseEnabled|");
                   sw.Write("ResetBetLossEnabled|");
                    if (chkResetBetLoss.Checked)
                    {
                        sw.WriteLine("1");
                    }
                    else
                    {
                        sw.WriteLine("0");
                    }
                    sw.WriteLine("ResetBetLossValue|"+nudResetBetLoss.Value.ToString());

                    sw.Write("ResetBetWinsEnabled|");
                    if (chkResetBetWins.Checked)
                        sw.WriteLine("1");
                    else
                        sw.WriteLine("0");
                    sw.WriteLine("ResetWinsValue|"+nudResetWins.Value.ToString());
                    sw.WriteLine("WinMultiplier|" + nudWinMultiplier.Value);
                    sw.WriteLine("WinMaxMultiplies|" + nudWinMaxMultiplies.Value);
                    sw.WriteLine("WinNBets|" + nudWinNBets.Value);
                    sw.WriteLine("WinDevider|" + nudWinDevider.Value);
                    sw.Write("WinMultiplyMode|");
                    if (rdbWinConstant.Checked)
                        sw.WriteLine("0");
                    else if (rdbWinDevider.Checked)
                        sw.WriteLine("1");
                    else if (rdbWinMaxMultiplier.Checked)
                        sw.WriteLine("2");
                    else if (rdbWinReduce.Checked)
                        sw.WriteLine("3");

                    string msg = "";
                    
                    
                    if (chkStopLossStreak.Checked) msg = "1";
                    else msg="0";
                    sw.WriteLine("StopAfterLoseStreakEnabled|"+msg);                    
                    sw.WriteLine("StopAfterLoseStreakValue|"+nudStopLossStreak.Value.ToString());
                    
                    if ( chkStopLossBtcStreak.Checked ) msg = "1";
                    else msg="0";
                    sw.WriteLine("StopAfterLoseStreakBtcEnabled|"+msg);
                    sw.WriteLine("StopAfterLoseStreakBtcValue|"+nudStopLossBtcStreal.Value.ToString());
                    
                    if (  chkStopLossBtc.Checked ) msg = "1";
                    else msg="0";
                    sw.WriteLine("StopAfterLoseBtcEnabled|"+msg);
                    sw.WriteLine("StopAfterLoseBtcValue|"+nudStopLossBtc.Value.ToString());
                    
                    
                    
                    if ( chkChangeLoseStreak.Checked ) msg = "1";
                    else msg="0";
                    sw.WriteLine("ChangeAfterLoseStreakEnabled|"+msg);
                    sw.WriteLine("ChangeAfterLoseStreakSize|"+nudChangeLoseStreak.Value.ToString());
                    sw.WriteLine("ChangeAfterLoseStreakTo|"+nudChangeLoseStreakTo.Value.ToString());


                    if (chkStopWinStreak.Checked) msg = "1";
                    else msg = "0";
                    sw.WriteLine("StopAfterWinStreakEnabled|" + msg);
                    sw.WriteLine("StopAfterWinStreakValue|" + nudStopWinStreak.Value.ToString());
                    if (chkStopWinBtcStreak.Checked) msg = "1";
                    else msg = "0";
                    sw.WriteLine("StopAfterWinStreakBtcEnabled|" + msg);
                    sw.WriteLine("StopAfterWinStreakBtcValue|" + nudStopWinBtcStreak.Value.ToString());
                    if (chkStopWinBtc.Checked) msg = "1";
                    else msg = "0";
                    sw.WriteLine("StopAfterWinBtcEnabled|" + msg);
                    sw.WriteLine("StopAfterWinBtcValue|" + nudStopWinBtc.Value.ToString());

                    if (chkChangeWinStreak.Checked) msg = "1";
                    else msg = "0";
                    sw.WriteLine("ChangeAfterWinStreakEnabled|" + msg);
                    sw.WriteLine("ChangeAfterWinStreakSize|" + nudChangeWinStreak.Value.ToString());
                    sw.WriteLine("ChangeAfterWinStreakTo|" + nudChangeWinStreakTo.Value.ToString());
                    
                    sw.WriteLine("ChangeChanceAfterLoseStreakEnabled|" + ((chkChangeChanceLose.Checked)?"1":"0"));
                    sw.WriteLine("ChangeChanceAfterLoseStreakSize|" + nudChangeChanceLoseStreak.Value.ToString("00"));
                    sw.WriteLine("ChangeChanceAfterLoseStreakValue|" + nudChangeChanceLoseTo.Value.ToString());

                    sw.WriteLine("ChangeChanceAfterWinStreakEnabled|" + ((chkChangeChanceWin.Checked) ? "1" : "0"));
                    sw.WriteLine("ChangeChanceAfterWinStreakSize|" + nudChangeChanceWinStreak.Value.ToString("00"));
                    sw.WriteLine("ChangeChanceAfterWinStreakValue|" + nudChangeChanceWinTo.Value.ToString());
                    sw.WriteLine("MutawaMultiplier|" + nudMutawaMultiplier.Value.ToString());
                    sw.WriteLine("MutawaWins|" + nudMutawaWins.Value.ToString());
                    sw.WriteLine("MutawaEnabled|"+(checkBox1.Checked?"1":"0"));
                    

                    sw.WriteLine("TrazalWin|" + nudTrazelWin.Value.ToString());
                    sw.WriteLine("TrazalWinTo|" + nudtrazelwinto.Value.ToString());
                    sw.WriteLine("TrazalLose|" + NudTrazelLose.Value.ToString("00"));
                    sw.WriteLine("TrazalLoseTo|" + nudtrazelloseto.Value.ToString());
                    sw.WriteLine("TrazelMultiPlier|" + nudTrazelMultiplier.Value.ToString());
                    sw.WriteLine("TrazelEnabled|" + (chkTrazel.Checked ? "1" : "0"));

                    sw.WriteLine("MKIncrement|" + nudMKIncrement.Value.ToString());
                    sw.WriteLine("MKDecrement|" + nudMKDecrement.Value.ToString());
                    sw.WriteLine("MKEnabled|" + (chkMK.Checked ? "1" : "0"));

                    
                    sw.WriteLine("LabReverse|" + (chkReverseLab.Checked ? "1" : "0"));
                    string labtmp = "";
                    foreach (string s in rtbBets.Lines)
                    {
                        if (labtmp != "")
                            labtmp += "?";
                        labtmp += s;
                    }
                    sw.WriteLine("LabValues|" + labtmp);
                    sw.WriteLine("LabComplete|" + (rdbLabStop.Checked ? "2" : "1"));

                    int Strat = 0;
                    if (rdbMartingale.Checked)
                        Strat = 0;
                    else if (rdbLabEnable.Checked)
                        Strat = 1;
                    else if (rdbFibonacci.Checked)
                        Strat = 2;
                    else if (rdbAlembert.Checked)
                        Strat = 3;
                    else if (rdbPreset.Checked)
                        Strat = 4;
                    sw.WriteLine("Strategy|" + Strat);

                    sw.WriteLine("FibonacciLoss|" + (rdbFiboLossIncrement.Checked ? "0" : rdbFiboLossReset.Checked ? "1" : "2"));
                    sw.WriteLine("FibonacciWin|" + (rdbFiboWinIncrement.Checked ? "0" : rdbFiboWinReset.Checked ? "1" : "2"));
                    sw.WriteLine("FibonacciLevel|" + (rdbFiboLevelStop.Checked ? "0" : "1"));
                    sw.WriteLine("FibonacciLevelEnabled|" + (chkFiboLevel.Checked ? "0" : "1"));
                    sw.WriteLine("FibonacciLossSteps|" + nudFiboLossIncrement.Value);
                    sw.WriteLine("FibonacciWinSteps|" + nudFiboWinIncrement.Value);
                    sw.WriteLine("FibonnaciLevelSteps|" + nudFiboLeve.Value);

                    sw.WriteLine("dAlembertLossIncrement|" + nudAlembertIncrementLoss.Value);
                    sw.WriteLine("dAlembertLossStretch|" + nudAlembertStretchLoss.Value);
                    sw.WriteLine("dAlembertWinIncrement|" + nudAlembertIncrementWin.Value);
                    sw.WriteLine("dAlembertWinStretch|" + nudAlembertStretchWin.Value);


                    string presettmp = "";
                    foreach (string s in rtbPresetList.Lines)
                    {
                        if (presettmp != "")
                            presettmp += "?";
                        presettmp += s;
                    }
                    sw.WriteLine("PresetValues|" + presettmp);
                    sw.WriteLine("PresetEnd|"+ (rdbPresetEndReset.Checked?0:rdbPresetEndStep.Checked?1:2));
                    sw.WriteLine("PresetEndStep|"+nudPresetEndStep.Value);
                    sw.WriteLine("PresetLoss|" + (rdbPresetLossReset.Checked ? 0 : rdbPresetLossStep.Checked ? 1 : 2));
                    sw.WriteLine("PresetLossStep|" + nudPresetLossStep.Value);
                    sw.WriteLine("PresetWin|" + (rdbPresetWinReset.Checked ? 0 : rdbPresetWinStep.Checked ? 1 : 2));
                    sw.WriteLine("PresetWinStep|" + nudPresetWinStep.Value);

                    sw.WriteLine("ReverseWin|" + (chkZigZagWins.Checked ? "1" : "0"));
                    sw.WriteLine("ReverseWinStreak|" + (chkZigZagWinsStreak.Checked ? "1" : "0"));
                    sw.WriteLine("ReverseLoss|" + (chkZigZagLoss.Checked ? "1" : "0"));
                    sw.WriteLine("ReverseLossStreak|" + (chkZigZagLossStreak.Checked ? "1" : "0"));
                    sw.WriteLine("ReverseBet|" + (chkZigZagBets.Checked ? "1" : "0"));

                    sw.WriteLine("ReverseWinValue|"+nudZigZagWins.Value);
                    sw.WriteLine("ReverseWinStreakValue|" + nudZigZagWinsStreak.Value);
                    sw.WriteLine("ReverseLossValue|" + nudZigZagLoss.Value);
                    sw.WriteLine("ReverseLossStreakValue|" + nudZigZagLossStreak.Value);
                    sw.WriteLine("ReverseBetValue|" + nudZigZagBets.Value);

                    sw.WriteLine("ResetBtcStreakLoss|"+(chkResetBtcStreakLoss.Checked?"1":"0"));
                    sw.WriteLine("ResetBtcStreakLossValue|"+nudResetBtcStreakLoss.Value.ToString());
                    sw.WriteLine("ResetBtcLoss|"+(chkResetBtcLoss.Checked?"1":"0"));
                    sw.WriteLine("ResetBtcLossValue|" + nudResetBtcLoss.Value.ToString());

                    sw.WriteLine("ResetBtcStreakProfit|" + (chkResetBtcStreakProfit.Checked ? "1" : "0"));
                    sw.WriteLine("ResetBtcStreakProfitValue|" + nudResetBtcStreakProfit.Value.ToString());
                    sw.WriteLine("ResetBtcProfit|" + (chkResetBtcProfit.Checked ? "1" : "0"));
                    sw.WriteLine("ResetBtcProfitValue|" + nudResetBtcProfit.Value.ToString());

                    sw.WriteLine("FirstResetLoss|" + (chkFirstResetLoss.Checked ? "1" : "0"));
                    sw.WriteLine("FirstResetWin|" + (chkFirstResetWin.Checked ? "1" : "0"));
                    

                }
                catch
                {

                }
            }
        }
        bool load()
        {
            
            return (load(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings"));
        }

        bool oldLoad(string File)
        {
            using (StreamReader sw = new StreamReader(File))
                {
                    string msg = sw.ReadLine();
                    string[] values = msg.Split(',');
                    if (msg.Contains(";"))
                        values = msg.Split(';');
                    int i = 0;
                    nudAmount.Value = decimal.Parse(values[i++]);
                    nudLimit.Value = decimal.Parse(values[i++]);
                    if (values[i++] == "1")
                        chkLimit.Checked = true;
                    else
                        chkLimit.Checked = false;
                    nudLowerLimit.Value = decimal.Parse(values[i++]);
                    if (values[i++] == "1")
                        chkLowerLimit.Checked = true;
                    else
                        chkLowerLimit.Checked = false;
                    nudMinBet.Value = decimal.Parse(values[i++]);
                    nudMultiplier.Value = decimal.Parse(values[i++]);
                    
                    txtTo.Text = values[i++];
                    string action = values[i++];
                    if (action == "0")
                    {
                        rdbInvest.Checked = true;
                    }
                    else if (action == "1")
                    {
                        rdbStop.Checked = true;
                    }
                    else
                        rdbWithdraw.Checked = true;


                    if (values[i++] == "1")
                    {
                        //chkStopOnWin.Checked = true;
                    }
                    else
                    { 
                        //chkStopOnWin.Checked = false;
                    }
                    if (!sw.EndOfStream)
                    {
                        msg = sw.ReadLine();
                        values = msg.Split(',');
                        if (msg.Contains(";"))
                            values = msg.Split(';');
                        i = 0;
                        nudChance.Value = decimal.Parse(values[i++]);
                        nudMaxMultiplies.Value = decimal.Parse(values[i++]);
                        nudNbets.Value = decimal.Parse(values[i++]);
                        nudDevider.Value = decimal.Parse(values[i++]);
                        string s = values[i++];
                        if (s == "0")
                            rdbMaxMultiplier.Checked = true;
                        else if (s == "1")
                            rdbDevider.Checked = true;
                        else if (s == "2")
                            rdbConstant.Checked = true;
                        else if (s == "3")
                            rdbReduce.Checked = true;

                    }
                    if (!sw.EndOfStream)
                    {
                        msg = sw.ReadLine();
                        values = msg.Split(',');
                        if (msg.Contains(";"))
                            values = msg.Split(';');
                        i = 0;                        
                        if (values[i++] == "1")
                        {}
                            //chkReverse.Checked = true;
                        else
                        { }
                            //chkReverse.Checked = false;                        
                        string cur = values[i++];
                        if (cur == "0")
                        {}
                            //rdbReverseBets.Checked = true;
                        else if (cur == "1")
                        {}//rdbReverseLoss.Checked = true;
                        else if (cur == "2")
                        { }// rdbReverseWins.Checked = true;
                        decimal tmpval = (decimal)dparse(values[i++], ref convert);
                        if (values.Length > i)
                        {
                            StatsWindows.nudLastStreakWin.Value = (decimal)dparse(values[i++], ref convert);
                            StatsWindows.nudLastStreakLose.Value = (decimal)dparse(values[i++], ref convert);
                        }
                    }
                    if (!sw.EndOfStream)
                    {
                        msg = sw.ReadLine();
                        values = msg.Split(',');
                        if (msg.Contains(";"))
                            values = msg.Split(';');
                        i = 0;
                        
                        chkResetBetLoss.Checked =(values[i++] == "1");
                        nudResetBetLoss.Value = (decimal)dparse(values[i++], ref convert);
                        chkResetBetWins.Checked = (values[i++] == "1");
                        nudResetWins.Value = (decimal)dparse(values[i++], ref convert);

                        nudWinMultiplier.Value = decimal.Parse(values[i++]);
                        nudWinMaxMultiplies.Value = decimal.Parse(values[i++]);
                        nudWinNBets.Value = decimal.Parse(values[i++]);
                        nudWinDevider.Value = decimal.Parse(values[i++]);
                        string cur = values[i++];
                        rdbWinConstant.Checked = (cur == "0");
                        rdbWinDevider.Checked = (cur == "1");
                        rdbWinMaxMultiplier.Checked = (cur == "2");
                        rdbWinReduce.Checked = (cur == "3");
                        chkBotSpeed.Checked = (values[i++] == "1");
                        chkResetSeed.Checked = (values[i++] == "1");
                        cur = values[i++];
                        rdbResetSeedBets.Checked = (cur == "0");
                        rdbResetSeedWins.Checked = (cur == "1");
                        rdbResetSeedLosses.Checked = (cur == "2");
                        if (values.Length >= i + 1)
                        {
                            nudResetSeed.Value = iparse(values[i++]);
                        }
                    }
                }
            variabledisable();
            return true;
        }

        public string getvalue(List<SavedItem> list, string item)
        {
            foreach (SavedItem cur in list)
            {
                if (cur.Name.ToUpper() == item.ToUpper())
                {
                    if (cur.Name == "ReverseOn")
                    {

                    }
                    return cur.Value;
                }

            }
            return "0-0-0";
        }

        bool load(string File)
        {
            try
            {
                string header = "";
                using (StreamReader sr = new StreamReader(File))
                {
                    header = sr.ReadLine();
                }
                               
                //if load file is not of version 2 or above, do old load
                if (!header.ToUpper().Contains("VERSION"))
                {
                    return oldLoad(File);
                }
                //else do normal load
                else
                {
                    List<SavedItem> saveditems = new List<SavedItem>();
                    using (StreamReader sr = new StreamReader(File))
                    {                        
                        while (!sr.EndOfStream)
                        {
                            string[] s = sr.ReadLine().Split('|');
                            saveditems.Add(new SavedItem(s[0],s[1]));
                        }
                    }
                    if (System.IO.File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings3"))
                    {
                        using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\settings3"))
                        {
                            while (!sr.EndOfStream)
                            {
                                string[] s = sr.ReadLine().Split('|');
                                saveditems.Add(new SavedItem(s[0], s[1]));
                            }
                        }
                    }
                    nudAmount.Value=decimal.Parse(getvalue( saveditems, "Amount"));
                    nudLimit.Value = decimal.Parse(getvalue(saveditems, "Limit"));
                    chkLimit.Checked= (getvalue(saveditems, "LimitEnabled") == "1");
                    nudLowerLimit.Value = decimal.Parse(getvalue(saveditems, "LowerLimit"));
                    chkLowerLimit.Checked = (getvalue(saveditems, "LowerLimitEnabled") == "1");
                    nudMinBet.Value = decimal.Parse(getvalue(saveditems, "MinBet"));
                    nudMultiplier.Value = decimal.Parse(getvalue(saveditems, "Multiplier"));
                    
                    txtTo.Text = getvalue(saveditems, "To");
                    string temp = getvalue(saveditems, "OnStop");
                    rdbInvest.Checked = (temp == "0");
                    rdbStop.Checked = (temp == "1");
                    rdbWithdraw.Checked = (temp == "2");
                    //chkStopOnWin.Checked = ("1"==getvalue(saveditems, "StopOnWin"));
                    nudChance.Value = decimal.Parse(getvalue(saveditems, "Chance"));
                    nudMaxMultiplies.Value = decimal.Parse(getvalue(saveditems, "MaxMultiply"));
                    nudNbets.Value = decimal.Parse(getvalue(saveditems, "NBets"));
                    nudDevider.Value = decimal.Parse(getvalue(saveditems, "devider"));
                    temp = getvalue(saveditems, "MultiplierMode");
                    rdbMaxMultiplier.Checked = (temp == "0");
                    rdbDevider.Checked = (temp == "1");
                    rdbConstant.Checked = (temp == "2");
                    rdbReduce.Checked = (temp=="3");
                    
                    StatsWindows.nudLastStreakWin.Value = (decimal)dparse(getvalue(saveditems, "LastStreakWin"), ref convert);
                    StatsWindows.nudLastStreakLose.Value = (decimal)dparse(getvalue(saveditems, "LastStreakLose"), ref convert);
                    chkResetBetLoss.Checked = ("1"==getvalue(saveditems, "ResetBetLossEnabled"));
                    nudResetBetLoss.Value = (decimal)dparse(getvalue(saveditems, "ResetBetLossValue"), ref convert);
                    chkResetBetWins.Checked = ("1"==getvalue(saveditems, "ResetBetWinsEnabled"));
                    nudResetWins.Value = (decimal)dparse(getvalue(saveditems, "ResetWinsValue"), ref convert);
                    nudWinMultiplier.Value = decimal.Parse(getvalue(saveditems, "WinMultiplier"));
                    nudWinMaxMultiplies.Value = decimal.Parse(getvalue(saveditems, "WinMaxMultiplies"));
                    nudWinNBets.Value = decimal.Parse(getvalue(saveditems, "WinNBets"));
                    nudWinDevider.Value = decimal.Parse(getvalue(saveditems, "WinDevider"));
                    temp = getvalue(saveditems, "WinMultiplyMode");
                    rdbWinConstant.Checked = ("0" == temp);
                    rdbWinDevider.Checked = ("1" == temp);
                    rdbWinMaxMultiplier.Checked = ("2" == temp);
                    rdbWinReduce.Checked = ("3" == temp);
                    chkBotSpeed.Checked = ("1"==getvalue(saveditems, "BotSpeedEnabled"));
                    nudBotSpeed.Value = (decimal)dparse(getvalue(saveditems, "BotSpeedValue"), ref convert);
                    chkResetSeed.Checked = ("1"==getvalue(saveditems, "ResetSeedEnabled"));
                    temp = getvalue(saveditems, "ResetSeedMode");
                    rdbResetSeedBets.Checked = ("0" == temp);
                    rdbResetSeedWins.Checked = ("1" == temp);
                    rdbResetSeedLosses.Checked = ("2" == temp);
                    nudResetSeed.Value = (decimal)dparse(getvalue(saveditems, "ResetSeedValue"), ref convert);

                    chkStopLossStreak.Checked = ("1" == getvalue(saveditems, "StopAfterLoseStreakEnabled"));
                    nudStopLossStreak.Value = (decimal)dparse(getvalue(saveditems, "StopAfterLoseStreakValue"), ref convert);
                    chkStopLossBtcStreak.Checked = ("1" == getvalue(saveditems, "StopAfterLoseStreakBtcEnabled"));
                    nudStopLossBtcStreal.Value = (decimal)dparse(getvalue(saveditems, "StopAfterLoseStreakBtcValue"), ref convert);
                    chkStopLossBtc.Checked = ("1" == getvalue(saveditems, "StopAfterLoseBtcEnabled"));
                    nudStopLossBtc.Value = (decimal)dparse(getvalue(saveditems, "StopAfterLoseBtcValue"), ref convert);

                    chkChangeLoseStreak.Checked = ("1" == getvalue(saveditems, "ChangeAfterLoseStreakEnabled"));
                    nudChangeLoseStreak.Value = (decimal)dparse(getvalue(saveditems, "ChangeAfterLoseStreakSize"), ref convert);
                    nudChangeLoseStreakTo.Value = (decimal)dparse(getvalue(saveditems, "ChangeAfterLoseStreakTo"), ref convert);


                    chkStopWinStreak.Checked = ("1" == getvalue(saveditems, "StopAfterWinStreakEnabled"));
                    nudStopWinStreak.Value = (decimal)dparse(getvalue(saveditems, "StopAfterWinStreakValue"), ref convert);
                    chkStopWinBtcStreak.Checked = ("1" == getvalue(saveditems, "StopAfterWinStreakBtcEnabled"));
                    nudStopWinBtcStreak.Value = (decimal)dparse(getvalue(saveditems, "StopAfterWinStreakBtcValue"), ref convert);
                    chkStopWinBtc.Checked = ("1" == getvalue(saveditems, "StopAfterWinBtcEnabled"));
                    nudStopWinBtc.Value = (decimal)dparse(getvalue(saveditems, "StopAfterWinBtcValue"), ref convert);

                    chkChangeWinStreak.Checked = ("1" == getvalue(saveditems, "ChangeAfterWinStreakEnabled"));
                    nudChangeWinStreak.Value = (decimal)dparse(getvalue(saveditems, "ChangeAfterWInStreakSize"), ref convert);
                    nudChangeWinStreakTo.Value = (decimal)dparse(getvalue(saveditems, "ChangeAfterWInStreakTo"), ref convert);

                    chkChangeChanceLose.Checked = ("1" == getvalue(saveditems, "ChangeChanceAfterLoseStreakEnabled"));
                    nudChangeChanceLoseStreak.Value = (decimal)dparse(getvalue(saveditems, "ChangeChanceAfterLoseStreakSize"), ref convert);
                    nudChangeChanceLoseTo.Value = (decimal)dparse(getvalue(saveditems, "ChangeChanceAfterLoseStreakValue"), ref convert);

                    chkChangeChanceWin.Checked = ("1" == getvalue(saveditems, "ChangeChanceAfterWinStreakEnabled"));
                    nudChangeChanceWinStreak.Value = (decimal)dparse(getvalue(saveditems, "ChangeChanceAfterWinStreakSize"), ref convert);
                    nudChangeChanceWinTo.Value = (decimal)dparse(getvalue(saveditems, "ChangeChanceAfterWinStreakValue"), ref convert);

                    nudMKIncrement.Value = (decimal)dparse(getvalue(saveditems, "MKIncrement"), ref convert);
                    nudMKDecrement.Value = (decimal)dparse(getvalue(saveditems, "MKDecrement"), ref convert);
                    chkMK.Checked = ("1" == getvalue(saveditems, "MKEnabled"));
                    txtQuickSwitch.Text = getvalue(saveditems, "QuickSwitchFolder");
                    if (txtQuickSwitch.Text!="")
                    {
                        btnStratRefresh_Click(btnStratRefresh, new EventArgs() );
                    }

                    
                    chkReverseLab.Checked = ("1" == getvalue(saveditems, "LabReverse"));

                   string[] tmp =getvalue(saveditems, "LabValues").Split('?');
                    if (tmp.Length>0)
                    { 
                        if (tmp[0]!="0-0-0")
                            rtbBets.Lines = getvalue(saveditems, "LabValues").Split('?');
                    }
                    rdbLabRestart.Checked = ("1" == getvalue(saveditems, "LabComplete"));
                    rdbLabStop.Checked = ("2" == getvalue(saveditems, "LabComplete"));

                    int tmpI = int.Parse(getvalue(saveditems, "Site"));
                    justDiceToolStripMenuItem.Checked = tmpI == 0;
                    primeDiceToolStripMenuItem.Checked = tmpI == 1;
                    pocketRocketsCasinoToolStripMenuItem.Checked = tmpI == 2;
                    diceToolStripMenuItem.Checked = tmpI == 3;
                    safediceToolStripMenuItem.Checked = tmpI == 4;
                    if (tmpI>4)
                    {
                        justDiceToolStripMenuItem.Checked = true; ;
                    }
                    tmpI = int.Parse(getvalue(saveditems, "SettingsMode"));
                    basicToolStripMenuItem.Checked = tmpI == 0;
                    advancedToolStripMenuItem.Checked = tmpI == 1;
                    programmerToolStripMenuItem.Checked = tmpI == 2;


                    int Strat = int.Parse(getvalue(saveditems, "Strategy"));
                    switch (Strat)
                    {
                        case 0: rdbMartingale.Checked = true; break;
                            case 1: rdbLabEnable.Checked = true; break;
                            case 2: rdbFibonacci.Checked = true; break;
                            case 3: rdbAlembert.Checked = true; break;
                            case 4: rdbPreset.Checked = true; break;
                    }

                    
                    temp = getvalue(saveditems,"FibonacciLoss" );
                    rdbFiboLossIncrement.Checked = temp=="0";
                    rdbFiboLossReset.Checked = temp=="1";
                    rdbFiboLossStop.Checked = temp=="2";
                    nudFiboLossIncrement.Value = decimal.Parse(getvalue(saveditems,"FibonacciLossSteps"));

                    temp = getvalue(saveditems,"FibonacciLevel" );
                    rdbFiboLevelReset.Checked = temp=="1";
                    rdbFiboLevelStop.Checked = temp == "2";
                    chkFiboLevel.Checked = getvalue(saveditems, "FibonacciLevelEnabled") == "1";
                    nudFiboLeve.Value = decimal.Parse(getvalue(saveditems, "FibonnaciLevelSteps"));

                    temp = getvalue(saveditems,"FibonacciWin" );
                    rdbFiboWinIncrement.Checked = temp=="0";
                    rdbFiboWinReset.Checked = temp=="1";
                    rdbFiboWinStop.Checked = temp=="2";
                    nudFiboWinIncrement.Value = decimal.Parse(getvalue(saveditems, "FibonacciWinSteps"));


                   
                    nudAlembertIncrementLoss.Value = decimal.Parse(getvalue(saveditems, "dAlembertLossIncrement"));
                    nudAlembertStretchLoss.Value = decimal.Parse(getvalue(saveditems, "dAlembertLossStretch"));
                    nudAlembertIncrementWin.Value = decimal.Parse(getvalue(saveditems, "dAlembertWinIncrement"));
                    nudAlembertStretchWin.Value = decimal.Parse(getvalue(saveditems, "dAlembertWinStretch"));


                  
                    tmp = getvalue(saveditems, "PresetValues").Split('?');
                    if (tmp.Length > 0)
                    {
                        if (tmp[0] != "0-0-0")
                            rtbBets.Lines = tmp;
                    }
                    temp = getvalue(saveditems, "PresetEnd");
                    rdbPresetEndReset.Checked = temp == "0";
                    rdbPresetEndStep.Checked = temp == "1";
                    rdbPresetEndStop.Checked = temp == "2";
                    temp = getvalue(saveditems, "PresetLoss");
                    rdbPresetLossReset.Checked = temp == "0";
                    rdbPresetLossStep.Checked = temp == "1";
                    rdbPresetLossStop.Checked = temp == "2";
                    temp = getvalue(saveditems, "PresetWin");
                    rdbPresetWinReset.Checked = temp == "0";
                    rdbPresetWinStep.Checked = temp == "1";
                    rdbPresetWinStop.Checked = temp == "2";

                    nudPresetEndStep.Value = decimal.Parse(getvalue(saveditems, "PresetEndStep"));
                    nudPresetLossStep.Value = decimal.Parse(getvalue(saveditems, "PresetLossStep"));
                    nudPresetWinStep.Value = decimal.Parse(getvalue(saveditems, "PresetWinStep"));


                    chkZigZagWins.Checked = getvalue(saveditems, "ReverseWin") == "1";
                    chkZigZagWinsStreak.Checked = getvalue(saveditems, "ReversWinStreak") == "1";
                    chkZigZagLoss.Checked = getvalue(saveditems, "ReverseLoss") == "1";
                    chkZigZagLossStreak.Checked = getvalue(saveditems, "ReverseLossStreak") == "1";
                    chkZigZagBets.Checked = getvalue(saveditems, "ReverseBets") == "1";

                    nudZigZagWins.Value = decimal.Parse(getvalue(saveditems, "ReverseWinValue"));
                    nudZigZagWinsStreak.Value = decimal.Parse(getvalue(saveditems, "ReverseWinStreakValue"));
                    nudZigZagLoss.Value = decimal.Parse(getvalue(saveditems, "ReverseLossValue"));
                    nudZigZagLossStreak.Value = decimal.Parse(getvalue(saveditems, "ReverseLossStreakValue"));
                    nudZigZagBets.Value = decimal.Parse(getvalue(saveditems, "ReverseBetValue"));


                    chkResetBtcStreakLoss.Checked = getvalue(saveditems, "ResetBtcStreakLoss") == "1";
                    nudResetBtcStreakLoss.Value = (decimal)dparse(getvalue(saveditems, "ResetBtcStreakLossValue"), ref convert); 
                    chkResetBtcLoss.Checked = getvalue(saveditems, "ResetBtcLoss") == "1";
                    nudResetBtcLoss.Value = (decimal)dparse(getvalue(saveditems, "ResetBtcLossValue"), ref convert);

                    chkResetBtcStreakProfit.Checked = getvalue(saveditems, "ResetBtcStreakProfit") == "1";
                    nudResetBtcStreakProfit.Value = (decimal)dparse(getvalue(saveditems, "ResetBtcStreakProfitValue"), ref convert);
                    chkResetBtcProfit.Checked = getvalue(saveditems, "ResetBtcProfit") == "1";
                    nudResetBtcProfit.Value = (decimal)dparse(getvalue(saveditems, "ResetBtcProfitValue"), ref convert);

                    chkFirstResetLoss.Checked = getvalue(saveditems, "FirstResetLoss") == "1";
                    chkFirstResetWin.Checked = getvalue(saveditems, "FirstResetWin") == "1";
                }

                
                
                variabledisable();
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool loadsettingsold()
        {
            using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\Psettings"))
            {
                //NCPuser,ncppass,autologin
                string info = sr.ReadLine();
                string[] chars = info.Split(' ');
                string suser = "";
                string spass = "";
                bool login = false;
                bool start = false;
                int word = 0;
                for (int i = 0; i < chars.Length; i++)
                {
                    int num = 0;
                    if (int.TryParse(chars[i], out num))
                    {
                        if ((char)num == ',' || (char)num == ';')
                            word++;
                        else
                            switch (word)
                            {
                                case 0: suser += (char)num; break;
                                case 1: spass += (char)num; break;
                                case 2: if ((char)num == '1') login = true; else login = false; break;
                                case 3: if ((char)num == '1') start = true; else start = false; break;
                            }
                    }
                }
                autologin = login;
                password = spass;
                username = suser;
                autostart = start;
                if (word == 2)
                {

                    info = sr.ReadLine();
                    chars = info.Split(' ');
                    suser = "";
                    spass = "";

                    login = false;
                    word = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        int num = 0;
                        if (int.TryParse(chars[i], out num))
                        {
                            if ((char)num == ',')
                                word++;
                            else
                                switch (word)
                                {
                                    case 0: suser += (char)num; break;
                                    case 1: spass += (char)num; break;
                                    case 2: if ((char)num == '1') login = true; else login = false; break;
                                    case 3: if ((char)num == '1') start = true; else start = false; break;
                                }
                        }
                    }
                    autologin = login;
                    password = spass;
                    username = suser;
                    autostart = start;
                }

                info = sr.ReadLine();
                string[] values = info.Split(',');
                if (info.Contains(";"))
                    values = info.Split(';');
                int j = 0;
                tray = (values[j++] == "1");
                Botname = values[j++];
                if (values[j++] != "1")
                {
                    Emails.Enable = false;
                    Emails.emailaddress = values[j++];
                    Emails.Lower = Emails.Streak = Emails.Withdraw = (values[j] == "1");
                    j += 3;
                }
                else
                {
                    Emails.Enable = true;
                    Emails.emailaddress = values[j++];
                    Emails.Withdraw = ((values[j++] == "1"));
                    Emails.Lower = ((values[j++] == "1"));
                    Emails.Streak = ((values[j++] == "1"));
                }
                Emails.StreakSize = iparse(values[j++]);
                if (values.Count() == j)
                {
                    Emails.SMTP = values[j++];
                }

                info = sr.ReadLine();
                values = info.Split(',');
                j = 0;
                SoundWithdraw = (values[j++] == "1");
                if (values[j++] != "1")
                {
                    Sound = false;
                    SoundLow = SoundStreak = (values[j] == "1");
                    j += 2;
                }
                else
                {
                    Sound = true;
                    SoundLow = ((values[j++] == "1"));
                    SoundStreak = ((values[j++] == "1"));
                }
                SoundStreakCount = iparse(values[j++]);

            }
            return true;
        }

        bool autoseeds = true;
        public void loadsettings()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\Psettings"))
                {

                    string test = sr.ReadLine();
                    if (test != "new")
                        loadsettingsold();
                    else
                    {
                        string info = sr.ReadLine();
                        string[] chars = info.Split(' ');
                        string suser = "";
                        string spass = "";
                        bool login = false;
                        bool start = false;
                        int word = 0;
                        for (int i = 0; i < chars.Length; i++)
                        {
                            int num = 0;
                            if (int.TryParse(chars[i], out num))
                            {
                                if ((char)num == ',' || (char)num == ';')
                                    word++;
                                else
                                    switch (word)
                                    {
                                        case 0: suser += (char)num; break;
                                        case 1: spass += (char)num; break;
                                        case 2: if ((char)num == '1') login = true; else login = false; break;
                                        case 3: if ((char)num == '1') start = true; else start = false; break;
                                    }
                            }
                        }
                        autologin = login;
                        password = spass;
                        username = suser;
                        autostart = start;
                        if (word == 2)
                        {

                            info = sr.ReadLine();
                            chars = info.Split(' ');
                            suser = "";
                            spass = "";

                            login = false;
                            word = 0;
                            for (int i = 0; i < chars.Length; i++)
                            {
                                int num = 0;
                                if (int.TryParse(chars[i], out num))
                                {
                                    if ((char)num == ',')
                                        word++;
                                    else
                                        switch (word)
                                        {
                                            case 0: suser += (char)num; break;
                                            case 1: spass += (char)num; break;
                                            case 2: if ((char)num == '1') login = true; else login = false; break;
                                            case 3: if ((char)num == '1') start = true; else start = false; break;
                                        }
                                }
                            }
                            autologin = login;
                            password = spass;
                            username = suser;
                            autostart = start;
                        }
                        List<SavedItem> saveditems = new List<SavedItem>();
                        while (!sr.EndOfStream)
                        {
                            string[] temp = sr.ReadLine().Split('|');
                            saveditems.Add(new SavedItem(temp[0],temp[1]));
                        }
                        
                        tray = ("1"==getvalue(saveditems, "Tray"));
                        Botname = getvalue(saveditems, "BotName");
                        Emails.Enable = ("1"==getvalue(saveditems, "enableEmail"));
                        Emails.emailaddress = getvalue(saveditems, "emailaddress");
                        Emails.Withdraw = ("1"==getvalue(saveditems, "emailwithdraw"));
                        Emails.Lower = ("1"==getvalue(saveditems, "emaillow"));
                        Emails.Streak = ("1" == getvalue(saveditems, "emailstreak"));
                        Emails.StreakSize = iparse(getvalue(saveditems, "emailstreakval"));
                        Emails.SMTP = getvalue(saveditems, "SMTP");

                        SoundWithdraw = ("1" ==getvalue(saveditems, "CoinEnabled"));
                        ching= getvalue(saveditems, "CoinPath");
                        Sound = ("1"==getvalue(saveditems, "AlarmEnabled"));
                        SoundLow = ("1" == getvalue(saveditems, "AlarmLowEnabled"));
                        SoundStreak = ("1" == getvalue(saveditems, "AlarmStreakEnabled"));
                        SoundStreakCount =iparse(getvalue(saveditems, "AlarmStreakValue"));
                        salarm= getvalue(saveditems, "AlarmPath");
                        Emails.StreakSize = (int)Emails.StreakSize;
                        autoseeds = getvalue(saveditems, "AutoGetSeed") != "0";
                        maxRows = iparse(getvalue(saveditems, "NumLiveBets"));
                    }

                }

                
                
            }

            catch
            {

            }
        }

        void writesettings(Settings TmpSet)
        {
            using (StreamWriter sw = new StreamWriter(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\Psettings"))
            {
                sw.WriteLine("new");
                string temp2 = TmpSet.txtJDUser.Text + "," + TmpSet.txtJDPass.Text + ",";
                if (TmpSet.chkJDAutoLogin.Checked)
                    temp2 += "1,";
                else temp2 += "0";
                if (TmpSet.chkJDAutoStart.Checked)
                    temp2 += "1,";
                else temp2 += "0";
                string jdline = "";

                foreach (char c in temp2)
                {
                    jdline += ((int)c).ToString() + " ";
                }
                sw.WriteLine(jdline);

                ////tray,botname,enableemail,emailaddress,emailwithdraw,emailinvest,emaillow,emailstreak,emailstreakval
                string msg = "";
                msg = (TmpSet.chkTray.Checked) ? "1" : "0";                
                sw.WriteLine("tray|"+msg);
                sw.WriteLine("botname|" + TmpSet.txtBot.Text);
                msg = (TmpSet.chkEmail.Checked) ? "1" : "0";  
                sw.WriteLine("enableemail|"+msg);
                sw.WriteLine("emailaddress|" + TmpSet.txtEmail.Text);
                msg = (TmpSet.chkEmailWithdraw.Checked) ? "1" : "0";  
                sw.WriteLine("emailwithdraw|"+msg);
                msg = (TmpSet.chkEmailLowLimit.Checked) ? "1" : "0";  
                sw.WriteLine("emaillow|"+msg);
                msg = (TmpSet.chkEmailStreak.Checked) ? "1" : "0";  
                sw.WriteLine("emailstreak|"+msg);
                sw.WriteLine("emailstreakval|" + TmpSet.nudEmailStreak.Value.ToString());
                sw.WriteLine("SMTP|" + Emails.SMTP);
                

                ////soundcoin,soundalarm,soundlower,soundstrea,soundstreakvalue

                msg = (TmpSet.chkSoundWithdraw.Checked) ? "1" : "0";
                sw.WriteLine("CoinEnabled|" + msg);
                sw.WriteLine("CoinPath|" + TmpSet.txtPathChing.Text);
                msg = (TmpSet.chkAlarm.Checked) ? "1" : "0";
                sw.WriteLine("AlarmEnabled|" + msg);
                msg = (TmpSet.chkSoundLowLimit.Checked) ? "1" : "0";
                sw.WriteLine("AlarmLowEnabled|" + msg);
                msg = (TmpSet.chkSoundStreak.Checked) ? "1" : "0";
                sw.WriteLine("AlarmStreakEnabled|" + msg);

                sw.WriteLine("AlarmStreakValue|" + TmpSet.nudSoundStreak.Value.ToString());
                sw.WriteLine("AlarmPath|" + TmpSet.txtPathAlarm.Text);

                sw.WriteLine("AutoGetSeed|"+ (autoseeds?"1":"0"));
                sw.WriteLine("NumLiveBets|" + TmpSet.nudLiveBetsNum.Value);

            }
        }
        
        private void btnImport_Click(object sender, EventArgs e)
        {
            bool valid = true;
            DialogResult d = MessageBox.Show("Importing a new profile will override default bet settings. Do you want to export current settings before importing?", "Import", MessageBoxButtons.YesNoCancel);
            if (d == System.Windows.Forms.DialogResult.No)
            {
                if (ofdImport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    load(ofdImport.FileName);
                    valid = true;
                }
                else
                    valid = false;
            }
            else if (valid)
                if (d == System.Windows.Forms.DialogResult.Yes && valid)
                {
                    valid = false;
                    if (ofdExport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        save(ofdExport.FileName);
                        valid = true;
                    }
                    if (valid)
                        if (ofdImport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            load(ofdImport.FileName);
                        }
                }
                else if (d == System.Windows.Forms.DialogResult.Cancel)
                {


                }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (ofdExport.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                save(ofdExport.FileName);

            }
        }
        #endregion

        #region test and parse variables

        private void variabledisable()
        {
            //set active controlls for on loss multiplier
            if (rdbConstant.Checked)
            {
                nudMaxMultiplies.Enabled = false;
                nudNbets.Enabled = false;
                nudDevider.Enabled = false;
            }
            if (rdbDevider.Checked || rdbReduce.Checked)
            {
                nudMaxMultiplies.Enabled = false;
                nudNbets.Enabled = true;
                nudDevider.Enabled = true;
            }
            if (rdbMaxMultiplier.Checked)
            {
                nudMaxMultiplies.Enabled = true;
                nudNbets.Enabled = false;
                nudDevider.Enabled = false;
            }

            //set active controlls for on win multiplier
            if (rdbWinConstant.Checked)
            {
                nudWinMaxMultiplies.Enabled = false;
                nudWinNBets.Enabled = false;
                nudWinDevider.Enabled = false;
            }
            if (rdbWinDevider.Checked || rdbWinReduce.Checked)
            {
                nudWinMaxMultiplies.Enabled = false;
                nudWinNBets.Enabled = true;
                nudWinDevider.Enabled = true;
            }
            if (rdbWinMaxMultiplier.Checked)
            {
                nudWinMaxMultiplies.Enabled = true;
                nudWinNBets.Enabled = false;
                nudWinDevider.Enabled = false;
            }
        }

        private void txtChance_Leave(object sender, EventArgs e)
        {
            if ((sender as Control).Name == "nudMultiplier")
            {
                if ((sender as NumericUpDown).Value != nudMutliplier2.Value)
                    nudMutliplier2.Value = (sender as NumericUpDown).Value;
            }
            if ((sender as Control).Name == "nudWinMultiplier")
            {
                if ((sender as NumericUpDown).Value != nudWinMultiplier2.Value)
                    nudWinMultiplier2.Value = (sender as NumericUpDown).Value;
            }
            if ((sender as Control).Name == "nudMinBet")
            {
                if ((sender as NumericUpDown).Value != nudMinbet2.Value)
                    nudMinbet2.Value = (sender as NumericUpDown).Value;
            }
            if ((sender as Control).Name == "nudChance")
            {
                if ((sender as NumericUpDown).Value != nudChance2.Value)
                    nudChance2.Value = (sender as NumericUpDown).Value;
            }

            testInputs();
            try
            {
                CurrentSite.chance =  (Chance);
                                
            }
            catch
            {

            }
        }

        double dparse(string text,ref bool success)
        {
            double number = -1;
            string test = "0.000001";
            double dtest = 0;
            if (double.TryParse(test, out dtest))
            {
                if (dtest != 0.000001)
                {
                    text = text.Replace(".", ",");
                }
                else
                {
                    text = text.Replace(",", ".");
                }
            }
            else
            {
                text = text.Replace(".", ",");
            }

            
            if (!double.TryParse(text, out number))
            {
                
                if (!double.TryParse(text, out number))
                {
                    success = false;
                    return -1;
                    
                }
            }
            success = true;
            return number;
        }
        public int iparse(string text)
        {
            int number = -1;
            if (!int.TryParse(text, out number))
            {
                if (text.Contains("."))
                    text = text.Replace('.', ',');
                else if (text.Contains(","))
                    text = text.Replace(',', '.');
                if (!int.TryParse(text, out number))
                    number = -1;
            }
            return number;
        }

        bool testInputs()
        {
            
            string sMessage = "";
            bool valid = true;
            Limit = (double)(nudLimit.Value);
            if (Limit == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Limit Field\n";
            }
            LowerLimit = (double)(nudLowerLimit.Value);
            if (LowerLimit == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Limit Field\n";
            }
            Amount = (double)(nudAmount.Value);
            if (Amount==-1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Amount Field\n";
            }
            if (txtTo.Text == "")
            {
                valid = false;
                sMessage += "Please enter a valid Address in the Address Field\n";
            }
            MinBet = (double)(nudMinBet.Value);
            if (MinBet==-1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Minimum Bet Field\n";
            }
            if (!programmerToolStripMenuItem.Checked)
            Chance = (double)(nudChance.Value);
            if (Chance == -1)
            {
                valid = false;
                sMessage += "Please enter a valid % in the Chance Field (Without the % sign)";
            }
            else
            {

            }
            Multiplier = (double)(nudMultiplier.Value);
            if (Multiplier == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Multiplier Field\n";
            }
            MaxMultiplies= (int)(nudMaxMultiplies.Value);
            if (MaxMultiplies==-1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Max Multplies Field\n";
            }
            Devidecounter = (int)( nudNbets.Value);
            if (Devidecounter==-1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the After n bets Field\n";
            }
            Devider = (double)(nudDevider.Value);
            if (Devider == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Times Multiplier By Field\n";
            }
            WinMultiplier = (int)(nudWinMultiplier.Value);
            if (WinMultiplier == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Multiplier Field\n";
            }
            WinMaxMultiplies = (int)(nudWinMaxMultiplies.Value);
            if (WinMaxMultiplies == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Max Multplies Field\n";
            }
            WinDevidecounter = (int)(nudWinNBets.Value);
            if (WinDevidecounter == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the After n bets Field\n";
            }
            WinDevider = (double)(nudWinDevider.Value);
            if (WinDevider == -1)
            {
                valid = false;
                sMessage += "Please enter a valid number in the Times Multiplier By Field\n";
            }
            populateFiboNacci();
            if (!valid)
                MessageBox.Show(sMessage);
            return valid;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            int max =  maxbets();
            if (max == -500)
                StatsWindows.lblMaxBets.Text = "500+";
            else
                StatsWindows.lblMaxBets.Text = max.ToString();

            variabledisable();
            
        }
        #endregion

        

        private void btnAbout_Click(object sender, EventArgs e)
        {
            new About().Show();
        }

        #region Tray Icon and popups

        private void cDiceBot_Resize(object sender, EventArgs e)
        {
            if (tray)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    
                    TrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                    TrayIcon.BalloonTipText = "DiceBot is still running";
                    TrayIcon.BalloonTipTitle = "DiceBot";
                    TrayIcon.BalloonTipIcon = ToolTipIcon.None;
                    TrayIcon.ShowBalloonTip(500);
                    this.Hide();
                }
            }
        }

        void menuitemclick(object sender, EventArgs e)
        {
            string name = (sender as ToolStripDropDownItem).Text;

            if (name.ToLower() == "show")
            {
                this.Show();
                this.WindowState = FormWindowState.Maximized;
                this.BringToFront();
            }
            else if (name.ToLower() == "start")
            {
                Start(false);
            }
            else if (name.ToLower() == "stop")
            {
                Stop();
            }
            else if (name.ToLower() == "close")
            {
                if (MessageBox.Show("Are you sure you want to close DiceBot?", "Close DeciBot", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    Application.Exit();
                }
            }
        }

        private void TrayIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            string curstreak = "";
            Thread.Sleep(200);
            if (Winstreak == 0)
            {
                curstreak = Losestreak.ToString() + " Losses";
            }
            else
            {
                curstreak = Winstreak.ToString() + "Wins";
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right  )
            {

                this.Show();
                this.WindowState = FormWindowState.Maximized;
                this.BringToFront();
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                TrayIcon.BalloonTipTitle = "DiceBot";
                TrayIcon.BalloonTipText = string.Format("Balance: {0:0.00000000}\n Profit: {1:0.00000000}\nCurrent Streak: {2}\nWorst Streak: {3}\nTime running: ", PreviousBalance, PreviousBalance - StartBalance, curstreak, WorstStreak) + (TotalTime + (DateTime.Now - dtStarted)).ToString(@"hh\:mm\:ss");
                TrayIcon.BalloonTipIcon = ToolTipIcon.None;
                TrayIcon.ShowBalloonTip(800);
            }
            
        }

        private void TrayIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Maximized;
            this.BringToFront();
        }


        #endregion

        
        #region Simulate and bet generator
        Simulation tempsim;
        
        Thread simthread;
        string server = "";
        string client = "";
        double tmpbalance = 0;
        int tmpwins = 0;
        int tmplosses = 0;
        double tmpStartBalance = 0;
        void runsim()
        {
            tmpbalance = PreviousBalance;
            tmpwins = Wins;
            tmplosses = Losses;
            tmpStartBalance = StartBalance;
            StartBalance = dPreviousBalance = (double)SimWindow.nudSimBalance.Value;
            Wins = Losses = 0;
            
            
            
            string chars = "0123456789abcdef";
            if (! (CurrentSite is dice999))
            {
                chars += "ghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ._";
            }
            server = "";

            for (int i = 0; i < 64; i++)
            {
                server += (chars[rand.Next(0, chars.Length)]);
            }
            client = "";
            if (CurrentSite is dice999)
            {
                client = rand.Next(0, int.MaxValue).ToString();
            }
            else
            for (int i = 0; i < 24; i++)
            {
                client += rand.Next(0, 10).ToString();
            }

            string sserver = "";
            foreach (byte b in server)
            {
                sserver += Convert.ToChar(b);
            }
            
            tempsim = new Simulation(dPreviousBalance.ToString("0.00000000"), (Wins+Losses).ToString(), sserver, client);
            RunningSimulation = true;
            stop = false;
            Lastbet = MinBet;
            Start(false);
            
        }

        void Simbet()
        {
            dtLastBet = DateTime.Now;
            EnableTimer(tmBet, false);
            Bet tmp = new Bet();
            if (Wins + Losses <= SimWindow.nudSimNumBets.Value)
            {
                string betstring = (Wins + Losses).ToString() + ",";
                double number = CurrentSite.GetLucky(server, client, Wins + Losses);
                tmp.Roll = (decimal)number;
                tmp.Chance = (decimal)Chance;
                tmp.Amount = (decimal)Lastbet;
                tmp.high = high;
                tmp.date = DateTime.Now;
                
                betstring += number.ToString() + "," + Chance.ToString() + ",";
                bool win = false;
                if (high)
                    betstring += ">" + (99.99 - Chance) + ",";
                else
                    betstring += "<" + Chance + ",";
                if (high && number > 99.99 - Chance)
                {
                    win = true;
                }
                else if (!high && number < Chance)
                {
                    win = true;
                }
                double betProfit = 0;
                if (win)
                {
                    betstring += "win,";
                    betstring += Lastbet + ",";
                    betProfit = (Lastbet * 99 / Chance) - Lastbet;
                    betstring += betProfit  + ",";
                    tmp.Profit = (decimal)betProfit;    

                }
                else
                {

                    betstring += "lose,";
                    betstring += Lastbet + ",";
                    betProfit = -Lastbet ;
                    betstring +=  betProfit +",";
                    tmp.Profit = (decimal)betProfit;
                }
                this.PreviousBalance = dPreviousBalance + betProfit;
                betstring += PreviousBalance + ",";
                betstring += profit;
                tempsim.bets.Add(betstring);
                int bets = Wins + Losses;
                if (bets % 1000 == 0)
                {
                    Updatetext(SimWindow.lblSimProgress, ((double)bets / (double)SimWindow.nudSimNumBets.Value * 100.00).ToString("00.00") + "%");
                }
                if (bets % 10000 == 0)
                {
                    using (StreamWriter sw = File.AppendText(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim"))
                    {
                        foreach (string tmpbet in tempsim.bets)
                        {
                            sw.WriteLine(tmpbet);
                        }

                    }
                    tempsim.bets.Clear();
                }

                GetBetResult(PreviousBalance, tmp);
            }
            else
                Stop();
            
        }

        delegate void DelAlterMsgLog(Control TextBox, string Text);
        public static void Updatetext(Control TextBox, string Text)
        {
            if (TextBox.InvokeRequired)
            {
                DelAlterMsgLog del = new DelAlterMsgLog(Updatetext);
                TextBox.Invoke(del, TextBox, Text);
            }
            else
            {
                TextBox.Text = Text;
            }
        }

        public void btnSim_Click(object sender, EventArgs e)
        {
            if (! stop)
            {
                MessageBox.Show("Please stop the bot before running a simulation.");
            }
            else
            { 
                bool go = true;
                if (SimWindow.nudSimNumBets.Value >= 1000000)
                {
                    go = (MessageBox.Show("To keep RAM usage to a minimum, "+
                                            "\nthe sim data is temporarily stored on your"+
                                            "\nlocal C: drive. This file can become very large," +
                                            "\nApproximately 80MB per 1M bets. This file is"+
                                            "\ndeleted when the bot is closed normally.\n\nContinue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes);
                }
                if (File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim"))
                {
                    File.Delete(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim");
                }
                tmrSimulation.Enabled = true;
                SimWindow.lblSimRun.Text = "Running Simulation, Please Wait";
                SimWindow.lblSimRun.ForeColor = Color.Red;
                simthread = new Thread(new ThreadStart(runsim));
                simthread.Start();
            }
        }

        

        public void btnExportSim_Click(object sender, EventArgs e)
        {
            SaveFileDialog svdExportSim = new SaveFileDialog();
            svdExportSim.DefaultExt = "csv";
            svdExportSim.AddExtension = true;
            if (svdExportSim.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                try
                {
                    if (lastsim == null)
                    {
                        MessageBox.Show("Please run a simulation first");
                    }
                    else
                    {
                       
                        File.Copy(Environment.GetEnvironmentVariable("APPDATA") + "\\DiceBot2\\tempsim", svdExportSim.FileName);
                        
                        MessageBox.Show("exported to " + svdExportSim.FileName);
                    }
                }
                catch
                {
                    MessageBox.Show("Failed exporting to " + svdExportSim.FileName);
                }
            }
        }

        public void GenerateBets_Click(string ClientSeed, string ServerSeed, long StartValue, long Amount)
        {
            if (ClientSeed != "" && ServerSeed != "")
            {
                List<string> Betlist = new List<string>();
                string headers = "betnumber,luckynumber,,Please note, This algorithm is still in testing, some Numbers Might be wrong.\n,,,Check the alternative roll verifier";
                Betlist.Add(headers);
                byte[] server = new byte[64];

                for (decimal i = StartValue; i < StartValue + Amount; i++)
                {
                    string curstring = i.ToString() + "," + CurrentSite.GetLucky(ServerSeed, ClientSeed, (int)i).ToString();
                    Betlist.Add(curstring);
                }
                try
                {
                    using (StreamWriter sw = new StreamWriter("LuckyNum-" + DateTime.Now.ToShortDateString().Replace("/","-") + ".csv"))
                    {
                        foreach (string s in Betlist)
                        {
                            sw.WriteLine(s);
                        }
                    }
                    MessageBox.Show("Saved bets to: " + "LuckyNum-" + DateTime.Now.ToShortDateString().Replace("/", "-") + ".csv");
                }
                catch
                {
                    MessageBox.Show("Failed saving bets to: " + "LuckyNum-" + DateTime.Now.ToShortDateString().Replace("/", "-") + ".csv");
                }
            }
            else
            {
                MessageBox.Show("Please enter a server seed and a client seed");
            }
        }

        private void tmrSimulation_Tick(object sender, EventArgs e)
        {
            if (!RunningSimulation)
            {
                tmrSimulation.Enabled = false;
                SimWindow.lblSimRun.ForeColor = Color.Green;
                SimWindow.lblSimRun.Text = "Simulation Completed";
                lastsim = tempsim;
            }
        }
        #endregion

        private void btnResetStats_Click(object sender, EventArgs e)
        {
            Wins = 0;
            Losses = 0;
            bool success = false;
            profit = 0;
            double tmp = CurrentSite.GetbalanceValue();
            if (success)
                StartBalance = tmp;
            Winstreak = Losestreak = BestStreak = laststreaklose = laststreakwin = WorstStreak = BestStreak2 = WorstStreak3 = BestStreak3 = WorstStreak3 = numstreaks = numwinstreasks = numlosesreaks = 0;
            avgloss = avgstreak = LargestBet = LargestLoss = LargestWin = avgwin = 0.0;
            UpdateStats();
        }

        private void btnSaveUser_Click(object sender, EventArgs e)
        {
            Settings tmpSet = new DiceBot.Settings(this);
            if (tmpSet.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                writesettings( tmpSet );
                loadsettings();
            }
        }

        
        private void nudBotSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (nudBotSpeed.Value != (decimal)0.0)
            {
                lblTimeBetween.Text = (1 / nudBotSpeed.Value).ToString("##0.0000") + "Seconds";
            }
        }



        

       

       

        #region charts
        //button for generating random charts - for testing purposes
        private void button1_Click(object sender, EventArgs e)
        {
            List<Bet> tmpBets = new List<Bet>();
            double previous = 0;
            for (int i = 0; i < r.Next(1000, 100000); i++)
            {
                
                int tmp = r.Next(0, 10);
                if (tmp % 2 == 0)
                {
                    previous -= tmp;
                }
                else
                    previous += tmp;
                tmpBets.Add(new Bet { Id = i, Profit = (decimal)previous });
            }

            Graph g = new Graph(tmpBets.ToArray());
            g.Show();
        }

    

        #region generate charts


        
        
        private void btnChartBetID_Click(object sender, EventArgs e)
        {
            Custom_Chart tmp = new Custom_Chart();
            if (tmp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (tmp.ChartType == 1)
                {
                    Graph g = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name, tmp.StartID));
                    g.Show();
                }
                else
                {
                    Graph g = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name, tmp.StartDate , tmp.EndDate));
                    g.Show();
                }
            }

        }

       
        private void btnGraphProfitBets_Click(object sender, EventArgs e)
        {
            bool created = false;
            if (LiveGraph != null)
            {
                if (LiveGraph.IsDisposed)
                {
                    LiveGraph = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name, OpenTime, DateTime.Now.AddYears(1)));
                    LiveGraph.Show();
                    created = true;
                }
            }
            else
            {
                LiveGraph = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name, OpenTime, DateTime.Now.AddYears(1)));
                LiveGraph.Show();
                created = true;
            }
            if (!created)
                MessageBox.Show("Live chart is already open. Please close the current live chart window before opening a new one.");
            //currentprofitbet();
        }

        private void btnGraphProfitTime_Click(object sender, EventArgs e)
        {
            Graph g = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name, OpenTime, OpenTime.AddYears(1)));
            g.Show();
        }

        private void btnChartAllTimeProfitBets_Click(object sender, EventArgs e)
        {
            bool created = false;
            if (LiveGraph != null)
            {
                if (LiveGraph.IsDisposed)
                {
                    LiveGraph = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name));
                    LiveGraph.Show();
                    created = true;
                }
            }
            else
            {
                LiveGraph = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name));
                LiveGraph.Show();
                created = true;
            }
            if (!created)
                MessageBox.Show("Live chart is already open. Please close the current live chart window before opening a new one.");
            
        }

        private void btnChartAllTimeProfitTime_Click(object sender, EventArgs e)
        {
            Graph g = new Graph(sqlite_helper.GetBetForCharts(CurrentSite.Name));
            g.Show();
        }


        #endregion
        #endregion


        private void btnStopOnWin_Click(object sender, EventArgs e)
        {
            stoponwin = true;
        }

        private void cmbSite_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            
        }

        private void btnBrowseStratFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtQuickSwitch.Text = fdb.SelectedPath;
            }
        }

        private void btnStratRefresh_Click(object sender, EventArgs e)
        {
            lsbStrats.Items.Clear();
            cmbStrat.Items.Clear();
            if (Directory.Exists(txtQuickSwitch.Text))
            {
                foreach (string x in Directory.GetFiles(txtQuickSwitch.Text))
                {
                    using (StreamReader sr = new StreamReader(x))
                    {
                        string tmptxt = sr.ReadLine();
                        if (tmptxt.StartsWith("SaveVersion"))
                        {
                            lsbStrats.Items.Add( new FileInfo(x).Name );
                            cmbStrat.Items.Add(new FileInfo(x).Name);
                        }
                    }
                }
            }
        }

        private void cmbStrat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (File.Exists(txtQuickSwitch.Text+"\\"+cmbStrat.SelectedItem.ToString()))
            {
                load(txtQuickSwitch.Text + "\\" + cmbStrat.SelectedItem.ToString());
            }
        }

        private void cmbStrat_Click(object sender, EventArgs e)
        {
            if (cmbStrat.Items.Count<1)
            {
                MessageBox.Show("Theres nothing here! You probably still need to specify a folder for this feature to work.\n\n"+
                    "Go to the Advanced Bet Settings tab, then click the browse button below the 'Quick Switch Folder' text box.\n"+
                    "Select a folder with some exported strategies in and click refresh. The usable files will be identified and the strategies loaded. You can now switch between them using the drop down menu.");
            }
        }

        
        private void btnBrowseLab_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofdLab = new OpenFileDialog();
            if (ofdLab.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(ofdLab.FileName))
                try
                {
                    string s = File.ReadAllText(ofdLab.FileName);
                    string[] ss = s.Split('\n');
                    foreach (string sss in ss)
                    {
                        dparse(sss, ref convert);
                        if (!convert)
                            break;
                    }
                    if (convert)
                    {
                        rtbBets.Text = s;
                        
                    }
                    else
                    {
                        MessageBox.Show("Invalid bets file. Please make sure there are only bets in the file, 1 per line. NO other characters are permitted.");
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid bets file. Please make sure there are only bets in the file, 1 per line. NO other characters are permitted.");
                }
            }
        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        #region api controlls
        ///multithread updates for api based sites
        delegate void dupdateControll(object value);

        public void updateBalance(object Balance)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateBalance), Balance);
            }
            else
            {
                lblApiBalance.Text = decimal.Parse(Balance.ToString(), System.Globalization.CultureInfo.InvariantCulture).ToString("0.00000000");
            }
        }

        public void updateDeposit(object Address)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateDeposit), Address);
            }
            else
            {
                txtApiAddress.Text = Address.ToString();
            }
        }

        public void updateWins(object Wins)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateWins), Wins);
            }
            else
            {
                lblApiWins.Text = (Wins).ToString();
            }
        }

        public void updateLosses(object Wins)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateLosses), Wins);
            }
            else
            {
                lblApiLosses.Text = (Wins).ToString();
            }
        }

        public void updateBets(object Bets)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateBets), Bets);
            }
            else
            {
                lblApiBets.Text = (Bets).ToString();
            }
        }

        public void updateProfit(object _Profit)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateProfit), _Profit);
            }
            else
            {

                decimal Profit = 0;
                Profit = Convert.ToDecimal(_Profit);
               
                 lblApiProfit.Text = ((decimal)Profit).ToString("0.00000000");
                if ((decimal)Profit==0)
                {
                    lblApiProfit.ForeColor = Color.Blue;
                }
                else if ((decimal)Profit > 0)
                {
                    lblApiProfit.ForeColor = Color.Green;
                }
                else 
                {
                    lblApiProfit.ForeColor = Color.Red;
                }
            }
        }

        public void updateWagered(object _Wagered)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateWagered), _Wagered);
            }
            else
            {
                decimal Wagered = 0;
                Wagered = Convert.ToDecimal(_Wagered);
                lblApiWagered.Text = (Wagered).ToString("0.00000000");
            }
        }

        public void updateBet(object _Bet)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateBet), _Bet);
            }
            else
            {
                decimal Bet = 0;
                Bet = Convert.ToDecimal(_Bet);
                nudApiBet.Value = (decimal)Bet;
            }
        }

        public void updateChance(object _Chance)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateChance), _Chance);
            }
            else
            {
                decimal Chance = 0;
                Chance = Convert.ToDecimal(_Chance);
                nudApiChance.Value = ((decimal)Chance);
            }
        }

        public void updatePayout(object _Payout)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updatePayout), _Payout);
            }
            else
            {
                decimal Payout = 0;
                Payout = Convert.ToDecimal(_Payout);
                nudApiPayout.Value = (decimal)Payout;
            }
        }

        public void updateBetProfit(object _BetProfit)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateBetProfit), _BetProfit);
            }
            else
            {
                decimal BetProfit = 0;
                BetProfit = Convert.ToDecimal(_BetProfit);
                lblApiBetProfit.Text = ((decimal)BetProfit).ToString("0.00000000");
            }
        }

        public void updateStatus(object Status)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(updateStatus), Status);
            }
            else
            {
                lblStatus.Text = Status.ToString();
            }
        }

        fChat PopoutChat = new fChat("");
        
        public void AddChat(object Message)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(AddChat), Message);
                
            }
            else
            {
                if (PopoutChat!=null)
                {
                    if (!PopoutChat.IsDisposed)
                    {
                        PopoutChat.GotMessage((string)Message);
                    }
                }
                
            }
        }


        //List<Bet> BetsToShow = new List<Bet>();
        int maxRows = 100;
        public void AddBet(object Bet)
        {
            if (InvokeRequired)
            {
                Invoke(new dupdateControll(AddBet), Bet);
            }
            else
            {
                if (LiveGraph != null)
                    if (!LiveGraph.IsDisposed)
                        LiveGraph.AddBet(Bet as Bet);
                sqlite_helper.AddBet(Bet as Bet, CurrentSite.Name);
                dataGridView1.DataBindings.Clear();
                Bet _Bet = (Bet as Bet);
                dataGridView1.Rows.Insert(0, _Bet.Id, _Bet.date, _Bet.Amount, _Bet.high, _Bet.Chance, _Bet.Roll,_Bet.Profit,_Bet.nonce );
                if ( dataGridView1.Rows.Count >0 )
                {
                    
                    if (dataGridView1.Rows[0].Cells[6].Value != null)
                    {
                        if (((bool)dataGridView1.Rows[0].Cells[3].Value ? (decimal)dataGridView1.Rows[0].Cells[5].Value < 100m - (decimal)(dataGridView1.Rows[0].Cells[4].Value) : (decimal)dataGridView1.Rows[0].Cells[5].Value > (decimal)(dataGridView1.Rows[0].Cells[4].Value)))
                        {
                            dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.Pink;
                        }
                        else
                        {
                            dataGridView1.Rows[0].DefaultCellStyle.BackColor = Color.LightGreen;
                        }
                    }
                }
                while (dataGridView1.Rows.Count > maxRows)
                {
                    dataGridView1.Rows.RemoveAt(dataGridView1.Rows.Count - 1);
                }
                
            }
        }

        private void btnLogIn_Click(object sender, EventArgs e)
        {
            if ((sender as Button).Text == "Log In")
            {
                CurrentSite.FinishedLogin -= CurrentSite_FinishedLogin;
                CurrentSite.FinishedLogin +=CurrentSite_FinishedLogin;
                
                CurrentSite.Login(txtApiUsername.Text, txtApiPassword.Text, txtApi2fa.Text);
                
            }
            else
            {
                if (CurrentSite!=null)
                {
                    Stop();
                    CurrentSite.Disconnect();
                    EnableNotLoggedInControls(false);
                }
            }
            txtApi2fa.Text = "";
        }


        #endregion

        private void btnRegister_Click(object sender, EventArgs e)
        {
            ConfirmPassword Conf = new ConfirmPassword();
            bool Valid = false;
            
            if (Conf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Valid = Conf.Password == txtApiPassword.Text;
            }
            if (Valid)
            {
                if (CurrentSite.Register(txtApiUsername.Text, txtApiPassword.Text))
                {
                    EnableNotLoggedInControls(true);
                }
            }
            else
            {
                MessageBox.Show("Registration Failed.");
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex==0)
            {

                string url = CurrentSite.BetURL+ dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                Process.Start(url);
            }
        }

        /// <summary>
        /// place single bet, HIGH
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            CurrentSite.amount = ((double)nudApiBet.Value);
            CurrentSite.chance = (double)(nudApiChance.Value);
            CurrentSite.PlaceBet(true);
        }

        /// <summary>
        /// place single bet, Low
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            CurrentSite.amount =((double)nudApiBet.Value);
            CurrentSite.chance = (double)(nudApiChance.Value);
            CurrentSite.PlaceBet(false);
        }

        private void nudApiBet_ValueChanged(object sender, EventArgs e)
        {
            if ((sender as NumericUpDown).Name == "nudApiBet")
            {

                lblApiBetProfit.Text = ((nudApiBet.Value * nudApiPayout.Value) - nudApiBet.Value).ToString("0.00000000"); 
            }
            else if ((sender as NumericUpDown).Name == "nudApiChance")
            {
                decimal payout = (100m - CurrentSite.edge) / (nudApiChance.Value);
                if (nudApiPayout.Value != payout)
                    nudApiPayout.Value = payout;
                lblApiBetProfit.Text = ((nudApiBet.Value * payout) - nudApiBet.Value).ToString("0.00000000"); 
            }
            else if ((sender as NumericUpDown).Name == "nudApiPayout")
            {
                decimal chance = (100m - CurrentSite.edge) / (nudApiPayout.Value);
                if (nudApiChance.Value != chance)
                    nudApiChance.Value = chance;
                lblApiBetProfit.Text = ((nudApiBet.Value * nudApiPayout.Value) - nudApiBet.Value).ToString("0.00000000"); 
            }
        }

        //settings mode combobox
        List<TabPage> Tabs = new List<TabPage>();
        bool ViewedAdvanced = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((sender as ToolStripMenuItem).Checked)
            {
                if ((sender as ToolStripMenuItem).Name == "basicToolStripMenuItem")
                {
                    pnlProgrammer.Visible = pnlAdvanced.Visible = false;
                    pnlBasic.Visible = true;
                    scMain.SplitterDistance = (scMain.Width - pnlBasic.Width) - 3;
                    if (ViewedAdvanced)
                        MessageBox.Show("Please note: Settings set in the advanced mode are still be active.");
                }
                else if ((sender as ToolStripMenuItem).Name == "advancedToolStripMenuItem")
                {
                    ViewedAdvanced = true;
                    pnlAdvanced.Visible = true;
                    pnlProgrammer.Visible = pnlBasic.Visible = false;
                    scMain.SplitterDistance = (scMain.Width - pnlAdvanced.Width) - 3;
                }
                else if ((sender as ToolStripMenuItem).Name == "programmerToolStripMenuItem")
                {
                    ViewedAdvanced = true;
                    pnlProgrammer.Visible = true;
                    pnlAdvanced.Visible = pnlBasic.Visible = false;
                    scMain.SplitterDistance = (scMain.Width - pnlProgrammer.Width) - 3;

                }
            }
        }

        private void btnBetHistory_Click(object sender, EventArgs e)
        {
            BetHistory tmp = new BetHistory(CurrentSite.Name);
            tmp.Show();
        }

        private void btnWithdraw_Click(object sender, EventArgs e)
        {
            if (CurrentSite.AutoWithdraw)
            {
                string Response = Interaction.InputBox("Amount to withdraw: ", "Withdraw", "0.00000000", -1, -1);
                double tmpAmount = 0;
                if (double.TryParse(Response, out tmpAmount))
                {
                    string Address = Interaction.InputBox("Bitcoin Address: ", "Withdraw", "", -1, -1);
                    System.Text.RegularExpressions.Regex txt = null;

                    txt = new System.Text.RegularExpressions.Regex(@"^[13][a-km-zA-HJ-NP-Z0-9]{26,33}$");

                    bool valid = txt.IsMatch(Address);
                    if (valid)
                    {

                        CurrentSite.Withdraw(tmpAmount, Address);
                    }
                    else

                        MessageBox.Show("Invalid Address");
                }
                else
                {
                    MessageBox.Show("Input not a valid number");
                }
            }
        }

        //will invest at default kelly for multikelly sites with a default, 0.5% for multikelly sites that have no default.
        private void btnInvest_Click(object sender, EventArgs e)
        {
            if (CurrentSite.AutoInvest)
            {
                string Response = Interaction.InputBox("Amount to invest: ", "Invest", "0.00000000", -1, -1);
                double tmpAmount = 0;
                if (double.TryParse(Response, out tmpAmount))
                {
                    CurrentSite.Invest(tmpAmount);
                    
                }
                else
                {
                    MessageBox.Show("Input not a valid number");
                }
            }
        }

        private void btnTip_Click(object sender, EventArgs e)
        {
            if (CurrentSite.Tip)
            {
                string User = Interaction.InputBox((CurrentSite.TipUsingName?"Username":"User ID")+" of user to tip:", "Tip", "",-1,-1 );
                if (!CurrentSite.TipUsingName)
                {
                    int ID = 0;
                    if (!int.TryParse(User, out ID))
                    {
                        updateStatus("Invalid UserID");
                        return;
                    }
                }
                string Amount = Interaction.InputBox("Amount to tip: ", "Tip", "0.00000000", -1,-1);
                double tmpAmount = 0;
                if (double.TryParse(Amount, out tmpAmount))
                {
                    CurrentSite.SendTip(User, tmpAmount);

                }
                else
                {
                    MessageBox.Show("Input not a valid number");
                }
            }
        }

        private void rdbPreset_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton tmp = sender as RadioButton;
            if (tmp != rdbAlembert)
            {
                if (rdbAlembert.Checked && tmp.Checked)
                    rdbAlembert.Checked = false;
            }
            if (tmp != rdbMartingale)
            {
                if (rdbMartingale.Checked && tmp.Checked)
                {
                    rdbMartingale.Checked = false;
                }
            }
            if (tmp != rdbLabEnable)
            {
                if (rdbLabEnable.Checked && tmp.Checked)
                    rdbLabEnable.Checked = false;
            }
            if (tmp !=rdbFibonacci)
            {
                if (rdbFibonacci.Checked && tmp.Checked)
                    rdbFibonacci.Checked = false;
            }
            if (tmp != rdbPreset)
            {
                if (rdbPreset.Checked && tmp.Checked)
                    rdbPreset.Checked = false;
            }
            if (tmp == rdbMartingale)
            {
                if (tmp.Checked)
                {
                    gbCustom.Enabled = true;
                    gbCustom.Text = "";
                }
                else
                {
                    gbCustom.Enabled = false;
                    gbCustom.Text = "These settings can only be used with martingale";
                }
            }
        }

        //Browse for preset list of bets
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofdLab = new OpenFileDialog();
            if (ofdLab.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(ofdLab.FileName))
                    try
                    {
                        string s = File.ReadAllText(ofdLab.FileName);
                        string[] ss = s.Split('\n');
                        foreach (string sss in ss)
                        {
                            dparse(sss, ref convert);
                            if (!convert)
                                break;
                        }
                        if (convert)
                        {
                            
                            rtbPresetList.Text = s;

                        }
                        else
                        {
                            MessageBox.Show("Invalid bets file. Please make sure there are only bets in the file, 1 per line. NO other characters are permitted.");
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Invalid bets file. Please make sure there are only bets in the file, 1 per line. NO other characters are permitted.");
                    }
            }
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            chrtEmbeddedLiveChart.Enabled = !chrtEmbeddedLiveChart.Enabled;
            if (!chrtEmbeddedLiveChart.Enabled)
                btnDisable.Text = "Start Chart";
            else
                btnDisable.Text = "Stop Chart";
        }

        private void btnChartReset_Click(object sender, EventArgs e)
        {
            chrtEmbeddedLiveChart.Series[0].Points.Clear();
            chrtEmbeddedLiveChart.Series[0].Points.AddXY(0, 0);
            
        }

        private void btnHideLive_Click(object sender, EventArgs e)
        {
            chrtEmbeddedLiveChart.Visible = !chrtEmbeddedLiveChart.Visible;
            if (chartToolStripMenuItem.Checked != chrtEmbeddedLiveChart.Visible)
                chartToolStripMenuItem.Checked = chrtEmbeddedLiveChart.Visible;
            if (chrtEmbeddedLiveChart.Visible)
            {
                btnHideLive.Text = "Hide Chart";
                splitContainer1.SplitterDistance = 250;
            }
            else
            {
                btnHideLive.Text = "Show Chart";
                splitContainer1.SplitterDistance = 25;
            }
        }

        private void txtApiUsername_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogIn_Click(btnLogIn, new EventArgs());
            }
        }

        
        private void panel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (nudMutliplier2.Value != nudMultiplier.Value)
                nudMultiplier.Value = nudMutliplier2.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (nudWinMultiplier2.Value != nudWinMultiplier.Value)
                nudWinMultiplier.Value = nudWinMultiplier2.Value;
        }

        private void nudChance2_ValueChanged(object sender, EventArgs e)
        {
            if (nudChance2.Value != nudChance.Value)
                nudChance.Value = nudChance2.Value;
        }

        private void nudMinbet2_ValueChanged(object sender, EventArgs e)
        {
            if (nudMinBet.Value != nudMinbet2.Value)
                nudMinBet.Value = nudMinbet2.Value;
        }

        DateTime LastMissingCheck = DateTime.Now;
        private void btnGetSeeds_Click(object sender, EventArgs e)
        {
            if (running)
            {
                MessageBox.Show("Please stop the bot before looking for missing seeds. This is an extremely expensive query to run and can cause other functions to stall or break.");
            }
            else
            { 
                GetMissingSeeds();
            }
        }
        
        void GetMissingSeeds()
        {
            LastMissingCheck = DateTime.Now;
            BetIDs = sqlite_helper.GetMissingSeedIDs(CurrentSite.Name);
        }
        List<long> BetIDs = new List<long>();
        
        private void tmrMissingSeeds_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - LastMissingCheck).TotalMinutes>5)
            {
                GetMissingSeeds();
            }
            if (BetIDs.Count > 0 && !CurrentSite.GettingSeed)
            {
                long tmp = BetIDs[0];
                BetIDs.RemoveAt(0);
                CurrentSite.GetSeed(tmp);

            }
            
        }

        private void ChatSend_Click(string Message)
        {
            if (Message!="")
            {
                CurrentSite.SendChatMessage(Message);
                
            }
        }
  

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void splitContainer2_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
        LuaInterface Lua = LuaRuntime.GetLua();
        //LuaContext Lua = new LuaContext();
        private void richTextBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            
            
        }
        
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void chartToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (chartToolStripMenuItem.Checked != chrtEmbeddedLiveChart.Visible)
                btnHideLive_Click(btnHideLive, new EventArgs());
            
        }

        private void viewToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void loginPanelToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            gbLogin.Visible = loginPanelToolStripMenuItem.Checked;
            
        }

        private void manualBettingToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
                gbManualBet.Visible = manualBettingToolStripMenuItem.Checked;
            
        }

        private void statsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (statsToolStripMenuItem.Checked)
            {
                StatsWindows.Show();
            }
            else
            {
                statsToolStripMenuItem.Visible = false;
                
            }
        }

        

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void beginnersGuidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://bitcointalk.org/index.php?topic=391870");
        }

        private void justDiceToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            EnableNotLoggedInControls(false);
            if (CurrentSite != null)
            {
                CurrentSite.Disconnect();
            }
            if (CurrentSite is PD)
            {
                (CurrentSite as PD).ispd = false;
            }
            if ((sender as ToolStripMenuItem).Checked)
            {
                foreach (ToolStripMenuItem t in siteToolStripMenuItem.DropDownItems)
                {
                    if (t == sender as ToolStripMenuItem)
                    {
                        t.Checked = true;
                    }
                    else
                    {
                        t.Checked = false;
                    }
                }
                switch ((sender as ToolStripMenuItem).Name)
                {
                    case "justDiceToolStripMenuItem": CurrentSite = new JD(this); siteToolStripMenuItem.Text = "Site " + "(JD)"; break;
                    case "pocketRocketsCasinoToolStripMenuItem": CurrentSite = new PRC(this); siteToolStripMenuItem.Text = "Site " + "(PRC)"; break;                    
                    case "diceToolStripMenuItem": CurrentSite = new dice999(this); siteToolStripMenuItem.Text = "Site " + "(999D)"; break;
                    case "primeDiceToolStripMenuItem": CurrentSite = new PD(this); siteToolStripMenuItem.Text = "Site " + "(PD)"; break;
                    case "safediceToolStripMenuItem": CurrentSite = new SafeDice(this); siteToolStripMenuItem.Text = "Site (SD)"; break;

                }
                rdbInvest.Enabled = CurrentSite.AutoInvest;
                if (!rdbInvest.Enabled)
                    rdbInvest.Checked = false;
                rdbWithdraw.Enabled = CurrentSite.AutoWithdraw;
                if (!rdbWithdraw.Enabled)
                    rdbWithdraw.Checked = false;
                if (UseProxy)
                    CurrentSite.SetProxy(proxHost, proxport, proxUser, proxPass);
            }
            
        }

        private void justDiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem t in siteToolStripMenuItem.DropDownItems)
            {
                if (t == sender as ToolStripMenuItem)
                {
                    t.Checked = true;
                }
                /*else
                {
                    t.Checked = false;
                }*/
            }
        }

        private void basicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem t in settingsModeToolStripMenuItem.DropDownItems)
            {
                t.Checked = t == sender as ToolStripMenuItem;
                
            }
        }

        private void btcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!diceToolStripMenuItem.Checked)
            {
                diceToolStripMenuItem.Checked = true;
            }
            foreach (ToolStripMenuItem t in  (sender as ToolStripMenuItem).DropDownItems)
            {
                t.Checked = t == sender as ToolStripMenuItem;
            }
        }

        private void btcToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as ToolStripMenuItem).Checked)
            {
                CurrentSite.Currency = (sender as ToolStripMenuItem).Text.ToLower();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }


        void SetLuaVars()
        {
            try
            {
                //Lua.clear();
                Lua["balance"] = (double)((int)(PreviousBalance * 100000000)) / 100000000.0;
                
                Lua["profit"] = ((double)(this.profit * 100000000)) / 100000000.0;
                Lua["currentstreak"] = (Winstreak > 0) ? Winstreak : -Losestreak;
                Lua["previousbet"] = Lastbet;
                Lua["nextbet"] = Lastbet;
                Lua["chance"] = Chance;
                Lua["bethigh"] = high;
                Lua["bets"] = Wins + Losses;
                Lua["wins"] = Wins;
                Lua["losses"] = Losses;
                
            }
            catch (Exception e)
            {
                Stop();
                WriteConsole("LUA ERROR!!");
                WriteConsole(e.Message);
            }
        }
        void GetLuaVars()
        {

            try
            {
                Lastbet = (double)Lua["nextbet"];
                Chance = (double)Lua["chance"];
                high = (bool)Lua["bethigh"];
                CurrentSite.amount = Lastbet;
                CurrentSite.chance = Chance;
            }
            catch
            {

            }
        }
        int LCindex = 0;
        private void textBox1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Enter)
            {

                SetLuaVars();
                LCindex = 0;
                LastCommands.Add(txtConsoleIn.Text);
                if (LastCommands.Count>26)
                { LastCommands.RemoveAt(0); }
                WriteConsole(txtConsoleIn.Text);
                if (txtConsoleIn.Text.ToLower() == "start()")
                {
                    LuaRuntime.SetLua(Lua);
                    try
                    {
                        LuaRuntime.Run(richTextBox3.Text);
                        Start(false);
                    }
                    catch (Exception ex)
                    {
                        WriteConsole("LUA ERROR!!");
                        WriteConsole(ex.Message);
                    }
                    
                }
                
                else
                {
                    try
                    {
                        LuaRuntime.SetLua(Lua);
                        LuaRuntime.Run(txtConsoleIn.Text);
                    }
                    catch (Exception ex)
                    {
                        WriteConsole("LUA ERROR!!");
                        WriteConsole(ex.Message);
                    }
                }
                
                txtConsoleIn.Text = "";
                GetLuaVars();
            }
            if (e.KeyCode == Keys.Up)
            {
                if (LCindex < LastCommands.Count)
                    LCindex++;
                if (LastCommands.Count>0)
                txtConsoleIn.Text = LastCommands[LastCommands.Count - LCindex];

            }
            if (e.KeyCode == Keys.Down)
            {
                if (LCindex >0)
                    LCindex--;
                if (LCindex <=0)
                {
                    txtConsoleIn.Text = "";
                }
                else if (LastCommands.Count > 0)
                txtConsoleIn.Text = LastCommands[LastCommands.Count - LCindex];
                

            }
        }
        List<string> LastCommands = new List<string>();
        private void txtConsoleIn_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtConsoleIn.Text = "";
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/ProgrammerMode.html");
        }

        private void sourceCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://github.com/seuntie900/DiceBot");
        }

        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        
        private void button4_Click_1(object sender, EventArgs e)
        {
            if (PopoutChat == null)
            {
                PopoutChat = new fChat("");
                PopoutChat.SendMessage+=PopoutChat_SendMessage;
                PopoutChat.Show();
            }
            else if (PopoutChat.IsDisposed)
            {
                PopoutChat = new fChat("");
                PopoutChat.SendMessage += PopoutChat_SendMessage;
                PopoutChat.Show();
            }
            else
            {
                PopoutChat.Show();
                
            }
            
            
        }

        void PopoutChat_SendMessage(string Message)
        {
            
            ChatSend_Click(Message);
        }

        private void donateToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Donate tmp = new Donate(CurrentSite.Name);
            tmp.Show();
        }

        string proxUser = "", proxPass = "", proxHost ="";
        int proxport = 0;
        bool UseProxy = false;
        private void proxySettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DiceBot.Proxy Prox = new Proxy();
            if (Prox.ShowDialog() == DialogResult.OK)
            {
                UseProxy = Prox.chkProxy.Checked;
                proxUser = Prox.txtUsername.Text;
                proxPass = Prox.txtPassword.Text;
                proxHost = Prox.txtHost.Text;
                proxport = (int)Prox.nudPort.Value;
                if (UseProxy)
                    CurrentSite.SetProxy(proxHost, proxport, proxUser, proxPass);
            }
        }

        private void luckyNumberVerifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Verify tmp = new Verify(this);
            tmp.ShowDialog();
        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
        
        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatsWindows.Show();
        }

        private void simulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SimWindow.Show();
        }

        private void pnlAdvancedAdvanced_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void btnHelpMartingale_Click(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/martingale.html");
        }

        private void btnHelpLabouchere_Click(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/labouchere.html");
        }

        private void btnHelpFibonacci_Click(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/fibonacci.html");
        }

        private void btnHelpAlembert_Click(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/alembert.html");
        }

        private void btnHelpPreset_Click(object sender, EventArgs e)
        {
            Process.Start("http://bot.seuntjie.com/presetlist.html");
        }

        private void btnOpenCode_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofdtmp = new OpenFileDialog();
            if (ofdtmp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(ofdtmp.FileName))
                    {
                        string tmp = sr.ReadToEnd();
                        richTextBox3.Text = tmp;
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid file!");
                }
            }
        }

        private void btnCodeSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog svdtmp = new SaveFileDialog();
            if (svdtmp.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                try
                {
                    File.WriteAllText(svdtmp.FileName, richTextBox3.Text);
                    MessageBox.Show("Saved!");
                }
                catch
                {
                    MessageBox.Show("Could not save code to file.");
                }
            }
        }
    }
}
