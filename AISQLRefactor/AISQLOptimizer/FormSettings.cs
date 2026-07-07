using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using System;
using System.Windows.Forms;
using System.Text.Json;

namespace AISQLOptimizer
{

    public partial class FormSettings : Form
    {
        private bool _suppressItemChecked = false;
        private readonly AppState _state;

        public InteractiveBrowserCredential Credential { get; private set; }
        public AIProjectClient ProjectClient { get; private set; }
        public string SelectedAgentName { get; private set; }

        //Constructor
        public FormSettings(AppState state)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            listView1.ItemChecked += listView1_ItemChecked;
            _state = state ?? throw new ArgumentNullException(nameof(state));

            //Restore Azure UI
            textBox1.Text = _state.TenantId;
            textBox2.Text = _state.Endpoint;

            //Restore SQL UI
            textBox3.Text = _state.SqlServer;
            textBox4.Text = _state.SqlDatabase;
            textBox5.Text = _state.SqlUsername;

            radioButton1.Checked = _state.UseWindowsAuth;
            radioButton2.Checked = !_state.UseWindowsAuth;
            textBox5.Enabled = !_state.UseWindowsAuth;
            textBox6.Enabled = !_state.UseWindowsAuth;
            textBox6.UseSystemPasswordChar = true;

            //Restore checkboxes
            checkBox1.Checked = _state.IncludeBatches;
            checkBox2.Checked = _state.IncludeStoredProcs;
            checkBox3.Checked = _state.IncludeTriggers;
            checkBox4.Checked = _state.IncludeFunctions;
            checkBox5.Checked = _state.IncludeViews;

            int tableCount = CountJsonArray(_state.strColumnTypesList);
            int indexCount = CountJsonArray(_state.strIndexesList);
            if (tableCount > 0 || indexCount > 0)
            {
                label11.Text = $"Connected: {tableCount} tables, {indexCount} indexes";
                label11.Visible = true;
            }
            else
            {
                label11.Visible = !string.IsNullOrWhiteSpace(_state.SqlDatabase);
            }
        }


        public void AttachFoundryRuntime(InteractiveBrowserCredential credential, AIProjectClient projectClient, string agentName)
        {
            Credential = credential;
            ProjectClient = projectClient;
            SelectedAgentName = agentName;

            if (Credential != null && ProjectClient != null)
            {
                label3.Text = "Connected to Azure AI Foundry";
                label3.ForeColor = Color.Green;
                button1.Text = "Connected";
                button1.ForeColor = Color.Green;

                this.Shown += async (s, e) =>
                {
                    await PopulateAgentsIfReadyAsync();
                };
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.ClientSize = new Size(this.ClientSize.Width, (int)(this.ClientSize.Height * 1.16));
            int YY = this.ClientSize.Height;
            int XX = this.ClientSize.Width;
            tabControl1.Height = (int)(YY * 0.89);

            //Agents tab layout
            label3.Location = new Point(10, (int)(YY * 0.02));
            label10.Location = new Point(10, (int)(YY * 0.29));

            textBox1.Location = new Point((int)(XX * 0.28), (int)(YY * 0.09));
            textBox2.Location = new Point((int)(XX * 0.28), (int)(YY * 0.14));
            label1.Location = new Point((int)(XX * 0.01), (int)(YY * 0.09));
            label2.Location = new Point((int)(XX * 0.01), (int)(YY * 0.14));

            listView1.Location = new Point(10, (int)(YY * 0.35));
            listView1.Height = (int)(YY * 0.40);
            int LV = listView1.Width;
            listView1.BeginUpdate();
            try
            {
                listView1.Clear();
                listView1.View = View.Details;
                listView1.CheckBoxes = true;
                listView1.FullRowSelect = true;
                listView1.GridLines = true;
                listView1.HideSelection = false;

                listView1.Columns.Add("Name", (int)(LV * 0.40));
                listView1.Columns.Add("Model", (int)(LV * 0.30));
                listView1.Columns.Add("Version", (int)(LV * 0.30));
            }
            finally
            {
                listView1.EndUpdate();
            }

            button2.Cursor = Cursors.Hand;
            button3.Cursor = Cursors.Hand;
            button4.Cursor = Cursors.Hand;

            button2.Location = new Point((int)(XX * 0.38), (int)(YY * 0.77));
            button2.FlatStyle = FlatStyle.Flat;
            button2.FlatAppearance.BorderSize = 1;
            button2.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 70, 150);   
            button2.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 130, 220);  

