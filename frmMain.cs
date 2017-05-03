/****************************** Module Header ******************************\
Module Name:    frmMain.cs
Project:        LA-Importer2
Created by:     Nehru Becirovic
Maintainer:     Geoff T. Wark
Date Created:   Jan-09-2014
Last Modified:  

Copyright (c) 2014 Thomas G. Carpenter Library.

This program facilitates the the loading of LibAnswers (Reference Analytics)
data into a SQL database.

This source is subject to the Microsoft Public License.
See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
All other rights reserved.

THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

// TODO: make the progress bar work

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Management.Automation;

namespace LA_Importer2
{
    public partial class frmMain : Form
    {
        private string connection_info = null;
        private string db_table = null;

        public frmMain()
        {
            InitializeComponent();
            MinimizeBox = false;
            MaximizeBox = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                StreamReader sr = File.OpenText(
                    "C:\\Users\\" + Environment.UserName
                    + "\\AppData\\Roaming\\LA-Importer2\\config");

                connection_info = sr.ReadLine();
                db_table = sr.ReadLine();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show("No config file found!\n\n"
                    + "Please create one named 'config' at "
                    + "%appdata%/LibAnswers/.\n"
                    + "(1st line = connection string; 2nd line = database table)",
                    "ERROR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void btnChooseSource_Click(object sender, EventArgs e)
        {
            ChooseSourceFile();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            ImportData(txtSourcePath.Text);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ChooseSourceFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtSourceName.Text = ofd.SafeFileName;
                txtSourcePath.Text = ofd.FileName;
            }
        }
        
        private void ImportData(string csv_file)
        {
            if (txtSourcePath.Text != "")
            {
                // a temp file is used here due to user permission issues
                string temp_csv = "C:\\Users\\" + Environment.UserName
                    + "\\AppData\\Roaming\\LA-Importer2\\delete_me.csv";
                DataTable dt = null;

                rtbOutput.Text = null;
                rtbOutput.Text += "Now importing...\n\n";

                ScrubCSV(csv_file, temp_csv);
                dt = ParseCSV(temp_csv);

                try
                {
                    SqlConnection cn = new SqlConnection(connection_info);
                    cn.Open();
                    using (SqlBulkCopy bulk = new SqlBulkCopy(cn))
                    {
                        bulk.DestinationTableName = db_table;
                        bulk.WriteToServer(dt);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    rtbOutput.Text += (ex + "\n");
                    return;
                }
                rtbOutput.Text += "Import complete! Thank you.";
            }
            else
            {
                MessageBox.Show("No source file selected!",
                    "ERROR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ScrubCSV(string csv_file, string temp_csv)
        {
            PowerShell ps = PowerShell.Create();
            string script = "Import-Csv " + csv_file + " | select id,date,time,"
                    + "\"entered by\",\"where you received the question\","
                    + "\"question type\",\"method used to answer\" | Export-Csv "
                    + "-Path " + temp_csv + " -NoTypeInformation";

            try
            {
                ps.AddScript(script);
                ps.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                rtbOutput.Text += (ex + "\n");
                return;
            }
            finally
            {
                ps.Dispose();
            }
        }

        private DataTable ParseCSV(string temp_csv)
        {
            StreamReader sr = null;
            string currentLine = null;
            DataTable dt = new DataTable();

            try
            {
                sr = File.OpenText(temp_csv);
                int i = 0;
                while ((currentLine = sr.ReadLine()) != null)
                {
                    currentLine = currentLine.Replace("\",\"", "|");
                    string[] data = currentLine.Split('|');
                    data[0] = data[0].TrimStart('"');
                    data[6] = data[6].TrimEnd('"');

                    if (i == 0)
                    {
                        foreach (var item in data)
                        {
                            dt.Columns.Add(new DataColumn());
                        }
                    }
                    else
                    {
                        DataRow row = dt.NewRow();
                        row.ItemArray = data;
                        dt.Rows.Add(row);
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                rtbOutput.Text += (ex + "\n");
                return null;
            }
            finally
            {
                sr.Close();
            }

            try
            {
                File.Delete(temp_csv);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                rtbOutput.Text += (ex + "\n");
            }

            return dt;
        }

        // menu strip
        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportData(txtSourcePath.Text);
        }

        private void changeSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChooseSourceFile();
        }

        private void viewHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: improve this
            MessageBox.Show(
                "Please contact geoff.wark@unf.edu if you are having trouble.",
                "LibAnswers Importer Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Question);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: improve this
            MessageBox.Show("LibAnswers Importer\n"
                + "Version 0.02\n\n"
                + "Copyright (c) 2014 Thomas G. Carpenter Library.\n"
                + "This source is subject to the Microsoft Public License.\n"
                + "See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.\n"
                + "All other rights reserved.",
                "About LibAnswers Importer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}