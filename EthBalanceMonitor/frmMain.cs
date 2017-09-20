// Copyright (C) 2017 valiant1x contact@intensecoin.com
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections.ObjectModel;
using System.Net;

namespace EthBalanceMonitor
{
    public partial class frmMain : Form
    {
        BindingList<Address> Addresses = new BindingList<Address>();
        List<string> AuditSites = new List<string>()
        {
            "https://a.chainpoint.org/",
            "https://b.chainpoint.org/",
            "https://c.chainpoint.org/"
        };

        public frmMain()
        {
            InitializeComponent();
            
            if (File.Exists("address.txt"))
            {
                try
                {
                    var lines = File.ReadAllLines("address.txt");
                    foreach(var line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || line.Trim().Length != 42)
                            continue;

                        if (Addresses.Where(x => x.Addr.Equals(line.Trim(), 
                            StringComparison.OrdinalIgnoreCase)).Count() == 0)
                            Addresses.Add(new Address(line.Trim()));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                try
                {
                    File.WriteAllText("address.txt", "");
                }
                catch { }              
            }

            AddLogMsg("API graciously powered by ethplorer.io");
            AddLogMsg("Donations greatly appreciated: 0xa81248aE54dE6521d3afd58848006676EE874bFC");
            AddLogMsg("Loaded " + Addresses.Count + " addresses to monitor.");

            //bind grid
            dgMain.DataSource = null;
            dgMain.AutoGenerateColumns = true;
            dgMain.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgMain.AllowDrop =
                dgMain.AllowUserToAddRows =
                dgMain.AllowUserToDeleteRows =
                false;

            dgMain.ReadOnly = true;

            dgMain.DataSource = new BindingSource { DataSource = Addresses };

            var _ = CheckTokenBalance();
        }

        private void tmrCheckBalance_Tick(object sender, EventArgs e)
        {
            if (tmrCheckBalance.Interval == 100)
                tmrCheckBalance.Interval = 15000;

        }

        async Task CheckTokenBalance()
        {
            while (true)
            {
                bool wasFresh = false;

                try
                {
                    //query server
                    string url = @"https://api.ethplorer.io/getAddressInfo/{ADDR}?apiKey=freekey";
                    var addr = Addresses.OrderBy(x => x.LastUpdated).First();
                    url = url.Replace("{ADDR}", addr.Addr);

                    wasFresh = (addr.LastUpdated == new DateTime());

                    HttpWebRequest request = HttpWebRequest.CreateHttp(url);
                    request.Timeout = 10000;
                    var response = await request.GetResponseAsync();
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string body = await reader.ReadToEndAsync();
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(body);

                        addr.BalEth = jsonResponse.ETH.balance;

                        float newTntBal = jsonResponse.tokens[0].balance / 100000000;

                        if (addr.BalTnt != 0 &&
                            addr.BalTnt != newTntBal)
                            AddLogMsg("*** Value changedfor " + addr.Addr + " -- previous: " +
                                addr.BalTnt + " -- new: " + newTntBal);

                        addr.BalTnt = newTntBal;
                        addr.LastUpdated = DateTime.Now;
                    }

                    //check node audit status
                    string auditUrl = AuditSites[0];
                    AuditSites.RemoveAt(0);                    
                    AuditSites.Add(auditUrl);

                    auditUrl += "nodes/" + addr.Addr;

                    request = HttpWebRequest.CreateHttp(auditUrl);
                    request.Timeout = 10000;
                    response = await request.GetResponseAsync();
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string body = await reader.ReadToEndAsync();
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(body);

                        foreach(dynamic audit in jsonResponse.recent_audits)
                        {
                            if (!audit.public_ip_test.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                !audit.time_test.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                !audit.calendar_state_test.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                !audit.minimum_credits_test.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                            {
                                addr.AuditPassed = false;
                                AddLogMsg("Audit faile for " + addr.Addr + ". " + body);
                            }
                            else
                                addr.AuditPassed = true;

                            //only check most recent
                            break;
                        }
                    }
                }
                catch { }
                
                dgMain.AutoResizeColumns();

                await Task.Delay(wasFresh ? 500 : 15000);
            }
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            if (Addresses.Count == 0)
            {
                MessageBox.Show("No addresses found! Put ETH addresses into address.txt file. 1 address per line.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Process.Start("address.txt");

                this.Close();
            }
        }

        void AddLogMsg(string msg)
        {
            txtLog.AppendText("[" + DateTime.Now.ToLongTimeString() + "] " + msg + Environment.NewLine);
        }

        private void dgMain_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgMain.Columns[e.ColumnIndex].Name == "BalTnt")
            {
                Address addr = Addresses[e.RowIndex];
                if (addr.BalTnt > 3100)
                {
                    e.CellStyle.BackColor = Color.Green;
                    e.CellStyle.ForeColor = Color.WhiteSmoke;
                }
            }

            if (dgMain.Columns[e.ColumnIndex].Name == "AuditPassed")
            {
                Address addr = Addresses[e.RowIndex];
                if (!addr.AuditPassed)
                {
                    e.CellStyle.BackColor = Color.OrangeRed;
                    e.CellStyle.ForeColor = Color.WhiteSmoke;
                }
            }
        }
    }
}