            button4.Location = new Point(10, (int)(YY * 0.92));
            button4.FlatStyle = FlatStyle.Flat;
            button4.FlatAppearance.BorderSize = 1;
            button4.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 70, 150);   
            button4.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 130, 220);  
            button4.BackColor = Color.FromArgb(20, 90, 175);    

            //SQLtab layout
            textBox3.Location = new Point((int)(XX * 0.22), (int)(YY * 0.08));
            textBox4.Location = new Point((int)(XX * 0.22), (int)(YY * 0.14));

            label4.Location = new Point((int)(XX * 0.04), (int)(YY * 0.02));
            label11.Location = new Point((int)(XX * 0.50), (int)(YY * 0.02));
            label5.Location = new Point((int)(XX * 0.06), (int)(YY * 0.08));
            label6.Location = new Point((int)(XX * 0.06), (int)(YY * 0.15));

            radioButton1.Location = new Point((int)(XX * 0.22), (int)(YY * 0.20));
            radioButton2.Location = new Point((int)(XX * 0.22), (int)(YY * 0.24));

            label7.Location = new Point((int)(XX * 0.24), (int)(YY * 0.30));
            label8.Location = new Point((int)(XX * 0.24), (int)(YY * 0.36));
            textBox5.Location = new Point((int)(XX * 0.36), (int)(YY * 0.30));
            textBox6.Location = new Point((int)(XX * 0.36), (int)(YY * 0.36));

            label9.Location = new Point((int)(XX * 0.04), (int)(YY * 0.44));
            checkBox1.Location = new Point((int)(XX * 0.22), (int)(YY * 0.48));
            checkBox2.Location = new Point((int)(XX * 0.22), (int)(YY * 0.515));
            checkBox3.Location = new Point((int)(XX * 0.22), (int)(YY * 0.55));
            checkBox4.Location = new Point((int)(XX * 0.22), (int)(YY * 0.585));
            checkBox5.Location = new Point((int)(XX * 0.22), (int)(YY * 0.62));

            label12.Location = new Point((int)(XX * 0.04), (int)(YY * 0.66));

            //Dedicated Panel for radioButton3 (Plan Cache) e radioButton4 (Query Store) to separate them from radioButton1 and radioButton2 
            var panelStatsSource = new Panel
            {
                Location = new Point((int)(XX * 0.22), (int)(YY * 0.695)),
                Size = new Size((int)(XX * 0.30), (int)(YY * 0.075)),
                BackColor = tabPage2.BackColor
            };
            tabPage2.Controls.Add(panelStatsSource);
            panelStatsSource.Controls.Add(radioButton3);
            panelStatsSource.Controls.Add(radioButton4);
            radioButton3.Location = new Point(0, 0);
            radioButton4.Location = new Point(0, (int)(YY * 0.035));

            //Default: Plan Cache.
            radioButton3.Checked = !_state.UseQueryStore;   // Plan Cache
            radioButton4.Checked = _state.UseQueryStore;   // Query Store

            button3.Location = new Point((int)(XX * 0.38), (int)(YY * 0.77));
            button3.FlatStyle = FlatStyle.Flat;
            button3.FlatAppearance.BorderSize = 1;
            button3.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 70, 150);   
            button3.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 130, 220);  
        }


        // Connects to Foundry and retrieves Agents list
        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            var oldText = button1.Text;
            button1.Text = "⏳ Loading...";
            bool connected = false;

            try
            {
                if (!Foundry_Auth_and_Connect())
                    return;
                label3.Text = "Connected to Azure AI Foundry";
                label3.ForeColor = Color.Green;

                listView1.BeginUpdate();
                listView1.Items.Clear();

                // List of deployed Agents (Foundry 2.x)
                int count = 0;
                await foreach (ProjectsAgentRecord agent in ProjectClient.AgentAdministrationClient.GetAgentsAsync())
                {
                    count++;

                    var (model, version) = await GetAgentModelAndVersionAsync(agent.Name);

                    var item = new ListViewItem(agent.Name ?? "")
                    {
                        Checked = false,
                        Tag = agent
                    };
                    item.SubItems.Add(model);     // colonna Model
                    item.SubItems.Add(version);   // colonna Version
                    listView1.Items.Add(item);
                }

                if (count == 0)
                {
                    MessageBox.Show("No agents found in this project.", "Foundry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                connected = true;
            }
            catch (AuthenticationFailedException ex)
            {
                MessageBox.Show("Authentication failed.\n\n" + ex.Message, "Azure Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (RequestFailedException ex)
            {
                MessageBox.Show($"Azure request failed.\n\nStatus: {ex.Status}\n{ex.Message}", "Azure AI Foundry Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listView1.EndUpdate();
                button1.Enabled = true;
                _state.TenantId = textBox1.Text.Trim();
                _state.Endpoint = textBox2.Text.Trim();
                if (connected)
                {
                    button1.Text = "Connected";
                    button1.ForeColor = Color.Green;
                }
                else
                {
                    button1.Text = oldText;
                }
            }
        }


        private bool Foundry_Auth_and_Connect()
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Insert Tenant ID and Endpoint", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            //Tenant
            Credential = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions { TenantId = textBox1.Text });

            //Project endpoint
            var endpoint = new Uri(textBox2.Text);

            var projectOptions = new AIProjectClientOptions { NetworkTimeout = TimeSpan.FromMinutes(6) }; //TIMEOUT Agent
            ProjectClient = new AIProjectClient(endpoint, Credential, projectOptions);
            return true;
        }


        // One checkbox selected at a time within listview
        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppressItemChecked) return;
            if (!e.Item.Checked) return;

            try
            {
                _suppressItemChecked = true;
                foreach (ListViewItem item in listView1.Items)
                {
                    if (!ReferenceEquals(item, e.Item) && item.Checked)
                        item.Checked = false;
                }
            }
            finally
            {
                _suppressItemChecked = false;
            }
        }


        // Selected Agent (returns the agent name)
        private string GetCheckedAgentName()
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked)
                    return (item.Tag as ProjectsAgentRecord)?.Name;
            }
            return null;
        }


        // Selected Agent model (reads the "Model" column of the checked row)
        private string GetCheckedAgentModel()
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked)
                    return item.SubItems.Count > 1 ? item.SubItems[1].Text : "";
            }
            return "";
        }


        private async Task PopulateAgentsIfReadyAsync()
        {
            if (ProjectClient == null)
                return;

            listView1.BeginUpdate();
            listView1.Items.Clear();

            await foreach (ProjectsAgentRecord agent in ProjectClient.AgentAdministrationClient.GetAgentsAsync())
            {
                var (model, version) = await GetAgentModelAndVersionAsync(agent.Name);

                var item = new ListViewItem(agent.Name ?? "")
                {
                    Tag = agent,
                    Checked = agent.Id == _state.SelectedAgentId
                };
                item.SubItems.Add(model);     // column Model
                item.SubItems.Add(version);   // column Version
                listView1.Items.Add(item);
            }
            listView1.EndUpdate();
        }


        //----------------------------------------------SQL TAB

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            textBox5.Enabled = false;
            textBox6.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            textBox5.Enabled = true;
            textBox6.Enabled = true;
        }


        private async Task<bool> ConnectAndGetDataFromSQL(string serverName, string databaseName)
        {
            // Build Connection string (SqlConnectionStringBuilder)
            var csb = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                InitialCatalog = databaseName,
                ConnectTimeout = 8
            };

            if (radioButton1.Checked)
            {
                csb.IntegratedSecurity = true;
                csb.Encrypt = false;
            }
            else if (radioButton2.Checked)
            {
                string username = textBox5.Text.Trim();
                string password = textBox6.Text;   

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Insert username and password for SQL Authentication");
                    return false;
                }

                csb.IntegratedSecurity = false;
                csb.UserID = username;
                csb.Password = password;
                csb.Encrypt = true;
                csb.TrustServerCertificate = true;
            }
            else
            {
                MessageBox.Show("Select authentication method");
                return false;
            }

            string connectionString = csb.ConnectionString;

            try
            {
                //Synchronous connection test
                using (SqlConnection testConn = new SqlConnection(connectionString))
                {
                    await testConn.OpenAsync();

                    //If chosen, verifies if Query Store is enabled and readable on the current database.
                    if (radioButton4.Checked && !await IsQueryStoreReadableAsync(testConn))
                    {
                        MessageBox.Show(
                            "Query Store not enabled (or not readable) on this database.\n" +
                            "Enable it (ALTER DATABASE [" + databaseName + "] SET QUERY_STORE = ON) " +
                            "or select Plan Cache.",
                            "Query Store not available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    //Save SQL verified credentials
                    _state.SqlServer = serverName;
                    _state.SqlDatabase = databaseName;
                    _state.UseWindowsAuth = radioButton1.Checked;
                    _state.UseQueryStore = radioButton4.Checked;
                    _state.SqlUsername = radioButton2.Checked ? textBox5.Text.Trim() : "";
                    button1.ForeColor = Color.Green;

                    //Save checkboxes
                    _state.IncludeBatches = checkBox1.Checked;
                    _state.IncludeStoredProcs = checkBox2.Checked;
                    _state.IncludeTriggers = checkBox3.Checked;
                    _state.IncludeFunctions = checkBox4.Checked;
                    _state.IncludeViews = checkBox5.Checked;

                    //Saves connection string in appState
                    _state.ConnectionString = connectionString;
                }

                // Scan DMV in memory 
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sqlBatches = @"
                        SELECT
                            DB_NAME(),
                            '',
                            LEFT(LTRIM(TRANSLATE(st.text, NCHAR(9)+NCHAR(10)+NCHAR(13)+NCHAR(160), N'    ')),36),
                            'SQL_BATCHES',
                            qs.total_elapsed_time / 1000000,
                            qs.total_worker_time / 1000000,
                            qs.total_logical_reads,
                            qs.execution_count,
                            st.text,
                            NULL, NULL, NULL
                        FROM sys.dm_exec_cached_plans AS cp
                        JOIN sys.dm_exec_query_stats AS qs ON cp.plan_handle = qs.plan_handle
                        CROSS APPLY sys.dm_exec_sql_text(cp.plan_handle) AS st
                        WHERE cp.objtype IN ('Adhoc', 'Prepared') AND st.text IS NOT NULL AND st.dbid = DB_ID();";

                    string sqlStored = @"
                        SELECT
                            DB_NAME(),
                            s.name,
                            p.name,
                            'SQL_STORED_PROCEDURES',
                            ISNULL(ps.total_elapsed_time, 0) / 1000000,
                            ISNULL(ps.total_worker_time, 0) / 1000000,
                            ISNULL(ps.total_logical_reads, 0),
                            ISNULL(ps.execution_count, 0),
                            sm.definition,
                            NULL, NULL, NULL
                        FROM sys.procedures p
                        JOIN sys.schemas s ON p.schema_id = s.schema_id
                        LEFT JOIN sys.dm_exec_procedure_stats ps ON ps.object_id = p.object_id AND ps.database_id = DB_ID()
                        LEFT JOIN sys.sql_modules sm ON p.object_id = sm.object_id;";

                    string sqlTriggers = @"
                        SELECT
                            DB_NAME(),
                            s.name,
                            tr.name,
                            'TRIGGERS',
                            ISNULL(ts.total_elapsed_time, 0) / 1000000,
                            ISNULL(ts.total_worker_time, 0)  / 1000000,
                            ISNULL(ts.total_logical_reads, 0),
                            ISNULL(ts.execution_count, 0),
                            sm.definition AS trigger_source_code,
                            NULL, NULL, NULL
                        FROM sys.triggers tr
                        JOIN sys.objects o ON tr.parent_id = o.object_id
                        JOIN sys.schemas s ON o.schema_id = s.schema_id
                        LEFT JOIN sys.dm_exec_trigger_stats ts ON ts.object_id = tr.object_id AND ts.database_id = DB_ID()
                        LEFT JOIN sys.sql_modules sm ON sm.object_id = tr.object_id
                        WHERE tr.is_ms_shipped = 0";

                    string sqlUdf = @"
                        SELECT
                            DB_NAME(),
                            s.name,
                            o.name,
                            'USER_DEFINED_FUNCTIONS',
                            ISNULL(fs.total_elapsed_time, 0) / 1000000,
                            ISNULL(fs.total_worker_time, 0)  / 1000000,
                            ISNULL(fs.total_logical_reads, 0),
                            ISNULL(fs.execution_count, 0),
                            sm.definition AS function_source_code,
                            NULL, NULL, NULL
                        FROM sys.objects o
                        JOIN sys.schemas s ON o.schema_id = s.schema_id
                        LEFT JOIN sys.dm_exec_function_stats fs ON fs.object_id = o.object_id AND fs.database_id = DB_ID()
                        LEFT JOIN sys.sql_modules sm ON sm.object_id = o.object_id
                        WHERE o.type IN ('FN','IF','TF')";

                    string sqlViews = @"
                        SELECT
                            DB_NAME(),
                            s.name,
                            v.name,
                            'VIEWS',
                            0, 0, 0, 0,
                            sm.definition AS view_source_code,
                            NULL, NULL, NULL
                        FROM sys.views v
                        JOIN sys.schemas s ON v.schema_id = s.schema_id
                        LEFT JOIN sys.sql_modules sm ON sm.object_id = v.object_id
                        WHERE v.is_ms_shipped = 0";


                    //Query Store: replaces the 4 sets of "metrics" while keeping the SAME schema (12 columns).
                    if (radioButton4.Checked)
                    {
                        sqlBatches = @"
                            SELECT
                                DB_NAME(),
                                '',
                                LEFT(LTRIM(TRANSLATE(qt.query_sql_text, NCHAR(9)+NCHAR(10)+NCHAR(13)+NCHAR(160), N'    ')),36),
                                'SQL_BATCHES',
                                ISNULL(SUM(rs.avg_duration * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_cpu_time * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_logical_io_reads * rs.count_executions), 0),
                                ISNULL(SUM(rs.count_executions), 0),
                                qt.query_sql_text,
                                NULL, NULL, NULL
                            FROM sys.query_store_query q
                            JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                            JOIN sys.query_store_plan p ON p.query_id = q.query_id
                            JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
                            WHERE q.object_id = 0
                            GROUP BY qt.query_sql_text, q.query_id;";

                        sqlStored = @"
                            SELECT
                                DB_NAME(),
                                s.name,
                                pr.name,
                                'SQL_STORED_PROCEDURES',
                                ISNULL(SUM(rs.avg_duration * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_cpu_time * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_logical_io_reads * rs.count_executions), 0),
                                ISNULL(SUM(rs.count_executions), 0),
                                sm.definition,
                                NULL, NULL, NULL
                            FROM sys.procedures pr
                            JOIN sys.schemas s ON pr.schema_id = s.schema_id
                            LEFT JOIN sys.sql_modules sm ON pr.object_id = sm.object_id
                            LEFT JOIN sys.query_store_query q ON q.object_id = pr.object_id
                            LEFT JOIN sys.query_store_plan p ON p.query_id = q.query_id
                            LEFT JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
                            GROUP BY s.name, pr.name, sm.definition;";

                        sqlTriggers = @"
                            SELECT
                                DB_NAME(),
                                s.name,
                                tr.name,
                                'TRIGGERS',
                                ISNULL(SUM(rs.avg_duration * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_cpu_time * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_logical_io_reads * rs.count_executions), 0),
                                ISNULL(SUM(rs.count_executions), 0),
                                sm.definition AS trigger_source_code,
                                NULL, NULL, NULL
                            FROM sys.triggers tr
                            JOIN sys.objects o ON tr.parent_id = o.object_id
                            JOIN sys.schemas s ON o.schema_id = s.schema_id
                            LEFT JOIN sys.sql_modules sm ON sm.object_id = tr.object_id
                            LEFT JOIN sys.query_store_query q ON q.object_id = tr.object_id
                            LEFT JOIN sys.query_store_plan p ON p.query_id = q.query_id
                            LEFT JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
                            WHERE tr.is_ms_shipped = 0
                            GROUP BY s.name, tr.name, sm.definition;";

                        sqlUdf = @"
                            SELECT
                                DB_NAME(),
                                s.name,
                                o.name,
                                'USER_DEFINED_FUNCTIONS',
                                ISNULL(SUM(rs.avg_duration * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_cpu_time * rs.count_executions), 0) / 1000000,
                                ISNULL(SUM(rs.avg_logical_io_reads * rs.count_executions), 0),
                                ISNULL(SUM(rs.count_executions), 0),
                                sm.definition AS function_source_code,
                                NULL, NULL, NULL
                            FROM sys.objects o
                            JOIN sys.schemas s ON o.schema_id = s.schema_id
                            LEFT JOIN sys.sql_modules sm ON sm.object_id = o.object_id
                            LEFT JOIN sys.query_store_query q ON q.object_id = o.object_id
                            LEFT JOIN sys.query_store_plan p ON p.query_id = q.query_id
                            LEFT JOIN sys.query_store_runtime_stats rs ON rs.plan_id = p.plan_id
                            WHERE o.type IN ('FN','IF','TF')
                            GROUP BY s.name, o.name, sm.definition;";
                    }

                    //Fill list in memory  
                    _state.CodeplexRows.Clear();

                    //Now Data come from SQL Scan, not from file.
                    _state.LoadedFileServer = "";
                    _state.LoadedFileDatabase = "";
                    _state.LoadedFileSqlServerVersion = "";

                    if (checkBox1.Checked) await AppendScanRowsAsync(conn, sqlBatches);
                    if (checkBox2.Checked) await AppendScanRowsAsync(conn, sqlStored);
                    if (checkBox3.Checked) await AppendScanRowsAsync(conn, sqlTriggers);
                    if (checkBox4.Checked) await AppendScanRowsAsync(conn, sqlUdf);
                    if (checkBox5.Checked) await AppendScanRowsAsync(conn, sqlViews);

                    // Assegna gli Id (1-based: 0 = "nessun oggetto" nella UI)
                    int nextId = 1;
                    foreach (var r in _state.CodeplexRows)
                        r.Id = nextId++;
                }

                //Cleanup code panels and populate TreeView 
                if (Application.OpenForms["Form1"] is Form1 mainForm)
                {
                    mainForm.ClearCodePanels();
                    await mainForm.LoadTreeViewAsync("executions");
                }

                //Get JSON table columns schema and data types
                _state.strColumnTypesList = await AIDataRetrieval.GetTableColumnsToJsonAsync(connectionString);

                //Get JSON index list 
                _state.strIndexesList = await AIDataRetrieval.GetIndexesToJsonAsync(connectionString);

                //Get SQL Server version (year)
                _state.SqlServerVersion = await AIDataRetrieval.GetSqlServerVersionAsync(connectionString);
                return true;
            }
            catch (Exception ex)
            {
                AIUtility.TraceLog("There's Connection Error: " + ex.Message);
                MessageBox.Show(ex.Message,"Connection / scan error: " + ex.GetType().Name,MessageBoxButtons.OK,MessageBoxIcon.Error);
                return false;
            }

        }


        // Verifies that Query Store is enabled and readable on the current database.
        // actual_state: 0=OFF, 1=READ_ONLY, 2=READ_WRITE, 3=ERROR. Leggibile solo 1 o 2.
        private static async Task<bool> IsQueryStoreReadableAsync(SqlConnection conn)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT actual_state FROM sys.database_query_store_options;", conn))
                {
                    object o = await cmd.ExecuteScalarAsync();
                    if (o == null || o == DBNull.Value) return false;
                    int state = Convert.ToInt32(o);
                    return state == 1 || state == 2;
                }
            }
            catch
            {
                //Not existing view (SQL Server < 2016) or not accessible => not available.
                return false;
            }
        }


        //Executes a scan query and appends the read rows to the in-memory list.
        private async Task AppendScanRowsAsync(SqlConnection conn, string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    _state.CodeplexRows.Add(new CodeplexRow
                    {
                        DbName = ReadString(reader, 0),
                        SchemaName = ReadString(reader, 1),
                        ObjectName = ReadString(reader, 2),
                        TypeDesc = ReadString(reader, 3),
                        Elapsed = ReadLong(reader, 4),
                        Cpu = ReadLong(reader, 5),
                        Reads = ReadLong(reader, 6),
                        Executions = ReadLong(reader, 7),
                        SourceSql = ReadString(reader, 8)
                        // AiOptimized, ThreadId restano null
                    });
                }
            }
        }

        private static string? ReadString(SqlDataReader r, int i)
            => r.IsDBNull(i) ? null : r.GetValue(i)?.ToString();

        // Legge un BIGINT (es. total_logical_reads / total_elapsed_time) senza perdita:  
        private static long? ReadLong(SqlDataReader r, int i)
        {
            if (r.IsDBNull(i)) return null;
            return Convert.ToInt64(r.GetValue(i));
        }


        //Counts elements of array JSON radice (= n. tables o n. indexes).
        private static int CountJsonArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.GetArrayLength()
                    : 0;
            }
            catch
            {
                return 0;
            }
        }


        //OK Blue button
        private void button4_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        //CANCEL Blue button
        private void button5_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }


        private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            textBox5.Enabled = false;
            textBox6.Enabled = false;
        }


        private void radioButton2_CheckedChanged_1(object sender, EventArgs e)
        {
            textBox5.Enabled = true;
            textBox6.Enabled = true;
        }


        //SQL Configuration
        private async void button3_Click(object sender, EventArgs e)
        {
            //It must be selected at least one object type, otherwise the scan produces no rows and the TreeView remains empty.
            if (!checkBox1.Checked && !checkBox2.Checked && !checkBox3.Checked
                && !checkBox4.Checked && !checkBox5.Checked)
            {
                MessageBox.Show(
                    "Select at least one object type (Batches, Stored Procedures, Triggers, Functions, Views) before connecting.",
                    "No object type selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            //SQL connection
            string serverName = textBox3.Text.Trim();
            string databaseName = textBox4.Text.Trim();
            bool connected = false;

            //If ServerName or DatabaseName is significant, try to connect
            if (!string.IsNullOrWhiteSpace(serverName) || !string.IsNullOrWhiteSpace(databaseName))
            {
                //Warns only if there is work to lose:
                bool fromFile = !string.IsNullOrWhiteSpace(_state.LoadedFileServer)
                             || !string.IsNullOrWhiteSpace(_state.LoadedFileDatabase);

                bool hasWork = _state.CodeplexRows.Exists(r => !string.IsNullOrWhiteSpace(r.AiOptimized));

                if ((fromFile || hasWork) &&
                    MessageBox.Show("Connecting will replace the current session content. Continue?",
                        "Connect to SQL", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                    return;

                //Start
                button3.Enabled = false;
                button3.Text = "⏳ Connecting...";
                button3.Refresh();

                connected = await ConnectAndGetDataFromSQL(serverName, databaseName);
            }
            else
            {
                MessageBox.Show("Data not valid");
                button3.Font = new Font(button3.Font, FontStyle.Regular);
            }

            if (connected)
            {
                _state.SqlDatabase = databaseName;
                _state.SqlServer = serverName;
                _state.SqlUsername = textBox5.Text.Trim();
                _state.UseWindowsAuth = radioButton1.Checked;
                _state.UseQueryStore = radioButton4.Checked;

                _state.IncludeBatches = checkBox1.Checked;
                _state.IncludeStoredProcs = checkBox2.Checked;
                _state.IncludeTriggers = checkBox3.Checked;
                _state.IncludeFunctions = checkBox4.Checked;
                _state.IncludeViews = checkBox5.Checked;

                button3.Text = "Connected";
                button3.ForeColor = Color.LightGreen;
                button3.Font = new Font(button3.Font, FontStyle.Bold);
                button3.Enabled = true;

                int tableCount = CountJsonArray(_state.strColumnTypesList);
                int indexCount = CountJsonArray(_state.strIndexesList);
                label11.Text = $"Connected: {tableCount} tables, {indexCount} indexes";
                label11.Visible = true;

                if (Application.OpenForms["Form1"] is Form1 mainForm)
                {
                    mainForm.UpdateDatabaseLabels(_state.SqlServer, _state.SqlDatabase);
                }

            }
            else if (!string.IsNullOrWhiteSpace(serverName) || !string.IsNullOrWhiteSpace(databaseName))
            {
                MessageBox.Show("Cannot connect to SQL Server");
                button3.Text = "Connect and Save";
                button3.Enabled = true;
                return;
            }
        }


        //Agent Configuration
        private void button2_Click(object sender, EventArgs e)
        {
            // Save selected agent
            string agentName = GetCheckedAgentName();
            if (string.IsNullOrWhiteSpace(agentName))
            {
                MessageBox.Show("You have to select an Agent");
                return;
            }
            if (ProjectClient == null)
            {
                MessageBox.Show("Connect to Azure AI Foundry first");
                return;
            }

            string agentModel = GetCheckedAgentModel();

            _state.SelectedAgentId = agentName;
            _state.SelectedAgentName = agentName;
            _state.SelectedAgentModel = agentModel;   
            SelectedAgentName = agentName;

            //AppState save
            _state.TenantId = textBox1.Text.Trim();
            _state.Endpoint = textBox2.Text.Trim();

            //Promotes the connection to Form1 (without closing the form)
            if (Application.OpenForms["Form1"] is Form1 mainForm)
            {
                mainForm.ApplyFoundryRuntime(Credential, ProjectClient, SelectedAgentName, agentModel);
            }

            button2.Text = "Selected";
            button2.ForeColor = Color.LightGreen;
        }


        private async Task<(string model, string version)> GetAgentModelAndVersionAsync(string agentName)
        {
            try
            {
                await foreach (ProjectsAgentVersion v in ProjectClient.AgentAdministrationClient.GetAgentVersionsAsync(
                    agentName: agentName,
                    limit: 1,
                    order: AgentListOrder.Descending,
                    after: null,
                    before: null,
                    cancellationToken: System.Threading.CancellationToken.None))
                {
                    var def = v.Definition as DeclarativeAgentDefinition;
                    return (def?.Model ?? "", Convert.ToString(v.Version) ?? "");
                }
            }
            catch
            {
                //If fails, leave the fields empty
            }
            return ("", "");
        }


    }
}
